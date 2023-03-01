using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace SFCFixScriptBuilder.RegistryHiveLoader
{
    public static class HiveLoader
    {
        public static readonly RegistryKey HKLM = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegLoadKey(IntPtr hKey, string lpSubKey, string lpFile);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegUnLoadKey(IntPtr hKey, string lpSubKey);

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern IntPtr RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("advapi32.dll")]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref UInt64 lpLuid);

        [DllImport("advapi32.dll")]
        static extern bool LookupPrivilegeValue(IntPtr lpSystemName, string lpName, ref UInt64 lpLuid);

        public static int LoadHive(string path, string name)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Please provide a valid path and/or hive name");
                return -1;
            }

            //Trim quotes ("") from the path in case the user has used Copy Path
            path = path.Trim('"');

            int result = RegLoadKey(HKLM.Handle.DangerousGetHandle(), name, path);

            return result;
        }

        public static int UnloadHive(string name)
        {
            return RegUnLoadKey(HKLM.Handle.DangerousGetHandle(), name);
        }

        public static void GrantPrivileges()
        {
            ulong luid = 0;
            bool throwaway;
            LookupPrivilegeValue(IntPtr.Zero, "SeRestorePrivilege", ref luid);
            RtlAdjustPrivilege((int)luid, true, false, out throwaway);
            LookupPrivilegeValue(IntPtr.Zero, "SeBackupPrivilege", ref luid);
            RtlAdjustPrivilege((int)luid, true, false, out throwaway);
        }

        public static void RevokePrivileges()
        {
            ulong luid = 0;
            bool throwaway;
            LookupPrivilegeValue(IntPtr.Zero, "SeRestorePrivilege", ref luid);
            RtlAdjustPrivilege((int)luid, false, false, out throwaway);
            LookupPrivilegeValue(IntPtr.Zero, "SeBackupPrivilege", ref luid);
            RtlAdjustPrivilege((int)luid, false, false, out throwaway);
        }
    }
}
