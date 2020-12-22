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
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHPayloadInterpeters.FAHFunctionPropertyStateResponses
{
    public class FPSR_PropertyValueRead : FAHFunctionPropertyStateResponse
    {
        public static KNXmessage CreateReadResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            try
            {
                if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo)
                {
                    throw new InvalidCastException();
                }

                int ChannelIndex = MessageToRespondTo.ObjectID;
                int propIndex = MessageToRespondTo.PropertyID;
                int fieldID = (int)MessageToRespondTo.FieldID;

                byte[] bData;//= atHomeDevice.Channels[ChannelIndex].Properties[propIndex].FieldData[fieldID].data;
                bool moreIndices;

                if(!atHomeDevice.ReadPropertyValue(ChannelIndex, propIndex, fieldID, out bData, out moreIndices))
                {
                    return MessageToRespondTo.CreateCommandNotSupportedMessage();
                }

                KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort)
                {
                    DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual
                };

                const int HEADERSIZE = 5;

                //Todo, check lenght?
                uint payloadSize = (uint)(HEADERSIZE + bData.Length);

                kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
                kNXmessage.Payload.ReadablePayloadPacket = new FPSR_PropertyValueRead(kNXmessage.Payload);
                FPSR_PropertyValueRead newPkg = (FPSR_PropertyValueRead)kNXmessage.Payload.ReadablePayloadPacket;
                newPkg.UpdatePacketSettings();
                newPkg.FPSRpayload = bData;
                newPkg.PropertyID = MessageToRespondTo.PropertyID;
                newPkg.ObjectID = MessageToRespondTo.ObjectID;
                newPkg.FieldID = (byte)MessageToRespondTo.FieldID;
                if(moreIndices)
                    newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.MoreIndices;
                else
                    newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;
                return kNXmessage;
            }
            catch
            {
                return MessageToRespondTo.CreateCommandNotSupportedMessage();
            }
        }

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            if (this.resultCode == KNXHelpers.knxPropertyReturnValues.MoreIndices)
            {
                faHDevice.WritePropertyMoreIncides(ObjectID, PropertyID, (ushort)(this.FieldID + 1));
            }

            moreIndices = faHDevice.WritePropertyValue(this.ObjectID, this.PropertyID, this.FieldID, this.FPSRpayload);
            return true;
        }

        public byte FieldID
        {
            get
            {
                return base.payloadReference.PayloadByteData[5];
            }
            set
            {
                base.payloadReference.PayloadByteData[5] = (byte)value;
            }
        }

        public FPSR_PropertyValueRead(KNXPayload OwnerPayload) : base(OwnerPayload)
        {
            //Field ID
            addAccountedBytes(5, 1);

            //Value count is variable
            addIgnoredBytes(6, 25);
        }
    }
}
