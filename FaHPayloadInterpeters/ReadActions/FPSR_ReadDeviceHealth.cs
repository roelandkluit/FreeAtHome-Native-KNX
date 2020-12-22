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

    This modules can be used to process and gerenate KNX payloads for FreeAtHome message types.
    Please note not all fields are reverse engineerd.
    
*/
using FreeAtHomeDevices;
using KNXBaseTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHPayloadInterpeters.FAHFunctionPropertyStateResponses
{
    public class FPSR_ReadDeviceHealth : FAHFunctionPropertyStateResponse
    {
        public FPSR_ReadDeviceHealth(KNXPayload ownerPayload) : base(ownerPayload)
        {
            /*
             * 2020-06-23 18:56:16.635; FunctionPropertyStateResponse  KNX_PRIORITY_NORMAL     H:6, Single, FF:0x00    [0xB8-0x01]     [0x00-0x01]      FPSR_ReadDeviceHealth>Ch000:1->Success [07 C9 00 00 42 B5 00 54 00 00 00 BF 00 00 00 03 00 00 00 00 00 06 00 06 00 00 00 00]0x02 0xC9 0x00 0x01 0x00 0x07 0xC9 0x00 0x00 0x42 0xB5 0x00 0x54 0x00 0x00 0x00 0xBF 0x00 0x00 0x00 0x03 0x00 0x00 0x00 0x00 0x00 0x06 0x00 0x06 0x00 0x00 0x00 0x00
             */
            //      0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22      23      24      25      26      27      28      29      30      31      32

            //      0x02	0xC9	0x00	0x01	0x00	0x07	0xC9	0x00	0x00	0x42	0xB5	0x00	0x54	0x00	0x00	0x00	0xBF	0x00	0x00	0x00	0x03	0x00	0x00	0x00	0x00	0x00	0x06	0x00	0x06	0x00	0x00	0x00	0x00
            // HEX  0x02    0xC9	0x00	0x01	0x00	0x07	0x78	0x00	0x00	0x04	0x31	0x00	0x03	0x00	0x00	0x00	0xBF	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00	0x00
            // DESC *       *       *       *       *       Volt----Volt    OperationTime--OperationTime    devicereboots                                                   biterrors---                                    parityerrors    spikeErrors-    othererrorvalue
            //ReadValue0x00 ch000 4

            //Voltage
                addAccountedBytes(5, 2);
            //var x = KNXHelpers.knx_to_float(ownerPayload.PayloadByteData, 5); 
            //Operation Time
            addAccountedBytes(7, 4);
            //var operationTime = KNXHelpers.knx_to_uint32(ownerPayload.PayloadByteData, 7);
            //Reboots
            addAccountedBytes(11, 2);
            //Biterrors?
            addAccountedBytes(19, 2);
            //parityerrors?
            addAccountedBytes(25, 2);
            //spikeerrors?
            addAccountedBytes(27, 2);
            //othererror?
            addAccountedBytes(29, 2);

            //ingore others
            addIgnoredBytes(13, 20);
        }

        public static KNXmessage CreateReadResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            try
            {
                if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadDevHealth)
                {
                    throw new InvalidCastException();
                }

                byte[] bData = atHomeDevice.DeviceHealthStatus.ByteData;

                KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketLong)
                {
                    DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual
                };

                const int HEADERSIZE = 5;

                //Todo, check lenght?
                uint payloadSize = (uint)(HEADERSIZE + bData.Length);

                kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
                kNXmessage.Payload.ReadablePayloadPacket = new FPSR_ReadDeviceHealth(kNXmessage.Payload);
                FPSR_ReadDeviceHealth newPkg = (FPSR_ReadDeviceHealth)kNXmessage.Payload.ReadablePayloadPacket;
                newPkg.UpdatePacketSettings();
                newPkg.FPSRpayload = bData;
                newPkg.PropertyID = MessageToRespondTo.PropertyID;
                newPkg.ObjectID = MessageToRespondTo.ObjectID;
                return kNXmessage;
            }
            catch
            {
                return MessageToRespondTo.CreateCommandNotSupportedMessage();
            }
        }

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            moreIndices = false;
            faHDevice.DeviceHealthStatus = new FahDeviceHealth(this.FPSRpayload);
            return true;
        }
    }
}
