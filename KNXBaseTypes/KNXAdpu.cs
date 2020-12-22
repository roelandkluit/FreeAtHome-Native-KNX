/*
 *  FreeAtHome KNX VirtualSwitch and Communication module. This software
    provides interaction over KNX to Free@Home bus devices.

    This software is not created, maintained or has any assosiation
    with ABB \ Busch-Jeager.

    Copyright (C) 2020 Roeland Kluit

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    The Software is provided to you by the Licensor under the License,
    as defined, subject to the following condition.

    Without limiting other conditions in the License, the grant of rights
    under the License will not include, and the License does not grant to
    you, the right to Sell the Software.

    For purposes of the foregoing, "Sell" means practicing any or all of
    the rights granted to you under the License to provide to third
    parties, for a fee or other consideration (including without
    limitation fees for hosting or consulting/ support services related
    to the Software), a product or service whose value derives, entirely
    or substantially, from the functionality of the Software.
    Any license notice or attribution required by the License must also
    include this Commons Clause License Condition notice.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

    These are supporting modules for KNX protocol processing
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes
{
    public class KNXAdpu
    {
        private KNXPayload OwnerPayload;

        public enum ApduType
        {
            // Application Layer services on Multicast Communication Mode 
            GroupValueRead = 0x000,
            GroupValueResponse = 0x040,
            GroupValueWrite = 0x080,

            // Application Layer services on Broadcast Communication Mode
            IndividualAddressWrite = 0x0c0,
            IndividualAddressRead = 0x100,
            IndividualAddressResponse = 0x140,
            IndividualAddressSerialNumberRead = 0x3dc,
            IndividualAddressSerialNumberResponse = 0x3dd,
            IndividualAddressSerialNumberWrite = 0x3de,

            // Application Layer Services on System Broadcast communication mode
            SystemNetworkParameterRead = 0x1c8,
            SystemNetworkParameterResponse = 0x1c9,
            SystemNetworkParameterWrite = 0x1ca,
            // Open media specific Application Layer Services on System Broadcast communication mode
            DomainAddressSerialNumberRead = 0x3ec,
            DomainAddressSerialNumberResponse = 0x3ed,
            DomainAddressSerialNumberWrite = 0x3ee,

            // Application Layer Services on Point-to-point Connection-Oriented Communication Mode (mandatory)
            // Application Layer Services on Point-to-point Connectionless Communication Mode (either optional or mandatory)
            PropertyValueExtRead = 0x1CC,
            PropertyValueExtResponse = 0x1CD,
            PropertyValueExtWriteCon = 0x1CE,
            PropertyValueExtWriteConResponse = 0x1CF,
            PropertyValueExtWriteUnCon = 0x1D0,
            PropertyExtDescriptionRead = 0x1D2,
            PropertyExtDescriptionResponse = 0x1D3,
            FunctionPropertyExtCommand = 0x1D4,
            FunctionPropertyExtState = 0x1D5,
            FunctionPropertyExtStateResponse = 0x1D6,
            MemoryExtWrite = 0x1FB,
            MemoryExtWriteResponse = 0x1FC,
            MemoryExtRead = 0x1FD,
            MemoryExtReadResponse = 0x1FE,
            MemoryRead = 0x200,
            MemoryResponse = 0x240,
            MemoryWrite = 0x280,
            UserMemoryRead = 0x2C0,
            UserMemoryResponse = 0x2C1,
            UserMemoryWrite = 0x2C2,
            UserManufacturerInfoRead = 0x2C5,
            UserManufacturerInfoResponse = 0x2C6,
            FunctionPropertyCommand = 0x2C7,
            FunctionPropertyState = 0x2C8,
            FunctionPropertyStateResponse = 0x2C9,
            DeviceDescriptorRead = 0x300,
            DeviceDescriptorResponse = 0x340,
            Restart = 0x380,
            //RestartMasterReset = 0x381,
            AuthorizeRequest = 0x3d1,
            AuthorizeResponse = 0x3d2,
            KeyWrite = 0x3d3,
            KeyResponse = 0x3d4,
            PropertyValueRead = 0x3d5,
            PropertyValueResponse = 0x3d6,
            PropertyValueWrite = 0x3d7,
            PropertyDescriptionRead = 0x3d8,
            PropertyDescriptionResponse = 0x3d9,

            //F@Home Specific;
            ABBSetBinaryInputType = 0x3C3,
            ABBResponseBinaryInputType = 0x3C2,

            // Secure Service
            SecureService = 0x3F1,

            //Not defined
            ApduNotPresent = 0xFFFF,
        }

        internal KNXAdpu(KNXPayload Owner)
        {
            OwnerPayload = Owner;
        }

        public bool hasDataAfterApdu
        {
            get
            {
                if (this.shortApdu)
                {
                    if ((OwnerPayload.PayloadByteData[1] & 0xF) > 0)
                    {
                        return true;
                    }
                    else
                    {
                        if (OwnerPayload.OwnerOfPayload.PayloadLenght == 1)
                            return true;
                        else
                            return false;
                    }

                }
                return false;
            }
        }

        public bool shortApdu
        {
            get
            {
                /*
                 *  Using specific APCIs, a tool or device can first examine which KNX interface objects a
                    particular device supports. As interface objects have been standardised by KNX
                    Association and have a unique identifier, the tool is able to identify the type of the device.
                    A tool can read out the (partially standardised) properties for each interface object or
                    overwrite them if required (e.g. if the property that determines the dimming speed is
                    found, this value can if required be increased or reduced).
                    In the case of the services “UserMessage” (APCI 1011) and “Escape” (APCI 1111), the 6
                    bits following the APCI are to be interpreted as an extension to the APCI. 
                */

                UInt16 apci = KNXHelpers.GetWord(OwnerPayload.PayloadByteData);

                switch ((apci & 0x3C0) >> 6)
                {
                        //Long apdu
                    case 0xF:
                    case 0xB:
                        return false;
                    default:
                        //Short Apdu
                        return true;
                }
            }
        }

        public ApduType apduType
        {
            get
            {
                UInt16 apci = KNXHelpers.GetWord(OwnerPayload.PayloadByteData);
                if (OwnerPayload.Tpdu.tpduType == KNXTpdu.TpduType.Data)
                {                                        
                    if (shortApdu) //short apci
                        apci &= 0x3c0;
                    else
                        apci &= 0x3ff;
                    return (ApduType)apci;
                }
                else
                {
                    return ApduType.ApduNotPresent;
                }
            }
            set
            {
                if(apduType == 0)
                {
                    byte[] data = KNXHelpers.uint16ToKnx((ushort)value);
                    OwnerPayload.PayloadByteData[0] = data[0];
                    OwnerPayload.PayloadByteData[1] = data[1];
                }
                else
                {
                    throw new InvalidOperationException("Cannot change the APDU for non-empty packets");
                }
            }
        }
    }
}
