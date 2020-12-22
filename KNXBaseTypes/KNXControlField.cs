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
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes
{
    public class knxControlField
    {
        private const byte CONTROL_FIELD_PRIORITY_MASK = 0x0C;
        private const byte COMMAND_FIELD_REPEAT_MASK = 0x20;
        private const byte MASK_PacketType = 0xD3;
        private const byte VALUE_PacketLong = 0x10;
        private const byte VALUE_PacketShort = 0x90;

        private byte bControlField;
        /*
         *  Octet 0 Control Field 
            frame type std. = 0
            0
            Repeat flag, rep = 0
            1
            Priority, low: 11b
            Priority, low: 11b
            0
            0
        */

        /*
         * Two types of Messages
         * uArt Messages
         * 
         *  Defined by serial interface
         * 
         * knx Messages
         * 
         *  KNX standard
         * 
         *  0       1       2        3       4       5       6       7
         *  +-------+-------+--------+-------+-------+-------+-------+-------+
         *  | Teleg | Fixed | Repeat | Fixed | Prio  | Prio  | Fixed | Fixed |
         *  | Type  |   0   |        |   1   |       |       |   0   |   0   |
         *  +-------+-------+--------+-------+-------+-------+-------+-------+  
         * 
         * Telegramtype
            0 = Uitgebreid frame: lengte van het telegram = 9..263 octetten
            1 = Standaardframe: lengte van het telegram = 8..23 octetten

         * Herhalingsstatus telegram
            0 = Herhaald
            1 = Niet herhaald (origineel)

        * Prioriteit telegram
            00 = Systeem
            10 = Dringend
            01 = Normaal
            11 = Laag
         */

        public enum KnxPriority : byte
        {
            KNX_PRIORITY_SYSTEM = 0x0,
            KNX_PRIORITY_HIGH = 0x4,
            KNX_PRIORITY_ALARM = 0x8,
            KNX_PRIORITY_NORMAL = 0xC
        };

        public enum KnxPacketType: byte
        {
            KNX_PacketShort = 0x90,
            KNX_PacketLong = 0x10,
            KNX_PacketUnknown = 0x0
        }

        public KnxPacketType PacketType 
        {
            get
            {
                byte knxMaskedValue = KNXHelpers.GetByteBitValue(bControlField, MASK_PacketType);

                switch(knxMaskedValue)
                {
                    case VALUE_PacketLong:
                        return KnxPacketType.KNX_PacketLong;

                    case VALUE_PacketShort:
                        return KnxPacketType.KNX_PacketShort;

                    default:
                        return KnxPacketType.KNX_PacketUnknown;
                }
            }
            set
            {
                if (value == KnxPacketType.KNX_PacketUnknown)
                    throw new InvalidDataException();
                else
                {
                    //Warning values might be lost when switching values! (HOPCOUNT etc.)
                    bControlField = KNXHelpers.SetByteBitValue(bControlField, MASK_PacketType, (byte)value);
                }
            }
        }

        internal knxControlField(byte Value)
        {
            bControlField = Value;
        }

        internal byte ByteValue
        {
            get
            {
                return bControlField;
            }
        }

        public knxControlField(KnxPacketType KnxPacketType, bool RepeatFrame, KnxPriority priority)
        {
            bControlField = 0;
            this.PacketType = KnxPacketType;
            this.RepeatFrame = RepeatFrame;
            this.Priority = priority;
        }

        public KnxPriority Priority
        {
            get
            {
                return (KnxPriority)KNXHelpers.GetByteBitValue(bControlField, CONTROL_FIELD_PRIORITY_MASK);
            }
            set
            {
                bControlField = KNXHelpers.SetByteBitValue(bControlField, CONTROL_FIELD_PRIORITY_MASK, (byte)value);
            }
        }

        public bool RepeatFrame
        {
            get
            {
                /*byte COMMAND_FIELD_REPEAT_MASK = 0x20;
                return (COMMAND_FIELD_REPEAT_MASK & bvalue) != COMMAND_FIELD_REPEAT_MASK;*/
                return KNXHelpers.GetByteBitValue(bControlField, COMMAND_FIELD_REPEAT_MASK, 5) == 0;
            }
            set
            {
                byte newValue = 1;
                if (value)
                    newValue = 0;

                bControlField = KNXHelpers.SetByteBitValue(bControlField, COMMAND_FIELD_REPEAT_MASK, newValue, 5);
            }
        }

        public override string ToString()
        {
            return string.Format("Command: 0x{0:X2} [{1}]", bControlField, Convert.ToString(bControlField, 2).PadLeft(8, '0'));
        }
    }
}
