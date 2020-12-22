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
    public class FPC_WriteRoomInfo : FAHFunctionPropertyCommand
    {
        public UInt16 Room
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

        public UInt16 Xcoords
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

        public UInt16 Ycoords
        {
            get
            {
                return KNXHelpers.knxToUint16(payloadReference.PayloadByteData, 9);
            }
            set
            {
                byte[] data = KNXHelpers.uint16ToKnx(value);
                payloadReference.UpdateBytes(data, 9, 2);
            }
        }

        public KNXmessage Process(FaHDevice atHomeDevice)
        {
            if (PropertyControl != FAHFunctionPropertyCommand.PropertyControlTypes.WriteFlr_RmNr)
            {
                throw new InvalidCastException();
            }

            int ChannelIndex = ObjectID;
            int propIndex = PropertyID;
            bool hasMoreIndices = false;

            atHomeDevice.WriteRoomInfo(ChannelIndex, PropertyID, Room, Xcoords, Ycoords);

            return CreateEmptySuccessMessage(hasMoreIndices);
        }

        public FPC_WriteRoomInfo(KNXPayload OwnerPayload) : base(OwnerPayload)
        {
            //Room
            addAccountedBytes(5, 2);
            //X
            addAccountedBytes(7, 2);
            //Y
            addAccountedBytes(9, 2);
        }
    }
}
