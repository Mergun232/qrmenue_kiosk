using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QRMENUE
{
    /// <summary>Yuvarlatılmış köşeli, ikonlu input container.</summary>
    public class RoundedInputPanel : Panel
    {
        public RoundedInputPanel()
        {
            BackColor = Color.FromArgb(245, 245, 248);
            Padding = new Padding(14, 10, 14, 10);
            DoubleBuffered = true;
        }

        protected override void OnResize(System.EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedCorners();
        }

        protected override void OnHandleCreated(System.EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundedCorners();
        }

        private void ApplyRoundedCorners()
        {
            if (Width < 4 || Height < 4) return;
            int r = 10;
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, r * 2, r * 2, 180, 90);
                path.AddArc(Width - r * 2, 0, r * 2, r * 2, 270, 90);
                path.AddArc(Width - r * 2, Height - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(0, Height - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                Region = new Region(path);
            }
        }
    }
}
