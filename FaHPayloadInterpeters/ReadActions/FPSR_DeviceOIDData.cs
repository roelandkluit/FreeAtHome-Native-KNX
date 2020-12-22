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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FAHPayloadInterpeters.FAHFunctionPropertyStateResponses
{
    public class FPSR_DeviceOIDData : FAHFunctionPropertyStateResponse
    {
        //private byte[] requestDataParms = null;

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            /*if (requestDataParms == null)
                Console.WriteLine("NULL");
            else
                Console.WriteLine("DATA");
            */
            int FieldIndex = FPSRpayload[0];
            if (FPSRpayload.Length < 5)
            {
                FieldIndex = 0;
            }
            moreIndices = faHDevice.WriteOIDData(ObjectID, PropertyID, FieldIndex, FPSRpayload);
            return true;
        }

        public static KNXmessage CreateResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc)
            {
                throw new InvalidCastException();
            }

            bool moreIndices = false;
            int OIDindex = MessageToRespondTo.PropertyID;
            byte[] bData;
            byte indice = 0;

            if (MessageToRespondTo.FieldID != null)
            {
                indice = (byte)(MessageToRespondTo.FieldID);
            }

            if(!atHomeDevice.ReadOIDData(MessageToRespondTo.ObjectID, MessageToRespondTo.PropertyID, indice, out bData, out moreIndices))
            {
                return MessageToRespondTo.CreateInvalidIndexMessage();
            }

            //OidChannel 5 needs ShortPkg! (currently based on packet payload lenght)
            KNXmessage kNXmessage;
            if (bData.Length < 5)
            {
                kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);
            }
            else
                kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketLong);

            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;

            const int HEADERSIZE = 5;

            //Todo, check lenght?
            uint payloadSize = (uint)(HEADERSIZE + bData.Length);

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FPSR_DeviceOIDData(kNXmessage.Payload);
            FPSR_DeviceOIDData newPkg = (FPSR_DeviceOIDData)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.FPSRpayload = bData;
            newPkg.PropertyID = MessageToRespondTo.PropertyID;
            newPkg.ObjectID = MessageToRespondTo.ObjectID;
            if(moreIndices)
                newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.MoreIndices;
            else
                newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;
            return kNXmessage;
        }

        public FPSR_DeviceOIDData(KNXPayload OwnerPayload) : this(OwnerPayload, null)
        { }

        public FPSR_DeviceOIDData(KNXPayload OwnerPayload, byte[] parmeters) : base(OwnerPayload)
        {
            //TODO implement request parameters; currently not implemented in DescriptorRead
            //this.requestDataParms = parmeters;
            //These values all seem come from the register with the FFFFFF mask + 1 (FPSR_DeviceChannelInfo), so be aware that channel might varry between device types.
            //

            if (this.resultCode == KNXHelpers.knxPropertyReturnValues.CommandNotSupported || this.resultCode == KNXHelpers.knxPropertyReturnValues.Failed || this.resultCode == KNXHelpers.knxPropertyReturnValues.InvalidIndex)
                return;

            switch(this.PropertyID)
            {
                case 0:
                    //Empty??
                    break;
                case 2:
                    //Console.Write("Inputs:");
                    /*
                    //Inputs
                                              0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18
                    ReadValue0x02 ch007   2   0x02    0xC9    0x07    0x02    0x02    0x01    0x00    0x01    0x00    0x0B    0x00    0x00    0x00    0x01    0x01    0x01    0x20    0x00    0x00
                    ReadValue0x02 ch007   2   0x02    0xC9    0x07    0x02    0x02    0x02    0x00    0x10    0x00    0x10    0x00    0x00    0x00    0x01    0x03    0x07    0x20    0x00    0x00
                    ReadValue0x02 ch007   2   0x02    0xC9    0x07    0x02    0x02    0x03    0x00    0x11    0x00    0x11    0x00    0x00    0x00    0x01    0x05    0x01    0x20    0x00    0x00
                    ReadValue0x02 ch007   2   0x02    0xC9    0x07    0x02    0x02    0x04    0x00    0x02    0x00    0x0C    0x00    0x00    0x00    0x01    0x01    0x0A    0x20    0x00    0x00
                    ReadValue0x02 ch007   2   0x02    0xC9    0x07    0x02    0x02    0x05    0x00    0x03    0x00    0x0D    0x00    0x00    0x00    0x01    0x02    0x01    0x20    0x00    0x00
                    ReadValue0x02 ch007   2   0x02    0xC9    0x07    0x02    0x02    0x06    0x00    0x04    0x00    0x0E    0x00    0x00    0x00    0x01    0x12    0x01    0x20    0x00    0x00
                    ReadValue0x02 ch007   2   0x02    0xC9    0x07    0x02    0x02    0x07    0x00    0x12    0x00    0x12    0x00    0x00    0x00    0x01    0x01    0x02    0x10    0x00    0x04
                    ReadValue0x00 ch007   2   0x02    0xC9    0x07    0x02    0x00    0x08    0x00    0x06    0x01    0xF6    0x00    0x00    0x00    0x01    0x01    0x0A    0x20    0x00    0x00
                                              *       *       *       *       *       Datpoint                                                  
                    */
                    payloadReference.ReadablePayloadPacket.addAccountedBytes(5, 1);
                    payloadReference.ReadablePayloadPacket.addIgnoredBytes(6, 15);
                    break;
                case 3:
                    //Console.Write("Outputs:");
                    /*
                    //Outputs
                                              0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18
                    ReadValue0x02 ch007   3   0x02    0xC9    0x07    0x03    0x02    0x01    0x01    0x00    0x00    0x0F    0x00    0x00    0x00    0x01    0x01    0x01    0x01    0x00    0x08
                    ReadValue0x02 ch007   3   0x02    0xC9    0x07    0x03    0x02    0x02    0x01    0x10    0x01    0x20    0x00    0x00    0x00    0x01    0x05    0x01    0x01    0x00    0x08
                    ReadValue0x02 ch007   3   0x02    0xC9    0x07    0x03    0x02    0x03    0x01    0x11    0x00    0x14    0x00    0x00    0x00    0x01    0x15    0x03    0x01    0x00    0x08
                    ReadValue0x00 ch007   3   0x02    0xC9    0x07    0x03    0x00    0x04    0x01    0x01    0x02    0x04    0x00    0x00    0x00    0x01    0x14    0x64    0x01    0x00    0x08
                                              *       *       *       *       *       Datpoint                                                                                                      
                                              *       
                    */
                    payloadReference.ReadablePayloadPacket.addAccountedBytes(5, 1);
                    payloadReference.ReadablePayloadPacket.addIgnoredBytes(6, 15);
                    break;
                case 4:
                    //Console.Write("Parameters:");
                    /*
                    //Parameters
                                              0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22      23      24      25      26      27      28      29      30      31      32      33      34      35      36
                    ReadValue0x02 ch007   4   0x02    0xC9    0x07    0x04    0x02    0x01    0x00    0xFC    0x00    0x00    0x00    0x01    0x0B    0x14    0x64    0x00    0x13    0x03    0x00    0xFB    0x01    0x8A    0x00    0xFA
                    ReadValue0x02 ch007   4   0x02    0xC9    0x07    0x04    0x02    0x02    0x01    0x8B    0x00    0x00    0x00    0x01    0x03    0x05    0x01    0x00    0x04    0x00    0x00    0x00    0x01    0x00    0x00    0x00    0x01    0x00    0x00    0x00    0x32    0x00    0x00    0x00    0x01    0x40    0x23    0x33    0x33
                    ReadValue0x02 ch007   4   0x02    0xC9    0x07    0x04    0x02    0x03    0x01    0xF4    0x00    0x00    0x00    0x01    0x03    0x05    0x01    0x00    0x05    0x00    0x00    0x00    0x64    0x00    0x00    0x00    0x0A    0x00    0x00    0x00    0x64    0x00    0x00    0x00    0x01    0x40    0x23    0x33    0x33
                    ReadValue0x02 ch007   4   0x02    0xC9    0x07    0x04    0x02    0x04    0x01    0xF5    0x00    0x00    0x00    0x01    0x03    0x05    0x01    0x00    0x12    0x00    0x00    0x00    0x64    0x00    0x00    0x00    0x0A    0x00    0x00    0x00    0x64    0x00    0x00    0x00    0x01    0x40    0x23    0x33    0x33
                    ReadValue0x00 ch007   4   0x02    0xC9    0x07    0x04    0x00    0x05    0x01    0x6D    0x00    0x00    0x00    0x01    0x03    0x07    0x05    0xFF    0xFF    0x00    0x00    0x00    0x3C    0x00    0x00    0x00    0x1E    0x00    0x00    0x07    0x08    0x00    0x00    0x00    0x0A    0x3F    0x80    0x00    0x00
                                              *       *       *       *       *       Datpoint                                                                                                      
                    */
                    payloadReference.ReadablePayloadPacket.addAccountedBytes(5, 1);
                    payloadReference.ReadablePayloadPacket.addIgnoredBytes(6, 35);
                    break;
                case 5:
                    //???                    
                    break;
                default:
                    throw new NotImplementedException();
                    //Console.WriteLine("******UNKN*******");
                    //break;
            }
        }
    }
}
