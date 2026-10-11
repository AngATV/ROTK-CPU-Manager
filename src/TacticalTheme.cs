using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RotkCpu
{
    internal sealed class TacticalPanel : Panel
    {
        internal Color LineColor = Color.FromArgb(63, 61, 58);
        internal bool RedEdge;
        internal TacticalPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var line = new Pen(LineColor))
                e.Graphics.DrawRectangle(line, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
            using (var brush = new SolidBrush(RedEdge ? Color.FromArgb(232, 65, 43) : Color.FromArgb(112, 103, 90)))
                e.Graphics.FillRectangle(brush, 0, 0, 3, Height);
            using (var line = new Pen(Color.FromArgb(84, 79, 72)))
            {
                e.Graphics.DrawLine(line, Width - 14, 0, Width - 1, 13);
                e.Graphics.DrawLine(line, Width - 20, 0, Width - 1, 19);
            }
        }
    }

    internal sealed class TacticalHeader : Control
    {
        internal TacticalHeader()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(34, 33, 32)))
            {
                for (int x = Width / 2; x < Width; x += 26)
                    g.DrawLine(pen, x, 0, x - 45, Height);
                for (int y = 10; y < Height; y += 20)
                    g.DrawLine(pen, Width / 2, y, Width, y);
            }
            using (var red = new SolidBrush(Color.FromArgb(232, 65, 43)))
            using (var white = new SolidBrush(Color.FromArgb(242, 234, 220)))
            using (var brandFont = new Font("Impact", 48, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var titleFont = new Font("Bahnschrift SemiCondensed", 27, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var smallFont = new Font("Segoe UI", 11, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                g.FillRectangle(red, 0, 8, 5, 56);
                g.DrawString("ROTK", brandFont, white, 18, 1);
                g.DrawString("CPU MANAGER", titleFont, white, 158, 13);
                g.DrawString("H1Z1  /  Z1 BATTLE ROYALE", smallFont, red, 161, 46);
                using (var scratch = new Pen(BackColor, 2))
                {
                    g.DrawLine(scratch, 24, 31, 139, 28);
                    g.DrawLine(scratch, 92, 11, 87, 49);
                }
                g.FillRectangle(red, 0, Height - 4, 94, 3);
                using (var pen = new Pen(Color.FromArgb(69, 59, 53)))
                    g.DrawLine(pen, 102, Height - 3, Width, Height - 3);
                for (int i = 0; i < 5; i++)
                    g.FillPolygon(red, new Point[] { new Point(Width - 65 + i * 13, Height - 13), new Point(Width - 59 + i * 13, Height - 13),
                        new Point(Width - 65 + i * 13, Height - 5), new Point(Width - 71 + i * 13, Height - 5) });
            }
        }
    }
}
