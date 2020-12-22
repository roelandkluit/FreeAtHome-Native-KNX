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

namespace FAHPayloadInterpeters.FAHFunctionPropertyStateResponses
{
    public class FPSR_IconInfo : FAHFunctionPropertyStateResponse
    {
        private const int VALUESIZE = 2;

        public UInt16 Icon
        {
            get
            {
                return KNXHelpers.knxToUint16(payloadReference.PayloadByteData, 5);
            }
            set
            {
                byte[] data = KNXHelpers.uint16ToKnx(value);
                payloadReference.UpdateBytes(data, 5, 2);
            }
        }

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            moreIndices = false;
            faHDevice.WriteIconInfo(ObjectID, PropertyID, Icon);
            return true;
        }

        public static KNXmessage CreateResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadIconId)
            {
                throw new InvalidCastException();
            }

            //int objId = MessageToRespondTo.ObjectID;
            UInt16 Icon = atHomeDevice.ReadIconInfo(MessageToRespondTo.ObjectID, MessageToRespondTo.PropertyID);//   KNXHelpers.GetCheckNullUint16Value(atHomeDevice.Channels[objId].Properties[MessageToRespondTo.PropertyID].IconId, 0xFFFF);

            /*
            if (Room == 0 && X== 0 && Y == 0)
            {
                atHomeDevice.ChannelProperties[MessageToRespondTo.ObjectID].Properties[MessageToRespondTo.PropertyID].RoomID = 0xFFFF;
                Room = 0xFFFF;
                atHomeDevice.ChannelProperties[MessageToRespondTo.ObjectID].Properties[MessageToRespondTo.PropertyID].X = 0xFFFF;
                X = 0xFFFF;
                atHomeDevice.ChannelProperties[MessageToRespondTo.ObjectID].Properties[MessageToRespondTo.PropertyID].X = 0xFFFF;
                Y = 0xFFFF;
                atHomeDevice.Serialize("input.json");
            }*/

            //TODO, add as actual param to device!
            //FahDeviceParametersNew p = new FahDeviceParametersNew();
            //p.dataType = FahDeviceParametersNew.ParameterType.deviceInfo;
            //p.Response = KNXHelpers.knxPropertyReturnValues.Success;

            /*
            if (atHomeDevice.FunctionList == null)
            {
                atHomeDevice.FunctionList = new byte[] { 0x01, 0x00, 0x12, 0x00, 0xEC, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01 };
                atHomeDevice.Serialize("input.json");
            }*/

            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);
            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;

            uint payloadSize = (uint)(FPSRHEADERSIZE + VALUESIZE);

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FPSR_IconInfo(kNXmessage.Payload);
            FPSR_IconInfo newPkg = (FPSR_IconInfo)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.PropertyID = MessageToRespondTo.PropertyID;
            newPkg.ObjectID = MessageToRespondTo.ObjectID;
            newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;
            newPkg.Icon = Icon;
            return kNXmessage;
        }

        //Default 0xFF 0xFF
        public FPSR_IconInfo(KNXPayload OwnerPayload) : base(OwnerPayload)
        {
            //IconID
            addAccountedBytes(5, 2);            
        }
    }
}
