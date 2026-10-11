using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace RotkCpu
{
    internal static class Tests
    {
        private static int checks;
        private static IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var descendant in Descendants(child))
                    yield return descendant;
            }
        }
        private static void LanguageAndUiTests()
        {
            Locale.Language = "en";
            Check(Locale.English.All(pair => Locale.T(pair.Key) == pair.Value), "Every English translation is available");
            Check(Locale.English.All(pair => Locale.TranslateExisting(pair.Key) == pair.Value), "Existing captions translate without overlapping replacements");
            Locale.Language = "fr";
            Check(Locale.English.All(pair => Locale.TranslateExisting(pair.Value) == pair.Key), "English captions return to French");
            var cpu = Synthetic("AuthenticAMD", 16, 2, false, true, true);
            var french = Profiles.Build(cpu);
            Locale.Language = "en";
            var english = Profiles.Build(cpu);
            Check(french.Zip(english, (a, b) => a.Key == b.Key && a.Ids.SequenceEqual(b.Ids) && a.Recommended == b.Recommended).All(x => x), "Language never changes CPU recommendations");
            Check(english.Single(x => x.Recommended).Name == "Large L3 cache group (X3D)", "AMD profile is translated");
            Check(Locale.Error(new System.ComponentModel.Win32Exception(5)).Contains("Access is denied"), "English Windows error text");
            Locale.Language = "fr";
            using (var bar = new TacticalScrollBar { Height = 200 })
            {
                bar.SetRange(1000, 200);
                bar.Value = 900;
                Check(bar.Maximum == 800 && bar.Value == 800, "Scroll clamps at the bottom");
                Check(bar.Thumb.Bottom <= 196, "Thumb stays inside its track");
                bar.Value = -99;
                Check(bar.Value == 0, "Scroll clamps at the top");
                bar.Wheel(-120);
                Check(bar.Value > 0, "Mouse wheel moves custom scrollbar");
                bar.SetRange(100, 200);
                Check(bar.Value == 0 && !bar.Enabled, "Resize resets scroll when content fits");
            }
            using (var form = new MainForm(true))
            {
                form.Show();
                Application.DoEvents();
                Check(form.FormBorderStyle == FormBorderStyle.None, "Main window has integrated chrome");
                var combo = Descendants(form).OfType<ComboBox>().Single();
                combo.SelectedIndex = combo.Items.Count - 1;
                string key = ((Profile)combo.SelectedItem).Key;
                form.ChangeLanguage("en");
                Check(((Profile)combo.SelectedItem).Key == key, "Switching language preserves selected profile");
                Check(Descendants(form).OfType<Button>().Any(x => x.Text == "APPLY AND MONITOR"), "Main action switches to English");
                Check(Descendants(form).OfType<TextBox>().Any(x => x.Text.Contains("Only H1Z1.exe")), "Existing log switches to English");
                Check(Descendants(form).OfType<CheckBox>().All(x => !x.Checked), "Language switch does not enable monitoring");
                form.ClientSize = new Size(944, 640);
                Application.DoEvents();
                var scroller = Descendants(form).OfType<TacticalScrollView>().First();
                scroller.ScrollToEnd();
                Check(scroller.Bar.Value == scroller.Bar.Maximum && scroller.Bar.Maximum > 0, "Small window scrolls to its last content");
                var maximize = Descendants(form).OfType<Button>().Single(x => x.AccessibleName == "Maximize / restore window");
                maximize.PerformClick();
                Application.DoEvents();
                Check(form.WindowState == FormWindowState.Maximized, "Integrated maximize button works");
                maximize.PerformClick();
                Application.DoEvents();
                Check(form.WindowState == FormWindowState.Normal, "Integrated restore button works");
                bool closed = false;
                form.FormClosed += delegate
                {
                    closed = true;
                };
                Descendants(form).OfType<Button>().Single(x => x.AccessibleName == "Close and restore").PerformClick();
                Check(closed, "Integrated close uses normal restoration path");
            }
            using (var guide = new GuideForm())
            {
                guide.Show();
                Application.DoEvents();
                Check(guide.FormBorderStyle == FormBorderStyle.None, "Guide has integrated chrome");
                Check(Descendants(guide).OfType<Label>().Any(x => x.Text == "04 / WITHOUT SMT OR HYPER-THREADING"), "Full guide is translated");
                var scroller = Descendants(guide).OfType<TacticalScrollView>().Single();
                scroller.ScrollToEnd();
                Check(scroller.Bar.Maximum > 0 && scroller.Bar.Value == scroller.Bar.Maximum, "Guide scroll reaches all sections");
                guide.Close();
            }
            Locale.Language = "fr";
        }
        private static void Check(bool value, string name)
        {
            checks++;
            if (!value)
                throw new Exception("FAIL: " + name);
        }
        private static void Throws(Action action, string name)
        {
            try
            {
                action();
            }
            catch (InvalidDataException) { Check(true, name); return; }
            Check(false, name);
        }
        private static CpuTopology Synthetic(string vendor, int cores, int threads, bool hybrid, bool caches, bool asymmetric)
        {
            var cpu = new CpuTopology { Vendor = vendor };
            for (int core = 0; core < cores; core++)
                for (int thread = 0; thread < threads; thread++)
                    cpu.Sets.Add(new CpuSet
                    {
                        Id = (uint)(100 + core * threads + thread),
                        Core = (byte)core,
                        Logical = (byte)(core * threads + thread),
                        Efficiency = (byte)(hybrid && core < cores / 2 ? 2 : 0),
                        Cache = (byte)(caches && core >= cores / 2 ? 1 : 0),
                        L3Bytes = (uint)((asymmetric && core < cores / 2 ? 96 : 32) * 1024 * 1024)
                    });
            return cpu;
        }
        private static void ProfileTests()
        {
            var intel = Synthetic("GenuineIntel", 16, 2, true, false, false);
            var profiles = Profiles.Build(intel);
            var recommended = profiles.Single(x => x.Recommended);
            Check(recommended.Key == "pcores", "Intel hybrid recommends P-cores");
            Check(recommended.Ids.Length == 16, "Intel SMT preserved");
            Check(profiles.Single(x => x.Key == "pcores-nosmt").Ids.Length == 8, "Intel no-HT uses real physical cores");
            var noHt = Profiles.Build(Synthetic("GenuineIntel", 16, 1, true, false, false));
            Check(noHt.Single(x => x.Recommended).Key == "pcores" && noHt.Count == 2, "Hybrid Intel without Hyper-Threading gets no redundant SMT trial");
            // Non-adjacent SMT IDs must still be grouped by the physical core, not even/odd indices.
            foreach (var set in intel.Sets)
                set.Logical = (byte)(set.Core + (set.Id % 2 == 0 ? 0 : 32));
            Check(Profiles.Build(intel).Single(x => x.Key == "all-nosmt").Ids.Length == 16, "Non-adjacent SMT mapping");
            var amd = Synthetic("AuthenticAMD", 16, 2, false, true, true);
            profiles = Profiles.Build(amd);
            recommended = profiles.Single(x => x.Recommended);
            Check(recommended.Key == "cache-0:0" && recommended.Ids.Length == 16, "Asymmetric AMD cache recommendation");
            Check(recommended.Ids.All(id => amd.Sets.Single(x => x.Id == id).Cache == 0), "Select larger cache, independent of CPU name");
            profiles = Profiles.Build(Synthetic("AuthenticAMD", 16, 2, false, true, false));
            Check(profiles.Single(x => x.Recommended).Key == "windows", "Symmetric AMD conservative default");
            Check(profiles.Count(x => x.Key.StartsWith("cache-")) == 4, "Symmetric AMD has experimental cache profiles");
            profiles = Profiles.Build(Synthetic("AuthenticAMD", 8, 2, false, false, false));
            Check(profiles.Single(x => x.Recommended).Key == "windows", "Single cache Windows default");
            Check(profiles.Count == 2, "Single cache SMT trial");
            var homogeneousIntel = Profiles.Build(Synthetic("GenuineIntel", 8, 2, false, false, false));
            Check(homogeneousIntel.Single(x => x.Recommended).Key == "windows"
                && homogeneousIntel.Single(x => x.Key == "all-nosmt").Ids.Length == 8,
                "Homogeneous Intel with HT gets Windows and a one-thread-per-core trial");
            Check(Profiles.Build(Synthetic("GenuineIntel", 8, 1, false, false, false)).Count == 1,
                "Homogeneous Intel without HT gets Windows only");
            var ryzen3600 = Profiles.Build(Synthetic("AuthenticAMD", 6, 2, false, true, false));
            Check(ryzen3600.Single(x => x.Recommended).Key == "windows"
                && ryzen3600.Single(x => x.Key == "all-nosmt").Ids.Length == 6 && ryzen3600.Count == 2,
                "Ryzen 3600 topology gets a six-thread trial and no three-core cache restriction");
            Check(Profiles.Build(Synthetic("Other", 4, 1, false, false, false)).Count == 1, "No meaningless profiles without SMT");
            var small = Synthetic("GenuineIntel", 4, 2, true, false, false);
            Check(Profiles.Build(small).Single(x => x.Recommended).Key == "windows", "Too few P-cores keeps Windows");
            intel.Sets[0].Flags = 2;
            Check(Profiles.Build(intel).All(x => !x.Ids.Contains(intel.Sets[0].Id)), "Reserved CPU excluded");
            intel.Sets[0].Flags = 1;
            Check(Profiles.Build(intel).Single(x => x.Key == "pcores").Ids.Contains(intel.Sets[0].Id), "Parked CPU is not mistaken for reserved CPU");
            intel.Sets.Last().Group = 1;
            Check(Profiles.Build(intel).Count == 1, "Multiple processor groups conservative fallback");
            amd.Sets[0].L3Bytes = 0;
            Check(Profiles.Build(amd).Single(x => x.Recommended).Key == "windows", "Incomplete cache data must not guess X3D");
        }
        private static void ParserTests()
        {
            var data = new byte[72];
            Buffer.BlockCopy(BitConverter.GetBytes(40U), 0, data, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(117U), 0, data, 8, 4);
            data[14] = 31;
            data[15] = 7;
            data[18] = 2;
            Buffer.BlockCopy(BitConverter.GetBytes(32U), 0, data, 40, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(118U), 0, data, 48, 4);
            var sets = CpuTopology.ParseSets(data);
            Check(sets.Count == 2 && sets[0].Id == 117 && sets[0].Logical == 31 && sets[0].Efficiency == 2, "Variable-size CPU blocks and offsets");
            data[44] = 99;
            Check(CpuTopology.ParseSets(data).Count == 1, "Unknown CPU record skipped");
            Throws(delegate
            {
                CpuTopology.ParseSets(new byte[7]);
            }, "Truncated CPU header");
            Throws(delegate
            {
                CpuTopology.ParseSets(new byte[32]);
            }, "Zero-size CPU block");
            data[0] = 255;
            Throws(delegate
            {
                CpuTopology.ParseSets(data);
            }, "Oversized CPU block");
            var cache = new byte[56];
            cache[0] = 2;
            cache[4] = 56;
            cache[8] = 3;
            Buffer.BlockCopy(BitConverter.GetBytes(96U * 1024 * 1024), 0, cache, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(1UL << 31), 0, cache, 40, 8);
            CpuTopology.ParseL3(cache, sets);
            Check(sets[0].L3Bytes == 96U * 1024 * 1024 && sets[1].L3Bytes == 0, "L3 mask and Windows 10 zero GroupCount");
            cache[38] = 2;
            Throws(delegate
            {
                CpuTopology.ParseL3(cache, sets);
            }, "Truncated cache group masks");
        }
        private static void GuardTests()
        {
            Check(GameGuard.Matches(@"C:\Games\ROTK", @"c:\games\rotk\h1z1.EXE"), "Exact path is case insensitive");
            Check(!GameGuard.Matches(@"C:\Games\ROTK", @"C:\Games\ROTK2\H1Z1.exe"), "Prefix collision rejected");
            Check(!GameGuard.Matches(@"C:\Games\ROTK", @"C:\Games\ROTK\Browser\H1Z1.exe"), "Subdirectory rejected");
            Check(!GameGuard.Matches(@"C:\Games\ROTK", @"C:\Games\ROTK\H1Z1_BE.exe"), "BattlEye launcher rejected");
            Check(!GameGuard.Matches(@"C:\Games\ROTK", @"C:\Games\ROTK\LaunchPad.exe"), "LaunchPad rejected");
            Check(!GameGuard.Matches(@"C:\Games\ROTK", @"C:\Steam\H1Z1\H1Z1.exe"), "Retail installation rejected");
            Check(GameGuard.Matches(@"C:\Games\ROTK\", @"C:\Games\ROTK\H1Z1.exe"), "Trailing slash handled");
            Check(GameGuard.Normalize(@"C:\") == @"C:\", "Drive root remains an absolute path");
            Check(GameGuard.Matches(@"C:\", @"C:\H1Z1.exe"), "Installation at drive root matches");
            Check(GameGuard.Normalize(@"\\server\share\") == Path.GetPathRoot(@"\\server\share\"), "Network share root remains absolute");
            Check(GameGuard.Matches(@"\\server\share\", @"\\server\share\H1Z1.exe"), "Installation at network share root matches");
        }

        private static void SettingsTests()
        {
            string temp = Path.Combine(Path.GetTempPath(), "ROTK-CPU-settings-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(temp, "settings.xml");
            try
            {
                Check(!Settings.Load(path).AutoEnabled, "Missing preferences keep monitoring disabled");
                var settings = new Settings { Folder = @"C:\Games\ROTK", ProfileKey = "pcores", Language = "en", AutoEnabled = true };
                settings.Save(path);
                var loaded = Settings.Load(path);
                Check(loaded.Folder == settings.Folder && loaded.ProfileKey == settings.ProfileKey && loaded.Language == "en" && loaded.AutoEnabled, "Preferences round trip");
                settings.AutoEnabled = false;
                settings.Save(path);
                Check(!Settings.Load(path).AutoEnabled && !File.Exists(path + ".tmp"), "Atomic replacement persists disabled monitoring");
                File.WriteAllText(path, "<Settings xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\" />");
                Throws(delegate
                {
                    Settings.Load(path);
                }, "Null preferences are rejected before startup");
                File.WriteAllText(path, "<!DOCTYPE Settings [<!ENTITY test 'en'>]><Settings><Language>&test;</Language></Settings>");
                bool rejected = false;
                try
                {
                    Settings.Load(path);
                }
                catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "XML document types are disabled");
            }
            finally
            {
                if (Directory.Exists(temp))
                    Directory.Delete(temp, true);
            }
        }
        private static void NativeTests(CpuTopology cpu)
        {
            Check(cpu.Sets.Count > 0 && cpu.CoreCount > 0, "Real Windows topology");
            Check(cpu.Sets.Select(x => x.Id).Distinct().Count() == cpu.Sets.Count, "Unique native CPU Set IDs");
            using (var self = Native.OpenProcess(Native.QueryLimitedInformation | Native.SetLimitedInformation, false, Process.GetCurrentProcess().Id))
            {
                var original = Native.ReadSets(self);
                try
                {
                    var ids = new uint[] { cpu.Sets.First(x => (x.Flags & 2) == 0).Id };
                    Native.WriteSets(self, ids);
                    Check(Native.ReadSets(self).SequenceEqual(ids), "Real CPU Sets apply/readback on TEST PROCESS only");
                    Native.WriteSets(self, new uint[0]);
                    Check(Native.ReadSets(self).Length == 0, "Windows management clears test-process CPU Sets");
                }
                finally { Native.WriteSets(self, original); }
                Check(Native.ReadSets(self).OrderBy(x => x).SequenceEqual(original.OrderBy(x => x)), "Test-process original settings restored");
            }
            using (var queryOnly = Native.OpenProcess(Native.QueryLimitedInformation, false, Process.GetCurrentProcess().Id))
            {
                var session = new GameSession { Handle = queryOnly, Changed = true, Original = new uint[0] };
                bool rejected = false;
                try
                {
                    session.Restore();
                }
                catch (System.ComponentModel.Win32Exception) { rejected = true; }
                Check(rejected && session.Changed, "Failed process wait preserves pending restoration");
            }
        }
        private static void WatcherTests(CpuTopology cpu)
        {
            string temp = Path.Combine(Path.GetTempPath(), "ROTK-CPU-tests-" + Guid.NewGuid().ToString("N"));
            string target = Path.Combine(temp, "ROTK"), other = Path.Combine(temp, "Other");
            var children = new List<Process>();
            var watcher = new GameWatcher();
            try
            {
                foreach (var folder in new string[] { target, other })
                {
                    Directory.CreateDirectory(folder);
                    File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(folder, "H1Z1.exe"));
                    File.WriteAllText(Path.Combine(folder, "rotkc.dll"), "test fixture only");
                    children.Add(Process.Start(new ProcessStartInfo(Path.Combine(folder, "H1Z1.exe"), "--fixture") { UseShellExecute = false, CreateNoWindow = true }));
                }
                Check(GameGuard.Validate(target) == target, "ROTK installation marker validation");
                File.Delete(Path.Combine(other, "rotkc.dll"));
                bool rejected = false;
                try
                {
                    GameGuard.Validate(other);
                }
                catch (IOException) { rejected = true; }
                Check(rejected, "Missing ROTK marker rejected");
                using (var a = Native.OpenProcess(Native.QueryLimitedInformation | Native.SetLimitedInformation, false, children[0].Id))
                using (var b = Native.OpenProcess(Native.QueryLimitedInformation | Native.SetLimitedInformation, false, children[1].Id))
                {
                    uint[] ids = cpu.Sets.Where(x => (x.Flags & 2) == 0).Take(2).Select(x => x.Id).ToArray();
                    Check(ids.Length == 2, "Integration fixture has two available logical CPUs");
                    uint[] initial = new uint[] { ids[0] }, applied = new uint[] { ids[1] };
                    Native.WriteSets(a, initial);
                    Native.WriteSets(b, initial);
                    var profile = new Profile { Name = "Fixture", Ids = applied };
                    watcher.Tick(target, profile, false);
                    Check(Native.ReadSets(a).SequenceEqual(initial), "Disabled monitoring does not mutate");
                    watcher.Tick(target, profile, true);
                    Check(Native.ReadSets(a).SequenceEqual(applied), "Watcher automatically applies to matching fixture");
                    Check(Native.ReadSets(b).SequenceEqual(initial), "Same EXE name in another folder is untouched");
                    Native.WriteSets(a, initial);
                    watcher.Tick(target, profile, true);
                    Check(Native.ReadSets(a).SequenceEqual(applied), "Watcher reapplies lost setting");
                    watcher.Tick(target, new Profile { Name = "Windows", Ids = new uint[0] }, true);
                    Check(Native.ReadSets(a).Length == 0, "Switch profile while running");
                    watcher.RestoreAll();
                    Check(Native.ReadSets(a).SequenceEqual(initial), "Restore preserves pre-existing nonempty setting");
                    watcher.Tick(target, profile, false);
                    Check(Native.ReadSets(a).SequenceEqual(initial), "Restored monitoring stays disabled");
                    children[0].Kill();
                    children[0].WaitForExit();
                    watcher.Tick(target, profile, true);
                    Check(watcher.Status.StartsWith("En attente"), "Exited process removed and waiting resumes");
                }
                children.Add(Process.Start(new ProcessStartInfo(Path.Combine(target, "H1Z1.exe"), "--fixture") { UseShellExecute = false, CreateNoWindow = true }));
                using (var relaunched = Native.OpenProcess(Native.QueryLimitedInformation, false, children[2].Id))
                {
                    var profile = new Profile { Name = "Relaunch", Ids = new uint[] { cpu.Sets.First(x => (x.Flags & 2) == 0).Id } };
                    var original = Native.ReadSets(relaunched);
                    watcher.Tick(target, profile, true);
                    Check(Native.ReadSets(relaunched).SequenceEqual(profile.Ids), "A new game instance is automatically detected");
                    watcher.RestoreAll();
                    Check(Native.ReadSets(relaunched).OrderBy(x => x).SequenceEqual(original.OrderBy(x => x)), "New game instance restored independently");
                }
            }
            finally
            {
                try
                {
                    watcher.RestoreAll();
                }
                finally
                {
                    foreach (var child in children)
                    {
                        if (!child.HasExited)
                        {
                            child.Kill();
                            child.WaitForExit();
                        }
                        child.Dispose();
                    }
                    if (Path.GetFullPath(temp).StartsWith(Path.GetFullPath(Path.GetTempPath()) + "ROTK-CPU-tests-", StringComparison.OrdinalIgnoreCase))
                        Directory.Delete(temp, true);
                }
            }
        }

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--fixture")
            {
                Thread.Sleep(60000);
                return 0;
            }
            try
            {
                if (args.Length == 2 && args[0] == "--check-language")
                {
                    Check(Locale.Language == args[1], "Compiled language default");
                    Locale.UsePreference(args[1] == "en" ? "fr" : "en");
                    Check(Locale.Language == args[1], "Dedicated version starts in its own language");
                    using (var form = new MainForm(true))
                        Check(Descendants(form).OfType<Button>().Any(x => x.Text == (args[1] == "en" ? "APPLY AND MONITOR" : "APPLIQUER ET SURVEILLER")), "Dedicated version has translated controls");
                    Console.WriteLine("PASS: " + args[1].ToUpperInvariant() + " build language");
                    return 0;
                }
                if (args.Length == 2 && args[0] == "--render")
                {
                    Native.SetProcessDPIAware();
                    Application.EnableVisualStyles();
                    Locale.Language = "fr";
                    using (var form = new MainForm(true))
                    {
                        form.Show();
                        Application.DoEvents();
                        using (var bitmap = new Bitmap(form.Width, form.Height))
                        {
                            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                            bitmap.Save(args[1]);
                        }
                        form.ClientSize = new Size(944, 640);
                        Application.DoEvents();
                        using (var bitmap = new Bitmap(form.Width, form.Height))
                        {
                            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                            bitmap.Save(Path.Combine(Path.GetDirectoryName(args[1]), "preview-small.png"));
                        }
                        form.ChangeLanguage("en");
                        form.ClientSize = new Size(1100, 850);
                        Application.DoEvents();
                        using (var bitmap = new Bitmap(form.Width, form.Height))
                        {
                            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                            bitmap.Save(Path.Combine(Path.GetDirectoryName(args[1]), "preview-en.png"));
                        }
                        form.Close();
                    }
                    using (var guide = new GuideForm())
                    {
                        guide.Show();
                        Application.DoEvents();
                        using (var bitmap = new Bitmap(guide.Width, guide.Height))
                        {
                            guide.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                            bitmap.Save(Path.Combine(Path.GetDirectoryName(args[1]), "preview-guide-en.png"));
                        }
                        var scroller = Descendants(guide).OfType<TacticalScrollView>().Single();
                        scroller.ScrollToEnd();
                        Application.DoEvents();
                        using (var bitmap = new Bitmap(guide.Width, guide.Height))
                        {
                            guide.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                            bitmap.Save(Path.Combine(Path.GetDirectoryName(args[1]), "preview-guide-bottom.png"));
                        }
                        guide.Close();
                    }
                    Console.WriteLine("Rendered: " + args[1]);
                    return 0;
                }
                Locale.Language = "fr";
                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                ProfileTests();
                ParserTests();
                GuardTests();
                SettingsTests();
                var cpu = CpuTopology.Detect();
                NativeTests(cpu);
                WatcherTests(cpu);
                LanguageAndUiTests();
                Console.WriteLine("PASS: " + checks + " checks");
                Console.WriteLine(cpu.Name + ": " + cpu.CoreCount + " cores / " + cpu.Sets.Count + " logical CPUs");
                foreach (var profile in Profiles.Build(cpu))
                    Console.WriteLine(profile + " [" + profile.Ids.Length + " CPU Sets]");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
