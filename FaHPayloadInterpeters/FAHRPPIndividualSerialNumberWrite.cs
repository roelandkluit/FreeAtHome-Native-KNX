using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes.FAHPayloadInterpeters
{
    public class FAHRPPIndividualSerialNumberWrite : FAHReadablePayloadPacket
    {
        public FreeAtHomeDeviceAddress deviceAddress;
        public KNXAddress kNXAddress;
        public FahSystemID NetworkID;
        public FAHRPPIndividualSerialNumberWrite(KNXPayload payload)
        {
            addAccountedBytes(0, 2);
            deviceAddress = FreeAtHomeDeviceAddress.FromByteArray(payload.ByteData, 2);
            addAccountedBytes(2, 6);
            kNXAddress = new KNXAddress(payload.ByteData[9], payload.ByteData[8]);
            addAccountedBytes(8, 2);
            NetworkID = new FahSystemID(payload.ByteData[10], payload.ByteData[11]); //todo Check byte order!
            addAccountedBytes(10, 2);
        }

        protected override string PrintOut()
        {
            return string.Format("FaHDev: {0} KNX: {1} ", deviceAddress, kNXAddress.knxAddress);
        }
    }
}
