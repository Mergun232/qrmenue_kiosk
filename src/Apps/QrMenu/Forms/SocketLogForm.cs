using System;
using System.Drawing;
using System.Windows.Forms;

namespace QRMENUE
{
    /// <summary>Socket dinleme sonuçlarını listeler (test için; sonra kaldırılabilir).</summary>
    public class SocketLogForm : Form
    {
        private TextBox _textBox;
        private readonly int _maxLines = 500;

        public SocketLogForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = AppDataLoader.Data?.Form_SocketLogTitle ?? "Socket olayları (test)";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            _textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false
            };
            this.Controls.Add(_textBox);
            this.ResumeLayout(false);
        }

        /// <summary>Olay adı ve veriyi listeye ekler (thread-safe, UI thread'e marshal eder).</summary>
        public void Append(string eventName, string payload)
        {
            if (_textBox == null || _textBox.IsDisposed) return;
            if (_textBox.InvokeRequired)
            {
                _textBox.BeginInvoke(new Action(() => Append(eventName, payload)));
                return;
            }
            var line = DateTime.Now.ToString("HH:mm:ss") + " [" + eventName + "] " + (payload ?? "");
            _textBox.AppendText(line + Environment.NewLine);

            if (_textBox.Lines.Length > _maxLines + 50)
            {
                var lines = _textBox.Lines;
                var newLines = new string[_maxLines];
                Array.Copy(lines, lines.Length - _maxLines, newLines, 0, _maxLines);
                _textBox.Lines = newLines;
                _textBox.SelectionStart = _textBox.Text.Length;
                _textBox.ScrollToCaret();
            }
        }
    }
}
