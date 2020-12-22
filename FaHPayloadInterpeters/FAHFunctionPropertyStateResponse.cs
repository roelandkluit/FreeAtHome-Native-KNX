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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FAHPayloadInterpeters.FAHFunctionPropertyStateResponses;
using FreeAtHomeDevices;
using KNXBaseTypes;

namespace FAHPayloadInterpeters
{
    public class FAHFunctionPropertyStateResponse : FAHReadablePayloadPacketEx
    {
        public const int FPSRHEADERSIZE = 5;
        /*
        //Payload
        ID     0       1       2       3       4       5       6       7       8       9       10      
        HEX    0x02    0xC7    0x00    0x01    0x02    0x02    0x07    0x46    0x8B    0xF4    0xD5            
        DEC    2       199     0       1       2       2       7       70      139     244     213
        VAL    APCI    APCI    ObjID   PropID  return  Data....
        */

        /*
        public static KNXmessage CreateResponsePayload(FahDeviceParameters parameters)
        {
            knxControlField.KnxPacketType pkgt = knxControlField.KnxPacketType.KNX_PacketShort;
            //ReadDesc Returns long packet
            switch ((FAHFunctionPropertyCommand.PropertyControlTypes)parameters.PropertyControl)
            {
                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc:
                case FAHFunctionPropertyCommand.PropertyControlTypes.DeleteConn:
                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo:
                    pkgt = knxControlField.KnxPacketType.KNX_PacketLong;
                    break;
            }

            KNXmessage kNXmessage = new KNXmessage(pkgt);
            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;

            uint payloadSize = 5;
            if (parameters.Data != null)
            {
                payloadSize += (uint)parameters.Data.Length;
            }

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FAHFunctionPropertyStateResponse(kNXmessage.Payload);
            FAHFunctionPropertyStateResponse newPkg = (FAHFunctionPropertyStateResponse)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.ObjectID = parameters.ObjectID;
            newPkg.PropertyID = parameters.PropertyID;
            newPkg.resultCode = parameters.Response;

            if (newPkg.resultCode == KNXHelpers.knxPropertyReturnValues.CommandNotSupported)
            {
                kNXmessage.ControlField.PacketType = knxControlField.KnxPacketType.KNX_PacketShort;
            }

            if (parameters.Data != null)
            {
                newPkg.FPSRpayload = parameters.Data;
            }
            return kNXmessage;
        }*/

        //public FPSRBasic PropertyStateResponsePayload { private set; get; }

        //private FAHPayloadInterpeters.FAHFunctionPropertyCommand.PropertyControlTypes functionPropertyCommand = FAHFunctionPropertyCommand.PropertyControlTypes.NotSet;

        public byte ObjectID
        {
            get
            {
                return (byte)(base.payloadReference.PayloadByteData[2]);
            }
            set
            {
                base.payloadReference.PayloadByteData[2] = value;
            }
        }

        public byte PropertyID
        {
            get
            {
                return (byte)(base.payloadReference.PayloadByteData[3]);
            }
            set
            {
                base.payloadReference.PayloadByteData[3] = value;
            }
        }

        public KNXHelpers.knxPropertyReturnValues resultCode
        {
            get
            {
                return (KNXHelpers.knxPropertyReturnValues)(base.payloadReference.PayloadByteData[4]);
            }
            set
            {
                base.payloadReference.PayloadByteData[4] = (byte)value;
            }
        }

        public byte[] FPSRpayload
        {
            get
            {
                return base.RemainderBytesAsPayload(5);
            }
            set
            {
                base.payloadReference.UpdateBytes(value, 5, value.Length);
            }
        }

        public FAHFunctionPropertyStateResponse(KNXPayload kNXPayload): base(kNXPayload)
        {
            if(kNXPayload.Apdu.apduType != KNXAdpu.ApduType.FunctionPropertyStateResponse && kNXPayload.Apdu.apduType != KNXAdpu.ApduType.FunctionPropertyCommand)
            {
                throw new InvalidCastException("Message type does not match");
            }
            addAccountedBytes(2, 3);
        }

        /*
        public override bool SaveToDevice(ref FaHDevice faHDevice)
        {
            if (functionPropertyCommand != FAHFunctionPropertyCommand.PropertyControlTypes.NotSet)
            {
                FahDeviceParametersOld devParms = CreateBasicDeviceParametes();
                devParms.ObjectID = ObjectID;
                devParms.PropertyID = PropertyID;
                devParms.Data = FPSRpayload;
                devParms.PropertyControlString = functionPropertyCommand.ToString();
                devParms.PropertyControl = (byte)functionPropertyCommand;
                devParms.Response = this.resultCode;
                devParms.ByteDataParm = ByteDataParm;
                faHDevice.AddDeviceParameters(ref devParms);                
                return true;
            }
            return false;
        }*/

        public FAHReadablePayloadPacketEx ProcessPayload(FAHPayloadInterpeters.FAHFunctionPropertyCommand.PropertyControlTypes fAHFunctionProperty, byte[] parmeters = null)
        {
            switch (fAHFunctionProperty)
            {
                case FAHFunctionPropertyCommand.PropertyControlTypes.GroupCommEnableCtl:
                    return new FPSR_GroupCommEnableCtl(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.__UnsupportedCommand__0x07:
                    //Unkown packet, seems also to be unknown to F@H devices
                    return new FPSR_ResultOnly(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.LoadStateMach0x10:
                    return new FPSR_LoadStateMachine(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadFuncList:
                    return new FPSR_FunctionList(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.UpdConsistencyTag:
                    return new FPSR_ConsistancyTag(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns:
                    return new FPSR_ConnectionInfo(this.payloadReference, parmeters);

                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadFlr_RmNr:
                    return new FPSR_RoomInfo(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadIconId:
                    return new FPSR_IconInfo(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadDevHealth:
                    return new FPSR_ReadDeviceHealth(this.payloadReference);                    

                case FAHFunctionPropertyCommand.PropertyControlTypes.WriteValue:
                    return new FPSR_PropertyValueRead(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.PtrInfoRead:
                    return new FPSR_PropertyValueRead(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc:
                    if (PropertyID == 4 && ObjectID == 0)
                    {
                        return new FPSR_DeviceParameterInfo(this.payloadReference);
                    }
                    else if(PropertyID >= 2 && ObjectID == 7)
                    {
                        return new FPSR_DeviceOIDData(this.payloadReference, parmeters);
                    }
                    else if (PropertyID == 1 )
                    {
                        return new FPSR_ChannelDescription(this.payloadReference);
                    }
                    else if(PropertyID >= 2)
                    {
                        return new FPSR_DescriptorValueRead(this.payloadReference);
                    }
                    else if ( this.payloadReference.PayloadByteData.Length == 5)
                    {
                        //Empty ReadDesc, no payload
                        return new FPSR_ResultOnly(this.payloadReference);
                    }
                    break;
                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo:
                    if (PropertyID == 1 && ObjectID == 0)
                    {
                        return new FPSR_BasicDeviceInfo(this.payloadReference);
                    }
                    else if(ObjectID > 0 && PropertyID == 1)
                    {
                        return new FPSR_DeviceChannelInfo(this.payloadReference);
                    }
                    else
                    {
                        return new FPSR_PropertyValueRead(this.payloadReference);
                    }
                case FAHFunctionPropertyCommand.PropertyControlTypes.DeleteConn:
                    return new FPSR_ResultOnly(this.payloadReference);

                case FAHFunctionPropertyCommand.PropertyControlTypes.WriteIconId:
                    if (this.payloadReference.PayloadByteData.Length == 5)
                    {
                        return new FPSR_ResultOnly(this.payloadReference);
                        //DataAccepted, no payload
                    }
                    break;

                case FAHFunctionPropertyCommand.PropertyControlTypes.WriteFlr_RmNr:
                    if (this.payloadReference.PayloadByteData.Length == 5)
                    {
                        return new FPSR_ResultOnly(this.payloadReference);
                        //DataAccepted, no payload
                    }
                    break;

                case FAHFunctionPropertyCommand.PropertyControlTypes.AssignConn:
                    if (this.payloadReference.PayloadByteData.Length == 5)
                    {
                        return new FPSR_ResultOnly(this.payloadReference);
                        //DataAccepted, no payload
                    }
                    break;
                default:
                    break;
            }
            return this;
        }

        protected override string PrintOut()
        {
            string psrpname = this.GetType().Name.Replace("FPSR_", "");                         

            if (FPSRpayload != null)
            {
                string hex = BitConverter.ToString(FPSRpayload).Replace('-', ' ');
                return string.Format("[{4}] Ch{0:D3}:{1}->{2} [{3}]", ObjectID, PropertyID, resultCode, hex, psrpname);
            }
            else
            {
                return string.Format("[{3}] Ch{0:D3}:{1}->{2} ", ObjectID, PropertyID, resultCode, psrpname);
            }
        }
    }
}
