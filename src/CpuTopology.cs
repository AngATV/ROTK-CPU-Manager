using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RotkCpu
{
    internal sealed class CpuSet
    {
        internal uint Id;
        internal ushort Group;
        internal byte Logical, Core, Cache, Efficiency, Flags;
        internal uint L3Bytes;
        internal string CoreKey
        {
            get
            {
                return Group + ":" + Core;
            }
        }
        internal string CacheKey
        {
            get
            {
                return Group + ":" + Cache;
            }
        }
    }

    internal sealed class CpuTopology
    {
        internal string Name = Locale.T("Processeur inconnu"), Vendor = "";
        internal List<CpuSet> Sets = new List<CpuSet>();
        internal string Warning = "";
        internal int CoreCount
        {
            get
            {
                return Sets.Select(x => x.CoreKey).Distinct().Count();
            }
        }

        internal static CpuTopology Detect()
        {
            var cpu = new CpuTopology();
            using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
            {
                if (key != null)
                {
                    cpu.Name = Convert.ToString(key.GetValue("ProcessorNameString", cpu.Name)).Trim();
                    cpu.Vendor = Convert.ToString(key.GetValue("VendorIdentifier", ""));
                }
            }
            uint length;
            if (!Native.GetSystemCpuSetInformation(IntPtr.Zero, 0, out length, IntPtr.Zero, 0) && Marshal.GetLastWin32Error() != 122)
                throw new Win32Exception();
            if (length == 0 || length > 1024 * 1024)
                throw new InvalidDataException(Locale.T("Topologie CPU indisponible."));
            IntPtr buffer = Marshal.AllocHGlobal((int)length);
            try
            {
                uint returned;
                if (!Native.GetSystemCpuSetInformation(buffer, length, out returned, IntPtr.Zero, 0))
                    throw new Win32Exception();
                var data = new byte[returned];
                Marshal.Copy(buffer, data, 0, data.Length);
                cpu.Sets = ParseSets(data);
            }
            finally { Marshal.FreeHGlobal(buffer); }
            if (cpu.Sets.Count == 0)
                throw new InvalidDataException(Locale.T("Aucun CPU Set disponible."));
            try
            {
                cpu.ReadL3();
            }
            catch (Exception ex) { cpu.Warning = Locale.T("Cache L3 non identifié : ") + Locale.Error(ex); }
            return cpu;
        }

        internal static List<CpuSet> ParseSets(byte[] data)
        {
            var sets = new List<CpuSet>();
            for (int offset = 0; offset < data.Length;)
            {
                if (data.Length - offset < 8)
                    throw new InvalidDataException(Locale.T("En-tête CPU tronqué."));
                uint size = BitConverter.ToUInt32(data, offset);
                if (size < 8 || size > data.Length - offset)
                    throw new InvalidDataException(Locale.T("Taille CPU invalide."));
                if (BitConverter.ToUInt32(data, offset + 4) == 0)
                {
                    if (size < 32)
                        throw new InvalidDataException(Locale.T("CPU Set tronqué."));
                    sets.Add(new CpuSet
                    {
                        Id = BitConverter.ToUInt32(data, offset + 8),
                        Group = BitConverter.ToUInt16(data, offset + 12),
                        Logical = data[offset + 14],
                        Core = data[offset + 15],
                        Cache = data[offset + 16],
                        Efficiency = data[offset + 18],
                        Flags = data[offset + 19]
                    });
                }
                offset += (int)size;
            }
            if (sets.Select(x => x.Id).Distinct().Count() != sets.Count)
                throw new InvalidDataException(Locale.T("Identifiants CPU dupliqués."));
            return sets;
        }

        private void ReadL3()
        {
            uint length = 0;
            if (!Native.GetLogicalProcessorInformationEx(2, IntPtr.Zero, ref length) && Marshal.GetLastWin32Error() != 122)
                throw new Win32Exception();
            if (length == 0 || length > 1024 * 1024)
                throw new InvalidDataException(Locale.T("Cache indisponible."));
            IntPtr buffer = Marshal.AllocHGlobal((int)length);
            try
            {
                if (!Native.GetLogicalProcessorInformationEx(2, buffer, ref length))
                    throw new Win32Exception();
                var data = new byte[length];
                Marshal.Copy(buffer, data, 0, data.Length);
                ParseL3(data, Sets);
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        internal static void ParseL3(byte[] data, List<CpuSet> sets)
        {
            for (int offset = 0; offset < data.Length;)
            {
                if (data.Length - offset < 8)
                    throw new InvalidDataException(Locale.T("En-tête cache tronqué."));
                uint size = BitConverter.ToUInt32(data, offset + 4);
                if (size < 8 || size > data.Length - offset)
                    throw new InvalidDataException(Locale.T("Taille cache invalide."));
                if (BitConverter.ToUInt32(data, offset) == 2)
                {
                    if (size < 56)
                        throw new InvalidDataException(Locale.T("Cache tronqué."));
                    if (data[offset + 8] == 3)
                    {
                        uint bytes = BitConverter.ToUInt32(data, offset + 12);
                        int count = BitConverter.ToUInt16(data, offset + 38);
                        // Windows 10 returns one GroupMask with GroupCount set to zero.
                        if (count == 0)
                            count = 1;
                        if (40 + count * 16 > size)
                            throw new InvalidDataException(Locale.T("Masques cache tronqués."));
                        for (int i = 0; i < count; i++)
                        {
                            ulong mask = BitConverter.ToUInt64(data, offset + 40 + i * 16);
                            ushort group = BitConverter.ToUInt16(data, offset + 48 + i * 16);
                            foreach (var set in sets.Where(x => x.Group == group && x.Logical < 64 && (mask & (1UL << x.Logical)) != 0))
                                set.L3Bytes = Math.Max(set.L3Bytes, bytes);
                        }
                    }
                }
                offset += (int)size;
            }
        }
    }
}
