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

    This modules can be used to monitor KNX FreeAtHome messages.    
*/
using KNXBaseTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAtHomeKNX
{
    public class ReadWriteKNXDataLog
    {
        private StreamWriter fReg;
        private string name;
        private bool write = false;
        private TextReader fRead;

        public ReadWriteKNXDataLog(string Filename, bool isWriteMode)
        {
            try
            {
                write = isWriteMode;
                name = Filename;
                if (isWriteMode)
                {

                    fReg = File.CreateText(Filename);
                }
                else
                {
                    fRead = File.OpenText(Filename);
                }
            }
            catch { }
        }

        public KNXmessage ReadNextMessage()
        {
            try
            {
                if (write)
                {
                    throw new InvalidOperationException();
                }
                string line = fRead.ReadLine();

                if (line == null)
                    return null;

                if (line.StartsWith("#"))
                {
                    return ReadNextMessage();
                }

                string[] data = line.Split(';');

                //Data
                string[] payload = data[1].Trim().Split(',');
                byte[] btdata = new byte[payload.Length];
                int i = 0;
                foreach (string s in payload)
                {
                    btdata[i] = Convert.ToByte(s.Trim().Substring(2), 16);
                    i++;
                }
                return KNXmessage.fromByteArray(btdata, DateTime.Parse(data[0]));
            }
            catch
            {
                return null;
            }
        }

        public void WriteComment(string Comment)
        {
            if (!write)
            {
                throw new InvalidOperationException();
            }
            try
            {

                string str = string.Format("#{0}", Comment);
                if (!string.IsNullOrEmpty(str))
                {
                    fReg.WriteLine(str);
                    fReg.Flush();
                }
            }
            catch { }

        }

        public void WriteOut(KNXmessage Message)
        {
            if(!write)
            {
                throw new InvalidOperationException();
            }
            try
            {
                string dtFormat = Message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");

                string str = string.Format("{0}; {1}", dtFormat, KNXHelpers.GetStringHex(Message.toByteArray()));
                if (!string.IsNullOrEmpty(str))
                {
                    fReg.WriteLine(str);
                    fReg.Flush();
                }
            }
            catch { }
        }
    }
}
