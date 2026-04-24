using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using QRMENUE;

namespace QRMENUE.Pavo
{
    /// <summary>
    /// Socket üzerinden gelen Pavo isteklerini arka planda tetikler.
    /// MainForm OnSocketEvent'te PavoPairing, PavoInitiateSale, PavoGetSaleResult, PavoPrintOut dinlenir.
    /// Tüm işlemler Task.Run ile arka planda çalışır.
    /// </summary>
    public static class PavoPosSocketBridge
    {
        private static Action<string, string> _logCallback;
        private static PavoApiClient _client;
        private static readonly object _lock = new object();

        private static int _pavoRequestSequence = Math.Max(100, Math.Abs(Environment.TickCount % 100000));
        private static readonly object _seqLock = new object();

        private static int GetNextSequence()
        {
            lock (_seqLock)
            {
                if (_pavoRequestSequence > 999999) _pavoRequestSequence = 100;
                return ++_pavoRequestSequence;
            }
        }

        /// <summary>Log callback ve client kaydeder. PavoPosForm açıldığında çağrılır.</summary>
        public static void Register(Action<string, string> logCallback, PavoApiClient client)
        {
            lock (_lock)
            {
                _logCallback = logCallback;
                _client = client;
            }
        }

        /// <summary>Kaydı kaldırır. Form kapandığında.</summary>
        public static void Unregister()
        {
            lock (_lock)
            {
                _logCallback = null;
                _client = null;
            }
        }

        private static void Log(string tag, string msg)
        {
            // SystemDiagnostics ile debug'a da yazdırılım
            System.Diagnostics.Debug.WriteLine($"[{tag}] {msg}");
            try { _logCallback?.Invoke(tag, msg ?? ""); } catch { }
        }

        private static PavoApiClient GetOrCreateClient()
        {
            lock (_lock)
            {
                if (_client != null) return _client;
            }
            var cfg = LoadConfig();
            if (cfg == null) return null;
            var c = new PavoApiClient(cfg.BaseUrl, cfg.SerialNumber, cfg.Fingerprint, cfg.BypassSsl);
            lock (_lock) { _client = c; }
            return c;
        }

        private static PavoConfig LoadConfig()
        {
            try
            {
                var path = AppPaths.PavoPosConfigPath;
                if (!File.Exists(path)) return null;
                var json = File.ReadAllText(path);
                return new JavaScriptSerializer().Deserialize<PavoConfig>(json);
            }
            catch { return null; }
        }

        /// <summary>Socket'ten "PavoPairing" geldiğinde. payload: tam JSON veya boş.</summary>
        public static void TriggerPairing(string payload = null)
        {
            Task.Run(async () =>
            {
                try
                {
                    var client = GetOrCreateClient();
                    if (client == null) { Log("Pavo", "pavopos.json yok veya geçersiz."); return; }
                    Log("Pavo", "Pairing başlatılıyor (socket)...");
                    var body = string.IsNullOrWhiteSpace(payload) ? "{}" : payload;
                    if (IsFullJson(body))
                    {
                        var result = await client.PostRawAsync("/Pairing", body);
                        Log("Pavo", "Pairing yanıt: " + Truncate(result, 500));
                    }
                    else
                    {
                        var result = await client.PairingAsync();
                        Log("Pavo", "Pairing yanıt: " + Truncate(result, 500));
                    }
                }
                catch (Exception ex) { Log("Pavo Hata", "Pairing: " + ex.Message); }
            });
        }

        /// <summary>Socket'ten "PavoInitiateSale" geldiğinde. payload: tam JSON veya { "orderNo", "totalPrice" }.</summary>
        public static void TriggerInitiateSale(string payload)
        {
            Task.Run(async () =>
            {
                try
                {
                    var client = GetOrCreateClient();
                    if (client == null) { Log("Pavo", "Client yok."); return; }
                    if (IsFullJson(payload) && HasProperty(payload, "Sale"))
                    {
                        Log("Pavo", "InitiateSale başlatılıyor (socket, tam JSON)...");
                        var result = await client.PostRawAsync("/InitiateSale", payload);
                        Log("Pavo", "InitiateSale yanıt: " + Truncate(result, 500));
                    }
                    else
                    {
                        string orderNo = null;
                        decimal totalPrice = 20m;
                        if (!string.IsNullOrWhiteSpace(payload))
                        {
                            var jo = JObject.Parse(payload);
                            orderNo = jo["orderNo"]?.ToString();
                            if (jo["totalPrice"] != null) decimal.TryParse(jo["totalPrice"].ToString(), out totalPrice);
                        }
                        if (string.IsNullOrWhiteSpace(orderNo)) orderNo = "PAVO" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        Log("Pavo", "InitiateSale başlatılıyor (socket): OrderNo=" + orderNo);
                        var items = new System.Collections.Generic.List<SaleItem>
                        {
                            new SaleItem { Name = "Test Ürün", IsGeneric = false, UnitCode = "KGM", TaxGroupCode = "KDV18", ItemQuantity = 1, UnitPriceAmount = totalPrice, GrossPriceAmount = totalPrice, TotalPriceAmount = totalPrice }
                        };
                        var result = await client.InitiateSaleAsync(orderNo, totalPrice, items);
                        Log("Pavo", "InitiateSale yanıt: " + Truncate(result, 500));
                    }
                }
                catch (Exception ex) { Log("Pavo Hata", "InitiateSale: " + ex.Message); }
            });
        }

        /// <summary>Socket'ten "PavoGetSaleResult" geldiğinde. payload: tam JSON veya { "orderNo" }. Sonuç PavoRequest ile aynı formatta onResult üzerinden iletilir.</summary>
        public static void TriggerGetSaleResult(string payload, Action<string> onResult = null)
        {
            Task.Run(async () =>
            {
                try
                {
                    var client = GetOrCreateClient();
                    if (client == null)
                    {
                        Log("Pavo", "Client yok.");
                        onResult?.Invoke("{\"HasError\":true,\"ErrorCode\":999,\"DeviceOffline\":true,\"Message\":\"Client yok.\"}");
                        return;
                    }
                    string orderNo = null;
                    string result = null;
                    if (IsFullJson(payload) && HasProperty(payload, "Sale"))
                    {
                        try { orderNo = JObject.Parse(payload)["Sale"]?["OrderNo"]?.ToString(); } catch { }
                        Log("Pavo", "GetSaleResult sorgulanıyor (socket, tam JSON)...");
                        result = await client.PostRawAsync("/GetSaleResult", payload);
                        Log("Pavo", "GetSaleResult yanıt: " + Truncate(result, 500));
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(payload))
                        {
                            var jo = JObject.Parse(payload);
                            orderNo = jo["orderNo"]?.ToString() ?? jo["OrderNo"]?.ToString();
                        }
                        if (string.IsNullOrWhiteSpace(orderNo))
                        {
                            Log("Pavo", "orderNo gerekli.");
                            onResult?.Invoke("{\"HasError\":true,\"ErrorCode\":998,\"Message\":\"orderNo gerekli.\"}");
                            return;
                        }
                        Log("Pavo", "GetSaleResult sorgulanıyor: " + orderNo);
                        result = await client.GetSaleResultAsync(orderNo);
                        Log("Pavo", "GetSaleResult yanıt: " + Truncate(result, 500));
                    }
                    if (result != null)
                    {
                        TryExtractAndSaveReceiptImage(result, orderNo);
                        onResult?.Invoke(MinimizePavoResult(result, orderNo));
                    }
                }
                catch (Exception ex)
                {
                    string realError = ex.Message;
                    if (ex.InnerException != null) realError += " | Inner: " + ex.InnerException.Message;
                    Log("Pavo Hata", "GetSaleResult: " + realError);
                    onResult?.Invoke("{\"HasError\":true,\"ErrorCode\":999,\"DeviceOffline\":true,\"Message\":\"" + realError.Replace("\"", "\\\"") + "\"}");
                }
            });
        }

        /// <summary>Socket'ten "PavoPrintOut" geldiğinde. payload: tam JSON veya { "image" }.</summary>
        public static void TriggerPrintOut(string payload)
        {
            Task.Run(async () =>
            {
                try
                {
                    var client = GetOrCreateClient();
                    if (client == null) { Log("Pavo", "Client yok."); return; }
                    if (IsFullJson(payload) && HasProperty(payload, "Print"))
                    {
                        Log("Pavo", "PrintOut başlatılıyor (socket, tam JSON)...");
                        var result = await client.PostRawAsync("/PrintOut", payload);
                        Log("Pavo", "PrintOut yanıt: " + Truncate(result, 300));
                    }
                    else
                    {
                        string image = null;
                        if (!string.IsNullOrWhiteSpace(payload))
                        {
                            var jo = JObject.Parse(payload);
                            image = jo["image"]?.ToString() ?? jo["Image"]?.ToString();
                        }
                        Log("Pavo", "PrintOut başlatılıyor (socket)...");
                        var result = await client.PrintOutAsync(image ?? "");
                        Log("Pavo", "PrintOut yanıt: " + Truncate(result, 300));
                    }
                }
                catch (Exception ex) { Log("Pavo Hata", "PrintOut: " + ex.Message); }
            });
        }

        /// <summary>Socket'ten "PavoPaymentMediators" veya "PavoCompleteSale" - tam JSON destekli.</summary>
        public static void TriggerPaymentMediators(string payload = null)
        {
            Task.Run(async () =>
            {
                try
                {
                    var client = GetOrCreateClient();
                    if (client == null) { Log("Pavo", "Client yok."); return; }
                    var body = string.IsNullOrWhiteSpace(payload) ? "{}" : payload;
                    Log("Pavo", "PaymentMediators sorgulanıyor (socket)...");
                    var result = await client.PostRawAsync("/PaymentMediators", body).ConfigureAwait(false);
                    Log("Pavo", "PaymentMediators yanıt: " + Truncate(result, 500));
                }
                catch (Exception ex) { Log("Pavo Hata", "PaymentMediators: " + ex.Message); }
            });
        }

        /// <summary>Socket'ten "PavoCompleteSale". Sonuç GetSaleResult / PavoRequest ile aynı şekilde minimize edilip onResult ile iletilir.</summary>
        public static void TriggerCompleteSale(string payload, Action<string> onResult = null)
        {
            Task.Run(async () =>
            {
                try
                {
                    var client = GetOrCreateClient();
                    if (client == null)
                    {
                        Log("Pavo", "Client yok.");
                        onResult?.Invoke("{\"HasError\":true,\"ErrorCode\":999,\"DeviceOffline\":true,\"Message\":\"Client yok.\"}");
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(payload) || !HasProperty(payload, "Sale"))
                    {
                        Log("Pavo", "CompleteSale için Sale objesi gerekli.");
                        onResult?.Invoke("{\"HasError\":true,\"ErrorCode\":998,\"Message\":\"CompleteSale için Sale objesi gerekli.\"}");
                        return;
                    }
                    string orderNo = null;
                    try { orderNo = JObject.Parse(payload)["Sale"]?["OrderNo"]?.ToString(); } catch { }

                    Log("Pavo", "CompleteSale başlatılıyor (socket)...");
                    var result = await client.PostRawAsync("/CompleteSale", payload).ConfigureAwait(false);
                    Log("Pavo", "CompleteSale yanıt: " + Truncate(result, 500));
                    Log("Pavo", "CompleteSale Full Result: " + result);
                    TryExtractAndSaveReceiptImage(result, orderNo);
                    onResult?.Invoke(MinimizePavoResult(result, orderNo));
                }
                catch (Exception ex)
                {
                    string realError = ex.Message;
                    if (ex.InnerException != null) realError += " | Inner: " + ex.InnerException.Message;
                    Log("Pavo Hata", "CompleteSale: " + realError);
                    onResult?.Invoke("{\"HasError\":true,\"ErrorCode\":999,\"DeviceOffline\":true,\"Message\":\"" + realError.Replace("\"", "\\\"") + "\"}");
                }
            });
        }

        private static bool IsFullJson(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            return s.StartsWith("{") && s.EndsWith("}");
        }

        private static bool HasProperty(string json, string prop)
        {
            try
            {
                var jo = JObject.Parse(json ?? "{}");
                return jo[prop] != null;
            }
            catch { return false; }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length > max ? s.Substring(0, max) + "..." : s;
        }

        /// <summary>Socket'ten "PavoRequest" - url + body ile doğrudan POST. Sunucu tüm bilgileri gönderir (POS IP, SerialNumber, vb.).</summary>
        /// <param name="onResult">JSON + hedef oda (CancelSale / GetCancellationResult → Firma, diğerleri → Personel).</param>
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
                    if (string.IsNullOrWhiteSpace(url)) { Log("Pavo", "PavoRequest: url gerekli."); return; }

                    if (bodyObj != null && bodyObj["TransactionHandle"] != null)
                    {
                        var th = bodyObj["TransactionHandle"];
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

                    // --- EKLENEN ASENKRON TAKİP (POLLING) MANTIĞI ---
                    if (url.IndexOf("/InitiateSale", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        url.IndexOf("/CompleteSale", StringComparison.OrdinalIgnoreCase) >= 0)
                    {

                        if (!string.IsNullOrEmpty(orderNo))
                        {
                            string getUrl = url.Replace("InitiateSale", "GetSaleResult")
                                               .Replace("CompleteSale", "GetSaleResult");
                                                       
                            Log("Pavo", $"[{orderNo}] HTTP Response beklenmeden paralel GetSaleResult takibi başlatılıyor...");
                            
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(1500).ConfigureAwait(false); // Cihaza isteği işlemesi için 1.5 saniyelik avans ver
                                await PollGetSaleResultAsync(getUrl, (JObject)bodyObj, orderNo, onResult, PavoRequestResultRoom.Personel).ConfigureAwait(false);
                            });
                            
                        }
                    }

                    while (needsRetry && retryCount < maxRetries)
                    {
                        var bodyJson = bodyObj != null ? bodyObj.ToString() : "{}";
                        if (retryCount == 0)
                            Log("Pavo", "PavoRequest gönderiliyor: " + url);
                        else
                            Log("Pavo", $"PavoRequest tekrar deneniyor ({retryCount}/{maxRetries}): " + url);

                        result = await PostToUrlAsync(url, bodyJson).ConfigureAwait(false);
                        Log("Pavo", $"Direct Result from {url}: {result}");
                        
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

                    if (url.IndexOf("/CancelSale", StringComparison.OrdinalIgnoreCase) >= 0 && bodyObj != null)
                    {
                        try
                        {
                            var cancelRes = JObject.Parse(result);
                            if (cancelRes["HasError"]?.Value<bool>() != true)
                            {
                                string pollOrderNo = orderNo;
                                if (string.IsNullOrWhiteSpace(pollOrderNo))
                                    pollOrderNo = cancelRes["Data"]?["OrderNo"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(pollOrderNo))
                                {
                                    string getCancelUrl = Regex.Replace(url, "CancelSale", "GetCancellationResult", RegexOptions.IgnoreCase);
                                    Log("Pavo", $"[{pollOrderNo}] CancelSale sonrası GetCancellationResult takibi başlatılıyor...");
                                    _ = Task.Run(async () =>
                                    {
                                        await Task.Delay(1500).ConfigureAwait(false);
                                        await PollGetSaleResultAsync(getCancelUrl, (JObject)bodyObj, pollOrderNo, onResult, PavoRequestResultRoom.Firma).ConfigureAwait(false);
                                    });
                                }
                            }
                        }
                        catch { }
                    }

                    if (url.IndexOf("CompleteSale", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log("Pavo", "CompleteSale Full Result: " + result);
                        TryExtractAndSaveReceiptImage(result, orderNo);
                    }
                    else
                    {
                        Log("Pavo", "PavoRequest yanıt: " + Truncate(result, 500));
                        if (url.IndexOf("GetSaleResult", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            url.IndexOf("GetCancellationResult", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            TryExtractAndSaveReceiptImage(result, orderNo);
                        }
                    }
                    
                    // TransactionHandle socket aktarımında gereksiz yük olmasın diye kaldırılır
                    string finalResultForSocket = result;
                    try
                    {
                        var resJo = JObject.Parse(result);
                        if (resJo["TransactionHandle"] != null)
                        {
                            resJo.Property("TransactionHandle")?.Remove();
                            finalResultForSocket = resJo.ToString(Newtonsoft.Json.Formatting.None);
                        }
                    }
                    catch { }

                    // orderNo already extracted above

                    if (url.IndexOf("/GetSaleResult", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("/GetCancellationResult", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("/CompleteSale", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("/InitiateSale", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("/CancelSale", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var socketRoom = (url.IndexOf("/CancelSale", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         url.IndexOf("/GetCancellationResult", StringComparison.OrdinalIgnoreCase) >= 0)
                            ? PavoRequestResultRoom.Firma
                            : PavoRequestResultRoom.Personel;
                        onResult?.Invoke(MinimizePavoResult(result, orderNo), socketRoom);
                    }
                    else
                    {
                        onResult?.Invoke(finalResultForSocket, PavoRequestResultRoom.Personel); // NodeJS Socket'e gönderim
                    }
                }
                catch (Exception ex)
                {
                    string realError = ex.Message;
                    if (ex.InnerException != null) realError += " | Inner: " + ex.InnerException.Message;
                    
                    Log("Pavo Hata", "PavoRequest: " + realError);
                    // Hata durumunda ErrorCode 999 döndürüyoruz ki cihaz kapalı / ulaşılamıyor durumu anlaşılsın
                    onResult?.Invoke("{\"HasError\":true,\"ErrorCode\":999,\"DeviceOffline\":true,\"Message\":\"" + realError.Replace("\"", "\\\"") + "\"}", PavoRequestResultRoom.Personel);
                }
            });
        }

        private static async Task PollGetSaleResultAsync(string url, JObject originalBody, string orderNo, Action<string, PavoRequestResultRoom> onResult, PavoRequestResultRoom socketRoom)
        {
            try
            {
                bool cancelPoll = url.IndexOf("GetCancellationResult", StringComparison.OrdinalIgnoreCase) >= 0;
                Log("Pavo", $"[{orderNo}] Otomatik {(cancelPoll ? "GetCancellationResult" : "GetSaleResult")} takibi başladı...");
                int maxRetries = 60; // 3 dakika boyunca her 3 saniyede 1 kez

                for (int i = 0; i < maxRetries; i++)
                {
                    await Task.Delay(3000).ConfigureAwait(false);

                    // Yeni body oluştur (Sadece TransactionHandle ve Sale:OrderNo içermeli)
                    var newBody = new JObject();
                    
                    if (originalBody["TransactionHandle"] != null)
                    {
                        newBody["TransactionHandle"] = originalBody["TransactionHandle"].DeepClone();
                    }
                    else
                    {
                        newBody["TransactionHandle"] = new JObject();
                    }
                    
                    newBody["TransactionHandle"]["TransactionSequence"] = GetNextSequence();
                    newBody["TransactionHandle"]["TransactionDate"] = DateTime.Now.ToString("s") + ".000000";
                    
                    newBody["Sale"] = new JObject { ["OrderNo"] = orderNo };

                    var pollResult = await PostToUrlAsync(url, newBody.ToString()).ConfigureAwait(false);
                    
                    TryExtractAndSaveReceiptImage(pollResult, orderNo);

                    var resJo = JObject.Parse(pollResult);
                    
                    try
                    {
                        var socketPollResult = (JObject)resJo.DeepClone();
                        socketPollResult.Property("TransactionHandle")?.Remove();
                        onResult?.Invoke(MinimizePavoResult(socketPollResult.ToString(), orderNo), socketRoom);
                    }
                    catch 
                    {
                        onResult?.Invoke(MinimizePavoResult(pollResult, orderNo), socketRoom); // Fallback
                    }

                    if (resJo["HasError"]?.Value<bool>() == true)
                    {
                        string errCode = resJo["ErrorCode"]?.ToString() ?? "?";
                        string errMsg = resJo["Message"]?.ToString() ?? "Bilinmeyen Hata";
                        
                        if (errCode == "130")
                        {
                            Log("Pavo", $"[{orderNo}] İşlem devam ediyor (130).");
                            continue; // Döngüyü kırma, takibe devam et
                        }
                        else if (errCode == "73")
                        {
                            Log("Pavo", $"[{orderNo}] 73 alındı, tekrar deneniyor.");
                            continue; // Tekrar dene (yeni sıra no ile)
                        }
                        
                        Log("Pavo Hata", $"[{orderNo}] Takip hatası ({errCode}: {errMsg}), takip durduruldu.");
                        break;
                    }

                    int statusId = resJo["Data"]?["StatusId"]?.Value<int>() ?? 0;
                    
                    // Devam edilecek (Beklemede olan) durumlar
                    // 1:Suspended, 2:PaymentWaiting, 3:DocumentCreating, 4:DocumentPending,
                    // 5:DocumentCreated ve 6+ terminal — takip burada biter (5 artık beklemede sayılmaz)
                    // 9:Signing, 12:PaymentCancelling, 15:DocumentCancelling, 17:DocumentCancelPending, 19:ERPProcessing, 22:InspectionPending
                    bool inProgress = (statusId == 1 || statusId == 2 || statusId == 3 || statusId == 4 ||
                                       statusId == 9 || statusId == 12 ||
                                       statusId == 15 || statusId == 17 || statusId == 19 || statusId == 22);

                    if (!inProgress)
                    {
                        Log("Pavo", $"[{orderNo}] İşlem sonlandı (StatusId: {statusId}). Takip bitirildi.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Pavo Hata", $"[{orderNo}] Otomatik takipte hata: " + ex.Message);
            }
        }

        private static async Task<string> PostToUrlAsync(string url, string bodyJson)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true
            };
            using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(90) }) // Fiş yazdırma vb. işlemler uzun sürebilir, eski 90 saniye değerine geri aldık
            {
                var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content).ConfigureAwait(false);
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
                // Remove TransactionHandle even on failure if parseable
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

                return jsonResult; // JSON parse hatası durumunda orjinalini dön
            }
        }

        private static void TryExtractAndSaveReceiptImage(string jsonResult, string orderNo)
        {
            try
            {
                var jo = JObject.Parse(jsonResult);
                // 130: işlem devam ediyor — Data genelde null; alt alanlara erişmeyelim (JValue hatası önlenir)
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
                        // JSON parçalaması: Çift encode olma ihtimalini kapsıyoruz
                        string prettyJson = string.Empty;
                        try
                        {
                            JToken parsedToken = JToken.Parse(receiptJsonStr);
                            
                            // Eğer içeriğin kendisi hala bir string'se (çift encode) tekrar ayrıştıralım
                            if (parsedToken.Type == JTokenType.String)
                            {
                                parsedToken = JToken.Parse(parsedToken.ToString());
                            }
                            
                            prettyJson = parsedToken.ToString(Newtonsoft.Json.Formatting.Indented);
                        }
                        catch
                        {
                            // Eğer geçerli bir JSON objesine dönüştürülemezse, ham haliyle kaydedelim
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

                        // Dönüştürülmüş RecieptInfo listesini oluştur ve kaydet
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

        private class PavoConfig
        {
            public string BaseUrl { get; set; }
            public string SerialNumber { get; set; }
            public string Fingerprint { get; set; }
            public bool BypassSsl { get; set; } = true;
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

                        // Eğer sadece tek bir metin varsa, QrMenue taslağındaki gibi 'Line' olarak ekle
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

                        // Birden fazla metin parçası (sütun) varsa 'Row' kullan
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
    }
}
