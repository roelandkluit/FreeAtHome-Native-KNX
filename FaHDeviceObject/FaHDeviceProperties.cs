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

    This modules is a custom representation of the data in a Free@home device, can be serialized as Json.
    Please note not all fields are reverse engineerd.
    
*/
using KNXBaseTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FreeAtHomeDevices.FaHSerializationhexHelper;

namespace FreeAtHomeDevices
{
    public class FaHDeviceProperties
    {
        public enum SensorActorInterfaceType
        {
            NotDefined = 0,
            Button1,
            ButtonLeft = Button1,
            Button2,
            ButtonRight = Button2,
            Button3,
            Button4,
            Actor1,
            Actor2,
            Actor3,
            Actor4,
        }
        //Probably encoded somewhere in OID data, currently unknown how to interpet.
        public enum ChannelType
        {
            NotDefined = 0,
            //These are send when a button is clicked (Dimming, Shutters, etc. not currently implemented)            
            chanOutputOnClickChannelType = 1,
            //This are the group values we have to act upon
            chanInputActorGroupMessage = 2,
            //These we send out when values changed
            chanOutputActorChangedValue = 3,

            /*intfOutputActor,
            intfInputActor,
            intfOutputSensor,
            intfInputSensor,
            intfDayNight*/
        }

        public int PropertyIndex;
        public FaHDevicePropertyData[] PropertyData;
        public FahDeviceOIDIndicedData[] IndicedOIDData;
        public FaHDevicePropertyData[] FunctionList;
        public byte[] DescriptorInfo;

        public FaHChannelAdressInformation[] ChannelAdresses;
        public ChannelType channelType = ChannelType.NotDefined;
        public SensorActorInterfaceType ActorSensorIndex = SensorActorInterfaceType.NotDefined;

        public UInt16? RoomID;
        public UInt16? X;
        public UInt16? Y;
        public UInt16? IconId;

        
    }

    public class FaHDevicePropertyData
    {
        public int index;
        public byte[] data;
    }

    public class FaHChannelAdressInformation
    {
        public HashSet<KNXAddress> GroupValueAddress = null;
        //public byte[] AdditionalData = null;
        public byte ChannelParameter;
    }

    public class FahDeviceOIDData
    {
        public FahDeviceOIDIndicedData[] IndicedData;
    }

    public class FahDeviceOIDIndicedData
    {
        public int Index;
        public byte[] Data;
    }


}
