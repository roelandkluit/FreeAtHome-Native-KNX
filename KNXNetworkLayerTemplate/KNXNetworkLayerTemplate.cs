/*
 *  FreeAtHome KNX VirtualSwitch and Communication module. This software
    provides interaction over KNX to Free@Home bus devices.

    This software is not created, maintained or has any assosiation
    with ABB \ Busch-Jeager.

    Copyright (C) 2020-2021 Roeland Kluit

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
*/

using KNXBaseTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KNXNetworkLayer
{
    public class KNXNetworkLayerTemplate
    {
        public const byte ADDR_NETWORK_MSG = 0;
        public const byte ADDR_NETWORK_KEEPALIVE = 1;
        public const byte ADDR_ADDKNXADDRTOACK = 10;
        public const byte ADDR_REMOVEKNXADDRTOACK = 11;

        public enum KnxPacketEvents
        {
            GotKNXPacket = 0,
            GotAckPacket,
            GotNackPacket,
            DeviceOffline,
            DeviceOnline,
            DeviceConnecting
        }
        
        public virtual void SendAck() { }
        public virtual bool SendKNXMessage(KNXmessage knxMsg) { return false; }

        public virtual void AddKNXAddressToAck(KNXAddress addr) { }
        public virtual void RemoveKNXAddressToAck(KNXAddress addr) { }

        public delegate void EventOnKNXMessage(KNXNetworkLayerTemplate caller, KNXmessage Message, KnxPacketEvents uartEvent);
        public event EventOnKNXMessage OnKNXMessage;

        public delegate void EventOnKNXEvent(KNXNetworkLayerTemplate caller, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent);
        public event EventOnKNXEvent OnKNXEvent;

        //protected internal void OnU


        protected internal void OnParentKNXMessage(KNXNetworkLayerTemplate caller, KNXmessage Message, KnxPacketEvents uartEvent)
        {
            OnKNXMessage?.Invoke(caller, Message, uartEvent);
        }

        protected internal void OnParentKNXEvent(KNXNetworkLayerTemplate caller, KnxPacketEvents uartEvent)
        {
            OnKNXEvent?.Invoke(caller, uartEvent);
        }

        public static byte CalculateChecksum(List<byte> frame)
        {
            byte cs = 0xFF;
            for (int it = 0; it != frame.Count; it++)
            {
                cs ^= frame[it];
            }
            return cs;
        }

        public static bool CheckChecksum(List<byte> frame, int checksum)
        {
            int cs = CalculateChecksum(frame);
            cs ^= checksum;
            return (cs == 0 ? true : false);
        }

    }
}
