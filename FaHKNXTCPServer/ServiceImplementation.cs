using KNXNetworkLayer;
using KNXUartModule;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FaHKNXTCPServer
{
    public partial class ServiceImplementation : ServiceBase
    {
        //private string WinNTServiceName;
        KNXUartConnection kNXUart;
        TCPknxServer tCPknxServer;

        /*public ServiceImplementation()
        {
            
        }*/

        private void CreateServer(string ComPort, uint TCPPort)
        {

            kNXUart = new KNXUartConnection(ComPort)
            {
                AllowWrite = true
            };
            kNXUart.OnKNXMessage += KNXUart_OnKNXMessage;
            kNXUart.OnKNXEvent += KNXUart_OnKNXEvent;

            if (!kNXUart.ResetAndInit())
            {
                throw new Exception("Cannot init");
            }

            tCPknxServer = new TCPknxServer("0.0.0.0", TCPPort);
            tCPknxServer.OnKNXMessage += TCPknxServer_OnKNXMessage;
            tCPknxServer.OnKNXAddressAdd += TCPknxServer_OnKNXAddressAdd;
            tCPknxServer.OnKNXAddressRemove += TCPknxServer_OnKNXAddressRemove;
            tCPknxServer.onClientConnect += TCPknxServer_onClientConnect;
            tCPknxServer.onClientDisconnect += TCPknxServer_onClientDisconnect;
        }

        private void TCPknxServer_onClientDisconnect(TCPknxServer caller, System.Net.IPAddress ClientIPAddress)
        {
            this.EventLog.WriteEntry("KNX IP Client Disconnect Event: " + ClientIPAddress.ToString());
        }

        private void TCPknxServer_onClientConnect(TCPknxServer caller, System.Net.IPAddress ClientIPAddress)
        {
            this.EventLog.WriteEntry("KNX IP Client Connect Event: " + ClientIPAddress.ToString());
        }

        private void KNXUart_OnKNXEvent(KNXNetworkLayerTemplate caller, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent)
        {
            this.EventLog.WriteEntry("KNX Service Event: " + uartEvent.ToString());
        }

        public ServiceImplementation(string WinNTServiceName)
        {
            InitializeComponent();
            //WinNTServiceName = ServiceName;
            this.ServiceName = WinNTServiceName;
        }

        internal void CommandLineStart()
        {            
            OnStart(new string[0]);
        }

        internal void CommandLineStop()
        {
            OnStop();
        }

        static public string CreateEventSource(string currentAppName)
        {
            string eventSource = currentAppName;
            bool sourceExists;
            try
            {
                // searching the source throws a security exception ONLY if not exists!
                sourceExists = EventLog.SourceExists(eventSource);
                if (!sourceExists)
                {   // no exception until yet means the user as admin privilege
                    EventLog.CreateEventSource(eventSource, "Application");
                }
            }
            catch (System.Security.SecurityException)
            {
                eventSource = "Application";
            }

            return eventSource;
        }

        protected override void OnStart(string[] args)
        {
            String COM = WINAPI_ServiceManager.GetStringParameterValue(ServiceName, "ComPort", "COM9");
            uint IPPORT = WINAPI_ServiceManager.GetDWORDParameterValue(ServiceName, "TCPPort", 9998);

            CreateEventSource(ServiceName);
            this.EventLog.Source = this.ServiceName;
            this.EventLog.WriteEntry("Service starting using ComPort: " + COM + " and IP Port: " + IPPORT, EventLogEntryType.Information, 100);

            if (COM=="" || IPPORT == 0)
            {
                throw new Exception("Parameters not configured");
            }

            CreateServer(COM, IPPORT);
            this.EventLog.WriteEntry("Service started using ComPort: " + COM + " and IP Port: " + IPPORT);

            //File.WriteAllText("C:\\Storage\\test.txt", ServiceName + COM + IPPORT);            
        }

        private void TCPknxServer_OnKNXAddressRemove(TCPknxServer caller, KNXBaseTypes.KNXAddress Address)
        {
            kNXUart.RemoveKNXAddressToAck(Address);
        }

        private void TCPknxServer_OnKNXAddressAdd(TCPknxServer caller, KNXBaseTypes.KNXAddress Address)
        {
            kNXUart.AddKNXAddressToAck(Address);
        }

        private void TCPknxServer_OnKNXMessage(TCPknxServer caller, KNXBaseTypes.KNXmessage Message)
        {
            Console.WriteLine("KNXTCPData");
            kNXUart.SendKNXMessage(Message);
        }

        private void KNXUart_OnKNXMessage(KNXNetworkLayerTemplate caller, KNXBaseTypes.KNXmessage Message, KNXNetworkLayerTemplate.KnxPacketEvents uartEvent)
        {
            Console.WriteLine("KNXLineData");
            tCPknxServer.SendKNXMessage(Message.toByteArray());
        }

        protected override void OnStop()
        {
            if(tCPknxServer!=null)
            {
                tCPknxServer.Dispose();
                tCPknxServer = null;
            }
            if (kNXUart != null)
            {
                kNXUart.Close();
                kNXUart = null;
            }
        }
    }
}
