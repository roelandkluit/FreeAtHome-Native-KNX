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
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KNXBaseTypes
{
    public class KNXmessage
    {
        /*
        //Standard Frame Type
        Octet 0   Octet 1   Octet2   Octet 3   Octet 4   Octet 5   Octet 6  Octet 7                                          Octet L
        +---------+------------------+-------------------+---------+--------+------------------------------------------------+----------+
        | Header  | Source           | Dest.             | DafLSDU | TPCI + | Data .....                                     | CheckSum |
        |         | Address          | Address           | Len     | APCI   |                                                |          |
        +---------+------------------+-------------------+---------+--------+------------------------------------------------+----------+
        1 byte    2 byte             2 byte              1 byte    1 byte   n byte                                           1 byte

        //Extended Frame Type
        Octet 0   Octet 1    Octet2   Octet 3   Octet 4   Octet 5   Octet 6   Octet 7   Octet 8   Octet 9                                 Octet N
        +---------+----------+------------------+-------------------+---------+---------+---------+---------------------------------------+----------+
        | Header  |Extended  | Source           | Dest.             | Data    | TPCI +  | APCI    | Data  .....                           | CheckSum |
        |         |FrameInfo | Address          | Address           | Lenght  | APCI    |         |                                       |          |
        +---------+----------+------------------+-------------------+---------+---------+---------+---------------------------------------+----------+
        1 byte    1 byte     2 byte             2 byte              1 byte    1 byte    1 byte    n byte                                  1 byte

        Header          = See below the structure of a cEMI header
        Source Address  = 0x0000 - filled in by router/gateway with its source address which is
                          part of the KNX subnet
        Dest. Address   = KNX group or individual address (2 byte)
        Data Length     = Number of bytes of data in the APDU excluding the TPCI/APCI bits
        APDU            = Application Protocol Data Unit - the actual payload including transport
                          protocol control information (TPCI), application protocol control
                          information (APCI) and data passed as an argument from higher layers of
                          the KNX communication stack

        ## ---- Header ----- #
          Bit  |
         ------+---------------------------------------------------------------
           7   | Frame Type  - 0x0 for extended frame
               |               0x1 for standard frame
         ------+---------------------------------------------------------------
           6   | Reserved
               |
         ------+---------------------------------------------------------------
           5   | Repeat Flag - 0x0 repeat frame on medium in case of an error
               |               0x1 do not repeat
         ------+---------------------------------------------------------------
           4   | System Broadcast - 0x0 system broadcast
               |                    0x1 broadcast
         ------+---------------------------------------------------------------
           3   | Priority    - 0x0 system
               |               0x1 normal
         ------+               0x2 urgent
           2   |               0x3 low
               |
         ------+---------------------------------------------------------------
           1   | Acknowledge Request - 0x0 no ACK requested
               | (L_Data.req)          0x1 ACK requested
         ------+---------------------------------------------------------------
           0   | Confirm      - 0x0 no error
               | (L_Data.con) - 0x1 error
         ------+---------------------------------------------------------------

         ## ---- ExtendedFrameInfo ----- #
          Bit  |
         ------+---------------------------------------------------------------
           7   | Destination Address Type - 0x0 individual address
               |                          - 0x1 group address
         ------+---------------------------------------------------------------
          6-4  | Hop Count (0-7)
         ------+---------------------------------------------------------------
          3-0  | Extended Frame Format - 0x0 standard frame
         ------+---------------------------------------------------------------
        */

        public DateTime Timestamp { private set; get; }

        public enum MessageDirectedType
        {
            NotAddressedToDevice,
            IndividualAdressed,
            GroupValueAdressed,
            Broadcast
        }

        public enum DestinationAddressFieldType : byte
        {
            Individual = 0, //false,
            Group = 1 // true
        }

        //0
        public knxControlField ControlField;
        public KNXPayload Payload;

        //1 Long packet only 
        public KnxExtendedControlField ExtendedControlField = new KnxExtendedControlField();

        //1&2 or 2&3
        public KNXAddress SourceAddress = new KNXAddress();
        //3&4
        public KNXAddress TargetAddress = new KNXAddress();

        KNXDafLSDU dafLSDU = new KNXDafLSDU(0);
        private byte LongPacketDatalenght;

        public byte PayloadLenght
        {
            get
            {
                if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketLong)
                {
                    if (Payload.PayloadByteData.Length != LongPacketDatalenght)
                    {
                        LongPacketDatalenght = (byte)Payload.PayloadByteData.Length;
                    }
                    return LongPacketDatalenght;
                }
                else if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketShort)
                {
                    if (Payload.PayloadByteData.Length != dafLSDU.PayLoadLenghtWithAPDU)
                    {
                        dafLSDU.PayLoadLenghtWithAPDU = (byte)(Payload.PayloadByteData.Length);
                    }
                    return dafLSDU.PayLoadLenghtWithAPDU;
                }
                else
                    throw new InvalidOperationException();
            }
            internal set
            {
                if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketLong)
                    LongPacketDatalenght = value;
                else if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketShort)
                    dafLSDU.PayLoadLenghtWithAPDU = value;
                else
                    throw new InvalidOperationException();
            }
        }

        public MessageDirectedType CheckIsMessageIntendedForMe(KNXAddress IndividualAddres, KNXAddress[] GroupAdresses)
        {
            if (this.DestinationAddressType == DestinationAddressFieldType.Individual)
            {
                if (this.TargetAddress.knxAddress == IndividualAddres.knxAddress)
                {
                    //directed to address
                    return MessageDirectedType.IndividualAdressed;
                }
            }

            if (this.TargetAddress.knxAddress == 0)
            {
                return MessageDirectedType.Broadcast;
            }
            else
            {
                if (GroupAdresses != null)
                {
                    foreach (KNXAddress k in GroupAdresses)
                    {
                        if (k == this.TargetAddress)
                        {
                            return MessageDirectedType.GroupValueAdressed;
                        }
                    }
                }
            }
            return MessageDirectedType.NotAddressedToDevice;
        }

        public byte HopCount
        {
            get
            {
                if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketLong)
                    return ExtendedControlField.HopCount;
                else if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketShort)
                    return dafLSDU.LSDU;
                else
                    throw new InvalidOperationException();
            }
            set
            {
                if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketLong)
                    ExtendedControlField.HopCount = value;
                else if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketShort)
                    dafLSDU.LSDU = value;
                else
                    throw new InvalidOperationException();
            }
        }

        public KNXmessage.DestinationAddressFieldType DestinationAddressType
        {
            get
            {
                if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketLong)
                    return ExtendedControlField.DestinationAddressType;
                else if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketShort)
                    return dafLSDU.DestinationAddressType;
                else
                    throw new InvalidOperationException();
            }
            set
            {
                if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketLong)
                    ExtendedControlField.DestinationAddressType = value;
                else if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketShort)
                    dafLSDU.DestinationAddressType = value;
                else
                    throw new InvalidOperationException();
            }
        }

        public string HeaderAsString
        {
            get
            {
                return string.Format("{0}{1}\t{5}\t{2}\t{3}\t{4}\t", "", Payload.Apdu.apduType, ExtendedControlField, SourceAddress, TargetAddress, ControlField.Priority);
                //return string.Format("{0}-{1}\t{5}\t{2}\t{3}\t{4}\t", Payload.Tpdu.tpduType, Payload.Apdu.apduType, ExtendedControlField, SourceAddress, TargetAddress, ControlField.Priority);
            }
        }

        public string ToHexString()
        {
            return KNXHelpers.GetStringHex(toByteArray());
        }

        public void ProcessKNXMessage(byte[] stagedPacket)
        {
            try
            {
                int dataIndex = 0;

                //Control Field
                ControlField = new knxControlField(stagedPacket[dataIndex++]);

                switch (ControlField.PacketType)
                {
                    case knxControlField.KnxPacketType.KNX_PacketLong:
                        //Extended Control Field, only for long packet
                        ExtendedControlField = new KnxExtendedControlField(stagedPacket[dataIndex++]);
                        break;
                    case knxControlField.KnxPacketType.KNX_PacketShort:
                        break;
                    default:
                        throw new InvalidDataException();
                }

                //Source address field
                SourceAddress = KNXAddress.FromReversedBytes(stagedPacket[dataIndex++], stagedPacket[dataIndex++]);

                //Target address field
                TargetAddress = KNXAddress.FromReversedBytes(stagedPacket[dataIndex++], stagedPacket[dataIndex++]);

                switch (ControlField.PacketType)
                {
                    case knxControlField.KnxPacketType.KNX_PacketLong:
                        //Payload Lenght, for long Packet
                        LongPacketDatalenght = (stagedPacket[dataIndex++]);
                        //Payload
                        Payload = new KNXPayload(stagedPacket, dataIndex++, this);
                        break;
                    case knxControlField.KnxPacketType.KNX_PacketShort:
                        //DafSuLen for short Packet
                        dafLSDU = new KNXDafLSDU(stagedPacket[dataIndex++]);
                        //Payload
                        Payload = new KNXPayload(stagedPacket, dataIndex++, this);

                        //Page https://support.knx.org/hc/en-us/articles/115003188529-Payload not totally clear on how this is formatted
                        //This implementation seems to work for F@H
                        /*if (PayloadLenght == 0)
                            Console.WriteLine("No payload\\Apci");
                        else if (PayloadLenght == 1)
                        {
                            Console.WriteLine("Short payload with Apci");
                            Apci = new KNXApci(stagedPacket[dataIndex], stagedPacket[dataIndex++]);
                        }
                        else
                        {
                            Console.WriteLine("Long payload with Apci");
                            Apci = new KNXApci(stagedPacket[dataIndex++], stagedPacket[dataIndex++]);
                        }*/
                        break;
                    default:
                        throw new InvalidDataException();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to process bytedata to KNXMessage", e);
            }            
        }

        public KNXmessage(): this (knxControlField.KnxPacketType.KNX_PacketShort)
        {
            /*ControlField = new knxControlField(0);
            Payload = new KNXPayload(this, 2);
            Timestamp = DateTime.Now;*/
        }

        public KNXmessage(knxControlField.KnxPacketType PacketType)
        {
            ControlField = new knxControlField(PacketType, false, knxControlField.KnxPriority.KNX_PRIORITY_NORMAL);
            Payload = new KNXPayload(this, 2);
            Timestamp = DateTime.Now;
        }

        /*public static KNXmessage NewEmptyKnxPacket()
        {
            return new KNXmessage();
        }

        public static KNXmessage NewEmptyKnxPacket(knxControlField.KnxPacketType PacketType)
        {
            return new KNXmessage(PacketType);
        }*/

        public static KNXmessage fromByteArray(List<byte> frame, DateTime timestamp)
        {
            return fromByteArray(frame.ToArray(), timestamp);
        }

        public static KNXmessage fromByteArray(byte[] frame, DateTime timestamp)
        {
            KNXmessage k = new KNXmessage
            {
                Timestamp = timestamp,
            };
            k.ProcessKNXMessage(frame);
            return k;
        }

        //Construct frame from KNX message
        public byte[] toByteArray()
        {
            List<byte> knxPacket = new List<byte>();
            if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketLong)
            {
                knxPacket.Add(ControlField.ByteValue);
                knxPacket.Add(ExtendedControlField.ByteValue); //EXT
                knxPacket.Add(SourceAddress.knxAddressHigh);
                knxPacket.Add(SourceAddress.knxAddressLow);
                knxPacket.Add(TargetAddress.knxAddressHigh);
                knxPacket.Add(TargetAddress.knxAddressLow);
                knxPacket.Add((byte)(PayloadLenght - 1));
            }
            else if (ControlField.PacketType == knxControlField.KnxPacketType.KNX_PacketShort)
            {
                knxPacket.Add(ControlField.ByteValue);
                knxPacket.Add(SourceAddress.knxAddressHigh);
                knxPacket.Add(SourceAddress.knxAddressLow);
                knxPacket.Add(TargetAddress.knxAddressHigh);
                knxPacket.Add(TargetAddress.knxAddressLow);
                knxPacket.Add(dafLSDU.ByteValue);
            }
            else
            {
                throw new NotImplementedException();
            }

            knxPacket.AddRange(Payload.PayloadByteData);
            return knxPacket.ToArray();
        }
    }
}
