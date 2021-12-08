using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FaHKNXTCPServer
{   
    static class Program
    {
        static string ServiceName = "KNXuartToTCPServer";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (!System.Environment.UserInteractive)
            {
                if (args.Length > 0)
                {
                    if (args[0].StartsWith("-service:"))
                    {
                        ServiceBase[] ServicesToRun;
                        ServicesToRun = new ServiceBase[]
                        {
                            new ServiceImplementation(args[0].Substring(9))
                        };
                        ServiceBase.Run(ServicesToRun);
                        return;
                    }
                }
            }
            else
            {
                if (args.Length == 1)
                {
                    if(args[0] == "-run")
                    {
                        ServiceImplementation serviceImplementation = new ServiceImplementation(ServiceName);
                        serviceImplementation.CommandLineStart();
                        Console.ReadLine();
                        serviceImplementation.CommandLineStop();
                    }
                    else if (args[0] == "-install")
                    {
                        Console.WriteLine("Installing service...");
                        if (WINAPI_ServiceManager.CreateService(ServiceName, ServiceName, Assembly.GetExecutingAssembly().Location + " -service:" + ServiceName, "KNXuart TinySerial to TCP Service", true))
                        {
                            ServiceImplementation.CreateEventSource(ServiceName);
                            WINAPI_ServiceManager.SetParameterStringValue(ServiceName, "ComPort", "COM9");
                            WINAPI_ServiceManager.SetParameterDWORDValue(ServiceName, "TCPPort", 9998);
                            Console.WriteLine("OK");
                        }
                        else
                            Console.WriteLine("Failed");
                        return;
                    }
                    else if (args[0] == "-uninstall")
                    {
                        Console.WriteLine("Uninstalling service...");
                        if (WINAPI_ServiceManager.RemoveService(ServiceName))
                            Console.WriteLine("OK");
                        else
                            Console.WriteLine("Failed");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Service Module, use -install to install and -uninstall to remove");
                }
            }
        }
    }
}
