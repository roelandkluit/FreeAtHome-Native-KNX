using KNXBaseTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAtHomeDevices
{
    //public class FahDeviceParametersOld
    //{
    //    [JsonConverter(typeof(StringEnumConverter))]
    //    public KNXAdpu.ApduType AdpuType;
    //    public string PropertyControlString;
    //    public byte PropertyControl;
    //    public byte ObjectID;
    //    public byte PropertyID;
    //    public byte[] ByteDataParm;
    //    [JsonConverter(typeof(StringEnumConverter))]
    //    public KNXHelpers.knxPropertyReturnValues Response;
    //    public byte[] Data;
    //}

    public class FahDeviceOIDData
    {
        public FahDeviceOIDIndicedData[] IndicedData;
    }

    public class FahDeviceOIDIndicedData
    {
        public int Index;
        public byte[] Data;
    }
}
