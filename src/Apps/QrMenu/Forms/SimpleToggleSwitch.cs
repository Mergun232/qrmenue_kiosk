using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QRMENUE
{
    /// <summary>Beyaz ayar kartları için basit aç/kapa anahtarı.</summary>
    public class SimpleToggleSwitch : Control
    {
        private bool _checked;

        public bool ToggleChecked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                ToggleCheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public event EventHandler ToggleCheckedChanged;

        public SimpleToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(52, 28);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            ToggleChecked = !ToggleChecked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int h = Height;
            int w = Width;
            int pad = 2;
            var track = new Rectangle(pad, pad, w - pad * 2, h - pad * 2);
            Color trackColor = _checked ? Color.FromArgb(140, 110, 200) : Color.FromArgb(200, 200, 210);
            using (var path = RoundedRect(track, track.Height / 2))
            using (var b = new SolidBrush(trackColor))
                g.FillPath(b, path);

            int knobSize = h - 8;
            int knobX = _checked ? w - knobSize - 4 : 4;
            int knobY = (h - knobSize) / 2;
            using (var k = new SolidBrush(Color.White))
            using (var path = RoundedRect(new Rectangle(knobX, knobY, knobSize, knobSize), knobSize / 2))
                g.FillPath(k, path);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
