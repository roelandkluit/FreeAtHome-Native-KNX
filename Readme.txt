FreeAtHome KNX VirtualSwitch and Communication module. This software
provides interaction over KNX to Free@Home bus devices.

This software is not created, maintained or has any assosiation
with ABB \ Busch-Jeager.

Copyright (C) 2020 Roeland Kluit - v0.1 Dec2020

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

The various projects are an implementation to enable Free@home communication, that uses KNX as transport layer.
I have reversed the majority of messages, however, the binary representation of the device configuration to channels and properties is not implemented.
When learning a new device, you have to manualy specify the Actor and Sensor channels in the JSON files.

KNXUartModule			-	Enables communication with the KNX(F@H) bus using a TinySerial 810 from Weinzierl
KNXBaseTypes			-	KNX layer classes and objects
FaHPayloadInterpeters	-	Classes to read and create the F@H messages (from the KNX data)
FaHDeviceObject			-	Class representation of a F@H device, used to Learn or Emulate a F@H device
FreeAtHomeMonitor		-	Class to monitor bus messages and replay bus traffic (used for reverse enginering and testing)
FaHDeviceLearner		-	Searches for other device (first to respond to discovery) on the bus and retrieves all the properties and fields
FahDeviceEmulator		-	Emulates a F@H device
								Use as input previously learned devices.
								Configuration using F@H SysAp
						-	FaHGroupMonitor
								Used to monitor value changes for specific GroupValues, group values to be monitored have to be specified manually.
FaHConnector			-	Test Application to test and interact with the F@H bus.

* The Switch 2_2-v2.1506.json file contains a 2\2 Sensor Actor device that can be used in the emulator.
* I used the learner only on a dedicated bus (1 device, 1 powersupply and the tinyserial) not sure what the SysAp thinks of it when you are using it on a fully equiped bus
* When learning a device, make sure it is EMPTY\RESET to FACTORTY DEFAULTS. Otherwise the template will have configuration settings and will not function properly when emulating
* All code is provided AS-IS, All modules except for the FaHDeviceLearner have been tested on a fully equiped bus with over 30 devices
