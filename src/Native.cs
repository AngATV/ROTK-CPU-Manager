using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace RotkCpu
{
    internal static class Native
    {
        internal const uint QueryLimitedInformation = 0x1000, SetLimitedInformation = 0x2000, Synchronize = 0x100000;
        internal const uint WaitTimeout = 0x102, WaitFailed = UInt32.MaxValue;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeProcessHandle OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(SafeProcessHandle process, uint flags, StringBuilder path, ref uint size);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetSystemCpuSetInformation(IntPtr buffer, uint length, out uint needed, IntPtr process, uint flags);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetLogicalProcessorInformationEx(int relationship, IntPtr buffer, ref uint length);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessDefaultCpuSets(SafeProcessHandle process, [Out] uint[] ids, uint count, out uint needed);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessDefaultCpuSets(SafeProcessHandle process, uint[] ids, uint count);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint WaitForSingleObject(SafeProcessHandle process, uint timeout);
        [DllImport("user32.dll")]
        internal static extern bool SetProcessDPIAware();

        internal static string ImagePath(SafeProcessHandle process)
        {
            var path = new StringBuilder(32768);
            uint size = (uint)path.Capacity;
            if (!QueryFullProcessImageName(process, 0, path, ref size))
                throw new Win32Exception();
            return path.ToString();
        }

        internal static uint[] ReadSets(SafeProcessHandle process)
        {
            uint needed;
            if (!GetProcessDefaultCpuSets(process, null, 0, out needed) && Marshal.GetLastWin32Error() != 122)
                throw new Win32Exception();
            for (int retry = 0; retry < 3; retry++)
            {
                var ids = new uint[needed];
                if (GetProcessDefaultCpuSets(process, ids, (uint)ids.Length, out needed))
                {
                    Array.Resize(ref ids, (int)needed);
                    return ids;
                }
                if (Marshal.GetLastWin32Error() != 122)
                    throw new Win32Exception();
            }
            throw new InvalidOperationException(Locale.T("La liste des CPU Sets change trop vite. Réessayez."));
        }

        internal static void WriteSets(SafeProcessHandle process, uint[] ids)
        {
            if (!SetProcessDefaultCpuSets(process, ids.Length == 0 ? null : ids, (uint)ids.Length))
                throw new Win32Exception();
        }
    }
}
