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
    public class KnxExtendedControlField
    {
        //Bit 0 -> AddressType (Unicast, Multicast)
        //Bit 1,2,3 -> HopCount
        //Bit 4,5,6,7 -> ExtendedFrameFormat
        private byte? bvalue = null;       

        public KnxExtendedControlField(byte value)
        {
            bvalue = value;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public KnxExtendedControlField() { }

        internal byte ByteValue
        {
            get
            {
                return (byte)((bvalue == null) ? 0 : bvalue);
            }
        }

        public override bool Equals(object obj)
        {
            Type T = obj.GetType();
            if (T == typeof(byte))
            {
                return bvalue == (byte)obj;
            }
            else if (T == typeof(int))
            {
                return bvalue == (int)obj;
            }
            else if (T.Equals(this))
            {
                return bvalue == ((KnxExtendedControlField)obj).ByteValue;
            }
            else
            {
                throw new Exception("Cannot compare types");
            }
        }

        public static bool operator ==(KnxExtendedControlField knxExtendedControlField, object byteValue)
        {
            return knxExtendedControlField.Equals(byteValue);
        }

        public static bool operator !=(KnxExtendedControlField knxExtendedControlField, object byteValue)
        {
            return !knxExtendedControlField.Equals(byteValue);
        }

        internal string ShortDestinationAddressType
        {
            get
            {
                if (DestinationAddressType == KNXmessage.DestinationAddressFieldType.Group)
                {
                    return "Group";
                }
                else
                {
                    return "Single";
                }
            }
        }

        public KNXmessage.DestinationAddressFieldType DestinationAddressType
        {
            get
            {
                if (bvalue == null)
                    throw new NullReferenceException();
                return KNXHelpers.GetByteBitValue((byte)bvalue, 0x80, 7) == 0 ? KNXmessage.DestinationAddressFieldType.Individual : KNXmessage.DestinationAddressFieldType.Group;
            }
            set
            {
                if (bvalue == null)
                    bvalue = 0;

                byte newval;
                if (value == KNXmessage.DestinationAddressFieldType.Individual)
                    newval = 0;
                else
                    newval = 1;

                bvalue = KNXHelpers.SetByteBitValue((byte)bvalue, 0x80, newval, 7);
            }
        }

        public byte HopCount
        {
            get
            {
                if (bvalue == null)
                    throw new NullReferenceException();

                return KNXHelpers.GetByteBitValue((byte)bvalue, 0x70, 4);
            }
            set
            {
                if (bvalue == null)
                    bvalue = 0;

                bvalue = KNXHelpers.SetByteBitValue((byte)bvalue, 0x70, value, 4);
            }
        }

        public byte ExtendedFrameFormat
        {
            get
            {
                if (bvalue == null)
                    throw new NullReferenceException();

                return KNXHelpers.GetByteBitValue((byte)bvalue, 0x0F);
            }
            set
            {
                if (bvalue == null)
                    bvalue = 0;

                bvalue = KNXHelpers.SetByteBitValue((byte)bvalue, 0x0F, value);
            }
        }

        public override string ToString()
        {
            if(bvalue == null)
                return string.Format("[NoExtdFrame]");
            else
                return string.Format("H:{0}, {1}, FF:0x{2:X2}", HopCount, ShortDestinationAddressType, ExtendedFrameFormat);
        }
    }
}