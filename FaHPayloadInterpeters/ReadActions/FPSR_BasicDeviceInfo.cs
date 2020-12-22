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
    public class FPSR_BasicDeviceInfo : FAHFunctionPropertyStateResponse
    {
        /*
         * 2020-06-06 15:32:05.250; Data-FunctionPropertyCommand   KNX_PRIORITY_NORMAL     [NoExtdFrame]   [0x00-0x01]     [0x6F-0x01]      Ch000:1->ReadBasicInfo
         * 2020-06-06 15:32:05.297; Data-FunctionPropertyStateResponse     KNX_PRIORITY_NORMAL     H:6, Single, FF:0x00    [0x6F-0x01]     [0x00-0x01]      Ch000:1->Success [FF EB FE FF 08 00 37 00 6A F6 D1 00 00 02 7E]     0xFF 0xEB 0xFE 0xFF 0x08 0x00 0x37 0x00 0x6A 0xF6 0xD1 0x00 0x00 0x02 0x7E
        */
        //
        //     0        1       2       3       4       5       6       7       8       9       10      11      12      13      14      15      16      17      18      19      20      21      22  
        // HEX 0x02     0xC9	0x00	0x01	0x00	0xFF	0xFD	0xFE	0xFF	0x15	0x56	0x37	0x00	0x7A	0x3C	0xF9	0x00	0x00	0x05	0x56	0x02	0x00	0x01	0x00
        // DEC 2        201		0		1		0		255		253		254		255		21		86		55		0		122		60		249		0		0		5		86		2		0		1		0

        //<device isTp = "true"  nameId="FFEB" profile="0E00" maxAPDULength="37" compilerVersion="007A3CF9" buildNumber="00000556" iconId="FFFE" protocolVersion="0002" minConfigVersion="0001" deviceFlavor="00" interface="TP" functionId="FEFF" shortSerialNumber="CYY" softwareId="1556" softwareVersion="2.1366" deviceId="1019" serialNumber="ABB700C77FC1" commissioningState="ready" copyId="c">
        // HEX 0x02     0xC9	0x00	0x01	0x00	0xFF	0xEB	0xFE	0xFF	0x08	0x00	0x37	0x00	0x6A	0xF6	0xD1	0x00	0x00	0x02	0x7E
        // DEC 2        201		0		1		0		255		235		254		255		8		0		55		0		106		246		209		0		0		2		126

        // NAME         *       *       *       *       NAMEID	NAMEID	FUNCTID	FUNCTID				            COMPILER------------COMPILER	BUILD------------------BUILD



        public override bool SaveToDevice(ref FaHDevice faHDevice, out bool moreIndices)
        {
            faHDevice.BasicDeviceInformation = this.FPSRpayload;
            moreIndices = false;
            return true;
        }

        public static KNXmessage CreateResponse(FAHFunctionPropertyCommand MessageToRespondTo, FaHDevice atHomeDevice)
        {
            try
            {
                if (MessageToRespondTo.PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.ReadBasicInfo)
                {
                    throw new InvalidCastException();
                }

                //TODO, add as actual param to device!
                //FahDeviceParametersNew p = new FahDeviceParametersNew();
                //p.dataType = FahDeviceParametersNew.ParameterType.deviceInfo;
                //p.Response = KNXHelpers.knxPropertyReturnValues.Success;
                /*
                if (atHomeDevice.BasicDeviceInformation == null)
                {
                    atHomeDevice.BasicDeviceInformation = new byte[] { 0xFF, 0xEB, 0xFE, 0xFF, 0x08, 0x00, 0x37, 0x00, 0x6A, 0xF6, 0xD1, 0x00, 0x00, 0x02, 0x7E };
                }*/

                KNXmessage kNXmessage = new KNXmessage(knxControlField.KnxPacketType.KNX_PacketLong);
                kNXmessage.DestinationAddressType = KNXmessage.DestinationAddressFieldType.Individual;

                const int HEADERSIZE = 5;

                //Todo, check lenght?
                uint payloadSize = (uint)(HEADERSIZE + atHomeDevice.BasicDeviceInformation.Length);

                kNXmessage.Payload.NewPayload(KNXAdpu.ApduType.FunctionPropertyStateResponse, payloadSize);
                kNXmessage.Payload.ReadablePayloadPacket = new FPSR_BasicDeviceInfo(kNXmessage.Payload);
                FPSR_BasicDeviceInfo newPkg = (FPSR_BasicDeviceInfo)kNXmessage.Payload.ReadablePayloadPacket;
                newPkg.UpdatePacketSettings();
                newPkg.FPSRpayload = atHomeDevice.BasicDeviceInformation;
                newPkg.PropertyID = MessageToRespondTo.PropertyID;
                newPkg.ObjectID = MessageToRespondTo.ObjectID;
                newPkg.resultCode = KNXHelpers.knxPropertyReturnValues.Success;
                return kNXmessage;
            }
            catch
            {
                return null;
            }
        }

        public UInt16 NameID
        {
            get
            {
                return KNXHelpers.knxToUint16(payloadReference.PayloadByteData, 5);
            }
            set
            {
                byte[] data = KNXHelpers.uint16ToKnx(value);
                payloadReference.UpdateBytes(data, 5, 2);
            }
        }

        public UInt16 FunctionID
        {
            get
            {
                return KNXHelpers.knxToUint16(payloadReference.PayloadByteData, 7);
            }
            set
            {
                byte[] data = KNXHelpers.uint16ToKnx(value);
                payloadReference.UpdateBytes(data, 7, 2);
            }
        }

        public UInt16 Compiler
        {
            get
            {
                return KNXHelpers.knxToUint16(payloadReference.PayloadByteData, 12);
            }
            set
            {
                byte[] data = KNXHelpers.uint16ToKnx(value);
                payloadReference.UpdateBytes(data, 12, 2);
            }
        }

        public UInt16 Build
        {
            get
            {
                return KNXHelpers.knxToUint16(payloadReference.PayloadByteData, 16);
            }
            set
            {
                byte[] data = KNXHelpers.uint16ToKnx(value);
                payloadReference.UpdateBytes(data, 16, 2);
            }
        }

        public FPSR_BasicDeviceInfo(KNXPayload ownerPayload) : base(ownerPayload)
        {
            //NameID
            addAccountedBytes(5, 2);
            //FunctionID
            addAccountedBytes(7, 2);
            //Compiler
            addAccountedBytes(12, 4);
            //Build
            addAccountedBytes(16, 4);
        }
    }
}
