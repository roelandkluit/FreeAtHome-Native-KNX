﻿/*
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

using KNXNetworkLayer;
using KNXUartModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaHTCPServer
{
    class Program
    {
        static KNXUartConnection kNXUart;
        static TCPknxServer tCPknxServer;

        static void Main(string[] args)
        {
            kNXUart = new KNXUartConnection(AppSettings.Default.ComPort)
            {
                AllowWrite = true
            };
            kNXUart.OnKNXMessage += KNXUart_OnKNXMessage;

            if (!kNXUart.ResetAndInit())
            {
                throw new Exception("Cannot init");
            }

            tCPknxServer = new TCPknxServer("0.0.0.0", 9998);
            tCPknxServer.OnKNXMessage += TCPknxServer_OnKNXMessage;
            tCPknxServer.OnKNXAddressAdd += TCPknxServer_OnKNXAddressAdd;
            tCPknxServer.OnKNXAddressRemove += TCPknxServer_OnKNXAddressRemove;

            Console.WriteLine("Ready");
            Console.ReadLine();

            tCPknxServer.Dispose();
            kNXUart = null;

        }

        private static void TCPknxServer_OnKNXAddressRemove(TCPknxServer caller, KNXBaseTypes.KNXAddress Address)
        {
            kNXUart.RemoveKNXAddressToAck(Address);
        }

        private static void TCPknxServer_OnKNXAddressAdd(TCPknxServer caller, KNXBaseTypes.KNXAddress Address)
        {
            kNXUart.AddKNXAddressToAck(Address);
        }

        private static void TCPknxServer_OnKNXMessage(TCPknxServer caller, KNXBaseTypes.KNXmessage Message)
        {
            Console.WriteLine("KNXTCPData");
            kNXUart.SendKNXMessage(Message);
        }

        private static void KNXUart_OnKNXMessage(KNXNetworkLayerTemplate caller, KNXBaseTypes.KNXmessage Message, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent)
        {
            Console.WriteLine("KNXLineData");
            tCPknxServer.SendKNXMessage(Message.toByteArray());
        }
    }
}
