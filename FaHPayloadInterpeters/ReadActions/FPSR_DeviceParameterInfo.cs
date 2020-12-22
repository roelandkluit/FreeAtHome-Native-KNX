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
    public class FPSR_DeviceParameterInfo : FAHFunctionPropertyStateResponse
    {
        /*
        * 2020-06-06 15:32:06.093; Data-FunctionPropertyCommand   KNX_PRIORITY_NORMAL     [NoExtdFrame]   [0x00-0x01]     [0x6F-0x01]      Ch000:4->ReadDesc [01]
        * 2020-06-06 15:32:06.172; Data-FunctionPropertyStateResponse     KNX_PRIORITY_NORMAL     H:6, Single, FF:0x00    [0x6F-0x01]     [0x00-0x01]      Ch000:4->Success [01 00 3B FF FF FF FF 07 14 C8 00 0C 01 00 49 00 00 00 05 00 4A 00 00 00 09 00 4B 00 00 00 06 00 4C 00 00 00 0A]             
        */
        // ID  0        1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22      23      24      25      26      27      28      29      30      31      32      33      34      35      36      37      38      39      40
        // HEX 0xC9     0x00    0x04    0x00    0x01    0x00    0x3B	0xFF	0xFF	0xFF	0xFF	0x07	0x14	0xC8	0x00	0x0C	0x01	0x00	0x49	0x00	0x00	0x00	0x05	0x00	0x4A	0x00	0x00	0x00	0x09	0x00	0x4B	0x00	0x00	0x00	0x06	0x00	0x4C	0x00	0x00	0x00	0x0A
        // DEC *        *       *       *       0       0		59		255		255		255		255		7		20		200		0		12		1		0		73		0		0		0		5		0		74		0		0		0		9		0		75		0		0		0		6		0		76		0		0		0		10
        //                                      Index   SWVER?  PARAMID MATCHCODE-------MATCHCODE       BIT?    DPT-----DPT     PARMID--PARMID  Value   optionNameID    MASK--------------------MASK    optionNameID    MASK--------------------MASK    optionNameID    MASK--------------------MASK    optionNameID    MASK--------------------MASK
        //
        // BIT? --> wizardOnly="false" deviceChannelSelector="false" channelSelector="true" writable="true" visible="true"
        //
        /*                  
         <parameters>
            <parameter nameId="003B" i="pm0000" optional="false" dependencyId="FFFF" wizardOnly="false" deviceChannelSelector="false" channelSelector="true" writable="true" visible="true" accessLevel="Enduser" parameterId="000C" matchCode="FFFFFFFF" dpt="14C8">
            <valueEnum>
                <option nameId="0049" mask="00000005" isDefault="true" key="1"/>
                <option nameId="004A" mask="00000009" isDefault="false" key="2"/>
                <option nameId="004B" mask="00000006" isDefault="false" key="3"/>
                <option nameId="004C" mask="0000000A" isDefault="false" key="4"/>
            </valueEnum>
            <value>1</value>
        </parameter>
        */

        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            moreIndices = false;
            faHDevice.DeviceParameterInfo = FPSRpayload;
            return true;
        }

        public byte FieldID
        {
            get
            {
                return base.payloadReference.PayloadByteData[5];
            }
            set
            {
                base.payloadReference.PayloadByteData[5] = (byte)value;
            }
        }

        public static KNXmessage CreateResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadDesc)
            {
                throw new InvalidCastException();
            }

            KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketLong)
            {
                DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual
            };

            const int HEADERSIZE = 5;

            //Todo, check lenght?
            uint payloadSize = (uint)(HEADERSIZE + atHomeDevice.DeviceParameterInfo.Length);

            kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
            kNXmessage.Payload.ReadablePayloadPacket = new FPSR_DeviceParameterInfo(kNXmessage.Payload);
            FPSR_DeviceParameterInfo newPkg = (FPSR_DeviceParameterInfo)kNXmessage.Payload.ReadablePayloadPacket;
            newPkg.UpdatePacketSettings();
            newPkg.FPSRpayload = atHomeDevice.DeviceParameterInfo;
            newPkg.PropertyID = MessageToRespondTo.PropertyID;
            newPkg.ObjectID = MessageToRespondTo.ObjectID;
            newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;

            //Part of the FPSRpayload at this moment!
            newPkg.FieldID = (byte)MessageToRespondTo.FieldID;
                        
            return kNXmessage;
        }

        public FPSR_DeviceParameterInfo(KNXPayload OwnerPayload) : base(OwnerPayload)
        {
            //Index?
            addAccountedBytes(4, 2);
            //ParamID
            addAccountedBytes(6, 1);
            //Matchcode
            addAccountedBytes(7, 4);
            //Bit?Value?
            addAccountedBytes(11, 1);
            //DPT
            addAccountedBytes(12, 2);
            //PARMID
            addAccountedBytes(14, 2);
            //Value
            addAccountedBytes(16, 1);
            uint i = 17;
            while (payloadReference.PayloadByteData.Length > i)
            {
                //Option NameID
                addAccountedBytes(i, 2);
                //Option Mask
                addAccountedBytes(i + 2, 4);
                i += 6;
            }
        }
    }
}