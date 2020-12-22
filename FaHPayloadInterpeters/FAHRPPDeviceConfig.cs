using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using KNXBaseTypes;

namespace KNXBaseTypes.FAHPayloadInterpeters
{
    public class FAHRPPDeviceConfig : FAHReadablePayloadPacket
    {
        /*
        public enum DeviceAction
        {
            Undefined = 0x0000,            
            DeviceProperty = 0x0004,
            Device0x7 = 0x0007,
            AddPairing = 0x0102,
            DeviceAddPairingB = 0x0103,
            Device0x104 = 0x0104,
            Device0x105 = 0x0105,
            ChannelUpdate = 0x0201,
            RemovePairing = 0x0202,
            DeviceDelPairingB = 0x0203,
            Device0x204 = 0x0204,
            DeviceIcon = 0x0301,
            UpdateCompleted = 0x0401,
            DeviceLocation = 0x0501,
            DeviceDetails = 0x0001,
            DeviceRuntimeStatus = 0x0601,


            //unsure
            __GetAction1 = 0x0501, //Location?


        }*/
        /*
        public enum UpdateAction
        {
            ActionNotDefined = 0x0,
            Remove = 0x82,
            Add = 0x81,
            __RelativeSetValueControl = 0x84,
            DeviceLocation = 0x85,
            __Pairing0x80Value = 0x80,
            GetDeviceRuntimeStatus = 0x06
        }
        
        public enum DeviceConfigItem
        {
            UpdatePairing = 0x2,
            __Update0x1 = 0x1,
            __Update0x3asPairing = 0x3,
            UpdateDeviceProperties = 0x4,
            SetItemTextOnPanel = 0xF0
        }
        */

        /*public enum Parameters
        {
            ParameterNone = 0x0,

        }*/

        /*
        public enum Properties
        {
            PropertyNone = 0x0,
            DimmerLoadType1 = 0x1,
            LedOperationMode = 0x5
        }*/
        
        public enum GroupAddressingType
        {
            _Undefined = 0x00,
            SwitchingOnOff = 0x01,
            _DayNightIndicator = 0x02,
            _Unkown_0x03 = 0x03,
            _Unkown_0x13 = 0x13,
            Dimlevel = 0x0A,
            _Unkown_0x0B = 0x0B
            //Dimlevel = 0xE0,           
        }

        public enum DeviceAction: byte
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
            
            __Unkown0x07 = 0x07,
        }

        private void GetActionFromPayload()
        {
            //Console.WriteLine("0x{0:X2} 0x{1:X2}", kNXPayload.ByteData[3], kNXPayload.ByteData[4]);            
            fprop_cmd = (DeviceAction)base.payloadReference.PayloadByteData[4];
            fprop_objidx = base.payloadReference.PayloadByteData[2];
            fprop_propid = base.payloadReference.PayloadByteData[3];
            base.addAccountedBytes(0, 5);

            /*
            if (fprop_objidx == 0)
            {
                Console.WriteLine("fprop-cmd-access-dev");
            }
            else if (fprop_objidx > 0 && fprop_propid == 1 && fprop_cmd == (DeviceAction)0x81)
            {
                Console.WriteLine("fprop-cmd-ch-setusername");
            }
            else if (fprop_objidx > 0 && fprop_propid == 1)
            {
                Console.WriteLine("fprop-cmd-access-ch-general-info");
            }
            else if (fprop_objidx > 0 && fprop_propid == 2)
            {
                Console.WriteLine("fprop-cmd-access-ch-idp");
            }
            else if (fprop_objidx > 0 && fprop_propid == 3)
            {
                Console.WriteLine("fprop-cmd-access-ch-odp");
            }
            else if (fprop_objidx > 0 && fprop_propid == 4)
            {
                Console.WriteLine("fprop-cmd-access-ch-param");
            }
            else if (fprop_objidx > 0 && fprop_propid == 5)
            { 
                Console.WriteLine("fprop-cmd-access-ch-scene");
            }
            else
            {
                Console.WriteLine("fprop-cmd-generic");
            }
            */

            /*if (fprop_objidx == 0)
            {
                fprop_objidx = 0xff;
            }
            else
            {
                fprop_objidx--;
            }*/
        }

        DeviceAction fprop_cmd;
        byte fprop_propid = 0;
        public byte fprop_objidx;
        public byte[] ByteData;
        public GroupAddressingType groupAddressingType = GroupAddressingType._Undefined;

        public FAHRPPDeviceConfig(KNXPayload payload)
        {
            GetActionFromPayload(payload);

            if (payload.Length > 5)
            {
                addAccountedBytes(5, (uint)(payload.Length - 5));
                ByteData = payload.GetBytes(5, payload.Length - 5);
            }
            else
            {
                ByteData = null;
            }
            
            switch (fprop_cmd)
            {
                /*case DeviceAction.DeviceRuntimeStatus:
                    //Update status requested
                    break;*/


                case DeviceAction.WriteIconId:
                    /*PropertyValue = KNXDataConversion.knx_to_uint16_rev(payload.GetBytes(5, 2));
                    addAccountedBytes(5, 2);*/
                    break;

                case DeviceAction.AssignConn:
                        groupAddressingType = (GroupAddressingType)payload.ByteData[5];
                        addAccountedBytes(5);
                        /*GroupAddress = new KNXAddress(KNXDataConversion.knx_to_uint16_rev(payload.GetBytes(6, 2)));
                        addAccountedBytes(6, 2);*/
                        break;

                case DeviceAction.DeleteConn:
                        groupAddressingType = (GroupAddressingType)payload.ByteData[5];
                        /*GroupAddress = new KNXAddress();
                        addAccountedBytes(5);
                        addAccountedBytes(6, 2); //0x00, 0x00 for new value*/
                    break;

                /*case DeviceAction.RemovePairing:                    
                    groupAddressingType = (GroupAddressingType)payload.ByteData[5];
                    addAccountedBytes(5);
                    if (isSetAction)
                    {
                        GroupAddress = new KNXAddress(KNXDataConversion.knx_to_uint16_rev(payload.GetBytes(6, 2)));
                        addAccountedBytes(6, 2);
                    }
                    else
                    {
                        GroupAddress = new KNXAddress(payload.ByteData[6]);
                        addAccountedBytes(6, 1);
                    }
                    break;                    

                case DeviceAction.DeviceAddPairingB:
                case DeviceAction.DeviceDelPairingB:
                    if (isSetAction)
                    {

                        GroupAddress = new KNXAddress(KNXDataConversion.knx_to_uint16_rev(payload.GetBytes(6, 2)));
                        addAccountedBytes(6, 2);
                    }
                    else
                    {
                        if (payload.Length == 6)
                        {
                            GroupAddress = new KNXAddress();
                        }
                        else if (payload.Length == 7)
                        {
                            GroupAddress = new KNXAddress(payload.ByteData[6]);
                            addAccountedBytes(6, 1);
                        }
                        else
                        {
                            throw new Exception("Add this lenght");
                        }
                    }
                    break;*/

                case DeviceAction.WriteFlr_RmNr:
                    //addAccountedBytes(5, 6);
                    //1 Byte Floor
                    //2 Byte Room
                    //3+4 X
                    //5+6 Y
                    break;

                    /*
                case DeviceAction.DeviceProperty:
                    //Console.WriteLine(isSetAction ? "Set" : "Get");
                    addAccountedBytes(5);
                    this.Parameter = payload.ByteData[5];
                    if(payload.Length == 7)
                    {
                        this.PropertyValue = payload.ByteData[6];
                    }
                    else if(payload.Length == 8)
                    {
                        this.PropertyValue = KNXDataConversion.knx_to_uint16(payload.GetBytes(6,2));
                    }
                    break;*/
                default:
                    //deviceAction = DeviceAction.Undefined;
                    break;         
            }            
        }

        protected override string PrintOut()
        {
            if (ByteData != null)
            {
                string hex = BitConverter.ToString(ByteData).Replace('-', ' ');
                return string.Format("Cfg: {0} ch{1:D3} {2} [{3}] ", fprop_cmd, fprop_objidx, fprop_propid, hex);
            }
            else
            {
                return string.Format("Cfg: {0} ch{1:D3} {2} ", fprop_cmd, fprop_objidx, fprop_propid);
            }
            /*
            string GetSet = isSetAction ? "Set" : "Get";
            if (deviceAction == DeviceAction.AddPairing || deviceAction == DeviceAction.RemovePairing)
            {
                return string.Format("Cfg: {4}{0}-{2} ch{1:d4} {3} ", deviceAction, Channel, groupAddressingType, GroupAddress.knxAddress, GetSet);
            }
            else if(Parameter > 0)
            {
                return string.Format("Cfg: {4}{0} ch{1:d4} pm{2:d4} val:{3} ", deviceAction, Channel, Parameter-1, PropertyValue, GetSet);
            }
            else
            {
                return string.Format("Cfg: {2}{0} ch{1:d4} ", deviceAction, Channel, GetSet);
            }*/

        }

    }
}
