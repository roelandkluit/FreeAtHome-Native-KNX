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
using KNXBaseTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHPayloadInterpeters
{
    public class ABBResponseBinaryInputType : FAHReadablePayloadPacketEx
    {
        public ABBResponseBinaryInputType(KNXPayload kNXPayload) : base(kNXPayload)
        {
            if (kNXPayload.Apdu.apduType != KNXAdpu.ApduType.ABBResponseBinaryInputType)
            {
                throw new InvalidCastException("Message type does not match");
            }

            //Channel ??
            addAccountedBytes(2);

            //Entry ??
            addAccountedBytes(3,2);

            //return value??
            addAccountedBytes(5);
        }

        public KNXHelpers.knxPropertyReturnValues resultCode
        {
            get
            {
                return (KNXHelpers.knxPropertyReturnValues)(base.payloadReference.PayloadByteData[5]);
            }
            set
            {
                base.payloadReference.PayloadByteData[5] = (byte)value;
            }
        }

        public byte Channel
        {
            get
            {
                return base.payloadReference.PayloadByteData[2];
            }
            set
            {
                base.payloadReference.PayloadByteData[2] = value;
            }
        }

        public KNXu16SimpleStruct Register
        {
            get
            {
                return new KNXu16SimpleStruct(base.payloadReference.PayloadByteData, 3);
            }
            set
            {
                Array.Copy(value.ToByteArray(), 0, base.payloadReference.PayloadByteData, 3, 2);
            }
        }

        protected override string PrintOut()
        {
            return string.Format("Ch{0:D3}:{1}->{2}", Channel, Register.u16value, resultCode);
        }
    }
}
