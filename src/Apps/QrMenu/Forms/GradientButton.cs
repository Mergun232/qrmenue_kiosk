using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QRMENUE
{
    /// <summary>Mor-mavi veya kırmızı gradient arka planlı modern buton.</summary>
    public class GradientButton : Panel
    {
        private bool _hover;
        private string _text = "Giriş Yap";

        /// <summary>true ise kırmızı gradient (Çıkış), false ise mor-mavi (Giriş Yap).</summary>
        public bool IsRedTheme { get; set; }

        public GradientButton()
        {
            Size = new Size(356, 48);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            Font = new Font("Segoe UI Semibold", 11f);
            ForeColor = Color.White;
        }

        public override string Text
        {
            get => _text;
            set { _text = value ?? ""; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int r = 12;
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(Width - r * 2, 0, r * 2, r * 2, 270, 90);
                path.AddArc(Width - r * 2, Height - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(0, Height - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                Color c1, c2, h1, h2, h3;
                if (IsRedTheme)
                {
                    c1 = Color.FromArgb(200, 50, 50);
                    c2 = Color.FromArgb(180, 30, 30);
                    h1 = Color.FromArgb(220, 70, 70);
                    h2 = Color.FromArgb(200, 50, 50);
                    h3 = Color.FromArgb(190, 40, 40);
                }
                else
                {
                    c1 = Color.FromArgb(120, 80, 180);
                    c2 = Color.FromArgb(0, 120, 215);
                    h1 = Color.FromArgb(140, 100, 200);
                    h2 = Color.FromArgb(30, 140, 230);
                    h3 = Color.FromArgb(20, 130, 220);
                }
                using (var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, Width, Height),
                    c1, c2,
                    LinearGradientMode.Horizontal))
                {
                    if (_hover)
                    {
                        var cb = new ColorBlend(3);
                        cb.Colors = new[] { h1, h2, h3 };
                        cb.Positions = new[] { 0f, 0.5f, 1f };
                        brush.InterpolationColors = cb;
                    }
                    g.FillPath(brush, path);
                }
            }
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_text, Font, new SolidBrush(ForeColor), new RectangleF(0, 0, Width, Height), sf);
        }
    }
}
