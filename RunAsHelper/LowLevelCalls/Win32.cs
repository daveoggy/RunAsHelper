//--------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace CreateProcessSample
{
    class Win32
    {
        #region "CONTS"

        const uint INFINITE = 0xFFFFFFFF;
        const uint WAIT_FAILED = 0xFFFFFFFF;

        #endregion

        #region "ENUMS"

        [Flags]
        public enum LogonFlags
        {
            LOGON_WITH_PROFILE = 0x00000001,
            LOGON_NETCREDENTIALS_ONLY = 0x00000002
        }

        [Flags]
        public enum LogonType
        {
            LOGON32_LOGON_INTERACTIVE = 2,
            LOGON32_LOGON_NETWORK = 3,
            LOGON32_LOGON_BATCH = 4,
            LOGON32_LOGON_SERVICE = 5,
            LOGON32_LOGON_UNLOCK = 7,
            LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
            LOGON32_LOGON_NEW_CREDENTIALS = 9
        }

        [Flags]
        public enum LogonProvider
        {
            LOGON32_PROVIDER_DEFAULT = 0,
            LOGON32_PROVIDER_WINNT35,
            LOGON32_PROVIDER_WINNT40,
            LOGON32_PROVIDER_WINNT50
        }

        #endregion

        #region "STRUCTS"

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        #endregion

        #region "FUNCTIONS (P/INVOKE)"

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool LogonUser 
        (
            string lpszUserName,
            string lpszDomain,
            string lpszPassword,
            LogonType dwLogonType,
            LogonProvider dwLogonProvider,
            out IntPtr phToken
        );

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CreateProcessAsUser 
        (
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CreateProcessWithLogonW 
        (
            string lpszUsername, 
            string lpszDomain, 
            string lpszPassword,
            LogonFlags dwLogonFlags, 
            string applicationName, 
            string commandLine,
            int creationFlags, 
            IntPtr environment,
            string currentDirectory,
            ref STARTUPINFO sui,
            out PROCESS_INFORMATION processInfo
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject 
        (
            IntPtr hHandle,
            uint dwMilliseconds
        );

        [DllImport("kernel32", SetLastError=true)]
        public static extern bool CloseHandle (IntPtr handle);
 
        #endregion

        #region "FUNCTIONS"

        public static void CreateProcessWithLogon(string strCommand, string strDomain, string strName, string strPassword)
        {
            // Variables
            var processInfo = new PROCESS_INFORMATION();
            var startInfo = new STARTUPINFO();

            try 
            {
                // Create process
                startInfo.cb = Marshal.SizeOf(startInfo);

                var bResult = CreateProcessWithLogonW(
                    strName, 
                    strDomain, 
                    strPassword, 
                    LogonFlags.LOGON_NETCREDENTIALS_ONLY, 
                    null,
                    strCommand, 
                    0,
                    IntPtr.Zero, 
                    null, 
                    ref startInfo, 
                    out processInfo
                );
                if (!bResult) { throw new Exception("CreateProcessWithLogonW error #" + Marshal.GetLastWin32Error()); }
            } 
            finally
            {
                // Close all handles
                CloseHandle(processInfo.hProcess);
                CloseHandle(processInfo.hThread);
            }
        }

        #endregion
    }
}