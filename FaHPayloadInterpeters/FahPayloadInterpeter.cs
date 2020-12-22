﻿/*
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

    This modules can be used to process and gerenate KNX payloads for FreeAtHome message types.
    Please note not all fields are reverse engineerd.
    
*/
using FreeAtHomeDevices;
using KNXBaseTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHPayloadInterpeters
{
    public static class FahPayloadInterpeter
    {                
        public static void TryToInterpret(ref KNXmessage Message)
        {
            if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.DeviceDescriptorResponse)
            {
                Message.Payload.ReadablePayloadPacket = new FAHDeviceDescriptorResponse(Message.Payload);                
            }
            else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.DeviceDescriptorRead)
            {
                Message.Payload.ReadablePayloadPacket = new FAHDeviceDescriptorRead(Message.Payload);
            }
            else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.FunctionPropertyStateResponse)
            {
                Message.Payload.ReadablePayloadPacket = new FAHFunctionPropertyStateResponse(Message.Payload);
            }
            else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.FunctionPropertyCommand)
            {
                Message.Payload.ReadablePayloadPacket = new FAHFunctionPropertyCommand(Message.Payload);
            }
            else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.ABBSetBinaryInputType)
            {
                Message.Payload.ReadablePayloadPacket = new ABBSetBinaryInputType(Message.Payload);
            }
            else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.GroupValueWrite)
            {
                Message.Payload.ReadablePayloadPacket = new FAHGroupValueWrite(Message.Payload);
            }
            else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.Restart)
            {
                Message.Payload.ReadablePayloadPacket = new FAHRestart(Message.Payload);
            }
            else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.IndividualAddressSerialNumberWrite)
            {
                Message.Payload.ReadablePayloadPacket = new FAHIndividualAddressSerialNumberWrite(Message.Payload);
            }
            else if (Message.Payload.Apdu.apduType == KNXAdpu.ApduType.ABBResponseBinaryInputType)
            {
                Message.Payload.ReadablePayloadPacket = new ABBResponseBinaryInputType(Message.Payload);
            }
        }
    }
}