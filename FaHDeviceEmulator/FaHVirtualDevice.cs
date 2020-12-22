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

    This modules can be used to emulate KNX FreeAtHome devices.    
*/
using FAHPayloadInterpeters;
using FAHPayloadInterpeters.FAHFunctionPropertyStateResponses;
using FreeAtHomeDevices;
using KNXBaseTypes;
using KNXUartModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace VirtualFahDevice
{
    public class FaHVirtualDevice
    {
        private FreeAtHomeDevices.FaHDevice thisDevice = new FreeAtHomeDevices.FaHDevice();
        private KNXUartConnection kNXUart;
        public bool ConsolePrintMessages = false;
        public bool ShowBusInfo = false;
        private string FahDeviceAddressName = "";
        private DateTime BootTime = DateTime.Now;
        System.Timers.Timer timer;
        bool AutoResetToOff = false;

        public string FahDeviceName
        {
            get
            {
                return FahDeviceAddressName;
            }
        }

        public FaHDevice DeviceConfig { get => thisDevice; }

        public delegate void EventOnActorChange(FaHVirtualDevice caller, FaHDeviceProperties.SensorActorInterfaceType SensorActor, UInt16 state);
        public event EventOnActorChange OnActorChange;

        public void ButtonClick(FaHDeviceProperties.SensorActorInterfaceType Button, byte Value)
        {
            thisDevice.ButtonClick(Button, Value);
        }

        public FaHVirtualDevice(KNXUartConnection kNXUart, string FaHDeviceAddress)
        {
            FahDeviceAddressName = FaHDeviceAddress;
            byte[] devAddr = KNXHelpers.HexStringToByteArray(FahDeviceAddressName);
            if (devAddr.Length != 6)
                throw new Exception("Invalid device name");
            if (devAddr[0] != 0xAB && devAddr[1] != 0xB7)
                throw new Exception("Device Should start with ABB7");

            this.kNXUart = kNXUart;
            this.kNXUart.OnKNXMessage += KNXUart_OnKNXMessage;
        }

        public void UpdateStatistics()
        {
            UpdateStatistics(false);
        }

        private void UpdateStatistics(bool InitalUpdate)
        {
            try
            {
                if (InitalUpdate)
                {
                    this.DeviceConfig.DeviceHealthStatus.DeviceReboots += 1;
                    this.DeviceConfig.Serialize(this.DeviceConfig.AutosaveFilename);
                    BootTime = DateTime.Now;
                }
                else
                {
                    TimeSpan t = DateTime.Now - BootTime;
                    uint timelen = (uint)t.TotalMinutes;
                    this.DeviceConfig.DeviceHealthStatus.Uptime += timelen;
                    BootTime = DateTime.Now;
                    this.DeviceConfig.Serialize(this.DeviceConfig.AutosaveFilename);
                }
            }
            catch { }
        }

        public void StartFaHDevice()
        {
            try
            {
                thisDevice = FaHDevice.DeserializeFromFile(FahDeviceAddressName + "-autosave.json", false);
                thisDevice.AutosaveFilename = FahDeviceAddressName + "-autosave.json";
            }
            catch
            {
                //Make sure to copy a template into the folder of the executable
                //For example copy Switch 2_2-v2.1506.json to inputdevice.json
                thisDevice = FaHDevice.DeserializeFromFile("inputdevice.json", false);
                thisDevice.AutosaveFilename = FahDeviceAddressName + "-autosave.json";
                thisDevice.FaHAddress.byteValue = KNXHelpers.HexStringToByteArray(FahDeviceAddressName);
            }            

            //Ensure KNX Ack's are send
            this.kNXUart.kNXAddressesToAck.Add(thisDevice.KnxAddress);

            //Update list, its cached.
            KNXAddress[] k = thisDevice.GroupValueAdresses;

            UpdateStatistics(true);

            timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed; 

            /*
             * TODO:
             *  - Device Reset
             *  - Group Value Read
            */

            thisDevice.OnGroupWriteEvent += thisDevice_OnGroupWriteEvent;
            thisDevice.OnGroupWriteSceneEvent += thisDevice_OnGroupWriteSceneEvent;
            thisDevice.OnActorChange += thisDevice_OnActorChange;

            kNXUart.SendKNXMessage(FAHDeviceDescriptorResponse.CreateResponse(thisDevice, new KNXAddress(0)));
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void thisDevice_OnGroupWriteSceneEvent(FaHDevice caller, KNXAddress TargetGroupValue, byte[] data)
        {
            KNXmessage GroupWriteMessage = FAHGroupValueWrite.CreateFAHGroupValueWrite(caller, TargetGroupValue, data, false);
            if (ConsolePrintMessages)
            {
                Console.Write(string.Format("Sending: {0}; {1} ", GroupWriteMessage.Timestamp.ToString(KNXHelpers.DateTimeFormat), GroupWriteMessage.HeaderAsString));
                GroupWriteMessage.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
            }
            kNXUart.SendKNXMessage(GroupWriteMessage);
        }

        private void thisDevice_OnActorChange(FaHDevice caller, FaHDeviceProperties.SensorActorInterfaceType SensorActor, ushort state)
        {
            if(AutoResetToOff && state == 1)
            {
                this.timer.Enabled = true;
                Console.WriteLine("Todo: Impletent Auto Reset");
            }
            this.OnActorChange?.Invoke(this, SensorActor, state);
        }

        private void thisDevice_OnGroupWriteEvent(FaHDevice caller, KNXAddress TargetGroupValue, byte[] data)
        {
            KNXmessage GroupWriteMessage = FAHGroupValueWrite.CreateFAHGroupValueWrite(caller, TargetGroupValue, data);
            if (ConsolePrintMessages)
            {
                Console.Write(string.Format("Sending: {0}; {1} ", GroupWriteMessage.Timestamp.ToString(KNXHelpers.DateTimeFormat), GroupWriteMessage.HeaderAsString));
                GroupWriteMessage.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
            }
            kNXUart.SendKNXMessage(GroupWriteMessage);

        }

        private void KNXUart_OnKNXMessage(KNXUartConnection caller, KNXBaseTypes.KNXmessage Message, KNXUartConnection.UartEvents uartEvent)
        {
            if (Message.ControlField.RepeatFrame)
                return;

            KNXmessage.MessageDirectedType msgDirectedType = Message.CheckIsMessageIntendedForMe(thisDevice.KnxAddress, thisDevice.GroupValueAdresses);
            if (msgDirectedType == KNXmessage.MessageDirectedType.NotAddressedToDevice)
            {
                //Message not for this device
                if (ShowBusInfo && ConsolePrintMessages )
                {
                    if (Message.SourceAddress == thisDevice.KnxAddress)
                    {
                        return;
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    string retM = string.Format("Bus: {0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                    Console.Write(retM);
                    Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
                    Console.ForegroundColor = ConsoleColor.White;
                    return;
                }
                else
                {
                    return;
                }
            }

            /*Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine("MsgGot: " + Message.ToHexString()); Console.ForegroundColor = ConsoleColor.White; */

            //Message is probably for this device, decode to examen and execute upon
            FahPayloadInterpeter.TryToInterpret(ref Message);
            KNXmessage knxMsgToRespond = null;

            switch (msgDirectedType)
            {
                //Message is broadcast to all devices
                case KNXmessage.MessageDirectedType.Broadcast:
                    switch (Message.Payload.Apdu.apduType)
                    {
                        case KNXAdpu.ApduType.DeviceDescriptorRead:
                            //waitForDiscoveryResponseSendCounter = (byte)new Random().Next();
                            KNXAddress DiscoverFromAddrRequested = Message.SourceAddress;
                            if (ConsolePrintMessages) Console.WriteLine("Sending Discovery Response");
                            KNXmessage kD = FAHDeviceDescriptorResponse.CreateResponse(thisDevice, DiscoverFromAddrRequested);
                            kNXUart.SendKNXMessage(kD);
                            UpdateStatistics();
                            break;

                        case KNXAdpu.ApduType.IndividualAddressSerialNumberWrite:
                            //New serial number write (set address)
                            var FaHIndAddrSerialNumberWrite = Message.Payload.ReadablePayloadPacket as FAHIndividualAddressSerialNumberWrite;
                            //Check if it is for this device by matching the FaHDeviceAddress "ABB...."
                            if (FaHIndAddrSerialNumberWrite.FaHDeviceAddress == thisDevice.FaHAddress)
                            {
                                bool moreIndices;
                                FaHIndAddrSerialNumberWrite.SaveToDevice(ref thisDevice, out moreIndices);
                                caller.kNXAddressesToAck.Clear();
                                caller.kNXAddressesToAck.Add(thisDevice.KnxAddress);
                                //Only Ack, no response

                                if (ConsolePrintMessages)
                                {
                                    string ret2 = string.Format("Recieve: {0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                                    Console.Write(ret2);
                                    Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
                                }
                            }
                            return;

                        default:
                            break;
                    }
                    break;

                //Message only for me, send to KNX address
                case KNXmessage.MessageDirectedType.IndividualAdressed:                    

                    switch (Message.Payload.Apdu.apduType)
                    {
                        case KNXAdpu.ApduType.DeviceDescriptorRead:
                            knxMsgToRespond = FAHDeviceDescriptorResponse.CreateResponse(thisDevice, Message.SourceAddress);
                            break;

                        case KNXAdpu.ApduType.FunctionPropertyCommand:
                            var fpc = Message.Payload.ReadablePayloadPacket as FAHFunctionPropertyCommand;
                            knxMsgToRespond = fpc.ProcessAndCreateResponse(thisDevice);
                            if(knxMsgToRespond==null)
                            {
                                fpc.CreateCommandNotSupportedMessage();
                                if (ConsolePrintMessages) Console.WriteLine("Not Send!");
                            }    
                            break;

                        case KNXAdpu.ApduType.Restart:
                            FAHRestart fAHRestart = new FAHRestart(Message.Payload);
                            knxMsgToRespond = fAHRestart.ProcessRebootPackage(thisDevice, Message.SourceAddress);
                            if (knxMsgToRespond == null)
                            {
                                UpdateStatistics(true);
                                knxMsgToRespond = FAHDeviceDescriptorResponse.CreateResponse(thisDevice, Message.SourceAddress);                                
                            }
                            break;

                        default:
                            Console.WriteLine("Not handled! " + Message.Payload.Apdu.apduType.ToString());
                            break;
                    }
                    break;

                //Message to one of the GroupValue addresses on this device
                case KNXmessage.MessageDirectedType.GroupValueAdressed:

                    switch (Message.Payload.Apdu.apduType)
                    {
                        case KNXAdpu.ApduType.GroupValueWrite:
                            caller.SendAck();
                            FAHGroupValueWrite fAHGroupValueWrite = new FAHGroupValueWrite(Message.Payload);
                            thisDevice.ProccessGroupWrite(Message.TargetAddress, fAHGroupValueWrite.MessageData);
                            break;
                        /*case KNXAdpu.ApduType.GroupValueRead:
                            FAHGroupValueRead fAHGroupValueRead = new FAHGroupValueRead(Message.Payload);
                            Console.Write("TodoGroupValueRead: {0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                            Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
                            break;
                        case KNXAdpu.ApduType.GroupValueResponse:
                            FAHGroupValueResponse fAHGroupValueReponse = new FAHGroupValueResponse(Message.Payload);
                            Console.Write("TodoGroupValueResponse: {0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                            Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
                            break;
                        */
                        default:
                            Console.WriteLine("Not handled! " + Message.Payload.Apdu.apduType.ToString());
                            break;
                    }
                    break;

                default:
                    Console.WriteLine("Message not accounted");
                    break;
            }

            if (ConsolePrintMessages)
            {
                Console.Write("Recieve: {0}; {1} ", Message.Timestamp.ToString(KNXHelpers.DateTimeFormat), Message.HeaderAsString);
                Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
            }

            if(knxMsgToRespond != null)
            {
                if (ConsolePrintMessages)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write("Send: {0}; {1} ", knxMsgToRespond.Timestamp.ToString(KNXHelpers.DateTimeFormat), knxMsgToRespond.HeaderAsString);
                    knxMsgToRespond.Payload.ReadablePayloadPacket.PrintUnaccountedBytes(false);
                    Console.WriteLine("MsgToSend: " + knxMsgToRespond.ToHexString());
                    Console.WriteLine("-------------------------------------------------");
                    Console.ForegroundColor = ConsoleColor.White;
                }

                kNXUart.SendKNXMessage(knxMsgToRespond);
            }
        }
    }
}
