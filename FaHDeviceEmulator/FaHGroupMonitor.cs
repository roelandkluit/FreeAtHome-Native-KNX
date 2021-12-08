using FAHPayloadInterpeters;
using KNXBaseTypes;
using KNXNetworkLayer;
using KNXUartModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualFahDevice
{
    public class FaHGroupMonitor
    {
        private KNXNetworkLayerTemplate kNXUart;
        public KNXAddress knxGroupToMonitor { private set; get; }
        private byte[] data = null;

        public byte[] GroupValue
        {
            get
            {
                if (data == null)
                    throw new Exception("Status not available");
                return data;
            }
        }

        public bool GroupValueAsBool
        {
            get
            {
                if (data == null)
                    throw new Exception("Status not available");
                return data[0] == 1;
            }
        }


        public double GroupValueAsDouble
        {
            get
            {
                if (data == null)
                    throw new Exception("Status not available");
                return KNXHelpers.knxDataToDouble(data, 0);
            }
        }

        public delegate void EventOnGroupValueChange(FaHGroupMonitor caller, byte[] data);
        public event EventOnGroupValueChange OnGroupValueChange;
       
        public FaHGroupMonitor(KNXNetworkLayerTemplate kNXUart, KNXAddress GroupToMonitor)
        {
            knxGroupToMonitor = GroupToMonitor;

            this.kNXUart = kNXUart;
            this.kNXUart.OnKNXMessage += KNXUart_OnKNXMessage;
            this.kNXUart.OnKNXEvent += KNXUart_OnKNXEvent;

            GetCurrentGroupValueStatus();
        }

        private void GetCurrentGroupValueStatus()
        {
            //Request current status
            FreeAtHomeDevices.FaHDevice d = new FreeAtHomeDevices.FaHDevice();
            d.KnxAddress.knxAddress = 0;
            KNXmessage a = FAHGroupValueRead.CreateFAHGroupValueRead(d, knxGroupToMonitor, new byte[] { });
            kNXUart.SendKNXMessage(a);
        }

        private void KNXUart_OnKNXEvent(KNXNetworkLayerTemplate caller, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent)
        {
            if(uartEvent == KNXNetworkLayerTemplate.KnxPacketEvents.DeviceOnline)
            {
                GetCurrentGroupValueStatus();
            }
        }

        private void KNXUart_OnKNXMessage(KNXNetworkLayerTemplate caller, KNXBaseTypes.KNXmessage Message, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent)
        {
            if (Message.ControlField.RepeatFrame)
                return;

            if (Message.TargetAddress == knxGroupToMonitor)
            {
                FahPayloadInterpeter.TryToInterpret(ref Message);

                switch (Message.Payload.Apdu.apduType)
                {
                    case KNXAdpu.ApduType.GroupValueWrite:
                        FAHGroupValueWrite fAHGroupValueWrite = new FAHGroupValueWrite(Message.Payload);
                        Console.Write("GroupMonitor: {0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                        data = fAHGroupValueWrite.MessageData;
                        OnGroupValueChange?.Invoke(this, data);
                        Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
                        break;

                    case KNXAdpu.ApduType.GroupValueRead:
                        FAHGroupValueRead fAHGroupValueRead = new FAHGroupValueRead(Message.Payload);
                        Console.Write("GroupMonitor: {0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                        Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
                        break;

                    case KNXAdpu.ApduType.GroupValueResponse:
                        FAHGroupValueResponse fAHGroupValueReponse = new FAHGroupValueResponse(Message.Payload);
                        Console.Write("GroupMonitor: {0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                        data = fAHGroupValueReponse.MessageData;
                        OnGroupValueChange?.Invoke(this, data);
                        Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
                        break;

                    default:
                        Console.WriteLine("???" + Message.Payload.Apdu.apduType);
                        break;
                }                
            }
        }
    }
}