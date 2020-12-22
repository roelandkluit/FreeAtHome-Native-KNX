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
    public class FPSR_ChannelDescription : FAHFunctionPropertyStateResponse
    {        
        private const int VALUESIZE = 6;

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            moreIndices = false;
            faHDevice.WriteChannelDescription(ObjectID, PropertyID, Description);
            return true;
        }

        public static KNXmessage CreateResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc)
            {
                throw new InvalidCastException();
            }

            KNXmessage kNXmessage;

            string Description;
            atHomeDevice.ReadChannelDescription(MessageToRespondTo.ObjectID, MessageToRespondTo.PropertyID, out Description);

            if (Description.Length > 10)
            {
                kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketLong);
            }
            else
            {
                kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);
            }
            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;
            uint payloadSize = (uint)(FPSRHEADERSIZE + DescriptionByteLenght(Description));

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FPSR_ChannelDescription(kNXmessage.Payload);
            FPSR_ChannelDescription newPkg = (FPSR_ChannelDescription)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.PropertyID = MessageToRespondTo.PropertyID;
            newPkg.ObjectID = MessageToRespondTo.ObjectID;
            newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;
            newPkg.Description = Description;
            return kNXmessage;
        }

        private static int DescriptionByteLenght(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            return data.Length;
        }

        public string Description
        {
            get
            {
                if (this.FPSRpayload != null)
                    return Encoding.UTF8.GetString(this.FPSRpayload);
                else
                    return "";
            }
            set
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                payloadReference.UpdateBytes(data, 5, data.Length);
            }
        }

        //Default NULL
        public FPSR_ChannelDescription(KNXPayload OwnerPayload) : base(OwnerPayload)
        {
            //Description
            addAccountedBytes(5, 50);
        }
    }
}
