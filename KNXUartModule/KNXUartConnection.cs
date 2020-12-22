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

    This modules allows you to use the TinySerial 810 from Weinzierl to connect to the KNX layer of Free@Home
    Replace this module with your code to transform the network messages into KNX and read the KNX messages in case you are using an other KNX to PC inferface
    
*/
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using KNXBaseTypes;

namespace KNXUartModule
{
    public class KNXUartConnection
    {
        private enum UartControlFieldValues : byte
        {
            UART_State_ind_NoError = 0x07,
            UART_Reset_ind = 0x03,
            L_Poll_Data_ind = 0xF0,
            L_DATA_conf_negative = 0x0B,
            L_DATA_conf_positive = 0x8B,
            MASK_PacketType = 0xD3,
            MASK_PacketLong = 0x10,
            MASK_PacketShort = 0x90,
            UART_State_NACK = 0x0C
        }

        private enum DeviceState : byte
        {
            Unkown = 0,
            ResetRequested,
            ResetAccepted,
            ResetFailed,
            ResetTimeout,
            StatusRequested,
            StatusOK
        }

        private enum TelegramState : byte
        {
            Unkown = 0,
            TelegramSend,
            TelegramAck,
            TelegramFail,
            ResetTimeout
        }

        public enum UartEvents
        {
            GotKNXPacket = 0,
            GotAckPacket,
            GotNackPacket,
            DeviceOffline,
            DeviceOnline
        }

        private enum readResult
        {
            readSucces,
            readFailed,
            readHandledInProcess,
            readGotConfPositive,
            readGotConfNegative,
            readGotNack
        }

        private string _ComPort = "COM1";
        private System.Timers.Timer tmr;
        private SafeSerialPort _port;
        private DeviceState dsState = DeviceState.Unkown;
        private TelegramState tsState = TelegramState.Unkown;
        private bool ledState = false;
        private bool isReading = false;
        public bool AllowWrite = false;
        private int CheckTstateCounter = 0;
        private bool _uartDeviceIsOnline = false;

        private Queue<KNXmessage> knxQueue = new Queue<KNXmessage>();

        public bool uartDeviceOnline
        {
            get
            {
                return _uartDeviceIsOnline;
            }

            private set
            {
                if (_uartDeviceIsOnline == value) return;
                _uartDeviceIsOnline = value;
                if (value == false)
                    OnUartEvent?.Invoke(this, UartEvents.DeviceOffline);
                else
                    OnUartEvent?.Invoke(this, UartEvents.DeviceOnline);
            }
        }

        public List<KNXAddress> kNXAddressesToAck = new List<KNXAddress>();

        public delegate void EventOnKNXMessage(KNXUartConnection caller, KNXmessage Message, UartEvents uartEvent);
        public event EventOnKNXMessage OnKNXMessage;
            
        public delegate void EventOnInvalidKNXMessage(KNXUartConnection caller, byte[] Message);
        public event EventOnInvalidKNXMessage OnInvalidKNXMessage;

        public delegate void EventOnUartEvent(KNXUartConnection caller, UartEvents uartEvent);
        public event EventOnUartEvent OnUartEvent;


        public string ComPort
        {
            get => _ComPort;
            set
            {
                if (!_port.SafeIsOpen)
                {
                    _ComPort = value;
                }
                else
                {
                    throw new Exception("Cannot modify port while connection is open.");
                }
            }
        }

        public KNXUartConnection() { }

        public KNXUartConnection(string comport)
        {
            _ComPort = comport;
        }

        public bool Connect()
        {
            try
            {
                _port = new SafeSerialPort(_ComPort, 19200, Parity.Even, 8, StopBits.One);
                if (!_port.SafeIsOpen)
                {
                    _port.ReadTimeout = 500;
                    _port.WriteTimeout = 500;
                    //_port.NewLine = "\r\n";
                    _port.Open();

                    //Using timer instead of events.
                    //Mono (Linux) does not support SerialPort on data event
                    tmr = new System.Timers.Timer();
                    tmr.Elapsed += Tmr_Elapsed;
                    tmr.Enabled = true;
                    tmr.Interval = 1;
                    dsState = DeviceState.Unkown;

                    //Console.WriteLine("OpenSucces: " + ComPort);
                    return true;
                }
                else
                {
                    //Console.WriteLine("OpenError" + ComPort);
                    return false;
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                return false;
            }
        }

        private void CheckState()
        {
            dsState = DeviceState.StatusRequested;
            _port.Write(new byte[] { 0x02 }, 0, 1);
        }

        public bool ResetAndInit()
        {
            if(_port==null)
            {
                Connect();
            }
            byte maxwait = 80;
            if (_port.SafeIsOpen)
            {
                Thread.Sleep(20);
                dsState = DeviceState.ResetRequested;
                while (dsState == DeviceState.ResetRequested)
                {
                    _port.Write(new byte[] { 0x01 }, 0, 1);
                    Thread.Sleep(100);
                    if (maxwait == 0)
                    {
                        /*_port.Write(new byte[] { 0x22, 0x00 }, 0, 2);
                        _port.Write(new byte[] { 0x1F, 0x01 }, 0, 2);
                        _port.Write(new byte[] { 0x1E, 0x01 }, 0, 2);
                        _port.Write(new byte[] { 0x22, 0x01 }, 0, 2);*/

                        return false;
                    }
                    maxwait--;
                }
                if (dsState == DeviceState.ResetAccepted)
                {
                    _port.Write(new byte[] { 0x22, 0x00 }, 0, 2);
                    Thread.Sleep(500);
                    _port.Write(new byte[] { 0x02 }, 0, 1);
                    uartDeviceOnline = true;
                    return true;
                }
                return true;
            }
            return false;
        }

        private void CheckForData()
        {
            if(!isReading)
            {
                if (_port.SafeIsOpen)
                {
                    if (_port.BytesToRead != 0)
                    {
                        isReading = true;
                        KNXmessage knx;
                        List<byte> rawknx;

                        readResult result = Read(out knx, out rawknx);

                        if (result == readResult.readSucces)
                        {
                            OnKNXMessage?.Invoke(this, knx, UartEvents.GotKNXPacket);
                            dsState = DeviceState.StatusOK;
                            uartDeviceOnline = true;
                        }
                        else if (result == readResult.readFailed)
                        {
                            //Console.WriteLine("[Failed]");
                            OnInvalidKNXMessage?.Invoke(this, rawknx.ToArray());
                        }
                        else
                        {
                            uartDeviceOnline = true;
                            //Handled in process
                        }
                    }
                }
                isReading = false;
            }
        }
        private void Tmr_Elapsed(object sender, ElapsedEventArgs e)
        {
            tmr.Enabled = false;
            try
            {
                if (_port == null)
                {
                    uartDeviceOnline = false;
                    Connect();

                }
                else if (!_port.SafeIsOpen)
                {
                    uartDeviceOnline = false;
                    if (!Connect())
                    {
                        tmr.Interval = 2000;
                    }
                }
                else
                {
                    if (CheckTstateCounter <= 0)
                    {
                        if(dsState == DeviceState.StatusRequested)
                        {
                            uartDeviceOnline = false;
                        }
                        dsState = DeviceState.StatusRequested;
                        CheckState();
                        CheckTstateCounter = 200;
                    }
                    else
                    {
                        CheckTstateCounter--;
                    }
                    CheckForData();

                    if(knxQueue.Count !=0)
                    {
                        this.WriteDirect(knxQueue.Dequeue(), false);
                    }
                }
            }
            finally
            {
                tmr.Enabled = true;
            }
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

        static private bool CheckChecksum(List<byte> frame, int checksum)
        {
            int cs = CalculateChecksum(frame);
            cs ^= checksum;
            return (cs == 0 ? true : false);
        }

        public void SendKNXMessage(KNXmessage knxMsg)
        {
            knxQueue.Enqueue(knxMsg);
        }        

        public bool WriteDirect(KNXmessage knxMsg, bool waitForAck)
        {
            return Write(knxMsg.toByteArray(), waitForAck);
        }

        public bool TinySerialLedOn
        {
            get
            {
                return ledState;
            }
            set
            {
                if (_port.SafeIsOpen && dsState != DeviceState.ResetAccepted)
                    throw new Exception("TinySerial 810 has not been initialized");
                byte[] dataOn = new byte[] { 0x2B };
                byte[] dataOff = new byte[] { 0x2A };
                ledState = value;
                if (value)
                    _port.Write(dataOn, 0, 1);
                else
                    _port.Write(dataOff, 0, 1);

            }
        }

        public void SendAck()
        {
            if (AllowWrite)
            {
                byte[] dataAck = new byte[] { 0x11 };
                _port.Write(dataAck, 0, 1);
            }
        }

        internal bool WriteRaw(byte[] data)
        {
            if (AllowWrite)
            {
                if (!_uartDeviceIsOnline)
                    throw new Exception("TinySerial 810 is offline");

                if (_port.SafeIsOpen)
                {
                    _port.Write(data, 0, data.Length);
                    return true;
                }
                return false;
            }
            else
                throw new Exception("Writing not enabled");
        }

        internal bool Write(byte[] tx_raw_frame, bool waitforAck = false, bool retry = false)
        {
            //printHex(tx_raw_frame.ToArray());
            byte byte_checksum = CalculateChecksum(new List<byte>(tx_raw_frame));

            if (AllowWrite)
            {
                byte maxwait = 150;
                if (!_uartDeviceIsOnline)
                    throw new Exception("TinySerial 810 is offline");

                if (_port.SafeIsOpen)
                {

                    if (tx_raw_frame.Length < 7) return false;

                    byte byte_prefix_start = 0x80;
                    foreach (byte byte_to_send in tx_raw_frame)
                    {
                        /*Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("0x{0:X2}, ", byte_prefix_start);
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write("0x{0:X2}, ", byte_to_send);*/
                        _port.Write(new byte[] { byte_prefix_start, byte_to_send }, 0, 2);
                        byte_prefix_start++;
                    }
                    byte byte_end = (byte)(0x40 + tx_raw_frame.Length);

                    _port.Write(new byte[] { byte_end, byte_checksum }, 0, 2);
                    /*Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("0x{0:X2}, ", byte_end);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("0x{0:X2}", byte_checksum);*/

                    tsState = TelegramState.TelegramSend;

                    if (waitforAck)
                    {
                        while (tsState == TelegramState.TelegramSend)
                        {
                            Thread.Sleep(5);
                            if (maxwait == 0)
                            {
                                return false;
                            }
                            maxwait--;
                        }
                        if (tsState == TelegramState.TelegramAck)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                if(tsState == TelegramState.TelegramFail)
                {
                    if(!retry)
                        return this.Write(tx_raw_frame, waitforAck, true);
                }
                return false;
            }
            else
                throw new Exception("Writing not enabled");
        }

        private bool SerialReadByte(out byte rx_byte, uint read_timeout)
        {
            if (_port.SafeIsOpen)
            {
                read_timeout++;
                while (read_timeout != 0)
                {
                    if (_port.BytesToRead != 0)
                    {
                        rx_byte = (byte)_port.ReadByte();
                        //Console.Write(" [0x{0:X2}] ", rx_byte);
                        return true;
                    }
                    else
                    {
                        read_timeout--;
                        Thread.Sleep(1);
                    }
                }
            }
            rx_byte = 0;
            return false;
        }

        private static bool isControlField(UartControlFieldValues ControlField, byte header)
        {
            if (header == (byte)ControlField)
            {
                return true;
            }
            return false;
        }

        private static bool isShortPacketType(byte header)
        {
            if ((header & (byte)UartControlFieldValues.MASK_PacketType) == (byte)UartControlFieldValues.MASK_PacketShort) //Standard packet 
            {
                return true;
            }
            return false;
        }

        private static bool isLongPacketType(byte header)
        {
            if ((header & (byte)UartControlFieldValues.MASK_PacketType) == (byte)UartControlFieldValues.MASK_PacketLong) //Long packet 
            {
                return true;
            }
            return false;
        }

        private readResult Read(out KNXmessage knx, out List<byte> rx_frame)
        {
            //3.2.3.2 Services from UART
            //The first character of each service

            knx = new KNXmessage();
            KNXAddress kNXAddress = new KNXAddress();
            uint read_timeout = 5;
            byte rx_byte;
            byte Lenght = 0;
            bool isLongFrameType = false;
            rx_frame = new List<byte>();


            //Console.WriteLine("Readingnewpacket:");

            // Get control field of UART service
            if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
            rx_frame.Add(rx_byte);

            if (dsState == DeviceState.ResetRequested)
            {
                //Check if reset accepted has been recieved
                if (isControlField(UartControlFieldValues.UART_Reset_ind, rx_byte))
                {
                    dsState = DeviceState.ResetAccepted;
                    return readResult.readHandledInProcess;
                }
                else
                {
                    dsState = DeviceState.ResetFailed;
                }
                return readResult.readFailed;
            }
            else if (isControlField(UartControlFieldValues.UART_State_ind_NoError, rx_byte))
            {
                dsState = DeviceState.StatusOK;
                //Alive                
                return readResult.readHandledInProcess;
            }
            else if (isControlField(UartControlFieldValues.UART_State_NACK, rx_byte))
            {
                //Nack packet.
                //Console.WriteLine("GotNack");
                return readResult.readGotNack;
            }
            else if (isControlField(UartControlFieldValues.UART_Reset_ind, rx_byte))
            {
                //Bus reset
                return readResult.readHandledInProcess;
            }
            else if (isControlField(UartControlFieldValues.L_Poll_Data_ind, rx_byte))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("TODO! L_Poll_Data_ind");
                Console.ForegroundColor = ConsoleColor.White;
                return readResult.readHandledInProcess;
            }

            else if (isControlField(UartControlFieldValues.L_DATA_conf_negative, rx_byte))
            {
                tsState = TelegramState.TelegramFail;
                return readResult.readGotConfNegative;
            }
            else if (isControlField(UartControlFieldValues.L_DATA_conf_positive, rx_byte))
            {
                tsState = TelegramState.TelegramAck;
                return readResult.readGotConfPositive;
            }
            else if (isLongPacketType(rx_byte)) //Long packet 
            {
                //Get the extended Control frame
                if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
                rx_frame.Add(rx_byte);

                isLongFrameType = true;
            }
            else if (isShortPacketType(rx_byte)) //Standard packet
            {
                isLongFrameType = false;
            }
            else
            {
                try
                {
                    Console.WriteLine("Unkown packet Control: 0x{0:X2}", rx_byte);
                    //knx.knxErrorcode = rx_byte;
                    while (SerialReadByte(out rx_byte, read_timeout) == true)
                    {
                        rx_frame.Add(rx_byte);
                    }
                    //printHex(rx_frame.ToArray());
                    //knx = KNXmessage.fromByteArray(rx_frame);                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to process unknown packet: " + e);
                    return readResult.readFailed;
                }
                return readResult.readFailed;
            }

            // source address
            if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
            rx_frame.Add(rx_byte);
            if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
            rx_frame.Add(rx_byte);

            // destination address
            if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
            kNXAddress.knxAddressHigh = rx_byte;
            rx_frame.Add(rx_byte);
            if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
            kNXAddress.knxAddressLow = rx_byte;
            rx_frame.Add(rx_byte);

            if (isLongFrameType)
            {
                //Lenght
                if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
                rx_frame.Add(rx_byte);
                Lenght = (byte)(rx_byte);

                //APCI
                if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
                rx_frame.Add(rx_byte);
            }
            else
            {
                // DAF, LSDU and length
                if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
                rx_frame.Add(rx_byte);
                Lenght = (byte)((rx_byte & 0x0F) + 1); //Last 4 bits are lenght
            }

            // payload
            for (int i = 0; i < Lenght; i++)
            {
                if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
                rx_frame.Add(rx_byte);
            }

            // checksum
            if (!SerialReadByte(out rx_byte, read_timeout)) return readResult.readFailed;
            //Console.WriteLine("DataLen: {0}, Checksum: {1}, Calculated {2}", Lenght, rx_byte, CalculateChecksum(rx_frame));

            if (!CheckChecksum(rx_frame, rx_byte))
            {
                return readResult.readFailed;
            }

            if (kNXAddressesToAck.Contains(kNXAddress))
            {
                SendAck();
            }

            knx = KNXmessage.fromByteArray(rx_frame, DateTime.Now);            

            return readResult.readSucces;
        }
    }
}
