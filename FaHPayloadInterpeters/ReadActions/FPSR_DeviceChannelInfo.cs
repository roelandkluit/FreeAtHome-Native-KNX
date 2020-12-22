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
    public class FPSR_DeviceChannelInfo : FAHFunctionPropertyStateResponse
    {
        /*
                0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17
         HEX:   0x02    0xC9	0x01	0x01	0x00	0x00	0x80	0x00	0x43	0x00	0x00	0x00	0x01	0x00	0x00	0x00	0x01	0x55   //Moredata
         HEX:   0x02    0xC9	0x02	0x01	0x00	0x00	0x40	0x00	0x45	0x00	0x00	0x00	0x02	0x00	0x00	0x00	0x01	0x2A   //Done
         HEX:   0x02    0xC9	0x03	0x01	0x00	0x00	0x60	0x00	0x46	0x00	0x00	0x00	0x02	0x00	0x00	0x00	0x01	0x2A
         HEX:   0x02    0xC9	0x04	0x01	0x00	0x00	0x80	0x00	0x44	0x00	0x00	0x00	0x04	0x00	0x00	0x00	0x01	0x55
         HEX:   0x02    0xC9	0x05	0x01	0x00	0x00	0x40	0x00	0x47	0x00	0x00	0x00	0x08	0x00	0x00	0x00	0x01	0x2A
         HEX:   0x02    0xC9	0x06	0x01	0x00	0x00	0x60	0x00	0x48	0x00	0x00	0x00	0x08	0x00	0x00	0x00	0x01	0x2A
         HEX:   0x02    0xC9	0x07	0x01	0x00	0x02	0x60	0x00	0x4F	0xFF	0xFF	0xFF	0xFF	0x00	0x00	0x00	0x00	0xAA    //FunctionID 12 ??
         DESC:    *        *       *       *    fprop   CHANID--CHANID  ChanNID-ChanNID MASK--------------------MASK                            COMBIND MoreDataFlag?
                                                retval

        //55            01010101
        //2A            00101010
        //AA            10101010

        <channels>
            +<channel nameId="0043" cid="ABB70080" i="ch0000" combined="true" mask="00000001">
             <channel nameId="0045" cid="ABB70040" i="ch0001" combined="true" mask="00000002"/>
             <channel nameId="0046" cid="ABB70060" i="ch0002" combined="true" mask="00000002"/>
            +<channel nameId="0044" cid="ABB70080" i="ch0003" combined="true" mask="00000004">
             <channel nameId="0047" cid="ABB70040" i="ch0004" combined="true" mask="00000008"/>
             <channel nameId="0048" cid="ABB70060" i="ch0005" combined="true" mask="00000008"/>
            +<channel nameId="004F" cid="ABB70260" i="ch0006" mask="FFFFFFFF">
        </channels>      
        */

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            faHDevice.WriteChannelInfo(this.ObjectID, this.FPSRpayload);
            moreIndices = false;
            return true;
        }

        public static KNXmessage CreateResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo && MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc)
            {
                throw new InvalidCastException();
            }

            int ChannelIndex = MessageToRespondTo.ObjectID;

            //TODO, add as actual param to device and index!
            //FahDeviceParametersNew p = new FahDeviceParametersNew();
            //p.dataType = FahDeviceParametersNew.ParameterType.deviceInfo;
            //p.Response = KNXHelpers.knxPropertyReturnValues.Success;
            /*
            if (atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo == null)
            {
                if(ChannelIndex==0)
                    atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo = new byte[] { 0x00, 0x80, 0x00, 0x43, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x55 };
                else if (ChannelIndex == 1)
                    atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo = new byte[] { 0x00, 0x40, 0x00, 0x45, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x2A };
                else if (ChannelIndex == 2)
                    atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo = new byte[] { 0x00, 0x60, 0x00, 0x46, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x2A };
                else if (ChannelIndex == 3)
                    atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo = new byte[] { 0x00, 0x80, 0x00, 0x44, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x55 };
                else if (ChannelIndex == 4)
                    atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo = new byte[] { 0x00, 0x40, 0x00, 0x47, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x01, 0x2A };
                else if (ChannelIndex == 5)
                    atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo = new byte[] { 0x00, 0x60, 0x00, 0x48, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x01, 0x2A };
                else if (ChannelIndex == 6)
                    atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo = new byte[] { 0x02, 0x60, 0x00, 0x4F, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xAA };                
                else
                    atHomeDevice.Channels[ChannelIndex].DeviceChannelInfo = new byte[] { 0x00};
                atHomeDevice.Serialize("input.json");
            }*/

            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketLong)
            {
                DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual
            };

            const int HEADERSIZE = 5;

            byte[] bData;

            if(!atHomeDevice.ReadChannelInfo(ChannelIndex, out bData))
            {
                return MessageToRespondTo.CreateCommandNotSupportedMessage();
            }

            //Todo, check lenght?
            uint payloadSize = (uint)(HEADERSIZE + bData.Length);

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FPSR_DeviceChannelInfo(kNXmessage.Payload);
            FPSR_DeviceChannelInfo newPkg = (FPSR_DeviceChannelInfo)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.FPSRpayload = bData;
            newPkg.PropertyID = MessageToRespondTo.PropertyID;
            newPkg.ObjectID = MessageToRespondTo.ObjectID;
            newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;
            return kNXmessage;
        }

        public FPSR_DeviceChannelInfo(KNXPayload ownerPayload) : base(ownerPayload)
        {     
            //Channel ID
            addAccountedBytes(5, 2);
            //Name ID
            addAccountedBytes(7, 2);
            //Mask
            addAccountedBytes(9, 4);
            //Combined
            addAccountedBytes(16, 1);
            //MoreDataFlag?
            addAccountedBytes(17, 1);


        }
    }
}
