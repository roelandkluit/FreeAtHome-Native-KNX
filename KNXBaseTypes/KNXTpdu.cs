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
    public class KNXTpdu
    {
        public enum TpduType
        {
            /*DataBroadcast,
            DataGroup,
            DataInduvidual,
            DataConnected,*/
            Data,
            Connect,
            Disconnect,
            Ack,
            Nack,
        };

        private KNXPayload OwnerPayload;
        //private byte byteTpduData;

        public KNXTpdu(KNXPayload Parent)
        {
            OwnerPayload = Parent;
        }

        internal byte ByteValue
        {
            get
            {
                return OwnerPayload.PayloadByteData[0];
            }
        }

        public bool Control
        {
            get
            {
                //Bit0 D7 --> https://support.knx.org/hc/en-us/articles/115003188529-Payload
                return (ByteValue & 0x80) > 0;
            }
        }

        public bool numbered
        {
            get
            {
                //Bit1 D6 --> https://support.knx.org/hc/en-us/articles/115003188529-Payload
                return (ByteValue & 0x40) > 0;
            }
        }

        public TpduType tpduType
        {
            get
            {
                if (Control)
                {
                    if (numbered)
                    {
                        if ((ByteValue & 1) == 0)
                            return TpduType.Ack;
                        else
                            return TpduType.Nack;
                    }
                    else if ((ByteValue & 1) == 0)
                        return TpduType.Connect;
                    else
                        return TpduType.Disconnect;
                }
                else
                {
                    /*
                    if (OwnerPayload.ownerOfPayload.DestinationAddressType == KNXmessage.DestinationAddressFieldType.Group)
                    {
                        if (OwnerPayload.ownerOfPayload.TargetAddress.knxAddress == 0)
                            return TpduType.DataBroadcast;
                        else
                            return TpduType.DataGroup;
                    }
                    else if (numbered)
                        return TpduType.DataConnected;
                    else
                        return TpduType.DataInduvidual;*/
                    return TpduType.Data;
                }
            }
        }
    }
}
