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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static FreeAtHomeDevices.FaHDeviceProperties;
using static FreeAtHomeDevices.FaHSerializationhexHelper;

namespace FreeAtHomeDevices
{
    public class FaHDevice
    {
        private List<KNXAddress> GroupValueAddresses = null;
        [JsonProperty]
        public KNXAddress KnxAddress { private set; get; } = new KNXAddress();
        [JsonProperty]
        public FaHDeviceAddress FaHAddress = new FaHDeviceAddress();
        public KNXAddress FaHSceneGroupValueAddress = new KNXAddress(60927);
        public FaHDeviceType DeviceType;
        public KNXu16SimpleStruct ConsistancyValue = new KNXu16SimpleStruct();
        public string Description;
        public FahSystemID SystemID { private set; get; } = new FahSystemID();
        public byte[] BasicDeviceInformation = null; //Make Specific into class-casting?
        public byte[] DeviceParameterInfo = null; //Make Specific into class-casting?        
        public FahDeviceHealth DeviceHealthStatus = new FahDeviceHealth();
        private uint DeviceChannelCount;
        [JsonProperty]
        private FaHDeviceChannel[] Channels;
        public string AutosaveFilename = "";
        [JsonProperty]
        public DateTime LastWriteTime { private set; get; }
        [JsonProperty]
        public double FahClassVersion { private set; get; } = 0.0;

        public delegate void EventOnGroupWriteEvent(FaHDevice caller, KNXAddress TargetGroupValue, byte[] data);
        public delegate void EventOnActorChange(FaHDevice caller, SensorActorInterfaceType SensorActor, UInt16 state);
        public event EventOnGroupWriteEvent OnGroupWriteEvent;
        public event EventOnGroupWriteEvent OnGroupWriteSceneEvent;
        public event EventOnActorChange OnActorChange;

        public void IncrementRebootCount()
        {
            DeviceHealthStatus.DeviceReboots++;
        }

        private void AutoSave()
        {
            if (AutosaveFilename != "")
            {
                try
                {
                    Serialize(AutosaveFilename);
                }
                catch { }
            }
        }

        private void ProcessSceneGroupWrite(UInt16 SceneID)
        {
            foreach(var Chan in Channels)
            {
                if (Chan != null && Chan.Properties[2] != null)
                {
                    //Might be more dynamic than always channel 2
                    if (Chan.Properties[2].channelType == ChannelType.chanInputActorGroupMessage)
                    {
                        //Might be more dynamic than always property 5
                        foreach (var data in Chan.Properties[5].PropertyData)
                        {
                            if (data != null)
                            {
                                byte Scene = data.data[1];
                                byte Value = data.data[2];
                                if (Scene < 0x40) //over 64 seems to be unsupported
                                {                                    
                                    if(Scene == SceneID)
                                    {
                                        //Console.WriteLine("Scene Execute: " + Scene + " value: " + Value);
                                        OnGroupWriteEvent?.Invoke(this, Chan.Properties[3].ChannelAdresses[1].GroupValueAddress.ElementAt(0).GetAsReversed(), new byte[] { Value });
                                        OnActorChange?.Invoke(this, Chan.Properties[2].ActorSensorIndex, Value);                                        
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ProccessGroupWrite(KNXAddress GroupValue, byte[] data)
        {
            try
            {
                if (GroupValue == FaHSceneGroupValueAddress.GetAsReversed())
                {
                    ProcessSceneGroupWrite(data[0]);
                    return;
                }
                else
                {

                    //Console.WriteLine(GroupValue);
                    int ChannelID = -1;
                    int PropertyID = -1;

                    //check if it is GroupValueWrite we have to act upon
                    if (GetGroupValueEntry(GroupValue, ref ChannelID, ref PropertyID, ChannelType.chanInputActorGroupMessage))
                    {
                        var OutChan = Channels[ChannelID].Properties[PropertyID + 1];
                        if (OutChan != null)
                        {
                            if (OutChan.channelType == FaHDeviceProperties.ChannelType.chanOutputActorChangedValue)
                            {
                                //WritePropertyValue(ChannelID, PropertyID, 1, data);
                                OnGroupWriteEvent?.Invoke(this, OutChan.ChannelAdresses[1].GroupValueAddress.ElementAt(0).GetAsReversed(), data);
                                OnActorChange?.Invoke(this, OutChan.ActorSensorIndex, data[0]);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot parse: " + e);
            }
        }

        public KNXu16SimpleStruct GenerateNewConsistancyTag()
        {
            Random rand = new Random();
            byte[] newRand = new byte[2];
            rand.NextBytes(newRand);
            ConsistancyValue = new KNXu16SimpleStruct(newRand);
            AutoSave();
            return ConsistancyValue;
        }

        public bool RemoveChannelConnection(int ChannelIndex, int propIndex, byte connectionIndex, KNXAddress GroupAdress, out bool moreIndices)
        {            
            moreIndices = false;
            try
            {
                var ChannelAdresses = this.Channels[ChannelIndex].Properties[propIndex].ChannelAdresses[connectionIndex];
                if (ChannelAdresses == null)
                {
                    return false;
                }
                foreach(var Channel in ChannelAdresses.GroupValueAddress)
                {
                    if(Channel == GroupAdress)
                    {
                        ChannelAdresses.GroupValueAddress.Remove(Channel);
                        moreIndices = (this.Channels[ChannelIndex].Properties.Length > propIndex);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public FaHDevice()
        {
            SystemID = new FahSystemID(0xff, 0xff);
            DeviceType = FaHDeviceType.TypeNotDefined;

            Random rnd = new Random();
            Byte[] rndConsitancyID = new Byte[2];
            rnd.NextBytes(rndConsitancyID);
            ConsistancyValue = new KNXu16SimpleStruct(rndConsitancyID);
        }

        public void SetAddressInformation(KNXAddress Address, FahSystemID fahSystemID)
        {
            if (this.KnxAddress != Address || fahSystemID != SystemID)
            {
                this.KnxAddress = Address;
                this.SystemID = fahSystemID;
            }
        }

        public bool ButtonClick(SensorActorInterfaceType ActorSensorIndex, byte value)
        {
            KNXAddress knxAddress = GetGroupValueForChannelType(ActorSensorIndex, value);
            if(knxAddress!=null)
            {
                OnGroupWriteEvent?.Invoke(this, knxAddress, new byte[] { value });
            }
            return false;
        }

        public void WriteChannelPropertyType(int ChannelIndex, int Property, FaHDeviceProperties.ChannelType channelType, SensorActorInterfaceType ActorSensorIndex)
        {
            Channels[ChannelIndex].Properties[Property].channelType = channelType;
            Channels[ChannelIndex].Properties[Property].ActorSensorIndex = ActorSensorIndex;
        }

        public FaHDeviceProperties.ChannelType GetChannelPropertyType(int ChannelIndex, int Property, out SensorActorInterfaceType ActorSensorIndex)
        {
            ActorSensorIndex = Channels[ChannelIndex].Properties[Property].ActorSensorIndex;
            return Channels[ChannelIndex].Properties[Property].channelType;
        }

        public void WriteChannelInfo(int ChannelIndex, byte[] data)
        {
            EnsureChannelExist(ChannelIndex);
            Channels[ChannelIndex].DeviceChannelInfo = data;
        }

        public void WriteChannelIndentifier(int ChannelIndex, KNXu16SimpleStruct data)
        {
            EnsureChannelExist(ChannelIndex);
            Channels[ChannelIndex].ChannelIdentifier = data;
        }

        public bool ReadChannelIndentifier(int ChannelIndex, out KNXu16SimpleStruct data)
        {
            data = null;
            try
            {
                data = Channels[ChannelIndex].ChannelIdentifier;
                if (data == null) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ReadChannelInfo(int ChannelIndex, out byte[] data)
        {
            try
            {
                data = Channels[ChannelIndex].DeviceChannelInfo;
                if (data == null)
                    return false;
                return true;
            }
            catch
            {
                data = null;
                return false;
            }
        }

        public bool ReadDescriptorValue(int ChannelIndex, int propIndex, out byte[] DescriptorData, out bool moreIndices)
        {
            moreIndices = false;
            DescriptorData = null;

            try
            {
                DescriptorData = Channels[ChannelIndex].Properties[propIndex].DescriptorInfo;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void WriteDescriptorValue(int ChannelIndex, int propIndex, byte[] propertyData)
        {
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            Channels[ChannelIndex].Properties[propIndex].DescriptorInfo = propertyData;
        }

        public bool ReadPropertyValue(int ChannelIndex, int propIndex, int FieldIndex, out byte[] propertyData, out bool moreIndices)
        {
            moreIndices = false;
            propertyData = null;

            try
            {
                propertyData = Channels[ChannelIndex].Properties[propIndex].PropertyData[FieldIndex].data;
                if (Channels[ChannelIndex].Properties[propIndex].PropertyData.Length != FieldIndex + 1)
                    moreIndices = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool WritePropertyValue(int ChannelIndex, int propIndex, int FieldID, byte[] propertyData)
        {
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            EnsurePropertyFieldExist(ChannelIndex, propIndex, FieldID);
            Channels[ChannelIndex].Properties[propIndex].PropertyData[FieldID].data = propertyData;

            //Check if value is set via Application\Web
            if(FieldID == 1 && Channels[ChannelIndex].Properties[propIndex].channelType == ChannelType.chanInputActorGroupMessage)
            {
                var OutChan = Channels[ChannelIndex].Properties[propIndex + 1];
                if (OutChan != null)
                {
                    if (OutChan.channelType == FaHDeviceProperties.ChannelType.chanOutputActorChangedValue)
                    {
                        if (propertyData.Length != 2)
                        {
                            Console.WriteLine("Not Implemented!");
                        }
                        else
                        {
                            //Console.WriteLine("SendGroupValueWritePropVal:" + OutChan.ChannelAdresses[1].GroupValueAddress.ElementAt(0).GetAsReversed().ToString());
                            OnGroupWriteEvent?.Invoke(this, OutChan.ChannelAdresses[1].GroupValueAddress.ElementAt(0).GetAsReversed(), new byte[] { propertyData[1] });
                            OnActorChange?.Invoke(this, OutChan.ActorSensorIndex, propertyData[1]);
                        }
                    }
                }
            }

            if(Channels[ChannelIndex].Properties[propIndex].PropertyData.Length == FieldID - 1)
            {
                return false;
            }
            return true;
        }

        public void WriteRoomInfo(int ChannelIndex, int propIndex, ushort RoomID, ushort X, ushort Y)
        {
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            Channels[ChannelIndex].Properties[propIndex].RoomID = RoomID;
            Channels[ChannelIndex].Properties[propIndex].X = X;
            Channels[ChannelIndex].Properties[propIndex].Y = Y;
        }

        public void ReadRoomInfo(int ChannelIndex, int propIndex, out ushort RoomID, out ushort X, out ushort Y)
        {
            RoomID = 0xFF;
            X = 0xFF;
            Y = 0XFF;
            try
            {
                RoomID = (ushort)Channels[ChannelIndex].Properties[propIndex].RoomID;
                X = (ushort)Channels[ChannelIndex].Properties[propIndex].X;
                Y = (ushort)Channels[ChannelIndex].Properties[propIndex].Y;
            }
            catch { }
        }

        public void WriteChannelDescription(int ChannelIndex, int propIndex, string Description)
        {
            EnsureChannelExist(ChannelIndex);
            Channels[ChannelIndex].Description = Description;
        }

        public void ReadChannelDescription(int ChannelIndex, int propIndex, out string Description)
        {
            Description = "";
            try
            {
                Description = Channels[ChannelIndex].Description;
            }
            catch { }
            if (Description == null)
                Description = "";
        }

        public void WriteIconInfo(int ChannelIndex, int propIndex, ushort IconID)
        {
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            Channels[ChannelIndex].Properties[propIndex].IconId = IconID;
        }

        public ushort ReadIconInfo(int ChannelIndex, int propIndex)
        {
            try
            {
                return (ushort)Channels[ChannelIndex].Properties[propIndex].IconId;
            }
            catch { }
            return 0xff; 
        }


        public bool ReadConnectionValue(int ChannelIndex, int propIndex, ushort FieldID, out byte secondField, out KNXAddress[] GroupValueAddress, out bool MoreIndices)
        {
            GroupValueAddress = null;
            //AdditionalData = null;
            secondField = 0;
            MoreIndices = false;
            try
            {
                if (Channels[ChannelIndex].Properties[propIndex].ChannelAdresses[FieldID] != null)
                {
                    var t = Channels[ChannelIndex].Properties[propIndex].ChannelAdresses[FieldID];
                    secondField = t.ChannelParameter;
                    GroupValueAddress = t.GroupValueAddress.ToArray();
                    //AdditionalData = t.AdditionalData;
                }
                else
                {
                    GroupValueAddress = null;// new KNXAddress[] { new KNXAddress(0) };
                    //AdditionalData = null;
                    return false;
                }
                if (Channels[ChannelIndex].Properties[propIndex].ChannelAdresses.Length != FieldID)
                {
                    MoreIndices = true;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void WritePropertyMoreIncides(int ChannelIndex, int propIndex, int FieldID)
        {
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            EnsurePropertyFieldExist(ChannelIndex, propIndex, FieldID);
        }


        public void WriteConnectionMoreIncides(int ChannelIndex, int propIndex, ushort FieldID)
        {
            //Ensure Channel Connection + 1 exist, for the more indices.
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            EnsureChannelConnectionExist(ChannelIndex, propIndex, FieldID);
        }

        public void WriteConnectionValue(int ChannelIndex, int propIndex, ushort FieldID, byte ChannelParameter, KNXAddress[] GroupValueAddress, out bool MoreIndices)
        {
            MoreIndices = false;
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            EnsureChannelConnectionExist(ChannelIndex, propIndex, FieldID);

            Channels[ChannelIndex].Properties[propIndex].ChannelAdresses[FieldID].ChannelParameter = ChannelParameter;

            if(Channels[ChannelIndex].Properties[propIndex].ChannelAdresses[FieldID].GroupValueAddress==null)
            {
                Channels[ChannelIndex].Properties[propIndex].ChannelAdresses[FieldID].GroupValueAddress = new HashSet<KNXAddress>();
            }

            if (GroupValueAddress != null)
            {
                Channels[ChannelIndex].Properties[propIndex].ChannelAdresses[FieldID].GroupValueAddress.UnionWith(GroupValueAddress);
                GroupValueAddresses = null;
            }

            if (Channels[ChannelIndex].Properties[propIndex].ChannelAdresses.Length != FieldID)
            {
                MoreIndices = true;
            }
        }

        public bool ReadFunctionList(int ChannelIndex, int propIndex, int FieldIndex, out byte[] OIDdata, out bool MoreIndices)
        {
            MoreIndices = false;
            OIDdata = null;
            try
            {
                OIDdata = Channels[ChannelIndex].Properties[propIndex].FunctionList[FieldIndex].data;
                if (Channels[ChannelIndex].Properties[propIndex].FunctionList.Length == FieldIndex + 1)
                    MoreIndices = false;
                else
                    MoreIndices = true;
                return true;
            }catch
            { return false; }
        }

        public bool WriteFunctionList(int ChannelIndex, int propIndex, int FieldIndex, byte[] OIDdata)
        {
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            EnsureFunctionListFieldExist(ChannelIndex, propIndex, FieldIndex);
            Channels[ChannelIndex].Properties[propIndex].FunctionList[FieldIndex].data = OIDdata;
            if (Channels[ChannelIndex].Properties[propIndex].FunctionList.Length == FieldIndex + 1)
            {
                return false;
            }
            return true;
        }

        public bool ReadOIDData(int ChannelIndex, int propIndex, int FieldIndex, out byte[] OIDdata, out bool moreIndices)
        {
            moreIndices = false;
            OIDdata = null;

            try
            {
                OIDdata = Channels[ChannelIndex].Properties[propIndex].IndicedOIDData[FieldIndex].Data;
                if (Channels[ChannelIndex].Properties[propIndex].IndicedOIDData.Length != FieldIndex + 1)
                    moreIndices = true;
                return true;
            }
            catch
            {
                return false;
            }
        }


        public bool WriteOIDData(int ChannelIndex, int propIndex, int FieldIndex, byte[] OIDdata)
        {
            EnsureChannelExist(ChannelIndex);
            EnsurePropertyExist(ChannelIndex, propIndex);
            EnsureOIDExist(ChannelIndex, propIndex, FieldIndex);
            Channels[ChannelIndex].Properties[propIndex].IndicedOIDData[FieldIndex].Data = OIDdata;
            if (Channels[ChannelIndex].Properties[propIndex].IndicedOIDData[FieldIndex].Data.Length == FieldIndex + 1)
            {
                return false;
            }
            return true;
        }

        private void EnsurePropertyFieldExist(int ChannelIndex, int propIndex, int FieldIndex)
        {
            var Field = Channels[ChannelIndex].Properties[propIndex];

            if (Field.PropertyData == null)
            {
                Field.PropertyData = new FaHDevicePropertyData[FieldIndex + 1];
            }
            else if (Field.PropertyData.Length < FieldIndex + 1)
            {
                Array.Resize(ref Field.PropertyData, FieldIndex + 1);
            }

            if (Field.PropertyData[FieldIndex] == null)
            {
                Field.PropertyData[FieldIndex] = new FaHDevicePropertyData();
                UpdateCounters();
            }
        }

        private void EnsureFunctionListFieldExist(int ChannelIndex, int propIndex, int FieldIndex)
        {
            var FunctionList = Channels[ChannelIndex].Properties[propIndex];

            if (FunctionList.FunctionList == null)
            {
                FunctionList.FunctionList = new FaHDevicePropertyData[FieldIndex + 1];
            }
            else if (FunctionList.FunctionList.Length < FieldIndex + 1)
            {
                Array.Resize(ref FunctionList.FunctionList, FieldIndex + 1);
            }

            if (FunctionList.FunctionList[FieldIndex] == null)
            {
                FunctionList.FunctionList[FieldIndex] = new FaHDevicePropertyData();
                UpdateCounters();
            }
        }

        private void EnsureOIDExist(int ChannelIndex, int propIndex, int oidIndex)
        {            
            var Field = Channels[ChannelIndex].Properties[propIndex];

            if (Field.IndicedOIDData == null)
            {
                Field.IndicedOIDData = new FahDeviceOIDIndicedData[oidIndex + 1];
            }
            else if (Field.IndicedOIDData.Length < oidIndex + 1)
            {
                Array.Resize(ref Field.IndicedOIDData, oidIndex + 1);
            }

            if (Field.IndicedOIDData[oidIndex] == null)
            {
                Field.IndicedOIDData[oidIndex] = new FahDeviceOIDIndicedData();
            }
        }

        private void EnsureChannelConnectionExist(int ChannelIndex, int propIndex, int channelIndex)
        {
            var Field = Channels[ChannelIndex].Properties[propIndex];

            if (Field.ChannelAdresses == null)
            {
                Field.ChannelAdresses = new FaHChannelAdressInformation[channelIndex + 1];
            }
            else if (Field.ChannelAdresses.Length < channelIndex + 1)
            {
                Array.Resize(ref Field.ChannelAdresses, channelIndex + 1);
            }

            if (Field.ChannelAdresses[channelIndex] == null)
            {
                Field.ChannelAdresses[channelIndex] = new FaHChannelAdressInformation();
            }
        }

        private void EnsurePropertyExist(int ChannelIndex, int propIndex)
        {
            var Channel = Channels[ChannelIndex];

            if (Channel.Properties == null)
            {
                Channel.Properties = new FaHDeviceProperties[propIndex + 1];
                Channel.Properties[0] = new FaHDeviceProperties();
            }
            else if (Channel.Properties.Length < propIndex + 1)
            {
                Array.Resize(ref Channel.Properties, propIndex + 1);
            }

            if (Channel.Properties[propIndex] == null)
            {
                Channel.Properties[propIndex] = new FaHDeviceProperties();
                UpdateCounters();
            }
        }

        private void EnsureChannelExist(int ChannelIndex)
        {
            if (Channels == null)
            {
                Channels = new FaHDeviceChannel[ChannelIndex + 1];
                Channels[0] = new FaHDeviceChannel();
            }
            else if(Channels.Length < ChannelIndex + 1)
            {
                Array.Resize(ref Channels, ChannelIndex + 1);
            }

            if(Channels[ChannelIndex] == null)
            {
                Channels[ChannelIndex] = new FaHDeviceChannel();
                UpdateCounters();
            }
        }

        public void UpdateCounters()
        {
            int i = -1;
            if (this.Channels != null)
            {
                foreach (var a in this.Channels)
                {
                    i++;
                    if (a != null)
                    {
                        a.ChannelIndex = i;

                        int j = -1;
                        if (a.Properties != null)
                        {
                            foreach (var b in a.Properties)
                            {
                                j++;
                                if (b != null)
                                {
                                    int k = -1;
                                    b.PropertyIndex = j;
                                    if (b.PropertyData != null)
                                    {
                                        foreach (var c in b.PropertyData)
                                        {
                                            k++;
                                            if (c != null)
                                            {
                                                c.index = k;
                                            }
                                        }
                                    }

                                    k = -1;
                                    if (b.FunctionList != null)
                                    {
                                        foreach (var c in b.FunctionList)
                                        {
                                            k++;
                                            if (c != null)
                                            {
                                                c.index = k;
                                            }
                                        }
                                    }

                                    k = -1;
                                    if (b.IndicedOIDData != null)
                                    {
                                        foreach (var c in b.IndicedOIDData)
                                        {
                                            k++;
                                            if (c != null)
                                            {
                                                c.Index = k;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Serialize(string fileName)
        {
            UpdateCounters();
            using (StreamWriter wr = new StreamWriter(fileName, false))
            {
                this.LastWriteTime = DateTime.UtcNow;
                this.FahClassVersion = 2.0;
                var json = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                json.Converters.Add(new BytesToHexConverter());
                json.NullValueHandling = NullValueHandling.Ignore;
                json.Serialize(wr, this);
            }
        }

        private KNXAddress GetGroupValueForChannelType(SensorActorInterfaceType sensorActorInterfaceType, UInt16 Value)
        {
            if (Channels != null)
            {
                int chanID = 0;
                foreach (var chan in Channels)
                {
                    if (chan != null)
                    {
                        int propID = 0;
                        foreach (var prop in chan.Properties)
                        {
                            if (prop != null && prop.ChannelAdresses != null)
                            {
                                if (prop.ActorSensorIndex == sensorActorInterfaceType)
                                {
                                    var chanaddr = prop.ChannelAdresses[1];
                                    if (chanaddr != null && chanaddr.GroupValueAddress.Count != 0)
                                    {
                                        return chanaddr.GroupValueAddress.First().GetAsReversed();
                                    }
                                    else
                                    {
                                        if (chan.Properties[5] != null)
                                        {
                                            if (chan.Properties[5].PropertyData[1] != null)
                                            {
                                                //Get Scene information for button Index (1 or 2) --> Value 0\1
                                                bool mIndice;
                                                byte[] propData;
                                                if (ReadPropertyValue(chan.ChannelIndex, 5, Value + 1, out propData, out mIndice))
                                                {
                                                    if (propData[1] < 0x40)
                                                    {
                                                        //Found get Scene info
                                                        //Console.WriteLine(propData[1]);
                                                        //Console.Write(chan.Properties[5].PropertyData[1].data[Value]);
                                                        //TOdo Fix send scene
                                                        OnGroupWriteSceneEvent?.Invoke(this, this.FaHSceneGroupValueAddress.GetAsReversed(), new byte[] { propData[1] });
                                                    }
                                                }
                                            }
                                        }
                                        Console.WriteLine();
                                    }
                                }
                            }
                            propID++;
                        }
                    }
                    chanID++;
                }
            }
            return null;
        }

        private bool GetGroupValueEntry(KNXAddress GroupValue, ref int ChannelID, ref int PropertyID, ChannelType channeltype)
        {
            if (Channels != null)
            {
                int chanID = 0;
                foreach (var chan in Channels)
                {
                    if (chan != null)
                    {
                        int propID = 0;
                        foreach (var prop in chan.Properties)
                        {
                            if (prop != null && prop.ChannelAdresses != null)
                            {
                                if(prop.channelType == channeltype)
                                { 
                                foreach (var chanaddr in prop.ChannelAdresses)
                                {
                                        if (chanaddr != null && chanaddr.GroupValueAddress != null)
                                        {
                                            foreach (var knxGroupValueAddresses in chanaddr.GroupValueAddress)
                                            {
                                                if (knxGroupValueAddresses != null)
                                                {
                                                    if (GroupValue == knxGroupValueAddresses.GetAsReversed())
                                                    {
                                                        ChannelID = chanID;
                                                        PropertyID = propID;
                                                        return true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            propID++;
                        }
                    }
                    chanID++;
                }
            }
            return false;
        }

        [JsonIgnore]
        public KNXAddress[] GroupValueAdresses
        {
            get
            {
                if (GroupValueAddresses != null)
                    return GroupValueAddresses.ToArray();
                else
                {
                    GroupValueAddresses = new List<KNXAddress>();
                    if (Channels != null)
                    {
                        foreach (var chan in Channels)
                        {
                            if (chan != null)
                            {
                                foreach (var prop in chan.Properties)
                                {
                                    if (prop != null && prop.ChannelAdresses != null)
                                    {
                                        foreach (var chanaddr in prop.ChannelAdresses)
                                        {
                                            if (chanaddr != null && chanaddr.GroupValueAddress != null)
                                            {
                                                foreach (var knxGroupValueAddresses in chanaddr.GroupValueAddress)
                                                {
                                                    if (knxGroupValueAddresses != null)
                                                    {
                                                        if (!GroupValueAddresses.Contains(knxGroupValueAddresses.GetAsReversed()))
                                                            GroupValueAddresses.Add(knxGroupValueAddresses.GetAsReversed());
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return GroupValueAddresses.ToArray();
                    }                    
                }
                return null;
            }            
        }

        public static FaHDevice DeserializeFromFile(string fileName, bool CreateIfNotExist = false)
        {
            try
            {
                using (TextReader tr = new StreamReader(fileName))
                {
                    var json = new JsonSerializer();
                    json.Converters.Add(new BytesToHexConverter());
                    //var sr = new StringReader(json);
                    FaHDevice d = (FaHDevice)json.Deserialize(tr, typeof(FaHDevice));
                    if(d.FahClassVersion != 2.0)
                    {
                        throw new Exception("Config File version mismatch, expecting 2.0 got " + d.FahClassVersion);
                    }
                    if (d == null)
                        throw new InvalidDataException();
                    return d;
                    //string strDevices = tr.ReadToEnd();
                    //return JsonConvert.DeserializeObject<FreeAtHomeDevices>(strDevices);
                }
            }
            catch
            {
                if (CreateIfNotExist)
                {
                    return new FaHDevice();
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Warning: setting channelcount deletes existing values!
        /// </summary>
        ///
        public uint ChannelCount
        {
            get
            {
                return DeviceChannelCount;
            }
            set
            {
                for (int i = 0; i <= value; i++)
                {
                    EnsureChannelExist(i);
                }
                DeviceChannelCount = value; 
            }
        }

        /*
         * 
        [JsonIgnore]
        public uint ChannelCount
        {
            get
            {                
                if (Channels != null)
                {
                    uint count = 0;
                    foreach(var channel in Channels)
                    {
                        if (channel != null && channel.ChannelIdentifier != null && channel.ChannelIdentifier.u16value != 0)
                            count++;
                    }
                    return count;
                    //uint cCount = 0;
                    return (uint)(Channels.Length - 1);
                }
                else
                    return 0;
            }
            set
            {
                for(int i = 0; i <= value; i++)
                {
                    EnsureChannelExist(i);
                }
                
            }
        }*/
    }
}
