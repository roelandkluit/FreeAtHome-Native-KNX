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
    public class KNXDafLSDU
    {
        private byte byteDafLSDU;

        public KNXDafLSDU(byte data)
        {
            byteDafLSDU = data;
        }

        internal byte ByteValue
        {
            get
            {
                return byteDafLSDU;
            }
        }

        public KNXmessage.DestinationAddressFieldType DestinationAddressType
        {
            get
            {
                return KNXHelpers.GetByteBitValue(byteDafLSDU, 0x80, 0) == 0x80 ? KNXmessage.DestinationAddressFieldType.Group : KNXmessage.DestinationAddressFieldType.Individual;
            }
            set
            {
                byte bNewvalue = 0x0;
                if (value == KNXmessage.DestinationAddressFieldType.Group)
                    bNewvalue = 0x80;
                byteDafLSDU = KNXHelpers.SetByteBitValue(byteDafLSDU, 0x80, bNewvalue);
            }
        }

        public byte LSDU
        {
            get
            {
                return KNXHelpers.GetByteBitValue(byteDafLSDU, 0x70, 4);
            }
            set
            {
                byteDafLSDU = KNXHelpers.SetByteBitValue(byteDafLSDU, 0x70, value, 4);
            }
        }

        public byte PayLoadLenght
        {
            get
            {
                return (byte)(KNXHelpers.GetByteBitValue(byteDafLSDU, 0xF));
            }
            set
            {
                if (value == 0)
                    throw new IndexOutOfRangeException();
                byteDafLSDU = KNXHelpers.SetByteBitValue(byteDafLSDU, 0xF, (byte)(value));
            }
        }

        public byte PayLoadLenghtWithAPDU
        {
            get
            {
                return (byte)(KNXHelpers.GetByteBitValue(byteDafLSDU, 0xF) + 1);
            }
            set
            {
                if (value == 0)
                    throw new IndexOutOfRangeException();
                byteDafLSDU = KNXHelpers.SetByteBitValue(byteDafLSDU, 0xF, (byte)(value - 1));
            }
        }
    }
}
