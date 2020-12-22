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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAHPayloadInterpeters;
using FreeAtHomeDevices;
using KNXBaseTypes;

namespace FAHPayloadInterpeters
{
    public class FAHDeviceDescriptorResponse : FAHReadablePayloadPacketEx
    {
        /*
         //Payload
         ID     0       1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22      23      24      25      26      28
                0x03	0x43	0x0E	0x00	0xAB	0xB7	0x00	0xC7	0x7F	0xC1	0xE6	0x58	0x10	0x19	0xC4	0xE4	0x15	0x56	0x05	0xEA	0x10	0x4A	0x10	0x68	0x06	0x0A	0x02	0x63
         HEX    0x03    0x43	0x06	0x00	0xAB	0xB7	0x00	0xC7	0x30	0xA9	0xFF	0xFF	0x10	0x10	0x55	0xEA	0x08	0x00	0x10	0x80	0x10	0x40	0x10	0x60	0x11	0x40
         DEC    3       67		6		0		171		183		0		199		48		169		255		255		16		16		85		234		8		0		16		128		16		64		16		96		17		64          
         VAL    APCI    Ap\Data ?	    ?       ADDR	ADDR	ADDR	ADDR    ADDR	ADDR    SYSID   SYSID	TYPE    TYPE    CONIST  CONSIST [CHAN_1.....]   [CHAN_2.....]   [CHAN_3.....]   [CHAN_4.....]   [CHAN_5.....]   [CHAN_6.....]
        */

        public override bool SaveToDevice(ref FreeAtHomeDevices.FaHDevice device, out bool moreIndices)
        {
            moreIndices = false;
            try
            {
                device.ChannelCount = ChannelCount;                
                for (int i = 1; i <= ChannelCount; i++)
                {
                    device.WriteChannelIndentifier(i, GetDeviceChannel((uint)i));
                }                
                device.DeviceType = faHDeviceType;
                device.FaHAddress = FahDeviceAddress;
                device.SetAddressInformation(this.payloadReference.OwnerOfPayload.SourceAddress, fahSystemID);
                device.ConsistancyValue = ConsistencyValue;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private const byte __FAHChannelStart = 16;

        public override void UpdatePacketSettings()
        {
            base.UpdatePacketSettings();
            base.payloadReference.OwnerOfPayload.ControlField.PacketType = knxControlField.KnxPacketType.KNX_PacketLong;
            if (base.payloadReference.OwnerOfPayload.TargetAddress.knxAddress == 0)
            {
                this.payloadReference.OwnerOfPayload.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Group;
            }
            else
            {
                this.payloadReference.OwnerOfPayload.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;
            }
            this.payloadReference.OwnerOfPayload.HopCount = 6;            
            this.payloadReference.OwnerOfPayload.ControlField.Priority = knxControlField.KnxPriority.KNX_PRIORITY_SYSTEM;
        }

        public static KNXmessage CreateResponse(FaHDevice atHomeDevice, KNXAddress AddressToSendTo)
        {
            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketLong); 
            //kNXmessage.HopCount = 6;
            kNXmessage.SourceAddress = atHomeDevice.KnxAddress;

            uint payloadSize = __FAHChannelStart + (atHomeDevice.ChannelCount * 2);

            kNXmessage.TargetAddress = AddressToSendTo;
            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.DeviceDescriptorResponse, payloadSize);            
            kNXmessage.Payload.ReadablePayloadPacket = new FAHDeviceDescriptorResponse(kNXmessage.Payload);
            FAHDeviceDescriptorResponse newPkg = (FAHDeviceDescriptorResponse)kNXmessage.Payload.ReadablePayloadPacket;
            
            newPkg.UpdatePacketSettings();
            newPkg.DescriptorType = 3;
            newPkg.__UnknownValue0 = new KNXu16SimpleStruct(0x0E, 0x00); //14 00
            newPkg.FahDeviceAddress = atHomeDevice.FaHAddress;
            newPkg.fahSystemID = atHomeDevice.SystemID;
            newPkg.faHDeviceType = atHomeDevice.DeviceType;
            newPkg.ConsistencyValue = atHomeDevice.ConsistancyValue;
            //kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Group;
            
            for (int i = 1; i <= atHomeDevice.ChannelCount; i++)
            {
                KNXu16SimpleStruct addr;
                if (atHomeDevice.ReadChannelIndentifier(i, out addr))
                {
                    newPkg.SetDeviceChannel((uint)i, addr);
                }
                //else NULL
            }
            return kNXmessage;
        }

        public byte DescriptorType
        {
            get
            {
                return (byte)(base.payloadReference.PayloadByteData[1] & 0xF);
            }
            set
            {
                if (value > 128)
                    throw new InvalidDataException("Data value would override ACPI messagetype");

                base.payloadReference.PayloadByteData[1] = KNXBaseTypes.KNXHelpers.SetByteBitValue(base.payloadReference.PayloadByteData[1], 0xF, value);
            }
        }

        public KNXu16SimpleStruct __UnknownValue0
        {
            get
            {
                return new KNXu16SimpleStruct(base.payloadReference.PayloadByteData, 2);
            }
            set
            {
                Array.Copy(value.ToByteArray(), 0, base.payloadReference.PayloadByteData, 2, 2);
            }

        }

        public FaHDeviceAddress FahDeviceAddress
        {
            get
            {
                return FaHDeviceAddress.FromByteArray(base.payloadReference.PayloadByteData, 4);
            }
            set
            {
                Array.Copy(value.byteValue, 0, base.payloadReference.PayloadByteData, 4, 6);
            }
        }

        public FahSystemID fahSystemID
        {
            get
            {
                return new FahSystemID(KNXHelpers.knxToUint16(base.payloadReference.PayloadByteData, 10));
            }
            set
            {
                Array.Copy(value.ByteArray, 0, base.payloadReference.PayloadByteData, 10, 2);
            }
        }

        public FaHDeviceType faHDeviceType
        {
            get
            {
                return FreeAtHomeDeviceTypeMethod.FromByteArray(0, base.payloadReference.PayloadByteData, 12);
            }
            set
            {
                Array.Copy(value.ToByteArray(), 0, base.payloadReference.PayloadByteData, 12, 2);
            }
        }

        public KNXu16SimpleStruct ConsistencyValue
        {
            get
            {
                return new KNXu16SimpleStruct(base.payloadReference.PayloadByteData, 14);
            }
            set
            {
                Array.Copy(value.ToByteArray(), 0, base.payloadReference.PayloadByteData, 14, 2);
            }
        }

        public ushort ChannelCount
        {
            get
            {
                return (ushort)(((base.payloadReference.PayloadByteData.Length - __FAHChannelStart) / 2));
            }
            set
            {
                if (ChannelCount != value)
                {
                    int newSize = __FAHChannelStart + (2 * (value - 1));
                    base.payloadReference.ResizePayloaddata(newSize);
                }
            }
        }

        public KNXu16SimpleStruct GetDeviceChannel(uint index)
        {
            //Index is numbered 1---X
            //Stored in array as 0--(X-1)
            index--;
            return new KNXu16SimpleStruct(base.payloadReference.PayloadByteData, (uint)(__FAHChannelStart + (index * 2)));
        }

        public void SetDeviceChannel(uint index, KNXu16SimpleStruct value)
        {
            //Index is numbered 1---X
            //Stored in array as 0--(X-1)
            index--;
            int byteindex = (int)((index * 2) + __FAHChannelStart);
            Array.Copy(value.ToByteArray(), 0, base.payloadReference.PayloadByteData, byteindex, 2);            
        }

        public FAHDeviceDescriptorResponse(KNXPayload kNXPayload) :base (kNXPayload)
        {
            if(kNXPayload.Apdu.apduType != KNXAdpu.ApduType.DeviceDescriptorResponse)
            {
                throw new InvalidCastException("Message type does not match");
            }

            defaultKnxPacketType = knxControlField.KnxPacketType.KNX_PacketLong;

            //Last 4 bits of second byte
            //DescriptorType = (byte)(kNXPayload.PayloadByteData[1] & 0xF);
            addAccountedBytes(1);

            //freeAtHomeDevice.FaHAddress = FaHDeviceAddress.FromByteArray(base.payloadReference.PayloadByteData, 4);
            addAccountedBytes(4, 6);

            //freeAtHomeDevice.NetworkID = new FahSystemID(KNXHelpers.knx_to_uint16(base.payloadReference.GetBytes(10, 2)));
            addAccountedBytes(10, 2);

            //freeAtHomeDevice.DeviceType = freeAtHomeDevice.DeviceType.FromByteArray(base.payloadReference.PayloadByteData, 12);
            addAccountedBytes(12, 2);
            
            //freeAtHomeDevice.ConsitancyValue = new KNXu16SimpleStruct(base.payloadReference.PayloadByteData, 14);
            addAccountedBytes(14, 2);

            //base.processingLevel = ProcessingLevel.ProcessingBasic;
            
            uint ChannelCount = (uint)(base.payloadReference.PayloadByteData.Length - __FAHChannelStart) / 2;

            for(int i = 0; i < ChannelCount; i++)
            {
                //freeAtHomeDevice.Channels[i] = FaHDeviceChannel.FromByteArray(base.payloadReference.GetBytes((i * 2) + __FAHChannelStart, 2));
                addAccountedBytes((uint)(i * 2) + __FAHChannelStart, 2);
            }
        }

        protected override string PrintOut()
        {
            return "";
            //return string.Format("Device {0} [{1}] Network: {2}", DeviceType, domainAddress, FaHAddress);
        }
    }
}
