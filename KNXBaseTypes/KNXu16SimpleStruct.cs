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

    These are supporting modules for KNX protocol processing
*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes
{
    //[JsonConverter(typeof(KNXu16SimpleStructJsonConverter))]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public class KNXu16SimpleStruct
    {
        [System.Runtime.InteropServices.FieldOffset(0)]
        public UInt16 u16value;

        [JsonIgnore]
        [System.Runtime.InteropServices.FieldOffset(0)]
        public byte u16valueHigh;

        [JsonIgnore]
        [System.Runtime.InteropServices.FieldOffset(1)]
        public byte u16valueLow;

        public override int GetHashCode()
        {
            return u16value;
        }

        public KNXu16SimpleStruct(UInt16 value)
        {
            //knxAddressLow = 0;
            //knxAddressHigh = 0;
            u16value = value;
        }

        public KNXu16SimpleStruct()
        {
            u16value = 0;
        }

        public static bool operator == (KNXu16SimpleStruct b1, KNXu16SimpleStruct b2)
        {
            if (b1 is null)
                return b2 is null;

            return b1.Equals(b2);
        }

        public static bool operator != (KNXu16SimpleStruct b1, KNXu16SimpleStruct b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object obj)
        {
            try
            {
                var tpe = obj as KNXu16SimpleStruct;
                return (tpe.u16value == this.u16value);
            }
            catch
            {
                return false;
            }
        }

        public KNXu16SimpleStruct(byte lowByte, byte highByte)
        {
            u16valueLow = lowByte;
            u16valueHigh = highByte;
        }

        public KNXu16SimpleStruct(byte[] SourceArray, uint index)
        {
            u16valueLow = SourceArray[index];
            u16valueHigh = SourceArray[index + 1];
        }

        public KNXu16SimpleStruct(byte[] SourceArray)
        {
            u16valueLow = SourceArray[0];
            u16valueHigh = SourceArray[1];
        }

        public byte[] ToByteArray()
        {
            return new byte[] { u16valueLow, u16valueHigh };
        }

        public override string ToString()
        {
            return string.Format("[0x{0:X2}-0x{1:X2}]", u16valueLow, u16valueHigh);
        }
    }

/*    public class KNXu16SimpleStructJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            KNXu16SimpleStruct kNXu16Simple = value as KNXu16SimpleStruct;
            writer.WriteValue(kNXu16Simple.u16value);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var hex = serializer.Deserialize<string>(reader);
                return new KNXu16SimpleStruct(UInt16.Parse(hex));
            }
            return Enumerable.Empty<KNXu16SimpleStruct>();
        }
    }*/
}
