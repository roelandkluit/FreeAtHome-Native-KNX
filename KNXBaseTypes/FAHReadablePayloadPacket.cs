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
    public class FAHReadablePayloadPacket
    {
        internal List<uint> bytesAccounted = new List<uint>();
        internal List<uint> bytesIgnored = new List<uint>();
        public KNXPayload payloadReference { private set; get; }

        public knxControlField.KnxPacketType defaultKnxPacketType { protected set; get; }

        public FAHReadablePayloadPacket(KNXPayload kNXPayload)
        {
            payloadReference = kNXPayload;
            if (kNXPayload.Apdu.hasDataAfterApdu)
            {
                addAccountedBytes(0, 1);
            }
            else
            {
                addAccountedBytes(0, 2);            
            }
            defaultKnxPacketType = knxControlField.KnxPacketType.KNX_PacketUnknown; 
        }

        public virtual string Name
        {
            get
            {
                return GetType().Name;
            }
        }

        public void addAccountedBytes(uint byteAccounted)
        {
            bytesAccounted.Add(byteAccounted);
        }

        public void addIgnoredBytes(uint byteAccounted)
        {
            bytesIgnored.Add(byteAccounted);
        }

        protected byte[] RemainderBytesAsPayload(uint index)
        {
            if(payloadReference.PayloadByteData.Length <= index)
            {
                return null;
            }
            else
            {
                return payloadReference.GetBytes((int)index, (int)(payloadReference.PayloadByteData.Length - index));
            }            
        }

        public void addAccountedBytes(uint byteAccounted, uint count)
        {
            while (count > 0)
            {
                bytesAccounted.Add(byteAccounted);
                byteAccounted++;
                count--;
            }
        }

        public void addIgnoredBytes(uint byteAccounted, uint count)
        {
            while (count > 0)
            {
                bytesIgnored.Add(byteAccounted);
                byteAccounted++;
                count--;
            }
        }


        protected virtual string PrintOut()
        {
            return "";
        }

        public virtual void UpdatePacketSettings()
        {
            this.payloadReference.OwnerOfPayload.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;
            this.payloadReference.OwnerOfPayload.ControlField.RepeatFrame = false;
            this.payloadReference.OwnerOfPayload.HopCount = 6;
        }

        public string GetPrintUnnactounedBytesAsString(bool hideAccounted = false)
        {
            string retString = "";
            uint i = 0;
            foreach (byte b in payloadReference.PayloadByteData)
            {
                if (i != 0)
                    retString += " ";

                if (bytesAccounted.Contains(i))
                {
                    if (hideAccounted)
                    {
                        i++;
                        continue;
                    }
                }


                if (i == 1 && !bytesAccounted.Contains(i))
                {
                    retString += string.Format("[0x{0:X2}|0x{1:X2}]", b & 0xF0, b & 0xF);
                }
                else
                {
                    retString += string.Format("0x{0:X2}", b);
                }
                i++;
            }
            return retString;
        }

        public void PrintUnaccountedBytes(bool hideAccounted = false)
        {
            if (bytesAccounted.Count >= 3)
                Console.Write(PrintOut());

            ConsoleColor c = Console.BackgroundColor;
            ConsoleColor f = Console.ForegroundColor;
            uint i = 0;
            foreach (byte b in payloadReference.PayloadByteData)
            {
                if (i != 0)
                    Console.Write(" ");

                if (bytesAccounted.Contains(i))
                {
                    Console.BackgroundColor = c;
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (hideAccounted)
                    {
                        i++;
                        continue;
                    }
                }
                else if(bytesIgnored.Contains(i))
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                }

                if (i == 1 && !bytesAccounted.Contains(i))
                {
                    Console.Write("[0x{0:X2}|0x{1:X2}]", b & 0xF0, b & 0xF);
                }
                else
                {
                    Console.Write("0x{0:X2}", b);
                }                
                i++;
            }
            Console.BackgroundColor = c;
            Console.ForegroundColor = f;
            Console.WriteLine();
        }
    }
}
