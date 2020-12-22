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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHPayloadInterpeters
{
    public class FAHIndividualAddressSerialNumberWrite: FAHReadablePayloadPacketEx
    {

        public static KNXmessage CreateFAHIndividualAddressSerialNumberWrite(FaHDevice faHDevice)
        {
            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);
            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Group;
            kNXmessage.SourceAddress = new KNXAddress(1);
            kNXmessage.HopCount = 6;
            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.IndividualAddressSerialNumberWrite, 12);
            kNXmessage.Payload.ReadablePayloadPacket = new FAHIndividualAddressSerialNumberWrite(kNXmessage.Payload);
            FAHIndividualAddressSerialNumberWrite newPkg = (FAHIndividualAddressSerialNumberWrite)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.FaHDeviceAddress = faHDevice.FaHAddress;
            newPkg.FahSystemID = faHDevice.SystemID;
            newPkg.kNXAddress = faHDevice.KnxAddress;
            return kNXmessage;
        }

        public FAHIndividualAddressSerialNumberWrite(KNXPayload kNXPayload) : base(kNXPayload)
        {
            if (kNXPayload.Apdu.apduType != KNXAdpu.ApduType.IndividualAddressSerialNumberWrite)
            {
                throw new InvalidCastException("Message type does not match");
            }
            //F@H Device ID
            addAccountedBytes(2, 6);
            //Device Address
            addAccountedBytes(8, 2);
            //Network ID
            addAccountedBytes(10, 2);

        }

        public FreeAtHomeDevices.FahSystemID FahSystemID
        {
            get
            {
                return new FreeAtHomeDevices.FahSystemID(base.payloadReference.PayloadByteData[10], base.payloadReference.PayloadByteData[11]); //todo Check byte order!
            }
            set
            {
                base.payloadReference.PayloadByteData[11] = value.SystemIDHigh;
                base.payloadReference.PayloadByteData[10] = value.SystemIDLow;
            }
        }

        public FreeAtHomeDevices.FaHDeviceAddress FaHDeviceAddress
        {
            get
            {
                return FreeAtHomeDevices.FaHDeviceAddress.FromByteArray(base.payloadReference.PayloadByteData, 2);
            }
            set
            {
                Array.Copy(value.byteValue, 0, base.payloadReference.PayloadByteData, 2, 6);
            }
        }

        public KNXAddress kNXAddress
        {
            get
            {
                return new KNXAddress(base.payloadReference.PayloadByteData[9], base.payloadReference.PayloadByteData[8]);                

            }
            set
            {
                base.payloadReference.PayloadByteData[9] = value.knxAddressLow;
                base.payloadReference.PayloadByteData[8] = value.knxAddressHigh;
            }
        }

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            faHDevice.SetAddressInformation(kNXAddress, FahSystemID);
            moreIndices = false;
            return true;
        }

        protected override string PrintOut()
        {
            return string.Format("Dev:{0} {1}:{2}", FaHDeviceAddress, FahSystemID, kNXAddress);
        }

    }
}
