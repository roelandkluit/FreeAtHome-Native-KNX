using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAHPayloadInterpeters;
using KNXBaseTypes;

namespace FAHPayloadInterpeters.FAHFunctionPropertyStateResponses
{
    public class FPSRBasic
    {
        /*public enum FPSRClassType
        {
            BasicDeviceInfo,
            DeviceDescriptor
        }*/

        protected KNXPayload payloadReference;

        public FPSRBasic(KNXPayload OwnerParent)
        {
            payloadReference = OwnerParent;
        }
    }
}
