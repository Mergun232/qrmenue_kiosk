using System.Threading.Tasks;
using PrintOptions;
using PrintOptions.Dto;
using Qrmenue.DTO;
using QRMENUE;
using QRMENUE.Business;
using QRMENUE.Pavo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using WebBrowser;
using Newtonsoft.Json;

namespace Qrmenue
{
    /// <summary>
    /// Ana form: Socket bağlıyken yazdırma sadece PrintRequest ile tetiklenir; socket yok/kopuksa timer ile printer-requests atar.
    /// 401 gelirse WebView login'e yönlendirilir. Detaylı akış için: SOCKET_VE_YAZDIRMA_AKISI.md
    /// </summary>
    public partial class MainForm : Form
    {
        private bool allowshowdisplay = false, isCheckProgress = false, isSafeClose = false;
        string _companyId;
        private int _printRequestTime, _stateInformTime = 1000;
        delegate void SetTextCallback();
        private Helpers helpers;
        public bool IsError = false;
        PrintService printService = new PrintService();
        PrinterDesignService printerDesignService = new PrinterDesignService();
        private LoginResultDTO floginResultDTO;
        private SocketService _socketService;
        private SocketLogForm _socketLogForm;

        private AppDataDTO _App => AppDataLoader.Data;

        private static string _Tx(string s, string fallback) => string.IsNullOrWhiteSpace(s) ? fallback : s;

        private static string _Fmt(string format, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(format)) return "";
            try { return string.Format(format, args); } catch { return format; }
        }

        private void _LogSys(string message) => _socketLogForm?.Append(_Tx(_App?.Log_Label_System, "Sistem"), message);

        private void _LogErr(string message) => _socketLogForm?.Append(_Tx(_App?.Log_Label_Error, "Hata"), message);

        private void _LogDbg(string message) => _socketLogForm?.Append(_Tx(_App?.Log_Label_Debug, "Debug"), message);

        public MainForm(string companyID, LoginResultDTO loginResultDTO = null)
        {
            InitializeComponent();
            floginResultDTO = loginResultDTO;
            helpers = new Helpers();

            // app_data.json PrinterRequestIntervalSeconds (varsayılan 5 sn)
            int intervalSec = AppDataLoader.Data?.PrinterRequestIntervalSeconds ?? 5;
            if (intervalSec <= 0) intervalSec = 5;
            _printRequestTime = intervalSec * 1000;
            tmrFicheCheck.Interval = _printRequestTime;
            tmrStateInformer.Interval = _stateInformTime;
            _companyId = companyID;

            tmrFicheCheck.Start();
            tmrStateInformer.Start();

            ApplyIconFromImages();
            StartSocketIfConfigured();
        }

        /// <summary>Login sonrası API'den SocketConnectUrl gelmişse socket'i başlatır. Bağlantı kurulunca yazdırma sadece PrintRequest ile tetiklenir; kopunca timer devreye girer.</summary>
        private void StartSocketIfConfigured()
        {
#if DEBUG
            _socketLogForm = new SocketLogForm();
            _socketLogForm.Show();
#else
            _socketLogForm = null; // Release: log penceresi gösterilmez
#endif
            // Socket adresi yoksa socket hiç başlatılmaz; yazdırma tamamen timer ile yapılır
            if (floginResultDTO == null || string.IsNullOrWhiteSpace(floginResultDTO.SocketConnectUrl))
            {
                _LogSys(_Tx(_App?.Log_SocketNotStarted, "Socket başlatılmadı: Login yanıtında SocketConnectUrl yok veya boş. API'nin DataList.SocketConnectUrl döndürdüğünden emin olun."));
                return;
            }
            string url = floginResultDTO.SocketConnectUrl.Trim();
            string firmaCode = (floginResultDTO.FirmaCode ?? "").Trim();
            string kullaniciId = floginResultDTO.KullaniciID ?? "";
            string emptyM = _Tx(_App?.Log_Marker_Empty, "(boş)");
            _LogSys(_Fmt(_Tx(_App?.Log_SocketStarting, "Socket başlatılıyor: {0} FirmaCode: {1} KullaniciID: {2}"), url, string.IsNullOrEmpty(firmaCode) ? emptyM : firmaCode, string.IsNullOrEmpty(kullaniciId) ? emptyM : kullaniciId));

            _socketService = new SocketService(url, firmaCode, kullaniciId, OnSocketEvent);
            _socketService.Connect();
        }

        /// <summary>Socket'ten gelen her olay buraya düşer. PrintRequest gelince printer-requests API tetiklenir (timer beklemeden). Pavo event'leri arka planda socket ile tetiklenir.</summary>
        private void OnSocketEvent(string eventName, string payload)
        {
            _socketLogForm?.Append(eventName, payload ?? "");
            if (eventName == "PrintRequest")
            {
                try
                {
                    _LogSys(_Tx(_App?.Log_PrintRequestDispatch, "PrintRequest alındı → printer-requests API tetikleniyor..."));
                    // PrintRequest gelince beklemeksizin asenkron kontrolü başlatıyoruz
                    _ = Task.Run(async () => await CheckPrintablesAsync());
                }
                catch { }
            }
            else if (eventName == "PavoRequest")
            {
                PavoPosSocketBridge.TriggerPavoRequest(payload ?? "", (resultJson, room) =>
                {
                    if (_socketService != null)
                    {
                        _ = _socketService.EmitPavoResultAsync(resultJson, room);
                    }
                });
            }
            else if (eventName == "PavoPairing")
            {
                PavoPosSocketBridge.TriggerPairing(payload ?? "");
            }
            else if (eventName == "PavoInitiateSale")
            {
                PavoPosSocketBridge.TriggerInitiateSale(payload ?? "");
            }
            else if (eventName == "PavoGetSaleResult")
            {
                PavoPosSocketBridge.TriggerGetSaleResult(payload ?? "", resultJson =>
                {
                    if (_socketService != null)
                        _ = _socketService.EmitPavoResultAsync(resultJson);
                });
            }
            else if (eventName == "PavoPrintOut")
            {
                PavoPosSocketBridge.TriggerPrintOut(payload ?? "");
            }
            else if (eventName == "PavoPaymentMediators")
            {
                PavoPosSocketBridge.TriggerPaymentMediators(payload ?? "");
            }
            else if (eventName == "PavoCompleteSale")
            {
                PavoPosSocketBridge.TriggerCompleteSale(payload ?? "", resultJson =>
                {
                    if (_socketService != null)
                        _ = _socketService.EmitPavoResultAsync(resultJson);
                });
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ApplyAppDataTexts();
            try
            {
                if (autoStartToolStripMenuItem != null)
                    autoStartToolStripMenuItem.Checked = KioskSettingsStore.Load().RunOnWindowsStartup;
            }
            catch { }
        }

        /// <summary>images/ klasöründeki simgeyi form ve sistem tepsisinde (gizli simgeler) kullanır.</summary>
        private void ApplyIconFromImages()
        {
            string iconPath = Path.Combine(Application.StartupPath, "images", "favicon_multi.ico");
            if (!File.Exists(iconPath)) return;
            try
            {
                using (var ico = new Icon(iconPath))
                {
                    if (this.Icon != null) this.Icon.Dispose();
                    this.Icon = (Icon)ico.Clone();
                    if (notifyIcon != null)
                    {
                        if (notifyIcon.Icon != null) notifyIcon.Icon.Dispose();
                        notifyIcon.Icon = (Icon)ico.Clone();
                    }
                }
            }
            catch { }
        }

        private void ApplyAppDataTexts()
        {
            var d = AppDataLoader.Data;
            if (d == null) return;
            if (label1 != null) label1.Text = d.Text15 ?? label1.Text;
            if (notifyIcon != null) notifyIcon.Text = d.Text24 ?? notifyIcon.Text;
            if (version100ToolStripMenuItem != null) version100ToolStripMenuItem.Text = d.Text23 ?? version100ToolStripMenuItem.Text;
            if (autoStartToolStripMenuItem != null) autoStartToolStripMenuItem.Text = d.Text17 ?? autoStartToolStripMenuItem.Text;
            if (quitToolStripMenuItem != null) quitToolStripMenuItem.Text = d.Text16 ?? quitToolStripMenuItem.Text;
            if (webSocketServerRunningOn22444ToolStripMenuItem != null) webSocketServerRunningOn22444ToolStripMenuItem.Text = d.Text25 ?? webSocketServerRunningOn22444ToolStripMenuItem.Text;
            if (pavoPosTestToolStripMenuItem != null) pavoPosTestToolStripMenuItem.Text = d.Menu_PavoPosTest ?? pavoPosTestToolStripMenuItem.Text;
            this.Text = d.Text24 ?? this.Text;
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isSafeClose = true;
            Application.Exit();
        }

        private void pavoPosTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var frm = new PavoPosForm();
                frm.Show();
            }
            catch (Exception ex)
            {
                _LogErr(_Fmt(_Tx(_App?.Log_PavoFormError, "PavoPosForm: {0}"), ex.Message));
            }
        }

        private void autoStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            autoStartToolStripMenuItem.Checked = !autoStartToolStripMenuItem.Checked;
            if (autoStartToolStripMenuItem.Checked)
            {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                try { key.DeleteValue("QRMENUE", false); } catch { }
                key.SetValue("Qiox", Application.ExecutablePath);
            }
            else
            {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.DeleteValue("Qiox", false);
            }
            try
            {
                var s = KioskSettingsStore.Load();
                s.RunOnWindowsStartup = autoStartToolStripMenuItem.Checked;
                KioskSettingsStore.Save(s);
            }
            catch { }
        }

        /// <summary>Belirli aralıklarla (örn. 5 sn) çalışır. Sadece socket YOK veya KOPUKSA printer-requests atar; socket bağlıyken tetikleme sadece PrintRequest ile olur (gereksiz istek atılmaz).</summary>
        private async void timer1_Tick(object sender, EventArgs e)
        {
            bool socketAktif = _socketService != null && _socketService.IsConnected;
            if (!isCheckProgress && !socketAktif)
            {
                await CheckPrintablesAsync();
            }
        }

        private async void tmrStateInformer_Tick(object sender, EventArgs e)
        {
            await InformSystemAsync();
        }

        /// <summary>printer-requests API'yi çağırır; 401 ise login'e yönlendirir, success ise dönen URL'lerden fiş alıp yazdırır.</summary>
        private async Task CheckPrintablesAsync()
        {
            try
            {
                if (floginResultDTO?.PrinterStart != true || string.IsNullOrEmpty(floginResultDTO.LoginToken))
                {
                    _LogSys(_Tx(_App?.Log_PrinterSkipped, "printer-requests atlandı: PrinterStart kapalı veya LoginToken yok."));
                    return;
                }

                isCheckProgress = true;
                string printerRequestsUrl = AppDataLoader.GetApiUrl("api/exe/printer-requests");
                _LogSys(_Fmt(_Tx(_App?.Log_PrinterRequestsCalling, "printer-requests API çağrılıyor: {0}"), printerRequestsUrl ?? ""));
                
                // Asenkron HTTP çağrısı
                string json = await Helpers.HttpHelperAsync(printerRequestsUrl, "GET", null, floginResultDTO.LoginToken).ConfigureAwait(false);
                var newFiche = SafeDeserialize<PrintRequestDTO>(json);

                // 401 kontrolü için JSON içindeki error kısmına bakıyoruz (HttpHelperAsync 401 durumda JSON döndürür)
                if (json != null && json.Contains("HTTP 401"))
                {
                    _LogSys(_Tx(_App?.Log_Auth401Redirect, "401 Auth hatası — WebView login sayfasına yönlendiriliyor."));
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() => {
                            var frm = Application.OpenForms.Cast<Form>().FirstOrDefault(x => x.Name == "Chromium");
                            if (frm != null) {
                                var temp = (Chromium)frm;
                                temp.Login();
                                temp.ManuelFocus();
                            }
                        }));
                    }
                    isCheckProgress = false;
                    return;
                }

                if (newFiche == null || string.IsNullOrEmpty(newFiche.result))
                {
                    _LogErr(_Tx(_App?.Log_PrinterResponseInvalid, "printer-requests yanıtı geçersiz veya boş."));
                    IsError = true;
                    isCheckProgress = false;
                    return;
                }

                _LogSys(_Fmt(_Tx(_App?.Log_PrinterResponseSummary, "printer-requests yanıt: result={0}, Url sayısı={1}"), newFiche.result ?? "", newFiche.Url?.Count ?? 0));
                if (newFiche.result == "success")
                    IsError = false;

                // API başarılı ve yazdırılacak fiş URL'leri döndüyse her URL'den fiş alıp yazdır
                if (newFiche.result == "success" && newFiche.Url != null)
                {
                    int printed = 0;
                    for (int i = 0; i < newFiche.Url.Count; i++)
                    {
                        var item = newFiche.Url[i];
                        if (string.IsNullOrEmpty(item))
                        {
                            _LogSys(_Fmt(_Tx(_App?.Log_UrlSkippedEmpty, "URL {0} boş, atlanıyor."), i + 1));
                            continue;
                        }
                        _LogSys(_Fmt(_Tx(_App?.Log_UrlNavigating, "Fiş URL'sine gidiliyor: {0}"), item));
                        try
                        {
                            // Her fiş URL'sini de asenkron alıyoruz
                            string contentJson = await Helpers.HttpHelperAsync(item, "GET", null).ConfigureAwait(false);
                            int len = string.IsNullOrEmpty(contentJson) ? 0 : contentJson.Length;
                            _LogSys(_Fmt(_Tx(_App?.Log_UrlResponseChars, "URL yanıtı: {0} karakter"), len));
                            var receipts = ParsePrinterContent(contentJson);
                            int receiptCount = receipts?.Count ?? 0;
                            _LogSys(_Fmt(_Tx(_App?.Log_ReceiptDtoCount, "RecieptDto sayısı: {0}"), receiptCount));
                            if (receipts != null && receipts.Count > 0)
                            {
                                foreach (var fiche in receipts)
                                {
                                    printerDesignService.PrintSlip(fiche); // Yazıcı yoksa PrinterDesignService varsayılana düşer
                                    printed++;
                                }
                            }
                            else if (receiptCount == 0)
                            {
                                _LogSys(_Tx(_App?.Log_NoReceiptFromUrl, "Bu URL'den fiş verisi gelmedi (JSON formatı RecieptDto ile uyumlu olmayabilir)."));
                                string preview = (contentJson ?? "").Length > 350 ? contentJson.Substring(0, 350) + "..." : contentJson;
                                _LogDbg(_Fmt(_Tx(_App?.Log_JsonPreview, "JSON önizleme: {0}"), preview));
                            }
                        }
                        catch (Exception ex)
                        {
                            _LogErr(_Fmt(_Tx(_App?.Log_PrintUrlError, "URL isteği/yazdırma: {0}"), ex.Message));
                        }
                    }
                    if (printed > 0)
                        _LogSys(_Fmt(_Tx(_App?.Log_PrintedCount, "Yazdırılan fiş sayısı: {0}"), printed));
                }
                isCheckProgress = false;
            }
            catch (Exception ex)
            {
                isCheckProgress = false;
                IsError = true;
                _ = SendExeLogAsync(ex.Message, "error");
            }
        }

        /// <summary>printer-content JSON'undan RecieptDto listesini çıkarır. Kök, data/Data sarmalı veya farklı property adları desteklenir.</summary>
        private static List<RecieptDto> ParsePrinterContent(string json)
        {
            var list = new List<RecieptDto>();
            string s = (json ?? "").Trim();
            if (string.IsNullOrEmpty(s) || s.Length < 2) return list;
            try
            {
                var root = SafeDeserialize<Root>(json);
                if (root?.RecieptDto != null && root.RecieptDto.Count > 0)
                {
                    return root.RecieptDto;
                }
                var js = new JavaScriptSerializer();
                var dict = js.Deserialize<Dictionary<string, object>>(json);
                if (dict == null) return list;
                object dataObj = null;
                if (dict.TryGetValue("data", out dataObj) || dict.TryGetValue("Data", out dataObj))
                {
                    if (dataObj is Dictionary<string, object> dataDict)
                    {
                        if (dataDict.TryGetValue("RecieptDto", out var rd) && rd is System.Collections.ArrayList arr && arr.Count > 0)
                        {
                            foreach (var item in arr)
                            {
                                var itemStr = js.Serialize(item);
                                var rec = js.Deserialize<RecieptDto>(itemStr);
                                if (rec != null) list.Add(rec);
                            }
                            return list;
                        }
                    }
                    else if (dataObj is System.Collections.ArrayList dataArr && dataArr.Count > 0)
                    {
                        foreach (var item in dataArr)
                        {
                            var itemStr = js.Serialize(item);
                            var rec = js.Deserialize<RecieptDto>(itemStr);
                            if (rec != null) list.Add(rec);
                        }
                        return list;
                    }
                }
                if (dict.TryGetValue("RecieptDto", out var rd2) && rd2 is System.Collections.ArrayList arr2)
                {
                    foreach (var item in arr2)
                    {
                        var itemStr = js.Serialize(item);
                        var rec = js.Deserialize<RecieptDto>(itemStr);
                        if (rec != null) list.Add(rec);
                    }
                }
                if (dict.TryGetValue("recieptDto", out var rd3) && rd3 is System.Collections.ArrayList arr3)
                {
                    foreach (var item in arr3)
                    {
                        var itemStr = js.Serialize(item);
                        var rec = js.Deserialize<RecieptDto>(itemStr);
                        if (rec != null) list.Add(rec);
                    }
                }
            }
            catch { }
            return list;
        }

        /// <summary>API yanıtı geçersiz/boş olduğunda ArgumentException çıkmasın diye güvenli JSON parse.</summary>
        private static T SafeDeserialize<T>(string json) where T : class, new()
        {
            string s = (json ?? "").Trim();
            if (string.IsNullOrEmpty(s) || s.Length < 2 || (s[0] != '{' && s[0] != '['))
                return new T();
            try
            {
                var settings = new JsonSerializerSettings { Error = (sender, args) => args.ErrorContext.Handled = true };
                var obj = JsonConvert.DeserializeObject<T>(s, settings);
                return obj ?? new T();
            }
            catch
            {
                return new T();
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Kullanıcı menüden 'Çıkış' demişse veya Windows kapanıyorsa veya Application.Exit çağrılmışsa
            if (isSafeClose || e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.ApplicationExitCall)
            {
                try
                {
                    if (notifyIcon != null)
                    {
                        notifyIcon.Visible = false;
                        notifyIcon.Icon?.Dispose();
                        notifyIcon.Dispose();
                    }
                    _socketService?.Disconnect();
                    _socketLogForm?.Close();
                }
                catch { }
                // Kapanmaya izin ver
            }
            else
            {
                // X butonuna basılmışsa kapatma, sadece gizle
                e.Cancel = true;
                this.Visible = false;
            }
        }

        /// <summary>Yeni api/exe: Durumu sunucuya POST ile bildirir (exe-state).</summary>
        private async Task InformSystemAsync()
        {
            if (floginResultDTO == null || string.IsNullOrEmpty(floginResultDTO.LoginToken))
                return;

            try
            {
                string stateUrl = AppDataLoader.GetApiUrl("api/exe/exe-state");
                string body = "{\"state\":\"online\",\"printer\":\"ready\"}";
                await Helpers.HttpHelperJsonAsync(stateUrl, body, floginResultDTO.LoginToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _ = SendExeLogAsync(ex.Message, "error");
            }
        }

        /// <summary>Hata/bilgi logunu api/exe/exe-log ile sunucuya gönderir.</summary>
        private async Task SendExeLogAsync(string message, string type = "error")
        {
            if (floginResultDTO == null || string.IsNullOrEmpty(floginResultDTO.LoginToken))
                return;
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    try { message = File.ReadAllText(AppPaths.ErrorLogPath); }
                    catch { }
                    if (string.IsNullOrEmpty(message)) message = AppDataLoader.Data?.Text22 ?? "Hiç Hata Yok.";
                }
                string url = AppDataLoader.GetApiUrl("api/exe/exe-log");
                var payload = new Dictionary<string, string> { { "type", type ?? "info" }, { "log", message ?? "" } };
                string body = new JavaScriptSerializer().Serialize(payload);
                await Helpers.HttpHelperJsonAsync(url, body, floginResultDTO.LoginToken).ConfigureAwait(false);
            }
            catch { }
        }
    }
}
