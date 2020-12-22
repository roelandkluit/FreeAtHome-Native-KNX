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

    This modules can be used to process and gerenate KNX payloads for FreeAtHome message types.
    Please note not all fields are reverse engineerd.
    
*/
using FreeAtHomeDevices;
using KNXBaseTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHPayloadInterpeters
{
    public class FAHRestart : FAHReadablePayloadPacketEx
    {
        public FAHRestart(KNXPayload kNXPayload) : base(kNXPayload)
        {
            if (kNXPayload.Apdu.apduType != KNXAdpu.ApduType.Restart)
            {
                throw new InvalidCastException("Message type does not match");
            }

            defaultKnxPacketType = knxControlField.KnxPacketType.KNX_PacketShort;

            //RestartType
            addAccountedBytes(1);

            //EraseCode
            addAccountedBytes(2);

            //Response
            addAccountedBytes(3); //or4

            //Channel
            addAccountedBytes(4); //or3

            //flags
            addAccountedBytes(5); //or3
        }

        public byte EraseCode
        {
            get
            {
                try
                {
                    return base.payloadReference.PayloadByteData[2];
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                base.payloadReference.PayloadByteData[2] = value;
            }
        }

        public byte Response
        {
            get
            {
                try
                {

                    return base.payloadReference.PayloadByteData[3];
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                base.payloadReference.PayloadByteData[3] = value;
            }
        }

        public byte Flags
        {
            get
            {
                try
                {
                    return base.payloadReference.PayloadByteData[5];
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                base.payloadReference.PayloadByteData[5] = value;
            }
        }

        public byte Channel
        {
            get
            {
                try
                {
                    return base.payloadReference.PayloadByteData[4];
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                base.payloadReference.PayloadByteData[4] = value;
            }
        }

        public byte RestartType
        {
            get
            {
                return (byte)(base.payloadReference.PayloadByteData[1] & 0xF);
            }
            set
            {
                if (value > 128)
                    throw new InvalidDataException("Data value would override ACPI messagetype");

                base.payloadReference.PayloadByteData[1] = KNXBaseTypes.KNXHelpers.SetByteBitValue(base.payloadReference.PayloadByteData[1], 0xF, value);
            }
        }

        public KNXmessage ProcessRebootPackage(FaHDevice atHomeDevice, KNXAddress AddressToSendTo)
        {            
            return null;
        }

        protected override string PrintOut()
        {
            return string.Format("Ch{0:D3}:{1}->{2}", Channel, EraseCode, Flags);
        }
    }
}
