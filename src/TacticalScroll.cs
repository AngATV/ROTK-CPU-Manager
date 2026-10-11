using System;
using System.Drawing;
using System.Windows.Forms;

namespace RotkCpu
{
    internal sealed class TacticalScrollBar : Control
    {
        private int maximum, viewport = 1, value, dragOffset, wheelRemainder;
        private bool dragging, hover;
        internal event EventHandler ValueChanged;
        internal int Maximum
        {
            get
            {
                return maximum;
            }
        }
        internal int Value
        {
            get
            {
                return value;
            }
            set
            {
                int next = Math.Max(0, Math.Min(maximum, value));
                if (this.value == next)
                    return;
                this.value = next;
                Invalidate();
                if (ValueChanged != null)
                    ValueChanged(this, EventArgs.Empty);
            }
        }
        internal TacticalScrollBar()
        {
            SetStyle(ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Width = 14;
            TabStop = true;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.ScrollBar;
            AccessibleName = "Scroll";
        }
        internal void SetRange(int contentHeight, int viewportHeight)
        {
            viewport = Math.Max(1, viewportHeight);
            maximum = Math.Max(0, contentHeight - viewport);
            Value = value;
            Enabled = maximum > 0;
            Invalidate();
        }
        internal Rectangle Thumb
        {
            get
            {
                int track = Math.Max(1, Height - 8);
                int length = maximum == 0 ? track : Math.Min(track, Math.Max(28, (int)((long)track * viewport / (viewport + maximum))));
                int top = maximum == 0 ? 4 : 4 + (int)((long)(track - length) * value / maximum);
                return new Rectangle(3, top, Math.Max(2, Width - 6), length);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var rail = new SolidBrush(Color.FromArgb(39, 35, 32)))
                e.Graphics.FillRectangle(rail, Width / 2 - 1, 4, 2, Math.Max(0, Height - 8));
            if (!Enabled)
                return;
            using (var brush = new SolidBrush(dragging || hover || Focused
                ? Color.FromArgb(255, 119, 86) : Color.FromArgb(182, 70, 48)))
                e.Graphics.FillRectangle(brush, Thumb);
            if (Thumb.Height >= 36)
                using (var grip = new Pen(Color.FromArgb(37, 26, 21)))
                    for (int i = -1; i <= 1; i++)
                        e.Graphics.DrawLine(grip, 4, Thumb.Top + Thumb.Height / 2 + i * 4, Width - 5, Thumb.Top + Thumb.Height / 2 + i * 4);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled)
                return;
            Focus();
            if (Thumb.Contains(e.Location))
            {
                dragging = true;
                dragOffset = e.Y - Thumb.Top;
                Capture = true;
            }
            else
                Value += e.Y < Thumb.Top ? -viewport : viewport;
            Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging)
            {
                int travel = Math.Max(1, Height - 8 - Thumb.Height);
                Value = (int)((long)(e.Y - dragOffset - 4) * maximum / travel);
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;
            Capture = false;
            Invalidate();
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture)
                dragging = false;
            Invalidate();
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hover = true;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = false;
            Invalidate();
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }
        protected override bool IsInputKey(Keys key)
        {
            return key == Keys.Up || key == Keys.Down || key == Keys.PageUp || key == Keys.PageDown || key == Keys.Home || key == Keys.End || base.IsInputKey(key);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Up:
                    Value -= 36;
                    break;
                case Keys.Down:
                    Value += 36;
                    break;
                case Keys.PageUp:
                    Value -= viewport;
                    break;
                case Keys.PageDown:
                    Value += viewport;
                    break;
                case Keys.Home:
                    Value = 0;
                    break;
                case Keys.End:
                    Value = maximum;
                    break;
                default:
                    return;
            }
            e.Handled = true;
        }
        internal void Wheel(int delta)
        {
            int lines = SystemInformation.MouseWheelScrollLines;
            if (!Enabled || lines == 0)
                return;
            wheelRemainder += delta;
            int ticks = wheelRemainder / 120;
            wheelRemainder %= 120;
            Value -= ticks * (lines < 0 ? viewport : lines * 18);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Wheel(e.Delta);
        }
    }

    internal sealed class TacticalScrollView : UserControl, IMessageFilter
    {
        private readonly Panel viewport = new Panel { Dock = DockStyle.Fill };
        internal readonly TacticalScrollBar Bar = new TacticalScrollBar { Dock = DockStyle.Right };
        private Control content;
        private bool arranging;
        internal TacticalScrollView()
        {
            Controls.Add(viewport);
            Controls.Add(Bar);
            Margin = new Padding(0);
            Application.AddMessageFilter(this);
            Bar.ValueChanged += delegate
            {
                if (content != null)
                    content.Top = -Bar.Value;
            };
            viewport.Resize += delegate
            {
                Arrange();
            };
            viewport.MouseWheel += delegate (object sender, MouseEventArgs e)
            {
                Bar.Wheel(e.Delta);
            };
        }
        internal void SetContent(Control control)
        {
            content = control;
            control.Dock = DockStyle.None;
            control.Location = Point.Empty;
            viewport.Controls.Add(control);
            control.SizeChanged += delegate
            {
                if (!arranging)
                    Arrange();
            };
            control.TextChanged += delegate
            {
                Arrange();
            };
            Arrange();
        }
        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg != 0x20A || !Visible || !Enabled || !IsHandleCreated)
                return false;
            // Global filters also see messages for modal windows; leave those to their own scroll views.
            Control recipient = Control.FromChildHandle(message.HWnd);
            if (recipient == null || recipient.FindForm() != FindForm())
                return false;
            long coordinates = message.LParam.ToInt64();
            Point pointer = new Point((short)(coordinates & 65535), (short)((coordinates >> 16) & 65535));
            if (!RectangleToScreen(ClientRectangle).Contains(pointer))
                return false;
            Control target = viewport;
            for (Control child = target.GetChildAtPoint(target.PointToClient(pointer)); child != null; child = target.GetChildAtPoint(target.PointToClient(pointer)))
            {
                target = child;
                if (target is ComboBox)
                    return false;
            }
            Bar.Wheel((short)((message.WParam.ToInt64() >> 16) & 65535));
            return true;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Application.RemoveMessageFilter(this);
            base.Dispose(disposing);
        }
        internal void Arrange()
        {
            if (content == null || arranging)
                return;
            arranging = true;
            try
            {
                int width = Math.Max(1, viewport.ClientSize.Width);
                content.Width = width;
                var box = content as TextBox;
                if (box != null)
                    content.Height = Math.Max(viewport.Height, TextRenderer.MeasureText(box.Text + "\n", box.Font,
                        new Size(Math.Max(1, width - 8), Int32.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 12);
                var flow = content as FlowLayoutPanel;
                if (flow != null)
                {
                    foreach (Control child in flow.Controls)
                        child.MaximumSize = new Size(Math.Max(1, width - 6), 0);
                    flow.MaximumSize = new Size(width, 0);
                    flow.PerformLayout();
                }
                Bar.SetRange(content.Height, viewport.Height);
                content.Top = -Bar.Value;
            }
            finally { arranging = false; }
        }
        internal void ScrollToEnd()
        {
            Arrange();
            Bar.Value = Bar.Maximum;
        }
        internal void ScrollToStart()
        {
            Bar.Value = 0;
        }
    }
}
