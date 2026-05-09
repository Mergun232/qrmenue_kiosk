using Qrmenue.DTO;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QRMENUE
{
    public partial class SettingsForm : Form
    {
        private KioskAppSettingsDTO _model;
        private Panel _header;
        private Button _btnClose;
        private Panel _scrollHost;
        private SimpleToggleSwitch _toggleStartup;
        private SimpleToggleSwitch _toggleAutoLogin;
        private Button _btnTr;
        private Button _btnDe;
        private Button _btnFr;
        private Button _btnEn;
        private Label _lblHeadGear;
        private Label _lblHeadTitle;

        public SettingsForm()
        {
            InitializeComponent();
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(this, true);
            _model = KioskSettingsStore.Load() ?? new KioskAppSettingsDTO();
            BuildLayout();
            ApplyModelToUi();
        }

        private void BuildLayout()
        {
            var d = AppDataLoader.Data;
            string settingsTitle = string.IsNullOrWhiteSpace(d?.Settings_Title) ? "Ayarlar" : d.Settings_Title;
            Text = settingsTitle;

            _header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(244, 244, 248)
            };
            _lblHeadGear = new Label
            {
                AutoSize = false,
                Size = new Size(32, 32),
                Location = new Point(18, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(45, 45, 55),
                Text = "\uE713",
                Cursor = Cursors.Default
            };
            ApplyMdl2SettingsGlyph(_lblHeadGear, 16f);

            _lblHeadTitle = new Label
            {
                Text = settingsTitle,
                Font = new Font("Segoe UI Semibold", 12.5f),
                ForeColor = Color.FromArgb(45, 45, 55),
                AutoSize = true,
                Location = new Point(50, 14),
                BackColor = Color.Transparent
            };

            _btnClose = new Button
            {
                Text = "✕",
                Size = new Size(36, 36),
                Location = new Point(ClientSize.Width - 44, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(80, 80, 90),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 238);
            _btnClose.Click += (s, e) => Close();

            _header.Controls.Add(_lblHeadGear);
            _header.Controls.Add(_lblHeadTitle);
            _header.Controls.Add(_btnClose);

            _scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 16)
            };

            // Manuel konumlu kartlar Padding ile kaymaz; sol/üst boşluk burada
            int insetL = 28;
            int insetT = 26;
            int insetR = 28;
            int contentW = Math.Max(360, ClientSize.Width - insetL - insetR);
            int gap = 12;
            int y = insetT;

            _toggleStartup = new SimpleToggleSwitch();
            y += AddCard(_scrollHost, insetL, y, contentW, gap,
                string.IsNullOrWhiteSpace(d?.Settings_AutoStart) ? "PC başladığında otomatik başla" : d.Settings_AutoStart,
                string.IsNullOrWhiteSpace(d?.Settings_AutoStart_Desc) ? "Windows açıldığında uygulama otomatik çalışır" : d.Settings_AutoStart_Desc,
                _toggleStartup);

            _toggleAutoLogin = new SimpleToggleSwitch();
            y += AddCard(_scrollHost, insetL, y, contentW, gap,
                string.IsNullOrWhiteSpace(d?.Settings_AutoLogin) ? "Otomatik giriş yap" : d.Settings_AutoLogin,
                string.IsNullOrWhiteSpace(d?.Settings_AutoLogin_Desc) ? "Kayıtlı bilgilerle otomatik giriş yapılır" : d.Settings_AutoLogin_Desc,
                _toggleAutoLogin);

            y += AddLanguageCard(_scrollHost, insetL, y, contentW, gap, d);

            y += AddExitButton(_scrollHost, insetL, y, contentW, gap, d);

            Controls.Add(_scrollHost);
            Controls.Add(_header);

            Resize += (s, e) => { _btnClose.Left = ClientSize.Width - 44; };
        }

        private int AddCard(Panel host, int left, int y, int cardW, int gap, string title, string desc, SimpleToggleSwitch toggle)
        {
            int h = 88;
            var card = new Panel
            {
                Location = new Point(left, y),
                Size = new Size(cardW, h),
                BackColor = Color.White
            };
            card.Paint += Card_OnPaint;

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = Color.FromArgb(35, 35, 45),
                AutoSize = false,
                Location = new Point(16, 14),
                Size = new Size(cardW - 100, 22),
                BackColor = Color.Transparent
            };
            var lblD = new Label
            {
                Text = desc,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(120, 120, 130),
                AutoSize = false,
                Location = new Point(16, 38),
                Size = new Size(cardW - 100, 40),
                BackColor = Color.Transparent
            };
            toggle.Location = new Point(cardW - 16 - toggle.Width, (h - toggle.Height) / 2);
            toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            card.Controls.Add(lblT);
            card.Controls.Add(lblD);
            card.Controls.Add(toggle);
            host.Controls.Add(card);
            return h + gap;
        }

        private int AddLanguageCard(Panel host, int left, int y, int cardW, int gap, AppDataDTO d)
        {
            int h = 100;
            var card = new Panel
            {
                Location = new Point(left, y),
                Size = new Size(cardW, h),
                BackColor = Color.White
            };
            card.Paint += Card_OnPaint;

            int bw = 52, bh = 34, gapBtn = 4;
            int buttonsRowW = bw * 4 + gapBtn * 3;
            int bx = Math.Max(16, cardW - 16 - buttonsRowW);
            int by = (h - bh) / 2;
            // Açıklama etiketi tüm kart genişliğine yayılırsa düğümlerin üstüne binip tıklamayı engelleyebilir
            int descMaxW = Math.Max(80, bx - 24 - 16);

            var lblT = new Label
            {
                Text = string.IsNullOrWhiteSpace(d?.Settings_Language) ? "Dil" : d.Settings_Language,
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = Color.FromArgb(35, 35, 45),
                AutoSize = false,
                Location = new Point(16, 14),
                Size = new Size(descMaxW, 22),
                BackColor = Color.Transparent
            };
            var lblD = new Label
            {
                Text = string.IsNullOrWhiteSpace(d?.Settings_Language_Desc) ? "Uygulama dilini seçin" : d.Settings_Language_Desc,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(120, 120, 130),
                AutoSize = false,
                Location = new Point(16, 38),
                Size = new Size(descMaxW, 44),
                BackColor = Color.Transparent
            };

            _btnTr = LangButton("TR", bx, by, bw, bh);
            _btnDe = LangButton("DE", bx + bw + gapBtn, by, bw, bh);
            _btnFr = LangButton("FR", bx + (bw + gapBtn) * 2, by, bw, bh);
            _btnEn = LangButton("EN", bx + (bw + gapBtn) * 3, by, bw, bh);
            _btnTr.Click += (s, e) => SetLang("TR");
            _btnDe.Click += (s, e) => SetLang("DE");
            _btnFr.Click += (s, e) => SetLang("FR");
            _btnEn.Click += (s, e) => SetLang("EN");

            card.Controls.Add(lblT);
            card.Controls.Add(lblD);
            card.Controls.Add(_btnTr);
            card.Controls.Add(_btnDe);
            card.Controls.Add(_btnFr);
            card.Controls.Add(_btnEn);
            foreach (var b in new[] { _btnTr, _btnDe, _btnFr, _btnEn })
                b?.BringToFront();
            host.Controls.Add(card);
            return h + gap;
        }

        private static Button LangButton(string text, int x, int y, int w, int h)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9f),
                Cursor = Cursors.Hand,
                TabStop = true,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        private int AddExitButton(Panel host, int left, int y, int cardW, int gap, AppDataDTO d)
        {
            int h = 52;
            var btn = new Button
            {
                Text = "Uygulamayı Kapat",
                Location = new Point(left, y),
                Size = new Size(cardW, h),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 11f),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                TabStop = true,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) =>
            {
                var msg = "Uygulamayı kapatmak istediğinize emin misiniz?";
                var title = "Uygulamayı Kapat";
                if (_model != null)
                {
                    // Varsa mevcut çevirileri kullanmaya çalışalım, yoksa varsayılan
                    if (_model.UiLanguage == "DE") { msg = "Möchten Sie die Anwendung wirklich schließen?"; title = "Anwendung schließen"; }
                    else if (_model.UiLanguage == "EN") { msg = "Are you sure you want to close the application?"; title = "Close Application"; }
                    else if (_model.UiLanguage == "FR") { msg = "Voulez-vous vraiment fermer l'application?"; title = "Fermer l'application"; }
                }

                if (MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };
            host.Controls.Add(btn);
            return h + gap;
        }

        private void Card_OnPaint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel p)) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = p.ClientRectangle;
            r.Width--;
            r.Height--;
            using (var pen = new Pen(Color.FromArgb(225, 226, 235), 1f))
            using (var path = RoundedRect(r, 10))
                e.Graphics.DrawPath(pen, path);
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

        private void SetLang(string code)
        {
            if (_model == null) return;
            _model.UiLanguage = code ?? "TR";
            UpdateLangButtons();
            // Anında kaydet + uygula; pencere açıkken de dil değişsin
            KioskSettingsStore.Save(_model);
            KioskSettingsStore.ApplyRunOnStartup(_model.RunOnWindowsStartup);
            AppDataLoader.ReloadWithLanguage(_model.UiLanguage ?? "TR");
            if (Owner is LoginForm lf)
                lf.RefreshLocalizedTexts();
            string newTitle = AppDataLoader.Data?.Settings_Title;
            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                Text = newTitle;
                if (_lblHeadTitle != null)
                    _lblHeadTitle.Text = newTitle;
            }
        }

        private void ApplyModelToUi()
        {
            _toggleStartup.ToggleChecked = _model.RunOnWindowsStartup;
            _toggleAutoLogin.ToggleChecked = _model.AutoLogin;
            UpdateLangButtons();
        }

        private void UpdateLangButtons()
        {
            string lang = (_model.UiLanguage ?? "TR").ToUpperInvariant();
            StyleLangBtn(_btnTr, lang == "TR");
            StyleLangBtn(_btnDe, lang == "DE");
            StyleLangBtn(_btnFr, lang == "FR");
            StyleLangBtn(_btnEn, lang == "EN");
        }

        /// <summary>LoginForm ile aynı: Segoe MDL2 / Fluent Settings (E713).</summary>
        private static void ApplyMdl2SettingsGlyph(Control c, float em)
        {
            if (c == null) return;
            const string mdl = "\uE713";
            foreach (var familyName in new[] { "Segoe MDL2 Assets", "Segoe Fluent Icons" })
            {
                try
                {
                    using (var ff = new FontFamily(familyName))
                    {
                        c.Font = new Font(ff, em, FontStyle.Regular, GraphicsUnit.Point);
                        c.Text = mdl;
                        return;
                    }
                }
                catch (ArgumentException) { }
            }
            c.Font = new Font("Segoe UI Symbol", em, FontStyle.Regular, GraphicsUnit.Point);
            c.Text = "\u2699";
        }

        private static void StyleLangBtn(Button b, bool sel)
        {
            if (b == null) return;
            if (sel)
            {
                b.BackColor = Color.FromArgb(120, 80, 180);
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderColor = Color.FromArgb(100, 60, 160);
            }
            else
            {
                b.BackColor = Color.FromArgb(238, 238, 244);
                b.ForeColor = Color.FromArgb(55, 55, 65);
                b.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 220);
            }
        }

        private void SaveFromUi()
        {
            _model.RunOnWindowsStartup = _toggleStartup.ToggleChecked;
            _model.AutoLogin = _toggleAutoLogin.ToggleChecked;
            KioskSettingsStore.Save(_model);
            KioskSettingsStore.ApplyRunOnStartup(_model.RunOnWindowsStartup);
            AppDataLoader.ReloadWithLanguage(_model.UiLanguage ?? "TR");
            if (Owner is LoginForm lf)
                lf.RefreshLocalizedTexts();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveFromUi();
            base.OnFormClosing(e);
        }
    }
}
