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

    These are supporting modules for KNX protocol processing
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace KNXBaseTypes
{
    public class KNXPayload
    {
        //todo change to internal
        public byte[] PayloadByteData { get => payloadByteData; private set => payloadByteData = value; }
        public KNXmessage OwnerOfPayload { get; private set; }
        public FAHReadablePayloadPacket ReadablePayloadPacket;
        public KNXAdpu Apdu;
        public KNXTpdu Tpdu;
        private byte[] payloadByteData = new byte[0];

        public byte[] GetBytes(int begin, int lenght)
        {
            try
            {
                byte[] d = new byte[lenght];
                Array.Copy(PayloadByteData, begin, d, 0, lenght);
                return d;
            }
            catch
            {
                throw;
            }
        }

        public void NewPayload(KNXAdpu.ApduType apdu, uint initialsize)
        {
            if (initialsize < 2)
                throw new InvalidExpressionException();

            payloadByteData = new byte[initialsize];
            OwnerOfPayload.PayloadLenght = (byte)(initialsize);
            Apdu.apduType = apdu;            
        }

        public void AppendPayload(byte[] data)
        {
            int origLen = PayloadByteData.Length;
            ResizePayloaddata(origLen + data.Length);
            UpdateBytes(data, (uint)origLen, data.Length);
        }

        public void ResizePayloaddata(int newSize)
        {
            if (newSize < 0)
                throw new InvalidOperationException();
            if (newSize != PayloadByteData.Length)
            {
                Array.Resize(ref payloadByteData, newSize);
                OwnerOfPayload.PayloadLenght = (byte)(newSize);
            }
        }

        public void UpdateBytes(byte[] bytesToUpdate, uint index, int count)
        {
            Array.Copy(bytesToUpdate, 0, payloadByteData, index, count);
        }

        public void SetBytes(byte[] payload)
        {
            PayloadByteData = payload;
            OwnerOfPayload.PayloadLenght = (byte)payload.Length;
            ReadablePayloadPacket = new FAHReadablePayloadPacket(this);
        }

        public KNXPayload(KNXmessage ownerMessage, int size)
        {
            OwnerOfPayload = ownerMessage;
            if (size < 2)
                throw new Exception("Expected minimum of 2 Bytes");
            PayloadByteData = new byte[size];
            Apdu = new KNXAdpu(this);
            Tpdu = new KNXTpdu(this);
            //ReadablePayloadPacket = new FAHReadablePayloadPacket(this);
        }

        internal KNXPayload(byte[] knxPacketData, int StartIndexPayload, KNXmessage ownerMessage)
        {
            this.OwnerOfPayload = ownerMessage;
            int newDataLen = knxPacketData.Length - StartIndexPayload;
            PayloadByteData = new byte[newDataLen];

            Array.Copy(knxPacketData, StartIndexPayload, PayloadByteData, 0, newDataLen);
            Apdu = new KNXAdpu(this);
            Tpdu = new KNXTpdu(this);
            ReadablePayloadPacket = new FAHReadablePayloadPacket(this);
        }

    }
}
