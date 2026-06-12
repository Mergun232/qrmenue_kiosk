using System;
using System.Drawing;
using System.Windows.Forms;
using QRMENUE.Pavo;

namespace QRMENUE
{
    /// <summary>
    /// Pavo POS Log İzleyici
    /// </summary>
    public partial class PavoPosForm : Form
    {
        private TextBox _logBox;
        private readonly int _maxLines = 500;

        public PavoPosForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = AppDataLoader.Data?.Form_PavoPosTitle ?? "Pavo POS Log İzleyici";
            this.Size = new Size(720, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            _logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                BackColor = Color.FromArgb(30, 30, 35),
                ForeColor = Color.LightGreen,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false
            };

            Panel topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(40, 40, 45)
            };

            Button btnClear = new Button
            {
                Text = "Temizle",
                Width = 100,
                Height = 28,
                Top = 6,
                Left = 10,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => {
                if (_logBox != null)
                {
                    _logBox.Clear();
                }
            };
            topPanel.Controls.Add(btnClear);

            this.Controls.Add(_logBox);
            this.Controls.Add(topPanel);
            _logBox.BringToFront();

            this.Load += PavoPosForm_Load;
            this.FormClosing += PavoPosForm_FormClosing;

            this.ResumeLayout(false);
        }

        private void PavoPosForm_Load(object sender, EventArgs e)
        {
            PavoPosSocketBridge.Register(OnSocketLog, null);
        }

        private void PavoPosForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            PavoPosSocketBridge.Unregister();
        }

        private void OnSocketLog(string tag, string msg)
        {
            if (_logBox == null || _logBox.IsDisposed) return;
            if (_logBox.InvokeRequired)
            {
                _logBox.BeginInvoke(new Action(() => AppendLog(tag, msg)));
                return;
            }
            AppendLog(tag, msg);
        }

        private void AppendLog(string tag, string msg)
        {
            if (_logBox == null || _logBox.IsDisposed) return;
            if (_logBox.InvokeRequired)
            {
                try { _logBox.BeginInvoke(new Action(() => AppendLog(tag, msg))); } catch { }
                return;
            }
            try
            {
                var line = DateTime.Now.ToString("HH:mm:ss") + " [" + (tag ?? "") + "] " + (msg ?? "");
                _logBox.AppendText(line + Environment.NewLine);

                if (_logBox.Lines.Length > _maxLines + 50)
                {
                    var lines = _logBox.Lines;
                    var newLines = new string[_maxLines];
                    Array.Copy(lines, lines.Length - _maxLines, newLines, 0, _maxLines);
                    _logBox.Lines = newLines;
                    _logBox.SelectionStart = _logBox.Text.Length;
                    _logBox.ScrollToCaret();
                }
            }
            catch { }
        }
    }
}
