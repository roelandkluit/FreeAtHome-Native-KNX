using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes.FAHPayloadInterpeters
{
    public class FAHRPPDeviceNaming : FAHReadablePayloadPacket
    {
        public enum Channel:byte
        {
            deviceNotSpecified,
            deviceActor1,
            deviceActor2,
            deviceSwitch,
            devicePanelIcon
        }
        public string ChannelName;
        public Channel SubDevice = Channel.deviceNotSpecified;

        /*
        public FAHRPPDeviceNaming(KNXPayload payload, KNXmessage.ApciType apduType)
        {
            if(payload[3] != 0x81 || payload[2] != 0x01)
            {
                throw new Exception("Unexpected payload msg config");
            }
            if (apduType == KNXmessage.ApciType.ABBSetOutput1DeviceName)
            {
                SubDevice = Channel.deviceActor1;
            }
            else if (apduType == KNXmessage.ApciType.ABBSetOutput2DeviceName)
            {
                SubDevice = Channel.deviceActor2;
            }
            else if (apduType == KNXmessage.ApciType.ABBSetOutputPanelDeviceName)
            {
                SubDevice = Channel.devicePanelIcon;
            }
            else if (apduType == KNXmessage.ApciType.ABBDeviceSetName)
            {
                SubDevice = Channel.deviceSwitch;
            }

            this.addAccountedBytes(0, 4);
            this.addAccountedBytes(4, (uint)payload.Length - 4);
            ChannelName = System.Text.Encoding.Default.GetString(payload.GetBytes(4, payload.Length - 4));
        }
        */
    }
}
