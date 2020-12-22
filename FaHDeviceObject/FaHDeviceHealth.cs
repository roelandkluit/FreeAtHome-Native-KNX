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

    This modules is a custom representation of the data in a Free@home device, can be serialized as Json.
    Please note not all fields are reverse engineerd.
    
*/
using KNXBaseTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAtHomeDevices
{
    public class FahDeviceHealth
    {
        //[JsonProperty]
        //  0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22      23      24      25      26      27      

        //  0x07	0xC9	0x00	0x00	0x42	0xB5	0x00	0x54	0x00	0x00	0x00	0xBF	0x00	0x00	0x00	0x03	0x00	0x00	0x00	0x00	0x00	0x06	0x00	0x06	0x00	0x00	0x00	0x00
        //  0x07	0x78	0x00	0x00	0x04	0x31	0x00	0x03	0x00	0x00	0x00	0xBF	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00
        //  Volt----Volt    OperationTime--OperationTime    devicereboots                                                   biterrors---                                    parityerrors    spikeErrors-    othererrorvalue

        [JsonProperty]
        public byte[] ByteData;

        public FahDeviceHealth() { }

        public FahDeviceHealth(byte[] data) { ByteData = data; }

        [JsonIgnore]
        public UInt16 Voltage
        {
            get
            {
                return (new KNXu16SimpleStruct(ByteData, 0)).u16value;

            }
            set
            {
                KNXu16SimpleStruct values = new KNXu16SimpleStruct(value);
                ByteData[0] = values.u16valueHigh;
                ByteData[1] = values.u16valueLow;
            }
        }

        [JsonIgnore]
        public UInt32 Uptime
        {
            get
            {
                byte[] valb = new byte[4];
                Array.Copy(ByteData, 2, valb, 0, 4);
                Array.Reverse(valb);
                return BitConverter.ToUInt32(valb, 0);
            }
            set
            {
                byte[] valb = BitConverter.GetBytes(value);
                Array.Reverse(valb);
                Array.Copy(valb, 0, ByteData, 2, 4);
            }
        }

        [JsonIgnore]
        public UInt16 DeviceReboots
        {
            get
            {
                return (new KNXu16SimpleStruct(ByteData, 6)).u16value;

            }
            set
            {
                KNXu16SimpleStruct values = new KNXu16SimpleStruct(value);
                ByteData[6] = values.u16valueLow;
                ByteData[7] = values.u16valueHigh;
            }
        }

    }
}
