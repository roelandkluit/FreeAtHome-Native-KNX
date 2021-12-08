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

    This modules can be used to monitor KNX FreeAtHome messages.    
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FAHPayloadInterpeters;
using FreeAtHomeDevices;
using FreeAtHomeKNX.Properties;
using KNXBaseTypes;
using KNXNetworkLayer;
using KNXUartModule;
using static FAHPayloadInterpeters.FAHFunctionPropertyCommand;
using FaHTCPClient;

namespace FreeAtHomeKNX
{
    class FreeAtHomeMonitor
    {    
        static ReadWriteKNXDataLog stdOut;
        //static ReadWriteKNXDataLog stdIn;

        //static FreeAtHomeDevices.FaHDevice fahDevToReturn = new FreeAtHomeDevices.FaHDevice();
        //static FreeAtHomeDevices.FaHDevice fahDevToLearn = new FreeAtHomeDevices.FaHDevice();

        static KNXmessage LastCreatedMsg = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);

        static public PropertyControlTypes lastRequestedPropertyControl = PropertyControlTypes.NotSet;
        static public byte[] ByteDataParm = null;

        static void Main(string[] args)
        {
            //Can be used to replay log messages to an existing device
            //fahDevToReturn = FaHDevice.DeserializeFromFile(@"ABB700C730A9-learn-metvloeren.json", true);
            //fahDevToLearn = FaHDevice.DeserializeFromFile(@"ABB700C730A9-learn-metvloeren.json", true);

            Console.SetWindowSize(Console.WindowWidth * 2, Console.WindowHeight);
            stdOut = new ReadWriteKNXDataLog("Output_" + DateTime.Now.Ticks + ".txt", true);
            //stdIn = new ReadWriteKNXDataLog(@"replay_log.txt", false);

            FaHTCPClient.TCPknxClient kNXUart = new FaHTCPClient.TCPknxClient("172.16.16.20", 9998);
            /*KNXUartModule.KNXUartConnection kNXUart = new KNXUartConnection(AppSettings.Default.ComPort)
            {
                AllowWrite = AppSettings.Default.KNXWriteEnabled
            };*/

            kNXUart.OnKNXMessage += KNXUart_OnKNXMessage;
            kNXUart.OnKNXEvent += KNXUart_OnKNXEvent;
            //kNXUart.ResetAndInit();

            /*
            int i = 0;
            while (true)
            {
                KNXmessage k = stdIn.ReadNextMessage();
                if (k == null) break;
                if (i > 0)
                    KNXUart_OnKNXMessage(null, k, KNXUartConnection.UartEvents.GotKNXPacket);

                i++;
            }*/

            while (true)
            {
                string ret = Console.ReadLine();
                if (ret.ToLower() == "exit")
                {
                    Console.WriteLine("Exit Accepted");
                    return;
                }

                if (ret.ToLower() == "x")
                {
                    //[0x00 - 0x01]     [0x14 - 0xC8]      0x00 0x80 0x66
                    //[0xEB - 0x01]     [0xB5 - 0x50]      0x00[0x80 | 0x09]

                    //[0x00 - 0x01]     [0x14 - 0xC8]      0x00 0x80 0x59
                    //[0x00 - 0x01]     [0x3D - 0x26]      0x00 0x80 0x45 0x1E 0xD1 0x58

                    FaHDevice nulldev = new FaHDevice();
                    nulldev.KnxAddress.u16valueHigh = 1;
                    KNXAddress TargetGroupValue = new KNXAddress(0x14, 0xC8);
                    byte[] data = new byte[] { 0x66 };

                    KNXmessage GroupWriteMessage = FAHGroupValueWrite.CreateFAHGroupValueWrite(nulldev, TargetGroupValue, data, false);
                    KNXUart_OnKNXMessage(null, GroupWriteMessage, KNXNetworkLayerTemplate.KnxPacketEvents.GotKNXPacket);

                    kNXUart.SendKNXMessage(GroupWriteMessage);

                    //*************************************************************
                    KNXAddress TargetGroupValue1 = new KNXAddress(0x3D, 0x26);
                    byte[] data1 = new byte[] { 0x45, 0x1E, 0xD1, 0x66 };

                    KNXmessage GroupWriteMessage1 = FAHGroupValueWrite.CreateFAHGroupValueWrite(nulldev, TargetGroupValue1, data1, true);
                    KNXUart_OnKNXMessage(null, GroupWriteMessage1, KNXNetworkLayerTemplate.KnxPacketEvents.GotKNXPacket);

                    kNXUart.SendKNXMessage(GroupWriteMessage1);

                    //**********************************************************************//



                }


                if (ret.ToLower()=="+")
                {
                    FaHDevice nulldev = new FaHDevice();
                    KNXAddress TargetGroupValue = new KNXAddress(0xC6, 0x4D);
                    byte[] data = new byte[] { 0x01 };

                    KNXmessage GroupWriteMessage = FAHGroupValueWrite.CreateFAHGroupValueWrite(nulldev, TargetGroupValue, data, true);
                    KNXUart_OnKNXMessage(null, GroupWriteMessage, KNXNetworkLayerTemplate.KnxPacketEvents.GotKNXPacket);

                    kNXUart.SendKNXMessage(GroupWriteMessage);


                    //2021 - 10 - 03 11:08:33.826; GroupValueWrite KNX_PRIORITY_HIGH[NoExtdFrame]   [0x00 - 0x01]     [0x3D - 0x26]      0x00 0x80 0x06 0xB0 0xFF 0x57
                }
                if (ret.ToLower() == "-")
                {
                    FaHDevice nulldev = new FaHDevice();
                    KNXAddress TargetGroupValue = new KNXAddress(0xC6, 0x4D);
                    byte[] data = new byte[] { 0x00 };

                    KNXmessage GroupWriteMessage = FAHGroupValueWrite.CreateFAHGroupValueWrite(nulldev, TargetGroupValue, data, true);
                    KNXUart_OnKNXMessage(null, GroupWriteMessage, KNXNetworkLayerTemplate.KnxPacketEvents.GotKNXPacket);


                    //[0xC6 - 0x4D]      0x00[0x80 | 0x01]

                    kNXUart.SendKNXMessage(GroupWriteMessage);


                    //2021 - 10 - 03 11:08:33.826; GroupValueWrite KNX_PRIORITY_HIGH[NoExtdFrame]   [0x00 - 0x01]     [0x3D - 0x26]      0x00 0x80 0x06 0xB0 0xFF 0x57
                }


                Console.WriteLine("# " + ret);
                stdOut.WriteComment(ret);
            }
        }

        private static void KNXUart_OnKNXEvent(KNXNetworkLayerTemplate caller, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent)
        {
            Console.WriteLine("[" + uartEvent + "]");
        }

        private static void KNXUart_OnKNXMessage(KNXNetworkLayerTemplate caller, KNXBaseTypes.KNXmessage Message, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent)
        {
            stdOut.WriteOut(Message);
            try
            {
                FahPayloadInterpeter.TryToInterpret(ref Message);

                if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.FunctionPropertyStateResponse)
                {
                    Message.Payload.ReadablePayloadPacket = ((FAHFunctionPropertyStateResponse)Message.Payload.ReadablePayloadPacket).ProcessPayload(lastRequestedPropertyControl, ByteDataParm);

                    if (((FAHFunctionPropertyStateResponse)Message.Payload.ReadablePayloadPacket).resultCode != KNXHelpers.knxPropertyReturnValues.CommandNotSupported)
                    {
                        /*
                        bool more;
                        if (!((FAHFunctionPropertyStateResponse)Message.Payload.ReadablePayloadPacket).SaveToDevice(ref fahDevToLearn, out more))
                        {
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.Write("Not saved: ");
                            Console.BackgroundColor = ConsoleColor.Black;
                        }*/
                    }
                }
                else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.FunctionPropertyCommand)
                {
                    ((FAHFunctionPropertyCommand)Message.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                }

                string ret = string.Format("{0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                Console.Write(ret);
                Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);

                switch (Message.Payload.Apdu.apduType)
                {
                    case KNXAdpu.ApduType.IndividualAddressSerialNumberWrite:
                        var fasnw = Message.Payload.ReadablePayloadPacket as FAHIndividualAddressSerialNumberWrite;
                        /*
                        bool more;
                        fasnw.SaveToDevice(ref fahDevToReturn, out more);
                        */
                        //Succes
                        return;

                    case KNXAdpu.ApduType.DeviceDescriptorRead:
                        //LastCreatedMsg = FAHDeviceDescriptorResponse.CreateResponse(fahDevToReturn, Message.SourceAddress);
                        return;

                    case KNXAdpu.ApduType.GroupValueWrite:
                        return;

                    case KNXAdpu.ApduType.FunctionPropertyCommand:
                        var fpc = Message.Payload.ReadablePayloadPacket as FAHFunctionPropertyCommand;
                        /*KNXmessage k = fpc.ProcessAndCreateResponse(fahDevToReturn);
                        if (k != null)
                        {
                            LastCreatedMsg = k;
                        }*/
                        break;

                    case KNXAdpu.ApduType.DeviceDescriptorResponse:
                        FAHDeviceDescriptorResponse fAHDeviceDescriptorResponse = Message.Payload.ReadablePayloadPacket as FAHDeviceDescriptorResponse;

                        /*bool mi;
                        fAHDeviceDescriptorResponse.SaveToDevice(ref fahDevToLearn, out mi);

                        var z = fAHDeviceDescriptorResponse.FahDeviceAddress;
                        if (fAHDeviceDescriptorResponse.FahDeviceAddress == fahDevToReturn.FaHAddress)
                        {
                            if (LastCreatedMsg.ToHexString() != Message.ToHexString())
                            {
                                Console.BackgroundColor = ConsoleColor.Blue;
                                Console.WriteLine("Gen: {0}", LastCreatedMsg.ToHexString());
                                Console.BackgroundColor = ConsoleColor.Red;
                                Console.WriteLine("Err: {0}", Message.ToHexString());
                            }
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.WriteLine("------------------------------------------------------------------------------------------------------------------------------------");
                            LastCreatedMsg = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);

                        }
                        */
                        break;

                    default:
                        /*if (LastCreatedMsg.ToHexString() != Message.ToHexString())
                        {
                            Console.BackgroundColor = ConsoleColor.Blue;
                            Console.WriteLine("Gen: {0}", LastCreatedMsg.ToHexString());
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.WriteLine("Err: {0}", Message.ToHexString());
                        }*/
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------------------");
                        LastCreatedMsg = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketShort);
                        break;
                }
            }
            catch(Exception e)
            {
                Console.Write("Error parsing: " + e);
            }
        }
    }
}
