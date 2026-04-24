using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using QRMENUE;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WebBrowser
{
    public partial class Chromium : Form
    {
        private WebView2 webView;
        private string loginUrl = "";

        public Chromium(string url)
        {
            loginUrl = url ?? "";
            InitializeComponent();
            this.Text = AppDataLoader.Data?.Text21 ?? this.Text;
#if DEBUG
            WindowState = FormWindowState.Normal;
#else
            this.FormBorderStyle = FormBorderStyle.None;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.WindowState = FormWindowState.Normal;
            this.StartPosition = FormStartPosition.Manual;
#endif
            webView = new WebView2();
            try
            {
                Directory.CreateDirectory(AppPaths.WebView2UserDataFolder);
                webView.CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = AppPaths.WebView2UserDataFolder
                };
            }
            catch { }
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);
            this.Load += Chromium_Load;
        }

        private async void Chromium_Load(object sender, EventArgs e)
        {
            ApplyIconFromImages();
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                // Sol altta link hover'da çıkan URL önizlemesini kapat ve zoom/swipe kapat
                if (webView.CoreWebView2?.Settings != null)
                {
                    webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    webView.CoreWebView2.Settings.IsPinchZoomEnabled = false;
                    webView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
                }

                string script = @"
                    let hiddenClickCount = 0;
                    let hiddenLastClickTime = 0;
                    window.addEventListener('pointerdown', function(e) {
                        if (e.clientX <= 150 && e.clientY >= window.innerHeight - 150) {
                            let now = Date.now();
                            if (now - hiddenLastClickTime > 2000) {
                                hiddenClickCount = 0;
                            }
                            hiddenClickCount++;
                            hiddenLastClickTime = now;
                            if (hiddenClickCount >= 7) {
                                hiddenClickCount = 0;
                                window.chrome.webview.postMessage('MinimizeApp');
                            }
                        }
                    });
                ";
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
                webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                var targetUrl = string.IsNullOrEmpty(loginUrl) ? "about:blank" : loginUrl;
                webView.Source = new Uri(targetUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, AppDataLoader.Data?.Dialog_WebView2Title ?? "WebView2", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CoreWebView2_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                if (message == "MinimizeApp")
                {
                    Form host = this;
                    host.WindowState = FormWindowState.Minimized;
                    try
                    {
                        host.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                using (var settings = new SettingsForm())
                                {
                                    settings.Owner = host;
                                    settings.ShowDialog(host);
                                }
                            }
                            catch { }
                        }));
                    }
                    catch { }
                }
            }
            catch { }
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

        /// <summary>Login sayfasına / kayıtlı URL'ye yönlendirir (örn. hata sonrası yeniden giriş).</summary>
        public void Login()
        {
            ChangeUrl(loginUrl);
        }

        public void ChangeUrl(string url)
        {
            if (string.IsNullOrEmpty(url) || webView?.CoreWebView2 == null) return;
            try
            {
                webView.CoreWebView2.Navigate(url);
            }
            catch { }
            webView.BringToFront();
        }

        private void Chromium_Closing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        public void ManuelFocus()
        {
            if (WindowState == FormWindowState.Minimized)
                WindowState = FormWindowState.Maximized;
            TopMost = true;
            Focus();
            BringToFront();
            Activate();
            TopMost = false;
        }
    }
}
