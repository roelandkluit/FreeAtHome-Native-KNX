/*
 *  FreeAtHome KNX VirtualSwitch and Communication module. This software
    provides interaction over KNX to Free@Home bus devices.

    This software is not created, maintained or has any assosiation
    with ABB \ Busch-Jeager.

    Copyright (C) 2020-2021 Roeland Kluit

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
*/
using FAHPayloadInterpeters;
using FaHTCPClient;
using KNXBaseTypes;
using KNXNetworkLayer;
using KNXUartModule;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirtualFahDevice;

namespace FaHConnector
{
    public partial class frmFaHConnector : Form
    {
        KNXNetworkLayerTemplate kNXUart;
        FaHVirtualDevice fahABB7001;
        FaHGroupMonitor fah0xf80x83;
        FaHGroupMonitor fah0x940x20; //Lamp plafond
        FaHGroupMonitor fah0xE00x31; //Lamp spiegel
        //FaHVirtualDevice fahABB7002;

        public frmFaHConnector()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            /*
            KNXUartConnection rkNXUart  = new KNXUartConnection(AppSettings.Default.ComPort)
            {
                AllowWrite = true
            };
            if (!rkNXUart.ResetAndInit())
            {
                throw new Exception("Cannot init");
            }
            kNXUart = rkNXUart;
            */
            TCPknxClient tCPknxClient = new TCPknxClient("172.16.16.20", 9998);
            kNXUart = tCPknxClient;

            fahABB7001 = new FaHVirtualDevice(kNXUart, "ABB700C00001");
            fahABB7001.ConsolePrintMessages = true;
            //fahABB7002 = new FaHVirtualDevice(kNXUart, "ABB700C00002");
            fah0xf80x83 = new FaHGroupMonitor(kNXUart, new KNXAddress(0xf8, 0x83));
            fah0x940x20 = new FaHGroupMonitor(kNXUart, new KNXAddress(0x94, 0x20));
            fah0xE00x31 = new FaHGroupMonitor(kNXUart, new KNXAddress(0xE0, 0x31));
            //fah0x550x13 = new FaHGroupMonitor(kNXUart, new KNXAddress(21779));
            fah0xf80x83.OnGroupValueChange += Group_OnGroupValueChange;
            fah0x940x20.OnGroupValueChange += Group_OnGroupValueChange;
            fah0xE00x31.OnGroupValueChange += Group_OnGroupValueChange;

            fahABB7001.OnActorChange += Switch_OnActorChange;
            //fahABB7002.OnActorChange += Switch_OnActorChange;            

            fahABB7001.StartFaHDevice();            

            
            //fahABB7002.StartFaHDevice();
        }

        private void Group_OnGroupValueChange(FaHGroupMonitor caller, byte[] data)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<FaHGroupMonitor, byte[]>(Group_OnGroupValueChange), new object[] { caller, data });
            }
            else
            {
                if (caller == fah0xf80x83)
                {
                    textBox1.Text = BitConverter.ToString(data) + "-->" + caller.GroupValueAsDouble;
                }
                else if (caller == fah0xE00x31)
                {
                    checkBox3.Checked = caller.GroupValueAsBool;
                }
                else if (caller == fah0x940x20)
                {
                    checkBox2.Checked = caller.GroupValueAsBool;
                }
            }
        }

        private void Switch_OnActorChange(FaHVirtualDevice caller, FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType SensorActor, ushort state)
        {
            if(this.InvokeRequired)
            {
                this.Invoke(new Action<FaHVirtualDevice, FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType, ushort>(Switch_OnActorChange), new object[] { caller, SensorActor, state });
            }
            else  
            {
                if (caller == fahABB7001)
                {
                    if (SensorActor == FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.Actor1)
                    {
                        ActorAbb7001_1.Checked = state == 1;
                    }
                    if (SensorActor == FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.Actor2)
                    {
                        ActorAbb7001_2.Checked = state == 1;
                    }
                }

                Console.WriteLine("************************************************************************");
                Console.WriteLine("Switch:" + caller.FahDeviceName + " SensorActor: " + SensorActor + " State: " + state);
                Console.WriteLine("************************************************************************");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            fahABB7001.ButtonClick(FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.ButtonLeft, 1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            fahABB7001.ButtonClick(FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.ButtonLeft, 0);            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            fahABB7001.ButtonClick(FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.ButtonRight, 1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            fahABB7001.ButtonClick(FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.ButtonRight, 0);
        }

        private void checkBox1_CheckStateChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                fahABB7001.ShowBusInfo = true;
            }
            else
            {
                fahABB7001.ShowBusInfo = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var t1 = fahABB7001.GetChannelOnState(FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.Actor1);
            if (t1)
            {
                fahABB7001.DeviceConfig.SetActorChannelValue(FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.Actor1, 0);
            }
            else
            {
                fahABB7001.DeviceConfig.SetActorChannelValue(FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.Actor1, 1);
            }
            /*
            ActorAbb7001_1.Checked = t1;
            var t2 = fahABB7001.GetChannelOnState(FreeAtHomeDevices.FaHDeviceProperties.SensorActorInterfaceType.Actor2);
            ActorAbb7001_2.Checked = t2;*/
        }
    }
}
