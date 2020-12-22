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
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public class KNXAddress : KNXu16SimpleStruct
    {
        public KNXAddress(UInt16 value)
        {
            //knxAddressLow = 0;
            //knxAddressHigh = 0;
            u16value = value;
        }

        [JsonIgnore]
        public UInt16 knxAddress
        {
            get
            {
                return u16value;
            }
            set
            {
                u16value = value;
            }
        }

        [JsonIgnore]
        public byte knxAddressHigh
        {
            get
            {
                return u16valueHigh;
            }
            set
            {
                u16valueHigh = value;
            }
        }

        [JsonIgnore]
        public byte knxAddressLow
        {
            get
            {
                return u16valueLow;
            }
            set
            {
                u16valueLow = value;
            }
        }

        public KNXAddress GetAsReversed()
        {
            return new KNXAddress(u16valueHigh, u16valueLow);
        }

        public string toDottedKNXAddress()
        {
            byte sub = u16valueLow;
            byte middle = (byte)(u16valueHigh & 0x7);
            byte main = (byte)(u16valueHigh & 0x1F);
            return string.Format("{0}/{1}/{2}", main, middle, sub);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            try
            {
                var tpe = obj as KNXAddress;
                return (tpe.knxAddress == this.knxAddress);
            }
            catch
            {
                return false;
            }
        }

        public KNXAddress()
        {
            u16value = 0;
        }

        public static KNXAddress FromReversedBytes(byte highByte, byte lowByte)
        {
            return new KNXAddress(lowByte, highByte);
        }

        public KNXAddress(byte[] SourceArray, uint index) : base(SourceArray, index)
        { 
        }

        public KNXAddress(byte lowByte, byte highByte)
        {
            u16valueLow = lowByte;
            u16valueHigh = highByte;
        }
    }
}