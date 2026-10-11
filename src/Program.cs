using System;
using System.Threading;
using System.Windows.Forms;

namespace RotkCpu
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Locale.UsePreference(null);
            bool first;
            using (var mutex = new Mutex(true, @"Local\ROTK-CPU-Portable-1", out first))
            {
                if (!first)
                {
                    MessageBox.Show(Locale.T("ROTK CPU Manager est déjà ouvert."), "ROTK CPU Manager");
                    return;
                }
                try
                {
                    if (!Environment.Is64BitOperatingSystem || Environment.OSVersion.Version.Major < 10)
                        throw new NotSupportedException(Locale.T("ROTK CPU Manager nécessite Windows 10 ou 11, 64 bits."));
                    Native.SetProcessDPIAware();
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());
                }
                catch (Exception ex) { MessageBox.Show(Locale.Error(ex), Locale.T("ROTK CPU Manager — démarrage impossible"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
