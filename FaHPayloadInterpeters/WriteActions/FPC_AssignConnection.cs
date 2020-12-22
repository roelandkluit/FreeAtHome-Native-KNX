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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FAHPayloadInterpeters.FAHFunctionPropertyStateResponses
{    
    public class FPC_AssignConnection : FAHFunctionPropertyCommand
    {
        //private const UInt16 PACKET_PAYLOAD_ASSIGNCONN = 3;

        public KNXmessage Process(FaHDevice atHomeDevice)
        {
            if (PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.AssignConn)
            {
                throw new InvalidCastException();
            }

            int ChannelIndex = ObjectID;
            int propIndex = PropertyID;
            bool hasMoreIndices;

            if (propIndex == 1) //Description
            {
                throw new Exception();
            }
            else
            {
                atHomeDevice.WriteConnectionValue(ChannelIndex, PropertyID, ConnectionID, 1, new KNXAddress[] { GroupValueAddress }, out hasMoreIndices);
            }

            return CreateEmptySuccessMessage(hasMoreIndices);
        }

        public byte[] AdditionalData
        {
            get
            {
                if (payloadReference.PayloadByteData.Length > 9)
                    return payloadReference.GetBytes(9, payloadReference.PayloadByteData.Length - 9);
                else
                    return null;
            }
            set
            {
                byte[] data = value;
                payloadReference.UpdateBytes(data, 9, data.Length);
            }
        }

        public KNXAddress GroupValueAddress
        {
            get
            {
                return new KNXAddress(payloadReference.PayloadByteData, 6);
            }
            set
            {
                byte[] data = value.ToByteArray();
                payloadReference.UpdateBytes(data, 6, 2);
            }
        }

        public Byte ConnectionID
        {
            get
            {
                return payloadReference.PayloadByteData[5];
            }
            set
            {
                payloadReference.PayloadByteData[5] = (byte)value;
            }
        }

        public FPC_AssignConnection(KNXPayload OwnerPayload) : base(OwnerPayload)
        {
            if (PropertyID == 1)
            {
                //Description
            }
            else
            {
                //Connection ID
                addAccountedBytes(5, 1);
                //GroupValue Address
                addAccountedBytes(6, 2);
            }
        }
    }
}
