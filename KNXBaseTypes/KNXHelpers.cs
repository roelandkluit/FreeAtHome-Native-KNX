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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes
{
    public class KNXHelpers
    {
        public static string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";


        public static UInt16 GetCheckNullUint16Value(UInt16? value, UInt16 defaultvalue = 0xFFFF)
        {
            if(value == null)
            {
                return defaultvalue;
            }
            else
            {
                return (UInt16)value;
            }
        }

        public static byte[] HexStringToByteArray(string hexString)
        {
            int hexStringLength = hexString.Length;
            byte[] b = new byte[hexStringLength / 2];
            for (int i = 0; i < hexStringLength; i += 2)
            {
                int topChar = (hexString[i] > 0x40 ? hexString[i] - 0x37 : hexString[i] - 0x30) << 4;
                int bottomChar = hexString[i + 1] > 0x40 ? hexString[i + 1] - 0x37 : hexString[i + 1] - 0x30;
                b[i / 2] = Convert.ToByte(topChar + bottomChar);
            }
            return b;
        }

        public static string GetStringHex(byte[] data)
        {
            string retString = "";
            bool first = true;
            foreach (byte b in data)
            {
                if (!first)
                    retString += ", ";
                retString += string.Format("0x{0:X2}", b);
                first = false;
            }
            return retString;
        }

        public enum knxPropertyReturnValues : sbyte
        {
            MoreIndices = 2,
            AdditionalData = 1,
            Success = 0,
            Failed = -1,
            InvalidIndex = -2,
            WriteSizeInvalid = -3,
            CommandNotSupported = -128,
        }

        public static UInt16 GetWord(byte[] data)
        {
            return (UInt16)((data[0] << 8) + data[1]);
        }

        public static UInt16 knxToUint16(byte[] knxdata, int startindex)
        {
            if (knxdata.Length < startindex + 2)
            {
                throw new Exception("Can only convert a 2 Byte object to uint16");
            }

            return (UInt16)((((UInt16)((UInt16)knxdata[startindex + 0]) << 8) & 0xFF00) | (((UInt16)knxdata[startindex + 1]) & 0x00FF));
        }

        public static byte[] uint16ToKnx(UInt16 value, UInt16 mask = 0xFFFF)
        {
            byte[] payload = new byte[2];
            payload[0] = (byte)((UInt16)(payload[0] & (~mask >> 8)) | ((value >> 8) & (mask >> 8)));
            payload[1] = (byte)(((UInt16)payload[1] & ~mask) | (value & mask));
            return payload;
        }

        public static byte SetByteBitValue(byte CurrentByteValue, byte BitMask, byte ValueToBitOr, byte byteBaseShiftValue = 0)
        {
            //Check if shifting of bit is needed
            if (byteBaseShiftValue != 0)
                ValueToBitOr = (byte)(ValueToBitOr << byteBaseShiftValue);

            //Clear any existing value by inverting the mask
            byte clearedexistingvalues = (byte)(CurrentByteValue & (0xFF - BitMask));
            return (byte)(clearedexistingvalues | ValueToBitOr);
        }

        public static byte GetByteBitValue(byte CurrentByteValue, byte BitMask, byte byteBaseShiftValue = 0)
        {
            //Check if shifting of bit is needed
            /*if (byteBaseShiftValue != 0)
                ValueToBitOr = (byte)(ValueToBitOr << byteBaseShiftValue);*/

            byte value = (byte)(CurrentByteValue & BitMask);
            if (byteBaseShiftValue != 0)
                value = (byte)(value >> byteBaseShiftValue);

            return value;
        }

        public static double knxDataToDouble(byte[] knxdata, int startindex)
        {
            //Convert a KNX 2 byte float object to a float
            if (knxdata.Length < startindex + 2)
            {
                throw new Exception("Can only convert a 2 Byte object to float");                
            }

            int data = knxdata[startindex + 0] * 256 + knxdata[startindex + 1];
            int sign = data >> 15;
            int exponent = (data >> 11) & 0x0f;
            float mantisse = (float)(data & 0x7ff);
            if (sign == 1)
            {
                mantisse = -2048 + mantisse;
            }

            return mantisse * Math.Pow(2, exponent) / 100;
        }
        /*
        public static UInt32 knx_to_uint32(byte[] knxdata, int startindex)
        {
            if (knxdata.Length < startindex + 4)
            {
                throw new Exception("Can only convert a 4 Byte object to uint32");
            }

            UInt32 val = ((((UInt32)knxdata[startindex + 0]) << 24) & 0xFF000000) |
                   ((((UInt32)knxdata[startindex + 1]) << 16) & 0x00FF0000) |
                   ((((UInt32)knxdata[startindex + 2]) << 8) & 0x0000FF00) |
                   (((UInt32)knxdata[startindex + 3]) & 0x000000FF);
            return val;
        }*/
    }
}
