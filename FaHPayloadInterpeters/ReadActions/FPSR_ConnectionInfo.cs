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

namespace FAHPayloadInterpeters.FAHFunctionPropertyStateResponses
{
    public class FPSR_ConnectionInfo : FAHFunctionPropertyStateResponse
    {
        private const uint PACKET_PAYLOAD_CONNECTIONID = 2;
        //private const uint PACKET_PAYLOAD_GROUPADDRESS = 4;

        private byte[] requestDataParms = null;

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            if(this.resultCode == KNXHelpers.knxPropertyReturnValues.MoreIndices)
            {
                faHDevice.WriteConnectionMoreIncides(ObjectID, PropertyID, (ushort)(this.FPSRpayload[0] + 1));
            }

            if (this.requestDataParms == null)
            {
                if (FPSRpayload.Length == 4)
                {
                    //There is a knxaddress in response; save
                    faHDevice.WriteConnectionValue(ObjectID, PropertyID, this.FPSRpayload[0], this.FPSRpayload[1], new KNXAddress[] { new KNXAddress(FPSRpayload, 2) }, out moreIndices);
                }
                else if (FPSRpayload.Length == 2)
                {
                    //There is knxaddress in response; save as null
                    faHDevice.WriteConnectionValue(ObjectID, PropertyID, this.FPSRpayload[0], this.FPSRpayload[1], null, out moreIndices);
                }
                else
                {
                    throw new NotImplementedException();
                }
                return true;
            }
            else
            {
                if (this.requestDataParms.Length == 2)
                {
                    //Contains groupvalue
                    if (FPSRpayload.Length >= 4)
                    {
                        if (this.requestDataParms[0] == this.ConnectionID && this.requestDataParms[1] == this.ConnectionSubIndexID)
                        {
                            faHDevice.WriteConnectionValue(ObjectID, PropertyID, (ushort)this.ConnectionID, (byte)this.ConnectionSubIndexID, this.GroupValueAddress, out moreIndices);
                            return true;
                        }
                        else
                        {
                            throw new InvalidDataException();
                        }
                    }
                    //Empty groupvalue
                    else if (FPSRpayload.Length == 2)
                    {
                        if (this.requestDataParms[0] == this.ConnectionID && this.requestDataParms[1] == this.ConnectionSubIndexID)
                        {
                            faHDevice.WriteConnectionValue(ObjectID, PropertyID, (ushort)this.ConnectionID, (byte)this.ConnectionSubIndexID, null, out moreIndices);
                            return true;
                        }
                        else
                        {
                            throw new InvalidDataException();
                        }
                    }
                    /*else
                    {
                        if (this.requestDataParms[0] == this.ConnectionID && this.requestDataParms[1] == this.ConnectionSubIndexID)
                        {
                            faHDevice.WriteConnectionValue(ObjectID, PropertyID, (ushort)this.ConnectionID, (byte)this.ConnectionSubIndexID, GroupValueAddress, AdditionalData, out moreIndices);
                            return true;
                        }
                        else
                        {
                            throw new InvalidDataException();
                        }                        
                    }*/
                }
                moreIndices = false;
                return false;
            }
        }

        public static KNXmessage CreateResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns)
            {
                throw new InvalidCastException();
            }


            int ChannelIndex = MessageToRespondTo.ObjectID;
            int propIndex = MessageToRespondTo.PropertyID;
            byte requestedIndice;
            byte propertyInfo;
            bool moreIndices;
            KNXAddress[] GroupValueAddress;
            //byte[] additionalData = null;
            uint payloadSize = (uint)(FPSRHEADERSIZE + 2);

            if (MessageToRespondTo.FPCpayload == null)
            {
                requestedIndice = 0;
                //There is no field requested; default to 0
            }
            else
            {
                requestedIndice = MessageToRespondTo.FPCpayload[0];
            }

            if (!atHomeDevice.ReadConnectionValue(ChannelIndex, propIndex, requestedIndice, out propertyInfo, out GroupValueAddress, out moreIndices))
                return MessageToRespondTo.CreateCommandNotSupportedMessage();

            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort)
            {
                DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual
            };

            if (GroupValueAddress != null)
            {
                payloadSize += (uint)GroupValueAddress.Length * 2; //Address is not empty, add space to store it.
                /*if (additionalData != null)
                {
                    payloadSize += (uint)additionalData.Length;
                }*/
            }

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FPSR_ConnectionInfo(kNXmessage.Payload);
            FPSR_ConnectionInfo newPkg = (FPSR_ConnectionInfo)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.ConnectionID = requestedIndice;
            newPkg.ConnectionSubIndexID = propertyInfo;
            if (GroupValueAddress != null)
            {
                newPkg.GroupValueAddress = GroupValueAddress;
                /*if(additionalData!=null)
                {

                }*/
            }
            newPkg.PropertyID = MessageToRespondTo.PropertyID;
            newPkg.ObjectID = MessageToRespondTo.ObjectID;
            if (moreIndices && requestedIndice != 0 ) //for 0 (no params) there are no more indices to report
                newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.MoreIndices;
            else
                newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;

            return kNXmessage;
            
        }

        public byte[] AdditionalData
        {
            get
            {
                if (payloadReference.PayloadByteData.Length > 9)
                    return payloadReference.GetBytes(9, payloadReference.PayloadByteData.Length - 9);
                else
                    return null;
            }
            set
            {
                byte[] data = value;
                payloadReference.UpdateBytes(data, 9, data.Length);
            }
        }

        public KNXAddress[] GroupValueAddress
        {
            get
            {
                //Check if GroupValue addresses are stored correctly
                if (payloadReference.PayloadByteData.Length > FPSRHEADERSIZE + 2)
                {
                    int count = (payloadReference.PayloadByteData.Length - (FPSRHEADERSIZE + 2)) / 2;
                    KNXAddress[] knOut = new KNXAddress[count];
                    for (int i = 0; i < count; i++)
                    {
                        knOut[i] = new KNXAddress(payloadReference.PayloadByteData, (uint)(FPSRHEADERSIZE + 2 + (2 * i)));
                    }
                    return knOut;
                }
                else
                    return null;
            }
            set
            {
                byte[] data = new byte[value.Length * 2];
                for(int i =0;i< value.Length;i++)
                {
                    //i*2 bugfix for position in groupvalue list
                    byte[] vdata = value[i].ToByteArray();
                    data[(i*2)] = vdata[0];
                    data[(i*2) + 1] = vdata[1];
                }
                payloadReference.UpdateBytes(data, 7, data.Length);
            }
        }

        public Byte? ConnectionID
        {
            get
            {
                if (payloadReference.PayloadByteData.Length > FPSRHEADERSIZE)
                    return payloadReference.PayloadByteData[5];
                else
                    return null;
            }
            set
            {
                if (value != null)
                {
                    payloadReference.PayloadByteData[5] = (byte)value;
                }
                else
                {
                    throw new InvalidDataException();
                }
            }
        }

        public Byte? ConnectionSubIndexID
        {
            get
            {
                if (payloadReference.PayloadByteData.Length > FPSRHEADERSIZE)
                    return payloadReference.PayloadByteData[6];
                else
                    return null;
            }
            set
            {
                if (value != null)
                {
                    payloadReference.PayloadByteData[6] = (byte)value;
                }
                else
                {
                    throw new InvalidDataException();
                }
            }
        }
        public FPSR_ConnectionInfo(KNXPayload OwnerPayload) : this(OwnerPayload, null)
        { }

        public FPSR_ConnectionInfo(KNXPayload OwnerPayload, byte[] parmeters) : base(OwnerPayload)
        {
            this.requestDataParms = parmeters;
            if (payloadReference.PayloadByteData.Length > FPSRHEADERSIZE)
            {
                //Connection ID
                addAccountedBytes(5, 1);

                //Static 1???
                addIgnoredBytes(5, 1);
            }

            if(payloadReference.PayloadByteData.Length > FPSRHEADERSIZE + PACKET_PAYLOAD_CONNECTIONID)
                //GroupValue Address
                addAccountedBytes(7, 2);            
        }
    }
}
