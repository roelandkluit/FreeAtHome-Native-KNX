using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FaHKNXTCPServer
{
    class WINAPI_ServiceManager
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_DELAYED_AUTO_START_INFO
        {
            public bool fDelayedAutostart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class SERVICE_DESCRIPTION
        {
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public String lpDescription;
        }

        #region SERVICE_ACCESS
        [Flags]
        private enum SERVICE_ACCESS : uint
        {
            STANDARD_RIGHTS_REQUIRED = 0xF0000,
            SERVICE_QUERY_CONFIG = 0x00001,
            SERVICE_CHANGE_CONFIG = 0x00002,
            SERVICE_QUERY_STATUS = 0x00004,
            SERVICE_ENUMERATE_DEPENDENTS = 0x00008,
            SERVICE_START = 0x00010,
            SERVICE_STOP = 0x00020,
            SERVICE_PAUSE_CONTINUE = 0x00040,
            SERVICE_INTERROGATE = 0x00080,
            SERVICE_USER_DEFINED_CONTROL = 0x00100,
            SERVICE_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
                SERVICE_QUERY_CONFIG |
                SERVICE_CHANGE_CONFIG |
                SERVICE_QUERY_STATUS |
                SERVICE_ENUMERATE_DEPENDENTS |
                SERVICE_START |
                SERVICE_STOP |
                SERVICE_PAUSE_CONTINUE |
                SERVICE_INTERROGATE |
                SERVICE_USER_DEFINED_CONTROL)
        }
        #endregion
        #region SCM_ACCESS
        [Flags]
        private enum SCM_ACCESS : uint
        {
            STANDARD_RIGHTS_REQUIRED = 0xF0000,
            SC_MANAGER_CONNECT = 0x00001,
            SC_MANAGER_CREATE_SERVICE = 0x00002,
            SC_MANAGER_ENUMERATE_SERVICE = 0x00004,
            SC_MANAGER_LOCK = 0x00008,
            SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,
            SC_MANAGER_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
                SC_MANAGER_CONNECT |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_LOCK |
                SC_MANAGER_QUERY_LOCK_STATUS |
                SC_MANAGER_MODIFY_BOOT_CONFIG
        }
        #endregion

        /// <summary>
        /// Service types.
        /// </summary>
        [Flags]
        private enum SERVICE_TYPE : uint
        {
            /// <summary>
            /// Driver service.
            /// </summary>
            SERVICE_KERNEL_DRIVER = 0x00000001,

            /// <summary>
            /// File system driver service.
            /// </summary>
            SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,

            /// <summary>
            /// Service that runs in its own process.
            /// </summary>
            SERVICE_WIN32_OWN_PROCESS = 0x00000010,

            /// <summary>
            /// Service that shares a process with one or more other services.
            /// </summary>
            SERVICE_WIN32_SHARE_PROCESS = 0x00000020,

            /// <summary>
            /// The service can interact with the desktop.
            /// </summary>
            SERVICE_INTERACTIVE_PROCESS = 0x00000100,
        }

        /// <summary>
        /// Service start options
        /// </summary>
        private enum SERVICE_START : uint
        {
            /// <summary>
            /// A device driver started by the system loader. This value is valid
            /// only for driver services.
            /// </summary>
            SERVICE_BOOT_START = 0x00000000,

            /// <summary>
            /// A device driver started by the IoInitSystem function. This value
            /// is valid only for driver services.
            /// </summary>
            SERVICE_SYSTEM_START = 0x00000001,

            /// <summary>
            /// A service started automatically by the service control manager
            /// during system startup. For more information, see Automatically
            /// Starting Services.
            /// </summary>        
            SERVICE_AUTO_START = 0x00000002,

            /// <summary>
            /// A service started by the service control manager when a process
            /// calls the StartService function. For more information, see
            /// Starting Services on Demand.
            /// </summary>
            SERVICE_DEMAND_START = 0x00000003,

            /// <summary>
            /// A service that cannot be started. Attempts to start the service
            /// result in the error code ERROR_SERVICE_DISABLED.
            /// </summary>
            SERVICE_DISABLED = 0x00000004,
        }

        /// <summary>
        /// Severity of the error, and action taken, if this service fails
        /// to start.
        /// </summary>
        private enum SERVICE_ERROR
        {
            /// <summary>
            /// The startup program ignores the error and continues the startup
            /// operation.
            /// </summary>
            SERVICE_ERROR_IGNORE = 0x00000000,

            /// <summary>
            /// The startup program logs the error in the event log but continues
            /// the startup operation.
            /// </summary>
            SERVICE_ERROR_NORMAL = 0x00000001,

            /// <summary>
            /// The startup program logs the error in the event log. If the
            /// last-known-good configuration is being started, the startup
            /// operation continues. Otherwise, the system is restarted with
            /// the last-known-good configuration.
            /// </summary>
            SERVICE_ERROR_SEVERE = 0x00000002,

            /// <summary>
            /// The startup program logs the error in the event log, if possible.
            /// If the last-known-good configuration is being started, the startup
            /// operation fails. Otherwise, the system is restarted with the
            /// last-known good configuration.
            /// </summary>
            SERVICE_ERROR_CRITICAL = 0x00000003,
        }

        private enum SERVICE_CONFIG : int
        {
            SERVICE_CONFIG_DESCRIPTION = 1,
            SERVICE_CONFIG_FAILURE_ACTIONS = 2,
            SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 3,
            SERVICE_CONFIG_FAILURE_ACTIONS_FLAG = 4,
            SERVICE_CONFIG_SERVICE_SID_INFO = 5,
            SERVICE_CONFIG_REQUIRED_PRIVILEGES_INFO = 6,
            SERVICE_CONFIG_PRESHUTDOWN_INFO = 7,
        }

        #region DeleteService
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteService(IntPtr hService);
        #endregion
        #region OpenService
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, SERVICE_ACCESS dwDesiredAccess);
        #endregion
        #region OpenSCManager
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, SCM_ACCESS dwDesiredAccess);
        #endregion
        #region CloseServiceHandle
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);
        #endregion
        
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeServiceConfig2(IntPtr hService, int dwInfoLevel, IntPtr lpInfo);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateService(IntPtr SC_HANDLE, string lpSvcName, string lpDisplayName, uint dwDesiredAccess, uint dwServiceType, uint dwStartType, uint dwErrorControl, string lpPathName, string lpLoadOrderGroup, uint lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

        static public bool CreateService(string ServiceName, string DisplayName, string ApplicationExecpath, string Description = "", bool delaystart = true)
        {
            IntPtr schSCManager = IntPtr.Zero;
            IntPtr schService = IntPtr.Zero;
            try
            {
                schSCManager = OpenSCManager(null, null, SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
                if (schSCManager != IntPtr.Zero)
                {
                    schService = CreateService(schSCManager, ServiceName, DisplayName, (uint)SERVICE_ACCESS.SERVICE_ALL_ACCESS, (uint)SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS, (uint)SERVICE_START.SERVICE_AUTO_START, (uint)SERVICE_ERROR.SERVICE_ERROR_NORMAL, ApplicationExecpath, null, 0, null, "NT AUTHORITY\\NetworkService", null);
                    if(schService != IntPtr.Zero)
                    {
                        if (Description != "")
                        {
                            SERVICE_DESCRIPTION desc = new SERVICE_DESCRIPTION();
                            desc.lpDescription = Description;

                            IntPtr lpDesc = Marshal.AllocHGlobal(Marshal.SizeOf(desc));

                            if (lpDesc == IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(lpDesc);
                                throw new Exception(String.Format("Unable to allocate memory, error was: 0x{0:X}", Marshal.GetLastWin32Error()));
                            }
                            Marshal.StructureToPtr(desc, lpDesc, false);

                            if (!ChangeServiceConfig2(schService, (int)SERVICE_CONFIG.SERVICE_CONFIG_DESCRIPTION, lpDesc))
                            {
                                throw new Exception(String.Format("Error setting service config, error was: 0x{0:X}", Marshal.GetLastWin32Error()));
                            }
                            Marshal.FreeHGlobal(lpDesc);
                        }

                        if (delaystart)
                        {
                            SERVICE_DELAYED_AUTO_START_INFO das;
                            das.fDelayedAutostart = true;
                            IntPtr lpdas = Marshal.AllocHGlobal(Marshal.SizeOf(das));

                            if (lpdas == IntPtr.Zero)
                            {
                                Marshal.FreeHGlobal(lpdas);
                                throw new Exception(String.Format("Unable to allocate memory, error was: 0x{0:X}", Marshal.GetLastWin32Error()));
                            }
                            Marshal.StructureToPtr(das, lpdas, false);

                            if (!ChangeServiceConfig2(schService, (int)SERVICE_CONFIG.SERVICE_CONFIG_DELAYED_AUTO_START_INFO, lpdas))
                            {
                                throw new Exception(String.Format("Error setting service config, error was: 0x{0:X}", Marshal.GetLastWin32Error()));
                            }
                            Marshal.FreeHGlobal(lpdas);
                        }
                        return true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("CreateService failed {0}", Marshal.GetLastWin32Error()));
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("OpenSCManager failed {0}", Marshal.GetLastWin32Error()));
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("NetCreateService failed {0}", ex));
            }
            finally
            {
                if (schSCManager != IntPtr.Zero)
                    CloseServiceHandle(schSCManager);
                // if you don't close this handle, Services control panel
                // shows the service as "disabled", and you'll get 1072 errors
                // trying to reuse this service's name
                if (schService != IntPtr.Zero)
                    CloseServiceHandle(schService);
            }
            return false;
        }

        static public bool SetParameterStringValue(string ServiceName, String Parameter, string Value)
        {
            return SetParameterValue(ServiceName, Parameter, Value, RegistryValueKind.String);
        }

        static public bool SetParameterDWORDValue(string ServiceName, String Parameter, uint Value)
        {
            return SetParameterValue(ServiceName, Parameter, Value, RegistryValueKind.DWord);
        }

        static public bool SetParameterValue(string ServiceName, String Parameter, object Value, RegistryValueKind valueKind)
        {
            try
            {
                Registry.SetValue(string.Format(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{0}\Parameters", ServiceName), Parameter, Value, valueKind);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        static public object GetParameterValue(string ServiceName, String Parameter)
        {
            return Registry.GetValue(string.Format(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{0}\Parameters", ServiceName), Parameter, null);
        }

        static public string GetStringParameterValue(string ServiceName, String Parameter, string Default = null)
        {
            try
            {
                var ret = GetParameterValue(ServiceName, Parameter);
                if(ret == null && Default != null)
                {
                    return Default;
                }
                return (string)ret;
            }
            catch(Exception e)
            {
                if (Default == null)
                    throw e;
                else
                    return Default;
            }
        }

        static public uint GetDWORDParameterValue(string ServiceName, String Parameter, uint? Default = null)
        {
            try
            {
                var ret = GetParameterValue(ServiceName, Parameter);
                if (ret == null && Default != null)
                {
                    return (uint)Default;
                }
                return uint.Parse(ret.ToString());
            }
            catch (Exception e)
            {
                if (Default == null)
                    throw e;
                else
                    return (uint)Default;
            }
        }

        static public bool RemoveService(string ServiceName)
        {
            IntPtr schSCManager = IntPtr.Zero;
            IntPtr schService = IntPtr.Zero;
            try
            {
                schSCManager = OpenSCManager(null, null, SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
                if (schSCManager != IntPtr.Zero)
                {
                    schService = OpenService(schSCManager, ServiceName, SERVICE_ACCESS.SERVICE_ALL_ACCESS);
                    if (schService != IntPtr.Zero)
                    {
                        if (DeleteService(schService) == false)
                        {
                            Console.WriteLine(string.Format("DeleteService failed {0}", Marshal.GetLastWin32Error()));
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("OpenSCManager failed {0}", Marshal.GetLastWin32Error()));
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(string.Format("NetRemoveService failed {0}", ex));
            }
            finally
            {
                if(schSCManager!=IntPtr.Zero)
                    CloseServiceHandle(schSCManager);
                // if you don't close this handle, Services control panel
                // shows the service as "disabled", and you'll get 1072 errors
                // trying to reuse this service's name
                if (schService != IntPtr.Zero)
                    CloseServiceHandle(schService);
            }
            return false;
        }
    }
}
