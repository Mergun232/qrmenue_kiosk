using Qrmenue;
using Qrmenue.DTO;
using QRMENUE;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using WebBrowser;


namespace QRMENUE
{
    public partial class LoginForm : Form
    {

        int Mouse_X, Mouse_Y, Movee;
        private bool _loginInProgress;
        private string _deviceId;

        public LoginForm()
        {
            InitializeComponent();
            // Dalgalanma (flicker) önleme
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(this, true);
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(panelLogin, true);
#if !DEBUG
            this.FormBorderStyle = FormBorderStyle.None;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.WindowState = FormWindowState.Normal;
            this.StartPosition = FormStartPosition.Manual;
#endif
            CenterLoginPanel();
        }

        private void picEyeIcon_Click(object sender, EventArgs e)
        {
            if (txtPassword.PasswordChar == '●')
            {
                txtPassword.PasswordChar = '\0';
                LoadEyeIcon(showPassword: true);
            }
            else
            {
                txtPassword.PasswordChar = '●';
                LoadEyeIcon(showPassword: false);
            }
        }

        /// <summary>Şifre göster/gizle ikonunu günceller. Gizliyken eye.png (tıkla=göster), görünürken eye_hidden.png (tıkla=gizle).</summary>
        private void LoadEyeIcon(bool showPassword)
        {
            if (picEyeIcon == null) return;
            string basePath = Path.Combine(AppPaths.InstallDirectory, "images");
            string path = showPassword
                ? Path.Combine(basePath, "eye_hidden.png")
                : Path.Combine(basePath, "eye.png");
            TryLoadIcon(picEyeIcon, path);
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            SuspendLayout();
            try
            {
                Application.AddMessageFilter(new HiddenMinimizeFilter());
                ApplyAppDataTexts();
                ApplyIconFromImages();
                LoadLogo();
                LoadIcons();
                ApplySettingsGearGlyph();
                ApplyRoundedPanel();
                CenterLoginPanel();
            }
            finally
            {
                ResumeLayout(true);
            }
#if DEBUG
            // Debug modunda Pavo POS test formunu aç - manuel test için (socket ayarlanmadan)
            try
            {
                var pavoForm = new PavoPosForm();
                pavoForm.Show();
            }
            catch { }
#endif
            try
            {
                string cfgPath = AppPaths.ConfigJsonPath;
                if (!File.Exists(cfgPath)) throw new FileNotFoundException();
                var config = new JavaScriptSerializer().Deserialize<ConfigDTO>(File.ReadAllText(cfgPath));

                if (config != null)
                {
                    txtCompanyCode.Text = config.CompanyCode;
                    txtUserName.Text = config.Username;
                    txtPassword.Text = config.Password;
                    _deviceId = config.DeviceId;
                    // config.json yalnızca "Beni hatırla" ile girişte yazılıyor; alanlar doluysa kutuyu ve ikonu eşitle
                    if (!string.IsNullOrWhiteSpace(config.Username)
                        || !string.IsNullOrWhiteSpace(config.CompanyCode)
                        || !string.IsNullOrWhiteSpace(config.Password))
                    {
                        chkRememberMe.Checked = true;
                        LoadRememberMeIcon();
                    }
                }
            }
            catch
            { }

            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = Guid.NewGuid().ToString();
            }

            try
            {
                var ks = KioskSettingsStore.Load();
                if (ks.AutoLogin
                    && !string.IsNullOrWhiteSpace(txtCompanyCode.Text)
                    && !string.IsNullOrWhiteSpace(txtUserName.Text)
                    && !string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    var autoTimer = new System.Windows.Forms.Timer { Interval = 550 };
                    autoTimer.Tick += (s, ev) =>
                    {
                        autoTimer.Stop();
                        autoTimer.Dispose();
                        RunLoginFlow();
                    };
                    autoTimer.Start();
                }
            }
            catch { }
        }

        /// <summary>Ayarlar kaydedildikten sonra app_data / dil metinlerini yeniler.</summary>
        public void RefreshLocalizedTexts()
        {
            ApplyAppDataTexts();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var f = new SettingsForm())
            {
                f.Owner = this;
                f.ShowDialog(this);
            }
        }

        private void LoginForm_Resize(object sender, EventArgs e)
        {
            CenterLoginPanel();
        }

        private void CenterLoginPanel()
        {
            if (panelLogin == null) return;
            int x = (this.ClientSize.Width - panelLogin.Width) / 2;
            int y = (this.ClientSize.Height - panelLogin.Height) / 2;
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            panelLogin.Location = new System.Drawing.Point(x, y);
        }

        private void ApplyAppDataTexts()
        {
            var d = QRMENUE.AppDataLoader.Data;
            if (d == null) return;
            if (lblRememberMe != null) lblRememberMe.Text = d.Text4 ?? "Beni hatırla";
            if (label4 != null) label4.Text = !string.IsNullOrWhiteSpace(d.Login_Title) ? d.Login_Title : (d.Text13 ?? label4.Text);
            if (gradientButton != null) gradientButton.Text = !string.IsNullOrWhiteSpace(d.Login_Button) ? d.Login_Button : (d.Text14 ?? "Giriş Yap");
            if (btnExit != null) btnExit.Text = d.Text16 ?? btnExit.Text;
            this.Text = d.Text14 ?? this.Text;
        }

        /// <summary>images/ klasöründeki simgeyi form penceresinde kullanır.</summary>
        private void ApplyIconFromImages()
        {
            string iconPath = Path.Combine(AppPaths.InstallDirectory, "images", "favicon_multi.ico");
            if (!File.Exists(iconPath)) return;
            try
            {
                this.Icon = new Icon(iconPath);
            }
            catch { }
        }

        /// <summary>images/logo.png varsa PictureBox'ta gösterir; yoksa alanı gizler.</summary>
        private void LoadLogo()
        {
            if (picLogo == null) return;
            string logoPath = Path.Combine(AppPaths.InstallDirectory, "images", "logo.png");
            if (!File.Exists(logoPath))
            {
                picLogo.Visible = false;
                return;
            }
            try
            {
                picLogo.Image?.Dispose();
                picLogo.Image = Image.FromFile(logoPath);
                picLogo.Visible = true;
            }
            catch
            {
                picLogo.Visible = false;
            }
        }

        /// <summary>Input alanlarındaki ikonları yükler. images/icon_company.png, icon_user.png, icon_key.png, eye.png</summary>
        private void LoadIcons()
        {
            string basePath = Path.Combine(AppPaths.InstallDirectory, "images");
            TryLoadIcon(picIconCompany, Path.Combine(basePath, "icon_company.png"));
            TryLoadIcon(picIconUser, Path.Combine(basePath, "icon_user.png"));
            TryLoadIcon(picIconKey, Path.Combine(basePath, "icon_key.png"));
            LoadEyeIcon(showPassword: false);
            LoadRememberMeIcon();
        }

        /// <summary>Segoe MDL2 / Fluent Settings glifi (E713); sistemde yoksa klasik dişli Unicode.</summary>
        private void ApplySettingsGearGlyph()
        {
            if (btnSettings == null) return;
            const string mdlSettings = "\uE713";
            float em = 17f;
            foreach (var familyName in new[] { "Segoe MDL2 Assets", "Segoe Fluent Icons" })
            {
                try
                {
                    using (var ff = new FontFamily(familyName))
                    {
                        btnSettings.Font = new Font(ff, em, FontStyle.Regular, GraphicsUnit.Point);
                        btnSettings.Text = mdlSettings;
                        btnSettings.ForeColor = Color.FromArgb(75, 75, 88);
                        return;
                    }
                }
                catch (ArgumentException) { }
            }
            btnSettings.Font = new Font("Segoe UI Symbol", 16f, FontStyle.Regular, GraphicsUnit.Point);
            btnSettings.Text = "\u2699";
            btnSettings.ForeColor = Color.FromArgb(75, 75, 88);
        }

        private void TryLoadIcon(PictureBox pic, string path)
        {
            if (pic == null) return;
            if (!File.Exists(path)) { pic.Visible = false; return; }
            try
            {
                pic.Image?.Dispose();
                pic.Image = Image.FromFile(path);
                pic.Visible = true;
            }
            catch { pic.Visible = false; }
        }

        /// <summary>Giriş paneline yuvarlatılmış köşe (oval hat) uygular.</summary>
        private void ApplyRoundedPanel()
        {
            if (panelLogin == null) return;
            int radius = 24;
            int w = panelLogin.Width;
            int h = panelLogin.Height;
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                path.AddArc(w - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                path.AddArc(w - radius * 2, h - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(0, h - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();
                panelLogin.Region = new Region(path);
            }
        }

        private void gradientButton_Click(object sender, EventArgs e)
        {
            RunLoginFlow();
        }

        private void RunLoginFlow()
        {
            if (_loginInProgress) return;
            if (string.IsNullOrEmpty(txtCompanyCode.Text) || string.IsNullOrEmpty(txtUserName.Text) || string.IsNullOrEmpty(txtPassword.Text))
            {
                string msg = QRMENUE.AppDataLoader.Data?.Text9 ?? "Lütfen tüm alanları doldurunuz!";
                string title = QRMENUE.AppDataLoader.Data?.Text10 ?? "Hata";
                MessageBox.Show(msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            else
            {
                _loginInProgress = true;
                try
                {
                    // API exe: POST /api/exe/login. Şifre sunucuda MD5 ile karşılaştırılıyor; düz metin gönderiyoruz (CURL örneği ile uyumlu).
                    var parameters = new ListDictionary();
                    parameters.Add("company_code", txtCompanyCode.Text.Trim());
                    parameters.Add("nick_name", txtUserName.Text.Trim());
                    parameters.Add("password", txtPassword.Text);
                    parameters.Add("remember_me", chkRememberMe.Checked ? 1 : 0);
                    parameters.Add("platform", 5); // 5 = Kiosk
                    parameters.Add("device_id", _deviceId);

                    string loginUrl = AppDataLoader.GetApiUrl("api/exe/login");
                    if (string.IsNullOrEmpty(loginUrl))
                    {
                        var d = AppDataLoader.Data;
                        MessageBox.Show(d?.Login_ApiError ?? "API adresi tanımlı değil.", d?.Text10 ?? "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[LOGIN] İstek URL: " + loginUrl);
                    System.Diagnostics.Debug.WriteLine("[LOGIN] Gönderilen: company_code=" + txtCompanyCode.Text.Trim() + ", nick_name=" + txtUserName.Text.Trim() + ", remember_me=" + (chkRememberMe.Checked ? 1 : 0) + ", platform=5, device_id=" + _deviceId);
#endif
                    string json = Helpers.HttpHelper(loginUrl, "POST", parameters);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[LOGIN] Yanıt (raw): " + (string.IsNullOrEmpty(json) ? "(boş)" : json));
#endif
                    var apiRes = new JavaScriptSerializer().Deserialize<LoginResponseExeDTO>(json ?? "{}");

                    if (apiRes == null)
                    {
                        var d0 = AppDataLoader.Data;
                        string cap = d0?.Text10 ?? "Hata";
                        string body = d0?.Login_ConnectionError ?? "Bağlantı yanıtı alınamadı.";
                        MessageBox.Show(body, cap, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (apiRes.result == "success" && apiRes.DataList != null)
                {
                    var logRes = new LoginResultDTO
                    {
                        result = "success",
                        LoginToken = apiRes.DataList.LoginToken,
                        Url = apiRes.DataList.WebviewLink,
                        SocketConnectUrl = apiRes.DataList.SocketConnectUrl,
                        FirmaCode = apiRes.DataList.FirmaCode ?? "",
                        KullaniciID = apiRes.DataList.KullaniciID.ToString(),
                        PrinterStart = apiRes.DataList.Printer,
                        Browser = apiRes.DataList.Browser,
                        FirmaID = apiRes.DataList.FirmaID.ToString()
                    };

                    var configToSave = new ConfigDTO
                    {
                        DeviceId = _deviceId,
                        Username = chkRememberMe.Checked ? txtUserName.Text : "",
                        Password = chkRememberMe.Checked ? txtPassword.Text : "",
                        CompanyCode = chkRememberMe.Checked ? txtCompanyCode.Text : ""
                    };
                    try
                    {
                        Directory.CreateDirectory(AppPaths.WritableDataDirectory);
                        File.WriteAllText(AppPaths.ConfigJsonPath, new JavaScriptSerializer().Serialize(configToSave));
                    }
                    catch { }

                    var main = new MainForm(logRes.FirmaID, logRes);
                    this.Hide();
                    main.Show();

                    if (logRes.Browser && !string.IsNullOrWhiteSpace(logRes.Url))
                    {
                        var browser = new Chromium(logRes.Url);
                        browser.Show();
                    }
                }
                else
                    {
                        var d1 = AppDataLoader.Data;
                        string fail = d1?.Login_Failed ?? "Giriş başarısız.";
                        string msg = apiRes.text1 ?? apiRes.title ?? apiRes.result ?? fail;
                        MessageBox.Show(msg ?? fail, apiRes.title ?? (d1?.Text10 ?? "Hata"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    var d2 = AppDataLoader.Data;
                    string unk = d2?.Login_UnknownError ?? "Bilinmeyen hata";
                    string msg = ex?.Message ?? unk;
                    string detail = ex?.StackTrace ?? "";
                    string detPrefix = string.IsNullOrWhiteSpace(d2?.Login_DetailPrefix) ? "Detay: " : d2.Login_DetailPrefix;
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[LOGIN] Hata: " + msg + "\r\n" + detail);
#endif
                    MessageBox.Show(msg + (string.IsNullOrEmpty(detail) ? "" : "\r\n\r\n" + detPrefix + detail), d2?.Text10 ?? "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _loginInProgress = false;
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void LoginForm_MouseDown(object sender, MouseEventArgs e)
        {
#if DEBUG
            Movee = 1;
            Mouse_X = e.X;
            Mouse_Y = e.Y;
#endif
        }

        private void picRememberMe_Click(object sender, EventArgs e)
        {
            chkRememberMe.Checked = !chkRememberMe.Checked;
            LoadRememberMeIcon();
        }

        /// <summary>Beni hatırla checkbox ikonunu günceller. checkbox_empty.png / checkbox_checked.png. İkon yoksa standart checkbox gösterilir.</summary>
        private void LoadRememberMeIcon()
        {
            if (picRememberMe == null) return;
            string basePath = Path.Combine(AppPaths.InstallDirectory, "images");
            string path = chkRememberMe.Checked
                ? Path.Combine(basePath, "checkbox_checked.png")
                : Path.Combine(basePath, "checkbox_empty.png");
            if (!File.Exists(path))
            {
                picRememberMe.Visible = false;
                if (lblRememberMe != null) lblRememberMe.Visible = false;
                chkRememberMe.Visible = true;
                chkRememberMe.Location = new System.Drawing.Point(32, 388);
                chkRememberMe.Size = new System.Drawing.Size(120, 36);
                chkRememberMe.Font = new Font("Segoe UI", 12f);
                chkRememberMe.Text = lblRememberMe?.Text ?? "Beni hatırla";
                return;
            }
            chkRememberMe.Visible = false;
            if (lblRememberMe != null) lblRememberMe.Visible = true;
            TryLoadIcon(picRememberMe, path);
            picRememberMe.Visible = true;
        }

        /// <summary>Dokunmatik ekranda input focus olduğunda Windows dokunmatik klavyesini açar.</summary>
        private void Input_GotFocus(object sender, EventArgs e)
        {
            ShowTouchKeyboard();
        }

        private void ShowTouchKeyboard()
        {
            try
            {
                string[] paths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "microsoft shared", "ink", "tabtip.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86), "microsoft shared", "ink", "tabtip.exe"),
                    "tabtip.exe"
                };
                foreach (var p in paths)
                {
                    if (File.Exists(p))
                    {
                        Process.Start(new ProcessStartInfo { FileName = p, UseShellExecute = true });
                        return;
                    }
                }
                Process.Start(new ProcessStartInfo { FileName = "tabtip.exe", UseShellExecute = true });
            }
            catch { }
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtCompanyCode_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>Form arka planına mor-mavi gradient uygular.</summary>
        private void LoginForm_Paint(object sender, PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, Width, Height),
                Color.FromArgb(74, 14, 103),
                Color.FromArgb(30, 58, 138),
                LinearGradientMode.ForwardDiagonal))
            {
                e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
            }
        }

        private void LoginForm_MouseUp(object sender, MouseEventArgs e)
        {
            Movee = 0;
        }

        private void LoginForm_MouseMove(object sender, MouseEventArgs e)
        {
#if DEBUG
            if (Movee == 1)
            {
                this.SetDesktopLocation(MousePosition.X - Mouse_X, MousePosition.Y - Mouse_Y);
            }
#endif
        }
    }

    public class HiddenMinimizeFilter : IMessageFilter
    {
        private const int WM_LBUTTONDOWN = 0x0201;
        private int _clickCount = 0;
        private DateTime _lastClickTime = DateTime.MinValue;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN)
            {
                var screenBounds = Screen.PrimaryScreen.Bounds;
                var p = Cursor.Position;
                // Ekranın sol alt köşesi: X <= 150 ve Y >= EkranYüksekliği - 150
                if (p.X <= 150 && p.Y >= screenBounds.Height - 150)
                {
                    if ((DateTime.Now - _lastClickTime).TotalSeconds > 2)
                        _clickCount = 0;

                    _clickCount++;
                    _lastClickTime = DateTime.Now;

                    if (_clickCount >= 7)
                    {
                        _clickCount = 0;
                        Form activeForm = Form.ActiveForm;
                        if (activeForm != null)
                        {
                            Form f = activeForm;
                            f.WindowState = FormWindowState.Minimized;
                            try
                            {
                                f.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        using (var settings = new SettingsForm())
                                        {
                                            settings.Owner = f;
                                            settings.ShowDialog(f);
                                        }
                                    }
                                    catch { }
                                }));
                            }
                            catch { }
                        }
                    }
                }
            }
            return false;
        }
    }
}
