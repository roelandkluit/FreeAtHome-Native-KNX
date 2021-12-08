/*
 *  FreeAtHome KNX TCP Server and Communication module. This software
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FaHKNXTCPServer
{
    public class TCPknxServer : IDisposable
    {
        List<TcpClient> tcpClients = new List<TcpClient>();
        TcpListener lcTCPserver = null;
        object tLockClientList = new object();
        Thread tMain;
        //Thread tServer;
        private bool disposedValue;
        private byte[] dataToWrite = new byte[0];
        object tLock = new object();

        public delegate void EventOnKNXMessage(TCPknxServer caller, KNXmessage Message);
        public event EventOnKNXMessage OnKNXMessage;

        public delegate void EventOnClientConnectionEvent(TCPknxServer caller, IPAddress ClientIPAddress);
        public event EventOnClientConnectionEvent onClientConnect;
        public event EventOnClientConnectionEvent onClientDisconnect;


        public delegate void EventOnAddRemoveAckAddr(TCPknxServer caller, KNXAddress Address);
        public event EventOnAddRemoveAckAddr OnKNXAddressAdd;
        public event EventOnAddRemoveAckAddr OnKNXAddressRemove;

        private bool SendDataToClients(byte[] data)
        {
            List<byte> dList = new List<byte>(data);
            dList.Add(KNXUartModule.KNXUartConnection.CalculateChecksum(dList));
            string base64 = Convert.ToBase64String(dList.ToArray()) + "\r\n";
            data = ASCIIEncoding.UTF8.GetBytes(base64);

            bool bRet = true;
            lock (tLockClientList)
            {
                foreach (var c in tcpClients)
                {
                    try
                    {
                        if (c != null)
                        {
                            if (c.Connected)
                            {
                                if (!SendDataToClient(data, c))
                                    bRet = false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            return bRet;
        }

        private static bool SendDataToClient(byte[] data, TcpClient client)
        {
            try
            {
                Console.WriteLine("Send KNX to client: " + client.Client.RemoteEndPoint.ToString());
                var stream = client.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        public byte[] LastDataWritten
        {
            get
            {
                lock (tLock)
                {
                    return dataToWrite;
                }
            }
        }

        public bool SendKNXMessage(byte[] message)
        {
            bool ret = SendDataToClients(message);
            lock (tLock)
            {
                dataToWrite = message;
            }
            return ret;
        }

        public TCPknxServer(string ip, uint port)
        {
            IPAddress localAddr = IPAddress.Parse(ip);
            lcTCPserver = new TcpListener(localAddr, (int)port);
/*            tServer = new Thread(new ParameterizedThreadStart(StartServiceThread));
            tServer.Start();
        }

        internal void StartServiceThread(object o)
        { 
            //IPAddress localAddr = IPAddress.Parse(ip);
            //lcTCPserver = new TcpListener(localAddr, (int)port);*/
            lcTCPserver.Start();
            tMain = new Thread(new ParameterizedThreadStart(StartListener));
            tMain.Start(lcTCPserver);
/*            while (true)
            {
                Thread.Sleep(1000);
            }*/
        }

        ~TCPknxServer()
        {
            Dispose();
        }

        public void StartListener(Object sObject)
        {
            TcpListener server = sObject as TcpListener;
            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();

                    onClientConnect?.Invoke(this,((IPEndPoint)client.Client.RemoteEndPoint).Address);

                    lock (tLockClientList)
                    {
                        foreach (var c in tcpClients)
                        {
                            if (c == null)
                            {
                                Console.WriteLine("CleanupNULL");
                                tcpClients.Remove(c);
                            }
                            else if (!c.Connected)
                            {
                                Console.WriteLine("CleanupDisconnect");
                                tcpClients.Remove(c);
                            }
                        }
                        tcpClients.Add(client);
                    }

                    Console.WriteLine("Connected!");

                    Thread t = new Thread(new ParameterizedThreadStart(HandleTCPClient));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }

        public void HandleTCPClient(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            IPAddress clientIP = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;
            try
            {
                while (client.Connected)
                {
                    while (client.Available > 0)
                    {
                        using (StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                try
                                {
                                    List<byte> knxmsg = new List<byte>(Convert.FromBase64String(line));
                                    if (knxmsg.Count < 3)
                                        continue;

                                    //Get Checksum and remove from array.
                                    byte checksum = knxmsg.Last();
                                    knxmsg.RemoveAt(knxmsg.Count - 1);

                                    if (KNXUartModule.KNXUartConnection.CheckChecksum(knxmsg, checksum))
                                    {
                                        if (knxmsg[0] == KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_NETWORK_MSG)
                                        {
                                            Console.Write("knxLayerCommand ");
                                            if (knxmsg[1] == KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_ADDKNXADDRTOACK)
                                            {
                                                if (knxmsg.Count == 4)
                                                {
                                                    KNXAddress k = new KNXAddress(knxmsg.ToArray(), 2);
                                                    Console.WriteLine("KnxAddToAck: {0}", k.knxAddress.ToString());
                                                    OnKNXAddressAdd(this, k);
                                                }
                                                else
                                                {
                                                    Console.Write("KnxAddToAck lenghtfailed");
                                                }
                                            }
                                            else if (knxmsg[1] == KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_REMOVEKNXADDRTOACK)
                                            {
                                                if (knxmsg.Count == 4)
                                                {

                                                    KNXAddress k = new KNXAddress(knxmsg.ToArray(), 2);
                                                    Console.WriteLine("KnxRemoveFromAck: {0}", k.knxAddress.ToString());
                                                    OnKNXAddressRemove(this, k);
                                                }
                                                else
                                                {
                                                    Console.Write("KnxRemoveFromAck lenghtfailed");
                                                }
                                            }
                                            else if (knxmsg[1] == KNXNetworkLayer.KNXNetworkLayerTemplate.ADDR_NETWORK_KEEPALIVE)
                                            { 
                                                //Child still alive (timer???)
                                            }
                                            else
                                            {
                                                Console.Write("Unkown");
                                            }
                                            continue;
                                        }
                                        else
                                        {
                                            KNXBaseTypes.KNXmessage kNXmessage = KNXBaseTypes.KNXmessage.fromByteArray(knxmsg.ToArray(), DateTime.Now);
                                            OnKNXMessage?.Invoke(this, kNXmessage);
                                            Console.Write("OK: ");
                                            Console.WriteLine(kNXmessage.ToHexString());
                                        }
                                    }
                                    else
                                    {
                                        Console.Write("CheckSumMismatch: " + line);
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("Cannot process Base64: " + line);
                                }
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Client Closed: {0}", e.ToString());
            }
            Console.WriteLine("Client Closed");
            onClientDisconnect?.Invoke(this, clientIP);
            if (client != null)
            {
                if (client.Connected)
                {
                    client.Close();
                }
            }
            lock (tLockClientList)
            {
                tcpClients.Remove(client);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    /*if(tServer!=null)
                    {
                        tServer.Abort();
                    }*/
                    if (lcTCPserver != null)
                    {
                        lcTCPserver.Stop();
                    }
                    if (tMain != null)
                    {
                        tMain.Abort();
                    }
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Server()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}