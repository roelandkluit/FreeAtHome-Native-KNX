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
    public class FPSR_FunctionList : FAHFunctionPropertyStateResponse
    {
        /*
        FunctionPropertyStateResponse     KNX_PRIORITY_NORMAL     H:6, Single, FF:0x00    [0x6F-0x01]     [0x00-0x01]      Ch007:1->Success 
        0      1       2       3       4       5       6        7       8       9       10      11      12      13      14      15      16      17      18      19      20      21
        0x02   0xC9    0x07    0x01    0x00    0x01    0x00     0x12    0x00    0xEC    0x00    0x00    0x00    0x00    0x00    0x00    0x00    0x01    0x00    0x00    0x00    0x01
        *      *       *       *       *       FlID
        */

        public static KNXmessage CreateResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadFuncList)
            {
                throw new InvalidCastException();
            }

            //TODO, add as actual param to device!
            //FahDeviceParametersNew p = new FahDeviceParametersNew();
            //p.dataType = FahDeviceParametersNew.ParameterType.deviceInfo;
            //p.Response = KNXHelpers.knxPropertyReturnValues.Success;

            /*
            if (atHomeDevice.FunctionList == null)
            {
                atHomeDevice.FunctionList = new byte[] { 0x01, 0x00, 0x12, 0x00, 0xEC, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01 };
                atHomeDevice.Serialize("input.
            ");
            }*/

            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketLong);
            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;

            const int HEADERSIZE = 5;
            bool moreIndices;

            byte[] bData; // atHomeDevice.Channels[MessageToRespondTo.ObjectID].Properties[MessageToRespondTo.PropertyID].FunctionList[(byte)MessageToRespondTo.FieldID].data;
            if(!atHomeDevice.ReadFunctionList(MessageToRespondTo.ObjectID, MessageToRespondTo.PropertyID, (int)MessageToRespondTo.FieldID, out bData, out moreIndices))
            {
                return MessageToRespondTo.CreateCommandNotSupportedMessage();
            }

            //Todo, check lenght?
            uint payloadSize = (uint)(HEADERSIZE + bData.Length);

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FPSR_FunctionList(kNXmessage.Payload);
            FPSR_FunctionList newPkg = (FPSR_FunctionList)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.FPSRpayload = bData;
            newPkg.PropertyID = MessageToRespondTo.PropertyID;
            newPkg.ObjectID = MessageToRespondTo.ObjectID;
            if (moreIndices)
                newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.MoreIndices;
            else
                newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;

            return kNXmessage;
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

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            moreIndices = faHDevice.WriteFunctionList(this.ObjectID, this.PropertyID, this.FieldID, this.FPSRpayload);
            return true;
        }

        public FPSR_FunctionList(KNXPayload OwnerPayload) : base(OwnerPayload)
        {
            //Function List ID
            addAccountedBytes(5, 1);

            //8x 2 byte function data, not known how to interpet. (size might vary per device)
            addIgnoredBytes(6, 8 * 2);
        }
    }
}
