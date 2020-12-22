using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace KNXUartModule
{
    internal class SafeSerialPort : SerialPort
    {
        private bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        public bool SafeIsOpen
        {
            get
            {
                if (IsRunningOnMono())
                    return File.Exists(base.PortName) && base.IsOpen;

                return base.IsOpen;
            }
        }

        private Stream theBaseStream;

        public SafeSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
            : base(portName, baudRate, parity, dataBits, stopBits)
        {

        }
        public SafeSerialPort(string portName, int baudRate)
            : base(portName, baudRate)
        {

        }

        public new void Open()
        {
            try
            {
                base.Open();
                theBaseStream = BaseStream;
                GC.SuppressFinalize(BaseStream);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public new void Dispose()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (base.Container != null))
            {
                base.Container.Dispose();
            }
            try
            {
                if (theBaseStream != null)
                {
                    if (theBaseStream.CanRead)
                    {
                        theBaseStream.Close();
                        GC.ReRegisterForFinalize(theBaseStream);
                    }
                }
            }
            catch
            {
                // ignore exception - bug with USB - serial adapters.
            }
            base.Dispose(disposing);
        }
    }
}

