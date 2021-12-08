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
using KNXNetworkLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FaHTCPClient
{
    public class TCPknxClient : KNXNetworkLayerTemplate
    {
        public bool autoReconnect = true;
        private Thread tMain;
        private TcpClient tcpClient;

        //public delegate void EventOnKNXMessage(TCPknxClient caller, KNXmessage Message);
        //public new event EventOnKNXMessage OnKNXMessage;


        /*private static byte CalculateChecksum(List<byte> frame)
        {
            byte cs = 0xFF;
            for (int it = 0; it != frame.Count; it++)
            {
                cs ^= frame[it];
            }
            return cs;
        }*/

        public bool Connected
        {
            get
            {
                try
                {
                    if (tcpClient == null)
                        return false;

                    return tcpClient.Connected;
                }
                catch
                {
                }
                return false;
            }
        }

        public override bool SendKNXMessage(KNXmessage knxMsg)
        {
            if(tcpClient.Connected)
            {
                return PrepareAndSendDataToServerIfConnected(knxMsg.toByteArray());
            }
            return false;
        }

        public void SendKNXKeepAlive()
        {
            List<byte> addrAdd = new List<byte>();
            addrAdd.Insert(0, KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_NETWORK_KEEPALIVE);
            addrAdd.Insert(0, KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_NETWORK_MSG);
            PrepareAndSendDataToServerIfConnected(addrAdd.ToArray());
        }


        public override void AddKNXAddressToAck(KNXAddress addr)
        {
            List<byte> addrAdd = new List<byte>(addr.ToByteArray());
            addrAdd.Insert(0, KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_ADDKNXADDRTOACK);
            addrAdd.Insert(0, KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_NETWORK_MSG);
            PrepareAndSendDataToServerIfConnected(addrAdd.ToArray());
        }

        public override void RemoveKNXAddressToAck(KNXAddress addr)
        {
            List<byte> addrRemove = new List<byte>(addr.ToByteArray());
            addrRemove.Insert(0, KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_REMOVEKNXADDRTOACK);
            addrRemove.Insert(0, KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_NETWORK_MSG);
            PrepareAndSendDataToServerIfConnected(addrRemove.ToArray());
        }

        private bool PrepareAndSendDataToServerIfConnected(byte[] data)
        {
            List<byte> dList = new List<byte>(data);
            dList.Add(CalculateChecksum(dList));
            string base64 = Convert.ToBase64String(dList.ToArray()) + "\r\n";
            data = ASCIIEncoding.UTF8.GetBytes(base64);

            bool bRet = true;

            if (tcpClient != null && tcpClient.Connected)
            {
                if (!SendDataToServer(data, tcpClient))
                    bRet = false;
            }
            return bRet;
        }

        private bool SendDataToServer(byte[] data, TcpClient client)
        {
            try
            {
                Console.WriteLine("Send KNX to server: " + client.Client.RemoteEndPoint.ToString());
                //lock (tClientLockObject)
                {
                    var stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception e)
            {
                try
                {
                    client.Close();
                }
                catch { }
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        public TCPknxClient(string ip, int port)
        {
            IPAddress ServerAddr = IPAddress.Parse(ip);
            IPEndPoint iPEndPoint = new IPEndPoint(ServerAddr, port);
            tMain = new Thread(new ParameterizedThreadStart(StartClient));
            tMain.Start(iPEndPoint);
        }      

        public void StartClient(Object sObject)
        {
            bool first = true;
            IPEndPoint iPEndPoint = sObject as IPEndPoint;

            while (first || autoReconnect)
            {
                try
                {
                    first = false;
                    tcpClient = new TcpClient();
                    this.OnParentKNXEvent(this, KNXNetworkLayerTemplate.KnxPacketEvents.DeviceConnecting);
                    Console.WriteLine("Connecting to: {0}", iPEndPoint.Address);
                    if (!tcpClient.ConnectAsync(iPEndPoint.Address, iPEndPoint.Port).Wait(5000))
                    {
                        Console.WriteLine("Failed to connect, retry loop");
                        continue;
                    }
                    this.OnParentKNXEvent(this, KNXNetworkLayerTemplate.KnxPacketEvents.DeviceOnline);
                    try
                    {
                        while (tcpClient.Connected)
                        {
                            while (tcpClient.Available > 0)
                            {
                                byte[] data = new byte[tcpClient.Available];

                                using (StreamReader reader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8))
                                {
                                    string line;
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        try
                                        {
                                            List<byte> knxmsg = new List<byte>(Convert.FromBase64String(line));
                                            byte checksum = knxmsg.Last();
                                            knxmsg.RemoveAt(knxmsg.Count - 1);

                                            if (CheckChecksum(knxmsg, checksum))
                                            {
                                                //Console.Write("OK: ");
                                                KNXBaseTypes.KNXmessage kNXmessage = KNXBaseTypes.KNXmessage.fromByteArray(knxmsg.ToArray(), DateTime.Now);
                                                base.OnParentKNXMessage(this, kNXmessage, KnxPacketEvents.GotKNXPacket);
                                            }
                                            else
                                            {
                                                Console.Write("CheckSumMismatch: " + line);
                                            }
                                        }
                                        catch
                                        {
                                            Console.Write("Cannot process Base64: " + line);
                                        }
                                    }
                                }
                            }
                        }
                        Thread.Sleep(25);
                    }
                    catch (Exception e)
                    {
                        this.OnParentKNXEvent(this, KNXNetworkLayerTemplate.KnxPacketEvents.DeviceOffline);
                        Console.WriteLine("Connection Failed: " + e);
                        Thread.Sleep(5000);
                    }            
                }
                catch(Exception e)
                {
                    this.OnParentKNXEvent(this, KNXNetworkLayerTemplate.KnxPacketEvents.DeviceOffline);
                    Console.WriteLine("Cannot Connect: " + e);
                    Thread.Sleep(5000);
                }
            }
        }

        public void Dispose()
        {
            autoReconnect = false;
            if (tcpClient != null)
            {
                if (tcpClient.Connected)
                {
                    tcpClient.Close();
                }
            }
            tcpClient = null;
        }
    }
}
