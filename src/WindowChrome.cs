using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RotkCpu
{
    internal class TacticalForm : Form
    {
        internal TacticalForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateWorkArea();
        }
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            UpdateWorkArea();
        }
        private void UpdateWorkArea()
        {
            if (IsHandleCreated && WindowState == FormWindowState.Normal)
                MaximizedBounds = Screen.FromHandle(Handle).WorkingArea;
        }
        protected override void WndProc(ref Message m)
        {
            const int hitTest = 0x84;
            if (m.Msg == hitTest && WindowState == FormWindowState.Normal)
            {
                long position = m.LParam.ToInt64();
                Point point = PointToClient(new Point((short)(position & 65535), (short)((position >> 16) & 65535)));
                int edge = Math.Max(6, (int)(6 * CreateGraphicsDpi()));
                bool left = point.X < edge, right = point.X >= ClientSize.Width - edge;
                bool top = point.Y < edge, bottom = point.Y >= ClientSize.Height - edge;
                int hit = top ? (left ? 13 : right ? 14 : 12) : bottom ? (left ? 16 : right ? 17 : 15) : left ? 10 : right ? 11 : 0;
                if (hit != 0)
                {
                    m.Result = new IntPtr(hit);
                    return;
                }
            }
            base.WndProc(ref m);
        }
        private float CreateGraphicsDpi()
        {
            using (var graphics = CreateGraphics())
                return graphics.DpiX / 96f;
        }
    }

    internal sealed class WindowChrome : Panel
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
        internal WindowChrome(Form owner, bool languagePicker, Action<string> changeLanguage)
        {
            Dock = DockStyle.Top;
            Height = 38;
            BackColor = Color.FromArgb(24, 24, 25);
            Padding = new Padding(14, 0, 6, 0);
            DoubleBuffered = true;
            var title = new Label
            {
                Text = "ROTK CPU Manager",
                Dock = DockStyle.Left,
                Width = 210,
                ForeColor = Color.FromArgb(188, 176, 157),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(title);
            MouseDown += delegate (object sender, MouseEventArgs e)
            {
                Drag(owner, e);
            };
            title.MouseDown += delegate (object sender, MouseEventArgs e)
            {
                Drag(owner, e);
            };
            DoubleClick += delegate
            {
                ToggleMax(owner);
            };
            title.DoubleClick += delegate
            {
                ToggleMax(owner);
            };
            if (languagePicker)
            {
                var languages = new FlowLayoutPanel
                {
                    Dock = DockStyle.Right,
                    Width = 112,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    Padding = new Padding(0, 7, 8, 0)
                };
                foreach (string code in new string[] { "fr", "en" })
                {
                    string selected = code;
                    var button = new Button
                    {
                        Text = code.ToUpperInvariant(),
                        Width = 43,
                        Height = 24,
                        FlatStyle = FlatStyle.Flat,
                        Margin = new Padding(1, 0, 1, 0),
                        TabStop = true,
                        ForeColor = Color.FromArgb(235, 224, 207),
                        BackColor = Locale.Language == code ? Color.FromArgb(153, 49, 34) : BackColor,
                        Font = new Font("Segoe UI", 8, FontStyle.Bold)
                    };
                    button.FlatAppearance.BorderColor = Color.FromArgb(77, 65, 55);
                    button.AccessibleName = code == "fr" ? "Français" : "English";
                    button.Click += delegate
                    {
                        if (Locale.Language == selected)
                            return;
                        changeLanguage(selected);
                        UpdateLanguage();
                    };
                    languages.Controls.Add(button);
                }
                Controls.Add(languages);
            }
            AddButton("—", "Réduire la fenêtre", delegate
            {
                owner.WindowState = FormWindowState.Minimized;
            }, false);
            AddButton("□", "Agrandir / restaurer la fenêtre", delegate
            {
                ToggleMax(owner);
            }, false);
            AddButton("×", "Fermer et restaurer", delegate
            {
                owner.Close();
            }, true);
        }
        internal void UpdateLanguage()
        {
            foreach (Control control in Controls)
                if (control is FlowLayoutPanel)
                    foreach (Control button in control.Controls)
                        button.BackColor = button.Text == Locale.Language.ToUpperInvariant() ? Color.FromArgb(153, 49, 34) : BackColor;
        }
        private void AddButton(string glyph, string description, EventHandler click, bool close)
        {
            var button = new Button
            {
                Text = glyph,
                Dock = DockStyle.Right,
                Width = 44,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(222, 210, 189),
                Font = new Font("Segoe UI", 15),
                TabStop = true,
                AccessibleName = Locale.T(description),
                BackColor = BackColor
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = close ? Color.FromArgb(194, 48, 31) : Color.FromArgb(60, 49, 40);
            button.Click += click;
            Controls.Add(button);
        }
        private static void ToggleMax(Form owner)
        {
            owner.WindowState = owner.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }
        private static void Drag(Form owner, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            ReleaseCapture();
            SendMessage(owner.Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
        }
    }
}
