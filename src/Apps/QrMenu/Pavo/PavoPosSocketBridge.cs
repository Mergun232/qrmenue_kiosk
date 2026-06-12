using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRMENUE;

namespace QRMENUE.Pavo
{
    public static class PavoPosSocketBridge
    {
        private static Action<string, string> _logCallback;

        private static int _pavoRequestSequence = Math.Max(100, Math.Abs(Environment.TickCount % 100000));
        private static readonly object _seqLock = new object();
        private static readonly HttpClient _httpClient;

        private static string _lastPavoBaseUrl;
        private static string _lastSerialNumber;
        private static string _lastFingerprint;
        private static DateTime _lastActivityTime = DateTime.MinValue;

        static PavoPosSocketBridge()
        {
            try
            {
                // TLS 1.2 ve TLS 1.1 protokol desteğini aktif hale getir
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11;
            }
            catch { }

            try
            {
                // Varsayılan 2 olan bağlantı sınırını artır
                System.Net.ServicePointManager.DefaultConnectionLimit = 500;
            }
            catch { }

            try
            {
                // Dinamik IP değişiklikleri için DNS yenileme süresi
                System.Net.ServicePointManager.DnsRefreshTimeout = 60000;
            }
            catch { }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(90)
            };

            // Heartbeat/Keep-alive görevini başlat
            StartHeartbeatTask();
        }

        private static int GetNextSequence()
        {
            lock (_seqLock)
            {
                if (_pavoRequestSequence > 999999) _pavoRequestSequence = 100;
                return ++_pavoRequestSequence;
            }
        }

        /// <summary>Log callback kaydeder. PavoPosForm açıldığında çağrılır.</summary>
        public static void Register(Action<string, string> logCallback, object obsoleteClient = null)
        {
            _logCallback = logCallback;
        }

        /// <summary>Kaydı kaldırır. Form kapandığında.</summary>
        public static void Unregister()
        {
            _logCallback = null;
        }

        private static readonly object _fileLock = new object();

        private static void Log(string tag, string msg)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[{tag}] {msg}");
                
                // Logları yerel dosyaya yaz
                WriteLogToFile(tag, msg);

                _logCallback?.Invoke(tag, msg);
            }
            catch { }
        }

        private static void WriteLogToFile(string tag, string msg)
        {
            try
            {
                string logDir = AppPaths.WritableDataDirectory;
                if (string.IsNullOrEmpty(logDir)) return;

                string filePath = Path.Combine(logDir, "pavoLog.txt");
                string backupPath = Path.Combine(logDir, "pavoLog_old.txt");

                lock (_fileLock)
                {
                    if (File.Exists(filePath))
                    {
                        var info = new FileInfo(filePath);
                        if (info.Length > 5 * 1024 * 1024) // 5 MB
                        {
                            try
                            {
                                if (File.Exists(backupPath))
                                    File.Delete(backupPath);
                                File.Move(filePath, backupPath);
                            }
                            catch { }
                        }
                    }

                    using (var sw = new StreamWriter(filePath, true, Encoding.UTF8))
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{tag}] {msg}");
                    }
                }
            }
            catch { }
        }

        /// <summary>PavoRequest - socket veya WebView IPC'den gelir; POS'a POST edilir.</summary>
        public static void TriggerPavoRequest(string payload, Action<string> onIpcResult)
        {
            TriggerPavoRequest(payload, (res, room) => onIpcResult?.Invoke(res));
        }

        /// <summary>PavoRequest - socket veya WebView IPC'den gelir; POS'a POST edilir.</summary>
        public static void TriggerPavoRequest(string payload, Action<string, PavoRequestResultRoom> onResult = null)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(payload)) { Log("Pavo", "PavoRequest: payload boş."); return; }
                    var jo = JObject.Parse(payload);
                    var url = jo["url"]?.ToString();
                    var bodyObj = jo["body"] as JObject;

                    // Log yükleme isteği kontrolü
                    bool isGetPavoLogs = false;
                    if (jo["GetPavoLogs"] != null)
                    {
                        isGetPavoLogs = true;
                    }
                    else if ((url ?? "").IndexOf("GetPavoLogs", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        isGetPavoLogs = true;
                    }
                    else if (bodyObj != null && bodyObj["GetPavoLogs"] != null)
                    {
                        isGetPavoLogs = true;
                    }

                    if (isGetPavoLogs)
                    {
                        string uploadUrl = jo["uploadUrl"]?.ToString();
                        if (string.IsNullOrEmpty(uploadUrl) && bodyObj != null)
                        {
                            uploadUrl = bodyObj["uploadUrl"]?.ToString();
                        }
                        if (string.IsNullOrEmpty(uploadUrl))
                        {
                            uploadUrl = AppDataLoader.GetApiUrl("api/exe/pavo-logs");
                        }

                        Log("Pavo", "Log dosyası sunucuya POST ediliyor: " + uploadUrl);

                        string token = null;
                        try
                        {
                            var openForms = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().ToList();
                            Log("Pavo Debug", "Açık Formlar: " + string.Join(", ", openForms.Select(f => $"{f.GetType().FullName} (Name: {f.Name})")));
                            
                            var mainForm = Qrmenue.MainForm.Instance;
                            if (mainForm != null)
                            {
                                token = mainForm.LoginToken;
                                Log("Pavo Debug", $"MainForm.Instance bulundu. Token durumu: {(string.IsNullOrEmpty(token) ? "BOŞ" : "DOLU (Uzunluk: " + token.Length + ")")}");
                            }
                            else
                            {
                                Log("Pavo Debug", "MainForm.Instance BULUNAMADI!");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log("Pavo Debug", "MainForm.Instance erişim hatası: " + ex.Message);
                        }

                        bool success = await UploadLogsAsync(uploadUrl, token).ConfigureAwait(false);
                        string statusMsg = success ? "Başarılı" : "Başarısız";
                        Log("Pavo", "Log yükleme sonucu: " + statusMsg);

                        string responseJson = "{\"HasError\":" + (success ? "false" : "true") + ",\"Message\":\"Log yükleme " + (success ? "başarılı" : "başarısız") + "\"}";
                        onResult?.Invoke(responseJson, PavoRequestResultRoom.Personel);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(url)) { Log("Pavo", "PavoRequest: url gerekli."); return; }

                    Log("Pavo", "İstek: " + payload);

                    // IP adresi ve temel URL bilgisini güncelle
                    try
                    {
                        var uri = new Uri(url);
                        _lastPavoBaseUrl = uri.GetLeftPart(UriPartial.Authority);
                    }
                    catch { }

                    _lastActivityTime = DateTime.Now;

                    if (bodyObj != null && bodyObj["TransactionHandle"] != null)
                    {
                        var th = bodyObj["TransactionHandle"];
                        _lastSerialNumber = th["SerialNumber"]?.ToString() ?? _lastSerialNumber;
                        _lastFingerprint = th["Fingerprint"]?.ToString() ?? _lastFingerprint;

                        if (th["TransactionSequence"] == null || th["TransactionSequence"].Type == JTokenType.Null)
                        {
                            // Eğer Sequence null gelmişse, garantili olarak her zaman büyüyen tekil bir sayı üret
                            th["TransactionSequence"] = GetNextSequence();
                        }
                    }

                    int retryCount = 0;
                    int maxRetries = 5;
                    string result = "";
                    bool needsRetry = true;
                    
                    string orderNo = null;
                    if (bodyObj != null && bodyObj["Sale"] != null)
                    {
                        orderNo = bodyObj["Sale"]["OrderNo"]?.ToString();
                    }

                    while (needsRetry && retryCount < maxRetries)
                    {
                        var bodyJson = bodyObj != null ? bodyObj.ToString() : "{}";
                        result = await PostToUrlAsync(url, bodyJson).ConfigureAwait(false);
                        
                        // Check if result has ErrorCode 73
                        needsRetry = false;
                        try
                        {
                            var resJo = JObject.Parse(result);
                            if (resJo["HasError"]?.Value<bool>() == true && resJo["ErrorCode"]?.Value<int>() == 73)
                            {
                                needsRetry = true;
                            }
                        }
                        catch { }

                        if (needsRetry)
                        {
                            retryCount++;
                            if (bodyObj != null && bodyObj["TransactionHandle"] != null)
                            {
                                var th = bodyObj["TransactionHandle"];
                                th["TransactionSequence"] = GetNextSequence();
                                th["TransactionDate"] = DateTime.Now.ToString("s") + ".000000";
                            }
                            await Task.Delay(500).ConfigureAwait(false); // 500ms bekle ve tekrar dene
                        }
                    }

                    if (url.IndexOf("CompleteSale", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("AddPaymentAndFinalizeSale", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("GetSaleResult", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("GetCancellationResult", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        TryExtractAndSaveReceiptImage(result, orderNo);
                    }
                    
                    // TransactionHandle IPC yanıtında gereksiz yük olmasın diye kaldırılır
                    string finalResultForIpc = result;
                    try
                    {
                        var resJo = JObject.Parse(result);
                        if (resJo["TransactionHandle"] != null)
                        {
                            resJo.Property("TransactionHandle")?.Remove();
                            finalResultForIpc = resJo.ToString(Newtonsoft.Json.Formatting.None);
                        }
                    }
                    catch { }

                    string ipcPayload;
                    if (url.IndexOf("/GetSaleResult", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("/GetCancellationResult", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("/CompleteSale", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("/AddPaymentAndFinalizeSale", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("/CancelSale", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        ipcPayload = MinimizePavoResult(result, orderNo);
                    }
                    else
                    {
                        ipcPayload = finalResultForIpc;
                    }

                    Log("Pavo", "Yanıt: " + ipcPayload);
                    
                    // Kiosk/Socket desteği için oda tespiti
                    var socketRoom = (url.IndexOf("/CancelSale", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     url.IndexOf("/GetCancellationResult", StringComparison.OrdinalIgnoreCase) >= 0)
                        ? PavoRequestResultRoom.Firma
                        : PavoRequestResultRoom.Personel;

                    onResult?.Invoke(ipcPayload, socketRoom);
                }
                catch (Exception ex)
                {
                    string realError = ex.Message;
                    if (ex.InnerException != null) realError += " | Inner: " + ex.InnerException.Message;
                    
                    Log("Pavo Hata", "PavoRequest: " + realError);
                    // Hata durumunda ErrorCode 999 döndürüyoruz ki cihaz kapalı / ulaşılamıyor durumu anlaşılsın
                    string errResult = "{\"HasError\":true,\"ErrorCode\":999,\"DeviceOffline\":true,\"Message\":\"" + realError.Replace("\"", "\\\"") + "\"}";
                    Log("Pavo", "Yanıt: " + errResult);
                    onResult?.Invoke(errResult, PavoRequestResultRoom.Personel);
                }
            });
        }

        /// <summary>Socket kaynaklı Pavo sonucunu WebView IPC (PavoResult) ile JS'e iletir. Socket emit yapılmaz.</summary>
        public static void DeliverPavoResultToWebView(string resultJson)
        {
            try
            {
                var webForm = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>()
                    .FirstOrDefault(x => x.Name == "WebForm") as WebBrowser.WebForm;
                if (webForm != null)
                    webForm.SendPavoResultToJs(resultJson);
            }
            catch (Exception ex)
            {
                Log("Pavo Hata", "IPC WebView iletim hatası: " + ex.Message);
            }
        }

        private static async Task<string> PostToUrlAsync(string url, string bodyJson)
        {
            try
            {
                var uri = new Uri(url);
                var servicePoint = System.Net.ServicePointManager.FindServicePoint(uri);
                if (servicePoint != null)
                {
                    // Bağlantıların sık tazelemeyle ağ değişimlerine hızlı adapte olması için süreyi 5 saniye yapıyoruz
                    servicePoint.ConnectionLeaseTimeout = 5000; 
                }
            }
            catch { }

            try
            {
                var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                try
                {
                    // Hata durumunda (bağlantı koptuğunda) havuzdaki bozuk soketleri temizle
                    var uri = new Uri(url);
                    var servicePoint = System.Net.ServicePointManager.FindServicePoint(uri);
                    if (servicePoint != null)
                    {
                        servicePoint.CloseConnectionGroup("");
                    }
                }
                catch { }
                throw;
            }
        }

        private static string MinimizePavoResult(string jsonResult, string fallbackOrderNo = null)
        {
            try
            {
                var jo = JObject.Parse(jsonResult);
                bool hasError = jo["HasError"]?.Value<bool>() ?? false;
                var minResult = new JObject
                {
                    ["HasError"] = hasError,
                    ["ErrorCode"] = jo["ErrorCode"],
                    ["Message"] = jo["Message"]
                };
                var dataObj = jo["Data"] as JObject;
                if (dataObj != null)
                {
                    minResult["StatusId"] = dataObj["StatusId"];
                    minResult["OrderNo"] = dataObj["OrderNo"] ?? fallbackOrderNo;
                }
                else
                {
                    minResult["StatusId"] = null;
                    minResult["OrderNo"] = fallbackOrderNo;
                }
                return minResult.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch
            {
                try
                {
                    var resJo = JObject.Parse(jsonResult);
                    if (resJo["TransactionHandle"] != null)
                    {
                        resJo.Property("TransactionHandle")?.Remove();
                        return resJo.ToString(Newtonsoft.Json.Formatting.None);
                    }
                }
                catch { }

                return jsonResult;
            }
        }

        private static void TryExtractAndSaveReceiptImage(string jsonResult, string orderNo)
        {
            try
            {
                var jo = JObject.Parse(jsonResult);
                if (jo["HasError"]?.Value<bool>() == true)
                {
                    var ec = jo["ErrorCode"];
                    if (ec != null && ec.Type != JTokenType.Null && ec.ToString() == "130")
                        return;
                }
                var dataObj = jo["Data"] as JObject;
                if (dataObj == null)
                    return;

                var receiptImageBase64 = dataObj["CustomerReceiptImage"]?.ToString();
                var receiptJsonStr = dataObj["CustomerReceiptJson"]?.ToString();
                var orderNoFromData = dataObj["OrderNo"]?.ToString() ?? orderNo;

                string folderPath = AppPaths.ReceiptsDirectory;
                bool folderEnsured = false;
                void EnsureFolder()
                {
                    if (!folderEnsured)
                    {
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                        folderEnsured = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(receiptImageBase64) && !string.IsNullOrWhiteSpace(orderNoFromData))
                {
                    EnsureFolder();
                    byte[] imageBytes = Convert.FromBase64String(receiptImageBase64);
                    
                    var directoryInfo = new DirectoryInfo(folderPath);
                    var files = directoryInfo.GetFiles("*.png").OrderBy(f => f.CreationTime).ToList();
                    
                    if (files.Count >= 10)
                    {
                        int filesToDelete = files.Count - 9;
                        for (int i = 0; i < filesToDelete; i++)
                        {
                            try { files[i].Delete(); } catch { }
                        }
                    }

                    string filePath = Path.Combine(folderPath, $"ReceiptImage_{orderNoFromData}.png");
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        using (System.Drawing.Image image = System.Drawing.Image.FromStream(ms))
                        {
                            image.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                            Log("Pavo", $"Fiş resmi kaydedildi: {filePath}");
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(receiptJsonStr) && !string.IsNullOrWhiteSpace(orderNoFromData))
                {
                    EnsureFolder();
                    try
                    {
                        string prettyJson = string.Empty;
                        try
                        {
                            JToken parsedToken = JToken.Parse(receiptJsonStr);
                            if (parsedToken.Type == JTokenType.String)
                            {
                                parsedToken = JToken.Parse(parsedToken.ToString());
                            }
                            prettyJson = parsedToken.ToString(Newtonsoft.Json.Formatting.Indented);
                        }
                        catch
                        {
                            prettyJson = receiptJsonStr;
                        }

                        var directoryInfo = new DirectoryInfo(folderPath);
                        var jsonFiles = directoryInfo.GetFiles("*.json").OrderBy(f => f.CreationTime).ToList();
                        
                        if (jsonFiles.Count >= 10)
                        {
                            int filesToDelete = jsonFiles.Count - 9;
                            for (int i = 0; i < filesToDelete; i++)
                            {
                                try { jsonFiles[i].Delete(); } catch { }
                            }
                        }

                        string originalJsonFilePath = Path.Combine(folderPath, $"ReceiptJSON_{orderNoFromData}_Original.json");
                        File.WriteAllText(originalJsonFilePath, prettyJson, Encoding.UTF8);

                        var recieptInfos = ConvertPavoToRecieptInfos(prettyJson);
                        string convertedJson = Newtonsoft.Json.JsonConvert.SerializeObject(recieptInfos, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
                        string jsonFilePath = Path.Combine(folderPath, $"ReceiptJSON_{orderNoFromData}.json");
                        File.WriteAllText(jsonFilePath, convertedJson, Encoding.UTF8);
                        Log("Pavo", $"Fiş JSON verisi kaydedildi: {jsonFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Log("Pavo Hata", "Fiş JSON kaydetme hatası: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Pavo Hata", "Fiş veri/resim kaydetme genel hatası: " + ex.Message);
            }
        }

        private static System.Collections.Generic.List<PrintOptions.Dto.RecieptInfo> ConvertPavoToRecieptInfos(string jsonContent)
        {
            var list = new System.Collections.Generic.List<PrintOptions.Dto.RecieptInfo>();
            try
            {
                var pavoData = new JavaScriptSerializer().Deserialize<PrintOptions.Dto.PavoReceiptData>(jsonContent);
                if (pavoData == null || pavoData.customerReceipt1 == null) return list;

                foreach (var pavoItem in pavoData.customerReceipt1)
                {
                    if (pavoItem.type == "space")
                    {
                        list.Add(new PrintOptions.Dto.RecieptInfo {
                            Type = "Line",
                            Text = " ",
                            FontInfo = new PrintOptions.Dto.FontInfo { Align = "Center", Height = "10" }
                        });
                    }
                    else if (pavoItem.type == "line")
                    {
                        list.Add(new PrintOptions.Dto.RecieptInfo {
                            Type = "Seperate",
                            FontInfo = new PrintOptions.Dto.FontInfo { Align = "Center" }
                        });
                    }
                    else if (pavoItem.type == "image")
                    {
                        list.Add(new PrintOptions.Dto.RecieptInfo {
                            Type = "Image",
                            Text = "https://qrmenue.com/images/gib.png",
                            FontInfo = new PrintOptions.Dto.FontInfo {
                                Width = (pavoItem.width ?? 75).ToString(),
                                Height = (pavoItem.height ?? 75).ToString(),
                                Align = "Center"
                            }
                        });
                    }
                    else if (pavoItem.type == "qrCode" && !string.IsNullOrEmpty(pavoItem.qrData))
                    {
                        list.Add(new PrintOptions.Dto.RecieptInfo {
                            Type = "Image",
                            Text = "https://qrmenue.com/tr/qrcreate?Code=" + Uri.EscapeDataString(pavoItem.qrData),
                            FontInfo = new PrintOptions.Dto.FontInfo { Width = "100", Height = "100", Align = "Center" }
                        });
                    }
                    else if (pavoItem.type == "text" || string.IsNullOrEmpty(pavoItem.type))
                    {
                        int colCount = 0;
                        if (!string.IsNullOrEmpty(pavoItem.leftText)) colCount++;
                        if (!string.IsNullOrEmpty(pavoItem.centerText)) colCount++;
                        if (!string.IsNullOrEmpty(pavoItem.rightText)) colCount++;

                        if (colCount == 0) continue;

                        string fontSize = pavoItem.fontSize == "large" ? "8" : "7";
                        string fontStyle = pavoItem.isBold == true ? "bold" : null;

                        if (colCount == 1)
                        {
                            string t = "";
                            string a = "Center";
                            if (!string.IsNullOrEmpty(pavoItem.centerText)) { t = pavoItem.centerText; a = "Center"; }
                            else if (!string.IsNullOrEmpty(pavoItem.leftText)) { t = pavoItem.leftText; a = "Left"; }
                            else if (!string.IsNullOrEmpty(pavoItem.rightText)) { t = pavoItem.rightText; a = "Right"; }

                            list.Add(new PrintOptions.Dto.RecieptInfo {
                                Type = "Line",
                                Text = t,
                                FontInfo = new PrintOptions.Dto.FontInfo { 
                                    Align = a, 
                                    FontSize = fontSize, 
                                    FontStyle = fontStyle 
                                }
                            });
                            continue;
                        }

                        var row = new PrintOptions.Dto.RecieptInfo { 
                            Type = "Row", 
                            FontInfo = new PrintOptions.Dto.FontInfo(), 
                            Columns = new System.Collections.Generic.List<PrintOptions.Dto.Column>() 
                        };
                        
                        string len = colCount == 2 ? "50" : "33";

                        if (!string.IsNullOrEmpty(pavoItem.leftText)) {
                            row.Columns.Add(new PrintOptions.Dto.Column { 
                                Text = pavoItem.leftText, 
                                Length = len, 
                                FontInfo = new PrintOptions.Dto.FontInfo { Align = "Left", FontSize = fontSize, FontStyle = fontStyle } 
                            });
                        }
                        if (!string.IsNullOrEmpty(pavoItem.centerText)) {
                            string centerLen = colCount == 3 ? "34" : len; 
                            row.Columns.Add(new PrintOptions.Dto.Column { 
                                Text = pavoItem.centerText, 
                                Length = centerLen, 
                                FontInfo = new PrintOptions.Dto.FontInfo { Align = "Center", FontSize = fontSize, FontStyle = fontStyle } 
                            });
                        }
                        if (!string.IsNullOrEmpty(pavoItem.rightText)) {
                            row.Columns.Add(new PrintOptions.Dto.Column { 
                                Text = pavoItem.rightText, 
                                Length = len, 
                                FontInfo = new PrintOptions.Dto.FontInfo { Align = "Right", FontSize = fontSize, FontStyle = fontStyle } 
                            });
                        }

                        list.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Pavo Hata", "ConvertPavoToRecieptInfos Hatası: " + ex.Message);
            }
            return list;
        }

        public static string GetRecentLogs(int maxLines = 500)
        {
            try
            {
                string logPath = Path.Combine(AppPaths.WritableDataDirectory, "pavoLog.txt");
                if (!File.Exists(logPath)) return "Log dosyası bulunamadı.";

                lock (_fileLock)
                {
                    var lines = File.ReadAllLines(logPath, Encoding.UTF8);
                    if (lines.Length <= maxLines)
                    {
                        return string.Join(Environment.NewLine, lines);
                    }
                    else
                    {
                        var recentLines = lines.Skip(lines.Length - maxLines).ToArray();
                        return string.Join(Environment.NewLine, recentLines);
                    }
                }
            }
            catch (Exception ex)
            {
                return "Log okuma hatası: " + ex.Message;
            }
        }

        public static async Task<bool> UploadLogsAsync(string uploadUrl, string loginToken = null)
        {
            try
            {
                string logPath = Path.Combine(AppPaths.WritableDataDirectory, "pavoLog.txt");
                if (!File.Exists(logPath)) return false;

                string logContent;
                lock (_fileLock)
                {
                    logContent = File.ReadAllText(logPath, Encoding.UTF8);
                }

                string metadata = $"--- METADATA ---\n" +
                                  $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"MachineName: {Environment.MachineName}\n" +
                                  $"OSVersion: {Environment.OSVersion}\n" +
                                  $"UserName: {Environment.UserName}\n" +
                                  $"----------------\n\n";
                logContent = metadata + logContent;

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    if (!string.IsNullOrEmpty(loginToken))
                    {
                        client.DefaultRequestHeaders.Add("LoginToken", loginToken);
                        Log("Pavo Log Yükleme", $"Header eklendi: LoginToken = {loginToken}");
                    }
                    else
                    {
                        Log("Pavo Log Yükleme", "Header eklenecek LoginToken BOŞ veya NULL!");
                    }
                    var content = new StringContent(logContent, Encoding.UTF8, "text/plain");
                    var response = await client.PostAsync(uploadUrl, content).ConfigureAwait(false);
                    
                    string resBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Log("Pavo Log Yükleme", $"HTTP {(int)response.StatusCode} | Yanıt: {resBody}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var resJo = JObject.Parse(resBody);
                            if (resJo["result"]?.ToString() == "success" || resJo["status"]?.ToString() == "200")
                            {
                                return true;
                            }
                        }
                        catch
                        {
                            if (resBody.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void StartHeartbeatTask()
        {
            Task.Run(async () =>
            {
                // Uygulama ilk açıldığında ağın oturması için 15 saniye bekle
                await Task.Delay(15000).ConfigureAwait(false);

                while (true)
                {
                    try
                    {
                        // 3 dakikada bir kontrol et
                        await Task.Delay(TimeSpan.FromMinutes(3)).ConfigureAwait(false);

                        // Eğer son 3 dakika içinde aktif bir istek veya başarılı bir ping yapılmışsa gönderme
                        if (DateTime.Now - _lastActivityTime < TimeSpan.FromMinutes(3))
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(_lastPavoBaseUrl))
                        {
                            string url = _lastPavoBaseUrl.TrimEnd('/') + "/GetDeviceInfo";
                            
                            var th = new
                            {
                                SerialNumber = _lastSerialNumber ?? "",
                                TransactionDate = DateTime.Now.ToString("s") + ".000000",
                                TransactionSequence = GetNextSequence(),
                                Fingerprint = _lastFingerprint ?? "qrmenu_pavo"
                            };

                            var payload = new
                            {
                                TransactionHandle = th,
                                DeviceInfo = new { AdditionalInfo = new { serialNumber = true, fingerPrint = true } }
                            };

                            string bodyJson = JsonConvert.SerializeObject(payload);

                            try
                            {
                                await PostToUrlAsync(url, bodyJson).ConfigureAwait(false);
                                // Başarılı ping de aktivite zamanını günceller
                                _lastActivityTime = DateTime.Now;
                            }
                            catch (Exception ex)
                            {
                                // Ping hatasını log dosyasına yaz (sessizce izlenebilsin diye)
                                Log("Pavo Ping Hata", "Ping isteği başarısız: " + ex.Message);
                            }
                        }
                    }
                    catch { }
                }
            });
        }
    }
}
