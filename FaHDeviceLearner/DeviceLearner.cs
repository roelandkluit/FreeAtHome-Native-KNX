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

    This modules allows you to 'read' a existing free at home device and save it to a json file.
    
    Best to learn devices that are erased (Select reboot, delete external, and disconnect device from bus on reboot.)

    I used a dedicated connection to learn with:
     - Separate Bus Power (6201)
     - A TinySerial-810
     - The device to learn

*/
using FAHPayloadInterpeters;
using FAHPayloadInterpeters.FAHFunctionPropertyStateResponses;
using FreeAtHomeDeviceLearner.Properties;
using FreeAtHomeDevices;
using KNXBaseTypes;
using KNXNetworkLayer;
using KNXUartModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FAHPayloadInterpeters.FAHFunctionPropertyCommand;

namespace FreeAtHomeDeviceLearner
{
    class DeviceLearner
    {
        public enum DeviceLearningState
        {
            None = 0,
            deviceDiscovery,
            deviceDiscoveryResponse,
            deviceReadSettings,
            //deviceSetSerialResponse,
        }
        static FaHDevice deviceToLearn = new FaHDevice();
        static FaHDevice SysApEmulator;
        static FahSystemID SystemID = new FahSystemID(0xFF, 0xFF);

        static DeviceLearningState devLearnState = DeviceLearningState.None;

        static public PropertyControlTypes lastRequestedPropertyControl = PropertyControlTypes.NotSet;
        static public byte[] ByteDataParm = null;
        static KNXmessage knxMsgtoProcess = null;
        static KNXmessage knxLastMsgProcessed = null;

        static void Main(string[] args)
        {
            Console.SetWindowSize(Console.WindowWidth * 2, Console.WindowHeight);

            KNXUartConnection kNXUart = new KNXUartConnection(AppSettings.Default.ComPort)
            {
                AllowWrite = true
            };
            
            kNXUart.OnKNXMessage += KNXUart_OnKNXMessage;
            kNXUart.ResetAndInit();
            kNXUart.AddKNXAddressToAck(new KNXAddress(0x00, 0x02));

            SysApEmulator = new FreeAtHomeDevices.FaHDevice
            {
                FaHAddress = FaHDeviceAddress.FromByteArray(new byte[] { 0xAB, 0xB7, 0x00, 0xD2, 0x32, 0x48 }, 0),
            };
            SysApEmulator.SetAddressInformation(new KNXAddress(0x00, 0x02), SystemID);

            if (!DiscoverDevice(ref kNXUart))
            {
                Console.WriteLine("Cannot find a device");
                return;
            }
            LearnDevice(ref kNXUart);           

            Console.ReadLine();

        }

        static bool DiscoverDevice(ref KNXUartConnection kNXUart)
        {
            if (deviceToLearn.KnxAddress.knxAddress == 0 || deviceToLearn.ChannelCount == 0)
            {
                ConsoleWriteHeader("Waiting for Device Discovery Response ");
                //*----------------------------------------------------------------------------
                devLearnState = DeviceLearningState.deviceDiscovery;
                KNXmessage knxDeviceDescriptorRead = FAHDeviceDescriptorRead.CreateFAHDeviceDescriptorRead();
                Console.Write(string.Format("{0}; {1} ", knxDeviceDescriptorRead.Timestamp.ToString(KNXHelpers.DateTimeFormat), knxDeviceDescriptorRead.HeaderAsString));
                knxDeviceDescriptorRead.Payload.ReadablePayloadPacket.PrintUnaccountedBytes();
                kNXUart.WriteDirect(knxDeviceDescriptorRead, false);

                while (devLearnState != DeviceLearningState.deviceDiscoveryResponse)
                {
                    Console.Write('.');
                    Thread.Sleep(1000);
                }
                Console.WriteLine("OK: " + deviceToLearn.FaHAddress);
                /*
                if (deviceToLearn.SystemID == new FahSystemID(0xFF, 0xFF) || deviceToLearn.KnxAddress == new KNXAddress(0xFF, 0xFF))
                {
                    deviceToLearn.SystemID = SystemID;
                    deviceToLearn.KnxAddress = new KNXAddress(0x6F, 0x01);
                }*/
                //deviceToLearn.SetAddressInformation(new KNXAddress(0xFF, 0xFF), SystemID);
            }
            else
            {
                Console.WriteLine("Device ID known: {0}-->{1}", deviceToLearn.FaHAddress, deviceToLearn.KnxAddress);
            }

            //*----------------------------------------------------------------------------

            //if (deviceToLearn.SystemID == new FahSystemID(0xFF, 0xFF) || deviceToLearn.KnxAddress == new KNXAddress(0xFF, 0xFF))
            {
                ConsoleWriteHeader("IndividualAddressSerialNumberWrite");
                KNXmessage knxSetSerialNumber = FAHIndividualAddressSerialNumberWrite.CreateFAHIndividualAddressSerialNumberWrite(deviceToLearn);
                if (!kNXUart.WriteDirect(knxSetSerialNumber, true))
                {
                    return false;
                }

                Console.WriteLine("OK: " + deviceToLearn.KnxAddress);
            }
            /*else
            {
                writehead("Keeping Address: " + deviceToLearn.KnxAddress);
            }*/
            devLearnState = DeviceLearningState.deviceReadSettings;
            return true;
        }

        static void LearnDevice(ref KNXUartConnection kNXUart)
        {
            byte counter = 0;
            bool moreIndices;            

            for (byte i = 1; i <= 10; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadCons");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, 7, 2, new byte[] { i, 1 });
                ConsoleWriteOnEmptyLine(string.Format("Sending {0} ", ReadCons.HeaderAsString));
                ReadCons.Payload.ReadablePayloadPacket.PrintUnaccountedBytes();

                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    ConsoleWriteLine("Invalid data");
            }

            for (byte i = 1; i <= 10; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadCons");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, 8, 3, new byte[] { i, 1 });
                ConsoleWriteOnEmptyLine(string.Format("Sending {0} ", ReadCons.HeaderAsString));
                ReadCons.Payload.ReadablePayloadPacket.PrintUnaccountedBytes();

                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    ConsoleWriteLine("Invalid data");
            }

            for (byte i = 1; i <= 10; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadCons");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, 8, 2, new byte[] { i, 1 });
                ConsoleWriteOnEmptyLine(string.Format("Sending {0} ", ReadCons.HeaderAsString));
                ReadCons.Payload.ReadablePayloadPacket.PrintUnaccountedBytes();

                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    ConsoleWriteLine("Invalid data");
            }

            for (byte i = 1; i <= deviceToLearn.ChannelCount; i++)
            {

                for (byte propID = 2; propID <= 3; propID++)
                {
                    moreIndices = true;
                    counter = 1;
                    while (moreIndices)
                    {
                        //*----------------------------------------------------------------------------
                        ConsoleWriteHeader("ReadCons ch:" + i + ":" + propID + " id:" + counter);
                        KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, i, propID, new byte[] { counter++, 0x01 });
                        ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                        if (!kNXUart.WriteDirect(ReadCons, true))
                            return;
                        else
                            if (!ProcessResponse(out moreIndices))
                            ConsoleWriteLine("Invalid data");
                    }
                }
            }

            for (byte i = 1; i <= 10; i++)
            {
                moreIndices = true;
                byte ix = 1;
                while (moreIndices)
                {
                    //*----------------------------------------------------------------------------
                    ConsoleWriteHeader("ReadBasicInfo");
                    KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, i, 5, new byte[] { ix++ });
                    ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                    if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                        return;
                    else
                        if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
                }
            }

            for (byte i = 1; i <= deviceToLearn.ChannelCount; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadCons");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, i, 1, null);
                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    ConsoleWriteLine("Invalid data");
            }

            for (byte i = 1; i <= 10; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadCons");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, 7, i, null);
                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    ConsoleWriteLine("Invalid data");
            }

            for (byte i = 0; i <= 10; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadCons");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, 8, i, null);
                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    ConsoleWriteLine("Invalid data");
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDevHealth");
                KNXmessage ReadDevHealth = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDevHealth, 0, 1, null);
                ((FAHFunctionPropertyCommand)ReadDevHealth.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDevHealth, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    return;
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc");
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, 1, 5, null);
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    ConsoleWriteLine("Invalid data");
            }

            for (byte i = 0; i <= deviceToLearn.ChannelCount + 5; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadBasicInfo Chn: " + i);
                KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, i, 1, null);
                ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    Console.WriteLine("Failed");
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc");
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, 0, 4, new byte[] { 0x01 });
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        return;
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadCons-ShouldFail");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, 0, 4, new byte[] { 0x01 });
                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (ProcessResponse(out moreIndices))
                        return;
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadBasicInfo-ShouldFail");
                KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, 0, 7, new byte[] { 0x01 });
                ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                    return;
                else
                    if (ProcessResponse(out moreIndices))
                        return;

            }

            for (byte i = 1; i <= deviceToLearn.ChannelCount; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadBasicInfo " + i);
                KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, i, 1, null);
                ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc");
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, 7, 5, null);
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
                //return;
            }

            for (byte i = 0; i < 10; i++)
            {
                moreIndices = true;
                counter = 1;
                while (moreIndices)
                {

                    //*----------------------------------------------------------------------------
                    ConsoleWriteHeader("ReadFunctList");
                    KNXmessage ReadFunctList = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadFuncList, i, 1, new byte[] { counter++ });
                    ((FAHFunctionPropertyCommand)ReadFunctList.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                    if (!kNXUart.WriteDirect(ReadFunctList, true))
                        return;
                    else
                        if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
                }
            }

            moreIndices = true;
            counter = 1;
            while (moreIndices)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc Ndice:"+ counter);
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, 7, 4, new byte[] { (byte)counter++ });
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadCons-ShouldFail");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, 7, 4, new byte[] { 0x01 });
                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (ProcessResponse(out moreIndices))
                    return;
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadBasicInfo-ShouldFail");
                KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, 7, 7, new byte[] { 0x01 });
                ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                    return;
                else
                    if (ProcessResponse(out moreIndices))
                    return;
            }

            moreIndices = true;
            counter = 1;
            while (moreIndices)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc OID:" + counter);
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, 7, 3, new byte[] { (byte)counter++ });
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            moreIndices = true;
            counter = 1;
            while (moreIndices)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc OID:" + counter);
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, 7, 2, new byte[] { (byte)counter++ });
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadBasicInfo");
                KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, 0, 4, new byte[] { 0x01 });
                ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                    return;
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc");
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, 0, 1, null);
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data"); 
            }

            for (byte i = 0; i <= deviceToLearn.ChannelCount + 2; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadFlr_RmNr");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadFlr_RmNr, i, 1, null);
                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            for (byte i = 1; i <= deviceToLearn.ChannelCount + 2; i++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadIconId");
                KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadIconId, i, 1, null);
                ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadCons, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadBasicInfo");
                KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, 0, 16, new byte[] { 0x00, 0x00 });
                ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc");
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, 1, 1, null);
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            if (true)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("__UnsupportedCommand__0x07");
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.__UnsupportedCommand__0x07, 1, 1, new byte[] { 0x07 });
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (ProcessResponse(out moreIndices))
                    return;
            }

            for (byte i = 1; i <= deviceToLearn.ChannelCount; i++)
            {

                for (byte propID = 2; propID <= 3; propID++)
                {
                    moreIndices = true;
                    counter = 1;
                    while (moreIndices)
                    {
                        //*----------------------------------------------------------------------------
                        ConsoleWriteHeader("ReadCons ch:" + i + ":" + propID + " id:" + counter);
                        KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, i, propID, new byte[] { counter++, 0x01 });
                        ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                        if (!kNXUart.WriteDirect(ReadCons, true))
                            return;
                        else
                            if (!ProcessResponse(out moreIndices))
                            ConsoleWriteLine("Invalid data");
                    }
                }
            }

            if(true)
            {
                byte i = 7;
                for (byte propID = 2; propID <= 3; propID++)
                {
                    moreIndices = true;
                    counter = 1;
                    while (moreIndices)
                    {
                        //*----------------------------------------------------------------------------
                        ConsoleWriteHeader("ReadCons ch:" + i + ":" + propID + " id:" + counter);
                        KNXmessage ReadCons = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadConns, i, propID, new byte[] { counter++, 0x01 });
                        ((FAHFunctionPropertyCommand)ReadCons.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                        if (!kNXUart.WriteDirect(ReadCons, true))
                            return;
                        else
                            if (!ProcessResponse(out moreIndices))
                            ConsoleWriteLine("Invalid data");
                    }
                }
            }

            for (byte channelID = 1; channelID <= deviceToLearn.ChannelCount; channelID++)
            {
                ConsoleWriteHeader("ReadBasicInfo channel:" + channelID);
                for (byte propID = 3; propID <= 5; propID++)
                {
                    moreIndices = true;
                    counter = 1;
                    while (moreIndices)
                    {
                        //*----------------------------------------------------------------------------
                        ConsoleWriteHeader("ReadBasicInfo ch:" + channelID + ":" + propID + " id:" + counter);
                        KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, channelID, propID, new byte[] { counter++ });
                        ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                        if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                            return;
                        else
                            if (!ProcessResponse(out moreIndices))
                                ConsoleWriteLine("Invalid data");
                    }
                }
            }

            if(true)
            {
                byte channelID = 7;
                ConsoleWriteHeader("ReadBasicInfo channel:" + channelID);
                for (byte propID = 3; propID <= 5; propID++)
                {
                    moreIndices = true;
                    counter = 1;
                    while (moreIndices)
                    {
                        //*----------------------------------------------------------------------------
                        ConsoleWriteHeader("ReadBasicInfo ch:" + channelID + ":" + propID + " id:" + counter);
                        KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, channelID, propID, new byte[] { counter++ });
                        ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                        if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                            return;
                        else
                            if (!ProcessResponse(out moreIndices))
                            ConsoleWriteLine("Invalid data");
                    }
                }
            }

            if (true)
            {
                byte channelID = 8;
                ConsoleWriteHeader("ReadBasicInfo channel:" + channelID);
                for (byte propID = 3; propID <= 5; propID++)
                {
                    moreIndices = true;
                    counter = 1;
                    while (moreIndices)
                    {
                        //*----------------------------------------------------------------------------
                        ConsoleWriteHeader("ReadBasicInfo ch:" + channelID + ":" + propID + " id:" + counter);
                        KNXmessage ReadBasicInfo = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo, channelID, propID, new byte[] { counter++ });
                        ((FAHFunctionPropertyCommand)ReadBasicInfo.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                        if (!kNXUart.WriteDirect(ReadBasicInfo, true))
                            return;
                        else
                            if (!ProcessResponse(out moreIndices))
                            ConsoleWriteLine("Invalid data");
                    }
                }
            }

            for (byte channelID = 1; channelID <= deviceToLearn.ChannelCount + 2; channelID++)
            {
                //*----------------------------------------------------------------------------
                ConsoleWriteHeader("ReadDesc:" + channelID);
                KNXmessage ReadDesc = FAHFunctionPropertyCommand.CreateFAHFunctionPropertyCommand(deviceToLearn, FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc, channelID, 1, null);
                ((FAHFunctionPropertyCommand)ReadDesc.Payload.ReadablePayloadPacket).GetPropertyControlForReply(ref ByteDataParm, ref lastRequestedPropertyControl);
                Console.WriteLine(ReadDesc.ToHexString());

                if (!kNXUart.WriteDirect(ReadDesc, true))
                    return;
                else
                    if (!ProcessResponse(out moreIndices))
                        ConsoleWriteLine("Invalid data");
            }

            Console.WriteLine("Setting Interface types we know");

            if (deviceToLearn.DeviceType == FaHDeviceType.SensorSwitchactuator22gang)
            {
                Console.WriteLine("Switch 2\\2");
                //Dit kunnen we sturen
                //deviceToLearn.WriteChannelPropertyType(1, 2, FaHDeviceProperties.ChannelType.NotDefined, FaHDeviceProperties.SensorActorInterfaceType.NotDefined);
                //deviceToLearn.WriteChannelPropertyType(4, 2, FaHDeviceProperties.ChannelType.NotDefined, FaHDeviceProperties.SensorActorInterfaceType.NotDefined);
                deviceToLearn.WriteChannelPropertyType(1, 3, FaHDeviceProperties.ChannelType.chanOutputOnClickChannelType, FaHDeviceProperties.SensorActorInterfaceType.ButtonLeft);
                deviceToLearn.WriteChannelPropertyType(4, 3, FaHDeviceProperties.ChannelType.chanOutputOnClickChannelType, FaHDeviceProperties.SensorActorInterfaceType.ButtonRight);

                //Deze moeten we monitoren
                deviceToLearn.WriteChannelPropertyType(7, 2, FaHDeviceProperties.ChannelType.chanInputActorGroupMessage, FaHDeviceProperties.SensorActorInterfaceType.Actor1);
                deviceToLearn.WriteChannelPropertyType(8, 2, FaHDeviceProperties.ChannelType.chanInputActorGroupMessage, FaHDeviceProperties.SensorActorInterfaceType.Actor2);

                //Dit moeten we sturen als waarde veranderd
                deviceToLearn.WriteChannelPropertyType(7, 3, FaHDeviceProperties.ChannelType.chanOutputActorChangedValue, FaHDeviceProperties.SensorActorInterfaceType.Actor1);
                deviceToLearn.WriteChannelPropertyType(8, 3, FaHDeviceProperties.ChannelType.chanOutputActorChangedValue, FaHDeviceProperties.SensorActorInterfaceType.Actor2);

                deviceToLearn.DeviceHealthStatus.DeviceReboots = 0;
                deviceToLearn.DeviceHealthStatus.Uptime = 0;
            }
            else
            {
                Console.WriteLine("Unkown");
            }

            ConsoleWriteHeader("Read Completed:" + deviceToLearn.FaHAddress);
            deviceToLearn.Serialize(deviceToLearn.FaHAddress + "-learn.json");
        }

        private static bool ProcessResponse(out bool moreIndices, bool keepKNxPacketResult = false, bool WriteToDevice = true)
        {
            while (knxMsgtoProcess == null)
            {
                //Todo add exit counter
                Thread.Sleep(5);
            }

            moreIndices = false;

            FahPayloadInterpeter.TryToInterpret(ref knxMsgtoProcess);

            if (knxMsgtoProcess.Payload.Apdu.apduType == KNXAdpu.ApduType.FunctionPropertyStateResponse)
            {
                knxMsgtoProcess.Payload.ReadablePayloadPacket = ((FAHFunctionPropertyStateResponse)knxMsgtoProcess.Payload.ReadablePayloadPacket).ProcessPayload(lastRequestedPropertyControl, ByteDataParm);
                FAHFunctionPropertyStateResponse fAHFunction = knxMsgtoProcess.Payload.ReadablePayloadPacket as FAHFunctionPropertyStateResponse;

                ConsoleWriteOnEmptyLine(string.Format("Processing {0} ", knxMsgtoProcess.HeaderAsString));
                knxMsgtoProcess.Payload.ReadablePayloadPacket.PrintUnaccountedBytes();

                if (keepKNxPacketResult)
                    knxLastMsgProcessed = knxMsgtoProcess;

                if (fAHFunction.resultCode == KNXHelpers.knxPropertyReturnValues.MoreIndices || fAHFunction.resultCode == KNXHelpers.knxPropertyReturnValues.Success)
                {
                    if(!WriteToDevice)
                    {
                        if (fAHFunction.resultCode == KNXHelpers.knxPropertyReturnValues.MoreIndices)
                        {
                            moreIndices = true;
                        }
                        else
                        {
                            moreIndices = false;
                        }
                        Thread.Sleep(100);
                        knxMsgtoProcess = null;
                        return true;
                    }
                    if (fAHFunction.SaveToDevice(ref deviceToLearn, out moreIndices))
                    {
                        if (fAHFunction.resultCode == KNXHelpers.knxPropertyReturnValues.MoreIndices)
                        {
                            moreIndices = true;
                        }
                        else
                        {
                            moreIndices = false;
                        }
                        Thread.Sleep(100);
                        knxMsgtoProcess = null;
                        return true;
                    }
                }
                knxMsgtoProcess = null;
            }
            return false;
        }

        private static void ConsoleWriteLine(string stringout)
        {
            if (Console.CursorLeft > 1)
            {
                Console.WriteLine();
            }
            Console.WriteLine(stringout);
        }

        private static void ConsoleWriteOnEmptyLine(string stringout)
        {
            if (Console.CursorLeft > 1)
            {
                Console.WriteLine();
            }
            Console.Write(stringout);
        }

        private static void ConsoleWriteHeader(string name)
        {
            if(Console.CursorLeft > 1)
            {
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Blue;
            name = name.PadRight(30);
            Console.WriteLine("==[{0}]=================================================================", name);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;

        }

        private static void KNXUart_OnKNXMessage(KNXNetworkLayerTemplate caller, KNXBaseTypes.KNXmessage Message, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent)
        {
            if (Message.ControlField.RepeatFrame)
                //Repeatframe
                return;
            if (Message.SourceAddress == SysApEmulator.KnxAddress)
                //Self
                return;

            switch (devLearnState)
            {
                case DeviceLearningState.deviceDiscovery:
                    if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.DeviceDescriptorResponse)
                    {
                        Message.Payload.ReadablePayloadPacket = new FAHDeviceDescriptorResponse(Message.Payload);
                        bool more;
                        ((FAHDeviceDescriptorResponse)Message.Payload.ReadablePayloadPacket).SaveToDevice(ref deviceToLearn, out more);
                        devLearnState = DeviceLearningState.deviceDiscoveryResponse;
                        return;
                    }
                    break;

                case DeviceLearningState.deviceReadSettings:
                    if(Message.SourceAddress == deviceToLearn.KnxAddress)
                    {
                        if(Message.ControlField.RepeatFrame)
                        {
                            //For now ignore
                            return;
                        }
                        knxMsgtoProcess = Message;
                    }                    
                    break;

                default:
                    Console.Write(string.Format("{0} ", Message.HeaderAsString));
                    Message.Payload.ReadablePayloadPacket.PrintUnaccountedBytes();
                    break;
            }
        }
    }
}