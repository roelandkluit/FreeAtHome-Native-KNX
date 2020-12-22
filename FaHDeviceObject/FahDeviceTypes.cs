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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAtHomeDevices
{
    public enum FaHDeviceType : UInt16
    {
        BinaryInput2_gang = 0xB005,
        BinaryInput2_gangB = 0xB006,
        BinaryInput4_gang = 0xB007,
        Switchactuator4_gang = 0xB002,
        Switchactuator8_gang = 0xB008,
        Valveactuator6_gang = 0xB003,
        Valveactuator12_gang = 0xB004,
        Shutteractuator4_gang = 0xB001,
        FanCoilactuator = 0xB009,
        SensorUnit1gang = 0x1000,
        SensorUnit1gangB = 0x9000,
        SensorUnit2gang = 0x9001,
        SensorUnit2gangB = 0x1002,
        SensorUnit2gangC = 0x9002,
        SensorUnit4gang = 0x9003,
        RoomTemperatureController = 0x1004,
        RoomTemperatureControllerB = 0x9004,
        RoomTemperatureControllerC = 0x9005,
        RoomtemperaturecontrollerD = 0x1006,
        RoomtemperaturecontrollerE = 0x9006,
        RoomtemperaturecontrollerF = 0x9007,
        Movementdetector = 0x1008,
        MovementdetectorB = 0x9008,
        MovementdetectorC = 0x9009,
        Movementdetectoractuator1gang = 0x100A,
        Movementdetectoractuator1gangB = 0x900A,
        Movementdetectoractuator1gangC = 0x900B,
        SensorSwitchactuator11gang = 0x100C,
        SensorSwitchactuator11gangB = 0x900C,
        SensorSwitchactuator21gang = 0x900D,
        SensorSwitchactuator21gangB = 0x100E,
        SensorSwitchactuator21gangC = 0x900E,
        SensorSwitchactuator41gang = 0x900F,
        SensorSwitchactuator22gang = 0x1010,
        SensorSwitchactuator22gangB = 0x9010,
        SensorSwitchactuator42gang = 0x9011,
        SensorBlindactuator11gang = 0x1013,
        SensorBlindactuator11gangB = 0x9013,
        SensorBlindactuator21gang = 0x9014,
        SensorBlindactuator21gangB = 0x1015,
        SensorBlindactuator21gangC = 0x9015,
        SensorBlindactuator41gang = 0x9016,
        SensorDimactuator11gang = 0x1017,
        SensorDimactuator11gangB = 0x9017,
        SensorDimactuator21gang = 0x9018,
        SensorDimactuator21gangB = 0x1019,
        SensorDimactuator21gangC = 0x9019,
        SensorDimactuator41gang = 0x901A,
        Dimactuator4gang = 0x101B,
        Dimactuator4gangB = 0x901B,
        Dimactuator4gangC = 0x101C,
        Dimactuator4gangD = 0x901C,
        SystemAccessPoint = 0x1012,
        SystemAccessPointA = 0x9012,
        SystemAccessPointB = 0x2012,
        ABB_WelcomeTouchMR = 0x1038,
        FlushmountedHeatingactuator1_gang = 0x1090,
        Heatingactuator = 0x9090,
        Testdevice = 0x10A0,
        FlushmountedBinaryinput2_gang = 0x90A0,
        FlushmountedFanCoilactuator1_gang = 0x10B0,
        FlushmountedFanCoilactuator1_gangB = 0x90B0,
        HueActuator = 0x10C0,
        HueActuatorB = 0x10C1,
        HueActuatorC = 0x10C2,
        HueActuatorD = 0x10C3,
        HueActuatorE = 0x10C4,
        TypeSysAPInterface = 0x1018,
        TypeBroadCastAddress = 0xFFFF,

        // E0 extended MSG, payload field 11+12
        /*
        TypeRemote01 = 0x1001,
        TypeRemote02 = 0x1002,
        TypeThermostat = 0x1004,
        TypeBinaryInput20 = 5,
        TypeSwitch11 = 0x1012,
        TypeSwitch12 = 0x1014,
        TypeSwitch22 = 0x1016,        
        TypeBlinds11 = 0x1019,
        TypeBlinds12 = 0x1021,
        TypeDimmer11 = 0x1023,
        TypeDimmer12 = 0x1025,
        //TypeWelcomePanel = 0x1038,*/
        TypeNotDefined = 0x0,
    }

    public static class FreeAtHomeDeviceTypeMethod
    {
        public static FaHDeviceType FromByteArray(this FaHDeviceType dummy, byte[] input, UInt16 begin)
        {
            if (input.Length - begin < 1)
                throw new InternalBufferOverflowException();

            FaHDeviceType DeviceType = (FaHDeviceType)KNXHelpers.knxToUint16(input, begin);
            return DeviceType;
        }

        public static byte[] ToByteArray(this FaHDeviceType thisDeviceType)
        {
            UInt16 deviceID = (UInt16)thisDeviceType;
            return KNXHelpers.uint16ToKnx(deviceID);
        }
    }
}
