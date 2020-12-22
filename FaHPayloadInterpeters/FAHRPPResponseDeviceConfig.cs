using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes.FAHPayloadInterpeters
{
    public class FAHRPPResponseDeviceConfig : FAHReadablePayloadPacket
    {
        //public byte configuredChannel = 0;
        //public byte[] consistencytag;
        //public byte[] responsedata;

        public byte fprop_objidx;
        public byte fprop_propid;
        public fpropReturnValues fprop_return;
        public byte[] ByteData;

        public enum fpropReturnValues: sbyte
        {
            MoreIndices = 2,
            AdditionalData = 1,
            Success = 0,
            Failed = -1,
            InvalidIndex = -2,
            WriteSizeInvalid = -3,
            CommandNotSupported = -128,
        }

        public FAHRPPResponseDeviceConfig(KNXPayload payload)
        {
            addAccountedBytes(0, 2);
            fprop_objidx = payload.ByteData[2];
            fprop_propid = payload.ByteData[3];
            fprop_return = (fpropReturnValues)payload.ByteData[4];
            addAccountedBytes(2, 3);
            
            /*if (fprop_objidx == 0)
            {
                fprop_objidx = 0xff;
            }
            else
            {
                fprop_objidx--;
            }*/

            if (payload.Length > 5)
            {
                addAccountedBytes(5, (uint)(payload.Length - 5));
                ByteData = payload.GetBytes(5, payload.Length - 5);
            }
            else
            {
                ByteData = null;
            }

            /*
            if (payload.ByteData[2] > 0)
            {
                configuredChannel = (byte)(payload.ByteData[2] - 1);
                responsedata = new byte[] { payload.ByteData[3], payload.ByteData[4] };
                addAccountedBytes(2, 3);
            }
            else if (payload.ByteData[2] == 0)
            {
                if (payload.ByteData[3] == 0x01 && payload.ByteData[4] == 0x00)
                {
                    addAccountedBytes(2, 3);
                    //Payload consistancey tag
                    if (payload.Length == 7)
                    {
                        consistencytag = new byte[] { payload.ByteData[5], payload.ByteData[6] };
                        addAccountedBytes(5, 2);
                    }                    
                }
            }*/
        }

        protected override string PrintOut()
        {
            if (ByteData != null)
            {
                string hex = BitConverter.ToString(ByteData).Replace('-', ' ');
                return string.Format("Rsp: ch{0:D3} {1}->{2} [{3}]", fprop_objidx, fprop_propid, fprop_return, hex);
            }
            else
            {
                return string.Format("Rsp: ch{0:D3} {1}->{2} ", fprop_objidx, fprop_propid, fprop_return);
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
    }
}
