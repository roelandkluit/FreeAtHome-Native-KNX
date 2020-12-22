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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAHPayloadInterpeters;
using FAHPayloadInterpeters.FAHFunctionPropertyStateResponses;
using FreeAtHomeDevices;
using KNXBaseTypes;

namespace FAHPayloadInterpeters
{
    public class FAHFunctionPropertyCommand : FAHReadablePayloadPacketEx
    {
        internal const uint FPCHEADERSIZE = 5;

        public static KNXmessage CreateFAHFunctionPropertyCommand(FaHDevice faHDevice, PropertyControlTypes propertyControlType, byte ObjectID, byte PropertyID, byte[] payload = null)
        {
            if (payload == null)
                payload = new byte[0];
            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);
            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Group;
            kNXmessage.SourceAddress = new KNXAddress(1);
            kNXmessage.TargetAddress = faHDevice.KnxAddress;
            kNXmessage.HopCount = 6;
            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;
            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyCommand, (uint)(5 + payload.Length));
            kNXmessage.Payload.ReadablePayloadPacket = new FAHFunctionPropertyCommand(kNXmessage.Payload);
            FAHFunctionPropertyCommand newPkg = (FAHFunctionPropertyCommand)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.PropertyControl = propertyControlType;
            if(payload.Length != 0)
                kNXmessage.Payload.UpdateBytes(payload, 5, payload.Length);
            newPkg.ObjectID = ObjectID;
            newPkg.PropertyID = PropertyID;
            return kNXmessage;
        }

        private KNXmessage CreateEmptyMessage(KNXHelpers.knxPropertyReturnValues returnCode)
        {
            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort)
            {
                DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual
            };

            uint payloadSize = FPCHEADERSIZE;

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FAHFunctionPropertyStateResponse(kNXmessage.Payload);
            FAHFunctionPropertyStateResponse newPkg = (FAHFunctionPropertyStateResponse)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.PropertyID = PropertyID;
            newPkg.ObjectID = ObjectID;
            newPkg.resultCode = returnCode;
            return kNXmessage;
        }

        public KNXmessage CreateCommandNotSupportedMessage()
        {
            return CreateEmptyMessage(KNXHelpers.knxPropertyReturnValues.CommandNotSupported);
        }

        public KNXmessage CreateCommandFailedMessage()
        {
            return CreateEmptyMessage(KNXHelpers.knxPropertyReturnValues.Failed);
        }


        public KNXmessage CreateEmptySuccessMessage(bool moreIndices = false)
        {
            if(moreIndices)
                return CreateEmptyMessage(KNXHelpers.knxPropertyReturnValues.MoreIndices);
            else
                return CreateEmptyMessage(KNXHelpers.knxPropertyReturnValues.Success);
        }

        public KNXmessage CreateInvalidIndexMessage()
        {
            return CreateEmptyMessage(KNXHelpers.knxPropertyReturnValues.InvalidIndex);
        }


        /*
         //Payload
         ID     0       1       2       3       4       5       6       7       8       9       10      
         HEX    0x02    0xC7    0x00    0x01    0x85    0x02    0x07    0x46    0x8B    0xF4    0xD5            
         DEC    2       199     0       1       133     2       7       70      139     244     213
         VAL    APCI    APCI    ObjID   PropID  Action  Data....
        */

        /*
        public static KNXmessage CreateFunctionPropertyCommand(byte ObjectID, Byte PropertyID, PropertyControlTypes propertyControl)
        {
            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);
            kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;
            kNXmessage.SourceAddress = new KNXAddress(1);
            kNXmessage.HopCount = 6;
            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyCommand, 5);
            kNXmessage.Payload.ReadablePayloadPacket = new FAHFunctionPropertyCommand(kNXmessage.Payload);
            FAHFunctionPropertyCommand newPkg = (FAHFunctionPropertyCommand)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.ObjectID = ObjectID;
            newPkg.PropertyID = PropertyID;
            newPkg.PropertyControl = propertyControl;
            return kNXmessage;
        }*/

        public enum PropertyControlTypes : byte
        {
            ReadBasicInfo = 0x00,
            ReadDesc = 0x01,
            ReadConns = 0x02,
            //ReadPtrStrList = 0x02,
            ReadIconId = 0x03,
            ReadFuncList = 0x04,
            ReadFlr_RmNr = 0x05,
            ReadDevHealth = 0x06,
            LoadStateMach0x10 = 0x10,
            EnableGroupComm = 0x11,
            PtrInfoRead = 0x14,
            ReadNeighTable = 0x20,
            WriteValue = 0x80,
            AssignConn = 0x81,
            DeleteConn = 0x82,
            WriteIconId = 0x83,
            UpdConsistencyTag = 0x84,
            WriteFlr_RmNr = 0x85,
            WriteAdrOffset = 0x86,
            StartCalibration = 0x87,
            LoadStateMach0x90 = 0x90,
            GroupCommEnableCtl = 0x91,
            WriteRFParam = 0xA1,
            __UnsupportedCommand__0x07 = 0x07,
            NotSet = 0xFF,
        }

        /*
         //Payload
         ID     0       1       2       3       4       5       6       7       8       9       10      
         HEX    0x02    0xC7    0x00    0x01    0x85    0x02    0x07    0x46    0x8B    0xF4    0xD5            
         DEC    2       199     0       1       133     2       7       70      139     244     213
         VAL    APCI    APCI    ObjID   PropID  Action  Data....
        */

        public byte[] FPCpayload
        {
            get
            {
                uint dataIndex = 5;
                if(CheckIfFieldIDisPresent())
                {
                    dataIndex = 6;
                }
                return base.RemainderBytesAsPayload(dataIndex);
            }
            /*set
            {

            }*/
        }

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

        public void GetPropertyControlForReply(ref byte[] ControlSecondIndex, ref PropertyControlTypes propertyControl)
        {
            propertyControl = this.PropertyControl;
            ControlSecondIndex = this.FPCpayload;
        }

        public PropertyControlTypes PropertyControl
        {
            get
            {
                return (PropertyControlTypes)(base.payloadReference.PayloadByteData[4]);
            }
            set
            {
                base.payloadReference.PayloadByteData[4] = (byte)value;
            }
        }        

        public FAHFunctionPropertyCommand(KNXPayload kNXPayload) : base(kNXPayload)
        {
            if (kNXPayload.Apdu.apduType != KNXAdpu.ApduType.FunctionPropertyCommand)
            {
                throw new InvalidCastException("Message type does not match");
            }
            base.defaultKnxPacketType = knxControlField.KnxPacketType.KNX_PacketShort;
            addAccountedBytes(2, 3);
            InterpetPropertyCommand();
        }

        protected override string PrintOut()
        {
            if (FPCpayload != null)
            {
                string hex = BitConverter.ToString(FPCpayload).Replace('-', ' ');
                return string.Format("Ch{0:D3}:{1}->{2} [{3}]", ObjectID, PropertyID, PropertyControl, hex);
            }
            else
            {
                return string.Format("Ch{0:D3}:{1}->{2} ", ObjectID, PropertyID, PropertyControl);
            }
            /*
            if (configuredChannel != 0)
            {
                return string.Format("Rsp: ch{0:D3} Resp: 0x{1:X2}{2:X2} ", configuredChannel, responsedata[0], responsedata[1]);
            }
            else if (consistencytag != null && consistencytag.Length == 2)
            {
                return string.Format("NCT: 0x{0:X2}{1:X2} ", consistencytag[0], consistencytag[1]);
            }
            else
            {
                return base.PrintOut();
            }*/
        }

        public KNXmessage ProcessAndCreateResponse(FaHDevice device)
        {
            KNXmessage k = null;
            switch(PropertyControl)
            {
                case PropertyControlTypes.__UnsupportedCommand__0x07:
                    k = CreateCommandNotSupportedMessage();
                    break;
                    
                case PropertyControlTypes.ReadConns:
                    //ConctionID
                    if (payloadReference.PayloadByteData.Length == 7)
                    {
                        k = FPSR_ConnectionInfo.CreateResponse(this, device);
                        //Should not exist?
                    }
                    else if (payloadReference.PayloadByteData.Length == 5)
                    {
                        k =  FPSR_ConnectionInfo.CreateResponse(this, device);
                    }
                    else
                    {
                        k = CreateCommandNotSupportedMessage();
                    }
                    break;

                case PropertyControlTypes.ReadBasicInfo:
                    if (this.ObjectID == 0 && PropertyID == 1)
                    {
                        k = FPSR_BasicDeviceInfo.CreateResponse(this, device);
                        break;
                    }
                    else if (ObjectID > 0 && PropertyID == 1)
                    {
                        k = FPSR_DeviceChannelInfo.CreateResponse(this, device);
                        break;
                    }
                    else
                    {
                        k = FPSR_PropertyValueRead.CreateReadResponse(this, device);
                        break;
                    }

                case PropertyControlTypes.ReadDevHealth:
                    k = FPSR_ReadDeviceHealth.CreateReadResponse(this, device);
                    break;

                case PropertyControlTypes.ReadDesc:
                    if (this.ObjectID == 0 && PropertyID == 4)
                    {
                        k = FPSR_DeviceParameterInfo.CreateResponse(this, device);
                    }
                    else if (PropertyID == 1)
                    {
                        k = FPSR_ChannelDescription.CreateResponse(this, device);
                    }
                    else if (PropertyID >= 2 && ObjectID == 7)
                    {
                        k = FPSR_DeviceOIDData.CreateResponse(this, device);
                    }
                    else if (PropertyID >= 2)
                    {
                        k = FPSR_DescriptorValueRead.CreateReadResponse(this, device);
                    }
                    break;

                case PropertyControlTypes.WriteValue:
                    payloadReference.ReadablePayloadPacket = new FPC_PropertyValueWrite(payloadReference);
                    FPC_PropertyValueWrite fPSR_PropertyValue = this.payloadReference.ReadablePayloadPacket as FPC_PropertyValueWrite;
                    k = fPSR_PropertyValue.Process(device);
                    break;

                case PropertyControlTypes.ReadFlr_RmNr:
                    k = FPSR_RoomInfo.CreateResponse(this, device);
                    break;

                case PropertyControlTypes.ReadIconId:
                    k = FPSR_IconInfo.CreateResponse(this, device);
                    break;

                case PropertyControlTypes.DeleteConn:
                    payloadReference.ReadablePayloadPacket = new FPC_DeleteConnection(payloadReference);
                    FPC_DeleteConnection fPC_Delete = this.payloadReference.ReadablePayloadPacket as FPC_DeleteConnection;
                    k = fPC_Delete.Process(device);
                    break;

                case PropertyControlTypes.AssignConn:
                    if (PropertyID == 1) //Property ID 1 == name of channel
                    {
                        payloadReference.ReadablePayloadPacket = new FPC_WriteDescription(payloadReference);
                        FPC_WriteDescription fPC_WriteDescription = this.payloadReference.ReadablePayloadPacket as FPC_WriteDescription;
                        k = fPC_WriteDescription.Process(device);
                    }
                    else
                    {
                        payloadReference.ReadablePayloadPacket = new FPC_AssignConnection(payloadReference);
                        FPC_AssignConnection fPC_Assign = this.payloadReference.ReadablePayloadPacket as FPC_AssignConnection;
                        k = fPC_Assign.Process(device);
                    }
                    break;

                case PropertyControlTypes.WriteIconId:
                    payloadReference.ReadablePayloadPacket = new FPC_WriteIcon(payloadReference);
                    FPC_WriteIcon fPC_WriteIcon = this.payloadReference.ReadablePayloadPacket as FPC_WriteIcon;
                    k = fPC_WriteIcon.Process(device);
                    break;

                case PropertyControlTypes.WriteFlr_RmNr:
                    payloadReference.ReadablePayloadPacket = new FPC_WriteRoomInfo(payloadReference);
                    FPC_WriteRoomInfo fPC_WriteRoomInfo = this.payloadReference.ReadablePayloadPacket as FPC_WriteRoomInfo;
                    k = fPC_WriteRoomInfo.Process(device);
                    break;

                case PropertyControlTypes.UpdConsistencyTag:
                    k = FPSR_ConsistancyTag.UpdateConsistancyTag(this, device);
                    break;


                case FAHFunctionPropertyCommand.PropertyControlTypes.ReadFuncList:
                    k = FPSR_FunctionList.CreateResponse(this, device);
                    break;

            }
            if (k!=null)
            {
                k.SourceAddress = device.KnxAddress;
                k.TargetAddress = this.payloadReference.OwnerOfPayload.SourceAddress;
            }
            return k;
        }

        public byte? FieldID
        {
            get
            {
                if(!CheckIfFieldIDisPresent()) return null; 
                return base.payloadReference.PayloadByteData[5];
            }
            set
            {
                if (!CheckIfFieldIDisPresent()) throw new InvalidOperationException();
                base.payloadReference.PayloadByteData[5] = (byte) value;
            }
        }

        private bool CheckIfFieldIDisPresent()
        {
            switch (PropertyControl)
            {
                //case PropertyControlTypes.ReadBasicInfo:
                case PropertyControlTypes.ReadFuncList:
                case PropertyControlTypes.ReadDesc:
                case PropertyControlTypes.WriteValue:
                case PropertyControlTypes.DeleteConn:
                case PropertyControlTypes.ReadBasicInfo:
                    if (base.payloadReference.PayloadByteData.Length > 5)
                    {
                        //Has field ID
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public void InterpetPropertyCommand()
        {
            switch (PropertyControl)
            {
                case PropertyControlTypes.ReadConns:
                    //ConctionID
                    if (payloadReference.PayloadByteData.Length == 7)
                    {
                        addAccountedBytes(5, 2);
                    }
                    break;

                case PropertyControlTypes.ReadBasicInfo:
                case PropertyControlTypes.ReadFuncList:
                    //Field ID
                    addAccountedBytes(5, 1);
                    break;

                case PropertyControlTypes.WriteIconId:
                    //PropertyValue = KNXDataConversion.knx_to_uint16_rev(payload.GetBytes(5, 2));
                    addAccountedBytes(5, 2);
                    break;

                case PropertyControlTypes.AssignConn:
                    //groupAddressingType = (GroupAddressingType)payload.ByteData[5];
                    //GroupAddress = new KNXAddress(KNXDataConversion.knx_to_uint16_rev(payload.GetBytes(6, 2)));
                    if (base.payloadReference.PayloadByteData.Length > 5)
                    {
                        //TODO?? change to PropertyID = 1
                        if (base.payloadReference.PayloadByteData[5] > 10)
                        {
                            addAccountedBytes(5, (uint)(base.payloadReference.PayloadByteData.Length - 5)); //Name
                        }
                        else
                        {
                            addAccountedBytes(5, 1); //Field ID
                            addAccountedBytes(6, 2); //KNX address
                        }
                    }
                    break;

                case PropertyControlTypes.DeleteConn:
                    //groupAddressingType = (GroupAddressingType)payload.ByteData[5];
                    //GroupAddress = new KNXAddress();                    
                    addAccountedBytes(5, 1); //Field ID
                    addAccountedBytes(6, 2); //KNX address //0x00, 0x00 for new value
                    break;

                case PropertyControlTypes.WriteFlr_RmNr:
                    addAccountedBytes(5, 6);
                    //1 Byte Floor
                    //2 Byte Room
                    //3+4 X
                    //5+6 Y
                    break;

                case PropertyControlTypes.WriteValue:
                    //entry
                    addAccountedBytes(5);
                    //value
                    addAccountedBytes(6);
                    break;

                default:
                    break;
            }
        }

    }
}


