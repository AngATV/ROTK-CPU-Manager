using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RotkCpu
{
    internal sealed class MainForm : TacticalForm
    {
        private readonly Color background = Color.FromArgb(17, 18, 19), card = Color.FromArgb(27, 28, 30),
            text = Color.FromArgb(239, 231, 217), muted = Color.FromArgb(181, 171, 156), accent = Color.FromArgb(246, 112, 87);
        private readonly TextBox folderBox = new TextBox(), journal = new TextBox();
        private readonly TacticalScrollView journalScroll = new TacticalScrollView();
        private readonly Label cpuLabel = new Label(), cpuDetails = new Label(), explanation = new Label(), selection = new Label(), status = new Label();
        private readonly Label profileReason = new Label();
        private readonly ComboBox profileBox = new ComboBox();
        private readonly CheckBox automatic = new CheckBox();
        private readonly Button applyButton = new Button();
        private readonly GameWatcher watcher = new GameWatcher();
        private readonly Timer timer = new Timer { Interval = 2000 };
        private readonly NotifyIcon tray = new NotifyIcon();
        private Settings settings;
        private CpuTopology cpu;
        private string lastMessage = "";
        private bool updating;
        private readonly bool previewMode;

        internal MainForm() : this(false) { }
        internal MainForm(bool preview)
        {
            previewMode = preview;
            Text = "ROTK CPU Manager";
            ClientSize = new Size(1100, Math.Min(850, Screen.PrimaryScreen.WorkingArea.Height - 90));
            MinimumSize = new Size(960, 690);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = background;
            ForeColor = text;
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
            Padding = new Padding(0);
            string loadWarning = "";
            try
            {
                settings = preview ? new Settings() : Settings.Load();
            }
            catch (Exception ex) { settings = new Settings(); loadWarning = Locale.T("Préférences illisibles, valeurs par défaut : ") + Locale.Error(ex); }
            if (!preview)
                Locale.UsePreference(settings.Language);
            BuildLayout();
            watcher.Log = AppendLog;
            folderBox.Text = settings.Folder;
            try
            {
                cpu = CpuTopology.Detect();
                cpuLabel.Text = cpu.Name;
                var classes = cpu.Sets.GroupBy(x => x.Efficiency).OrderByDescending(x => x.Key).ToList();
                cpuDetails.Text = cpu.CoreCount + Locale.T(" cœurs physiques   /   ") + cpu.Sets.Count + " threads   /   "
                    + cpu.Sets.Select(x => x.CacheKey).Distinct().Count() + Locale.T(" groupe(s) de cache");
                if (cpu.Vendor == "GenuineIntel" && classes.Count > 1)
                    cpuDetails.Text += "\r\n" + classes[0].Select(x => x.CoreKey).Distinct().Count() + Locale.T(" P-cores identifiés · autres cœurs laissés disponibles pour Windows");
                var profiles = Profiles.Build(cpu);
                foreach (var profile in profiles)
                    profileBox.Items.Add(profile);
                profileBox.SelectedItem = profiles.FirstOrDefault(x => x.Key == settings.ProfileKey) ?? profiles.First(x => x.Recommended);
                if (!String.IsNullOrEmpty(cpu.Warning))
                    AppendLog(cpu.Warning);
            }
            catch (Exception ex)
            {
                cpuLabel.Text = Locale.T("Détection CPU indisponible");
                cpuDetails.Text = Locale.Error(ex);
                settings.AutoEnabled = false;
                applyButton.Enabled = false;
                automatic.Enabled = false;
                AppendLog(Locale.T("Aucun réglage ne sera appliqué : ") + Locale.Error(ex));
            }
            automatic.Checked = settings.AutoEnabled;
            UpdateProfile();
            if (loadWarning.Length > 0)
                AppendLog(loadWarning);
            AppendLog(Locale.T("Seul H1Z1.exe du dossier ROTK choisi peut être ciblé."));
            WireEvents();
            if (!preview)
            {
                tray.Icon = Icon;
                tray.Text = Locale.T("ROTK CPU Manager — détection dédiée");
                tray.Visible = true;
                var menu = new ContextMenuStrip();
                menu.Items.Add(Locale.T("Ouvrir ROTK CPU Manager"), null, delegate
                {
                    ShowWindow();
                });
                menu.Items.Add(Locale.T("Quitter et restaurer"), null, delegate
                {
                    Close();
                });
                tray.ContextMenuStrip = menu;
                tray.DoubleClick += delegate
                {
                    ShowWindow();
                };
                timer.Start();
                RefreshGame();
            }
        }

        private Label MakeLabel(string value, float size, Color color, bool bold)
        {
            return new Label
            {
                Text = value,
                ForeColor = color,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
        private Panel CreateCard()
        {
            return new TacticalPanel { BackColor = card, Dock = DockStyle.Fill, Padding = new Padding(16), Margin = new Padding(0, 0, 0, 10) };
        }
        private void StyleButton(Button button, string caption, bool primary)
        {
            button.Text = caption;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(100, 90, 77);
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(248, 83, 59) : Color.FromArgb(47, 43, 38);
            button.BackColor = primary ? Color.FromArgb(209, 50, 32) : card;
            button.ForeColor = primary ? Color.White : text;
            button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.Height = 40;
            button.UseVisualStyleBackColor = false;
        }

        private void BuildLayout()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            var page = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            page.Controls.Add(layout);
            Controls.Add(page);
            Controls.Add(new WindowChrome(this, true, ChangeLanguage));
            layout.Controls.Add(new TacticalHeader { Dock = DockStyle.Fill, BackColor = background, ForeColor = text, Font = Font, Margin = new Padding(0, 0, 0, 10) }, 0, 0);
            var statusCard = new TacticalPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 28, 25),
                RedEdge = true,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 4, 10, 4)
            };
            status.Dock = DockStyle.Fill;
            status.ForeColor = text;
            status.Text = Locale.T("En attente de ROTK · surveillance désactivée");
            status.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            status.TextAlign = ContentAlignment.MiddleLeft;
            statusCard.Controls.Add(status);
            layout.Controls.Add(statusCard, 0, 1);
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            layout.Controls.Add(body, 0, 2);
            var leftColumn = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 14, 0) };
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftColumn.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            var leftHost = new TacticalScrollView { Dock = DockStyle.Fill, Margin = new Padding(0) };
            var left = new TableLayoutPanel { Dock = DockStyle.Top, Height = 446, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
            foreach (float height in new float[] { 104, 128, 214 })
                left.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            leftHost.SetContent(left);
            leftColumn.Controls.Add(leftHost, 0, 0);
            body.Controls.Add(leftColumn, 0, 0);

            left.Controls.Add(CreateInstallationCard(), 0, 0);

            left.Controls.Add(CreateCpuCard(), 0, 1);

            left.Controls.Add(CreateProfileCard(), 0, 2);

            leftColumn.Controls.Add(CreateActions(), 0, 1);

            body.Controls.Add(CreateProfileGuide(), 1, 0);

            layout.Controls.Add(CreateJournal(), 0, 3);
        }

        private Panel CreateInstallationCard()
        {
            var install = CreateCard();
            var installLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            installLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            installLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            installLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            installLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var installTitle = MakeLabel(Locale.T("01  /  INSTALLATION"), 9, muted, true);
            installLayout.Controls.Add(installTitle, 0, 0);
            installLayout.SetColumnSpan(installTitle, 2);
            folderBox.Dock = DockStyle.Fill;
            folderBox.ReadOnly = true;
            folderBox.BackColor = background;
            folderBox.ForeColor = text;
            folderBox.BorderStyle = BorderStyle.FixedSingle;
            folderBox.AccessibleName = Locale.T("Dossier d'installation ROTK");
            installLayout.Controls.Add(folderBox, 0, 1);
            var browse = new Button { Dock = DockStyle.Fill, Margin = new Padding(10, 0, 0, 0) };
            StyleButton(browse, Locale.T("Choisir…"), false);
            browse.Click += delegate
            {
                ChooseFolder();
            };
            installLayout.Controls.Add(browse, 1, 1);
            install.Controls.Add(installLayout);
            return install;
        }

        private Panel CreateCpuCard()
        {
            var cpuCard = CreateCard();
            var cpuLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            cpuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            cpuLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            cpuLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            cpuLayout.Controls.Add(MakeLabel(Locale.T("02  /  PROCESSEUR"), 9, muted, true), 0, 0);
            cpuLabel.Dock = DockStyle.Fill;
            cpuLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            cpuLabel.AutoEllipsis = true;
            cpuDetails.Dock = DockStyle.Fill;
            cpuDetails.ForeColor = muted;
            cpuDetails.Font = new Font("Segoe UI", 9);
            cpuLayout.Controls.Add(cpuLabel, 0, 1);
            cpuLayout.Controls.Add(cpuDetails, 0, 2);
            cpuCard.Controls.Add(cpuLayout);
            return cpuCard;
        }

        private Panel CreateProfileCard()
        {
            var profileCard = CreateCard();
            var profileLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            foreach (float height in new float[] { 24, 36, 66 })
                profileLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            profileLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            profileLayout.Controls.Add(MakeLabel(Locale.T("03  /  PROFIL CPU"), 9, accent, true), 0, 0);
            profileBox.Dock = DockStyle.Fill;
            profileBox.DropDownStyle = ComboBoxStyle.DropDownList;
            profileBox.DrawMode = DrawMode.OwnerDrawFixed;
            profileBox.ItemHeight = 25;
            profileBox.FlatStyle = FlatStyle.Flat;
            profileBox.DrawItem += delegate (object sender, DrawItemEventArgs e)
            {
                bool selected = (e.State & DrawItemState.Selected) != 0;
                using (var brush = new SolidBrush(selected && (e.State & DrawItemState.ComboBoxEdit) == 0 ? Color.FromArgb(77, 43, 34) : background))
                    e.Graphics.FillRectangle(brush, e.Bounds);
                if (e.Index >= 0)
                    TextRenderer.DrawText(e.Graphics, profileBox.Items[e.Index].ToString(), profileBox.Font,
                        new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height), text,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                e.DrawFocusRectangle();
            };
            profileBox.BackColor = background;
            profileBox.ForeColor = text;
            profileBox.AccessibleName = Locale.T("Profil CPU ROTK");
            profileLayout.Controls.Add(profileBox, 0, 1);
            explanation.Dock = DockStyle.Fill;
            explanation.ForeColor = muted;
            explanation.Font = new Font("Segoe UI", 9);
            explanation.Padding = new Padding(0, 8, 0, 0);
            profileLayout.Controls.Add(explanation, 0, 2);
            selection.Dock = DockStyle.Fill;
            selection.ForeColor = accent;
            selection.Font = new Font("Segoe UI", 9);
            profileLayout.Controls.Add(selection, 0, 3);
            profileCard.Controls.Add(profileLayout);
            return profileCard;
        }

        private TableLayoutPanel CreateActions()
        {
            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = new Padding(0) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47));
            actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            StyleButton(applyButton, Locale.T("APPLIQUER ET SURVEILLER"), true);
            applyButton.Dock = DockStyle.Fill;
            applyButton.Margin = new Padding(0, 0, 8, 4);
            actions.Controls.Add(applyButton, 0, 0);
            var restore = new Button { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) };
            StyleButton(restore, Locale.T("RESTAURER ET ARRÊTER"), false);
            restore.Click += delegate
            {
                StopAndRestore();
            };
            actions.Controls.Add(restore, 1, 0);
            automatic.Text = Locale.T("À chaque lancement du jeu");
            automatic.AutoSize = true;
            automatic.Margin = new Padding(0, 8, 0, 0);
            automatic.ForeColor = muted;
            automatic.Font = new Font("Segoe UI", 9);
            actions.Controls.Add(automatic, 0, 1);
            actions.SetColumnSpan(automatic, 2);
            return actions;
        }

        private Panel CreateProfileGuide()
        {
            var guide = CreateCard();
            guide.Margin = new Padding(0, 0, 0, 10);
            var guideFrame = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
            guideFrame.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            guideFrame.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            var guideScroll = new TacticalScrollView { Dock = DockStyle.Fill, Margin = new Padding(0) };
            var guideLayout = new TableLayoutPanel { Height = 308, ColumnCount = 1, RowCount = 4 };
            foreach (float height in new float[] { 28, 132, 28, 120 })
                guideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            guideLayout.Controls.Add(MakeLabel(Locale.T("EXPLICATION DU PROFIL"), 9, accent, true), 0, 0);
            profileReason.Dock = DockStyle.Fill;
            profileReason.ForeColor = text;
            profileReason.Font = new Font("Segoe UI", 9.5f);
            guideLayout.Controls.Add(profileReason, 0, 1);
            guideLayout.Controls.Add(MakeLabel(Locale.T("COMPARER LES PROFILS"), 9, accent, true), 0, 2);
            guideLayout.Controls.Add(MakeLabel(Locale.T("1. Comparez avec Gestion Windows.\r\n2. Gardez une scène et des options similaires.\r\n3. Regardez surtout les à-coups et le temps par image, puis répétez le test."), 9.5f, muted, false), 0, 3);
            var learn = new Button { Dock = DockStyle.Fill, Margin = new Padding(3, 3, 3, 5) };
            StyleButton(learn, Locale.T("COMPRENDRE LES RÉGLAGES  ›"), false);
            learn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            learn.Click += delegate
            {
                using (var help = new GuideForm())
                    help.ShowDialog(this);
            };
            guideFrame.Controls.Add(learn, 0, 1);
            guideScroll.SetContent(guideLayout);
            guideFrame.Controls.Add(guideScroll, 0, 0);
            guide.Controls.Add(guideFrame);
            return guide;
        }

        private Panel CreateJournal()
        {
            var logPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            journal.Multiline = true;
            journal.ReadOnly = true;
            journal.ScrollBars = ScrollBars.None;
            journal.BackColor = background;
            journal.ForeColor = muted;
            journal.BorderStyle = BorderStyle.FixedSingle;
            journal.Font = new Font("Consolas", 9);
            journal.AccessibleName = Locale.T("Journal de détection et d'application");
            journalScroll.Dock = DockStyle.Fill;
            journalScroll.SetContent(journal);
            logPanel.Controls.Add(journalScroll);

            return logPanel;
        }

        private Profile Selected
        {
            get
            {
                return profileBox.SelectedItem as Profile;
            }
        }
        private void WireEvents()
        {
            timer.Tick += delegate
            {
                RefreshGame();
            };
            profileBox.SelectedIndexChanged += delegate
            {
                if (updating)
                    return;
                UpdateProfile();
                if (Selected == null)
                    return;
                settings.ProfileKey = Selected.Key;
                TrySave();
                RefreshGame();
            };
            applyButton.Click += delegate
            {
                automatic.Checked = true;
                RefreshGame();
            };
            automatic.CheckedChanged += delegate
            {
                if (updating)
                    return;
                if (!automatic.Checked)
                {
                    StopAndRestore();
                    return;
                }
                try
                {
                    settings.Folder = GameGuard.Validate(folderBox.Text);
                    if (Selected == null)
                        throw new InvalidOperationException(Locale.T("Aucun profil CPU disponible."));
                    settings.ProfileKey = Selected.Key;
                    settings.AutoEnabled = true;
                    TrySave();
                    AppendLog(Locale.T("Surveillance activée · ") + Selected.Name);
                    RefreshGame();
                }
                catch (Exception ex) { SetAutomatic(false); ShowError(ex); }
            };
            Resize += delegate
            {
                if (WindowState == FormWindowState.Minimized)
                    Hide();
            };
            FormClosing += delegate (object sender, FormClosingEventArgs e)
            {
                timer.Stop();
                try
                {
                    watcher.RestoreAll();
                }
                catch (Exception ex)
                {
                    SetAutomatic(false);
                    TrySave();
                    e.Cancel = true;
                    ShowWindow();
                    ShowError(ex);
                    timer.Start();
                }
            };
            FormClosed += delegate
            {
                tray.Visible = false;
                tray.Dispose();
                timer.Dispose();
            };
        }

        private void UpdateProfile()
        {
            if (Selected == null)
                return;
            explanation.Text = Selected.Explanation;
            profileReason.Text = GuideForm.ProfileReason(Selected);
            if (Selected.Ids.Length == 0)
                selection.Text = Locale.T("Aucune restriction CPU Sets · gestion Windows");
            else
            {
                var selectedSets = cpu.Sets.Where(x => Selected.Ids.Contains(x.Id)).ToList();
                selection.Text = selectedSets.Select(x => x.CoreKey).Distinct().Count() + Locale.T(" cœurs / ") + selectedSets.Count + Locale.T(" threads sélectionnés\r\nCPU logiques : ")
                    + String.Join(", ", selectedSets.Select(x => x.Logical.ToString()).ToArray());
            }
        }
        private void RefreshGame()
        {
            if (Selected == null || previewMode)
                return;
            try
            {
                watcher.Tick(folderBox.Text, Selected, automatic.Checked);
                status.Text = watcher.Status;
            }
            catch (Exception ex)
            {
                // A vanished/invalid installation must stop enforcement and release existing sessions.
                SetAutomatic(false);
                status.Text = Locale.T("Vérifiez le dossier ROTK · aucun nouveau profil appliqué");
                try
                {
                    watcher.RestoreAll();
                }
                catch (Exception restoreError)
                {
                    status.Text = Locale.T("Surveillance arrêtée · restauration à réessayer");
                    AppendLog(Locale.T("Restauration à réessayer : ") + Locale.Error(restoreError));
                }
                TrySave();
                AppendLog(Locale.Error(ex));
            }
        }
        private void SetAutomatic(bool enabled)
        {
            updating = true;
            automatic.Checked = enabled;
            updating = false;
            settings.AutoEnabled = enabled;
        }
        private void StopAndRestore()
        {
            SetAutomatic(false);
            TrySave();
            try
            {
                watcher.RestoreAll();
                RefreshGame();
            }
            catch (Exception ex) { status.Text = Locale.T("Restauration incomplète · réessayez"); ShowError(ex); }
        }
        private void ChooseFolder()
        {
            using (var dialog = new FolderBrowserDialog { Description = Locale.T("Sélectionnez le dossier contenant H1Z1.exe et rotkc.dll"), ShowNewFolderButton = false, SelectedPath = folderBox.Text })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    string folder = GameGuard.Validate(dialog.SelectedPath);
                    watcher.RestoreAll();
                    folderBox.Text = folder;
                    settings.Folder = folder;
                    TrySave();
                    AppendLog(Locale.T("Installation sélectionnée : ") + folder);
                    RefreshGame();
                }
                catch (Exception ex) { ShowError(ex); }
            }
        }
        private void TrySave()
        {
            if (previewMode)
                return;
            try
            {
                settings.Save();
            }
            catch (Exception ex) { AppendLog(Locale.T("Préférences non enregistrées : ") + Locale.Error(ex)); }
        }
        private void AppendLog(string message)
        {
            if (message == lastMessage)
                return;
            lastMessage = message;
            journal.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
            if (journal.Lines.Length > 150)
                journal.Lines = journal.Lines.Skip(journal.Lines.Length - 100).ToArray();
            journal.SelectionStart = journal.TextLength;
            journalScroll.ScrollToEnd();
        }
        private void ShowError(Exception ex)
        {
            AppendLog(Locale.Error(ex));
            MessageBox.Show(this, Locale.Error(ex), "ROTK CPU Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }
        internal void ChangeLanguage(string language)
        {
            string key = Selected == null ? "" : Selected.Key;
            updating = true;
            try
            {
                Locale.Language = language;
                settings.Language = Locale.Language;
                Locale.Refresh(this, folderBox);
                foreach (var chrome in Controls.OfType<WindowChrome>())
                    chrome.UpdateLanguage();
                if (tray.ContextMenuStrip != null)
                    foreach (ToolStripItem item in tray.ContextMenuStrip.Items)
                        item.Text = Locale.TranslateExisting(item.Text);
                tray.Text = Locale.T("ROTK CPU Manager — détection dédiée");
                if (cpu != null)
                {
                    profileBox.Items.Clear();
                    var profiles = Profiles.Build(cpu);
                    foreach (var profile in profiles)
                        profileBox.Items.Add(profile);
                    profileBox.SelectedItem = profiles.FirstOrDefault(x => x.Key == key) ?? profiles.First(x => x.Recommended);
                }
                UpdateProfile();
                lastMessage = Locale.TranslateExisting(lastMessage);
                TrySave();
            }
            finally { updating = false; }
            RefreshGame();
        }
    }
}
