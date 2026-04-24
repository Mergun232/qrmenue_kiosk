using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QRMENUE
{
    /// <summary>app_data.json içeriği — düz JSON veya kökte Languages.{tr|de|fr|en} blokları.</summary>
    public class AppDataDTO
    {
        public string ApiLink { get; set; }
        /// <summary>Printer-requests endpoint'inin kaç saniyede bir çağrılacağı. Varsayılan 5.</summary>
        public int PrinterRequestIntervalSeconds { get; set; }
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public string Text3 { get; set; }
        public string Text4 { get; set; }
        public string Text5 { get; set; }
        public string Text6 { get; set; }
        public string Text7 { get; set; }
        public string Text8 { get; set; }
        public string Text9 { get; set; }
        public string Text10 { get; set; }
        public string Text11 { get; set; }
        public string Text12 { get; set; }
        public string Text13 { get; set; }
        public string Text14 { get; set; }
        public string Text15 { get; set; }
        public string Text16 { get; set; }
        public string Text17 { get; set; }
        public string Text18 { get; set; }
        public string Text19 { get; set; }
        public string Text20 { get; set; }
        public string Text21 { get; set; }
        public string Text22 { get; set; }
        public string Text23 { get; set; }
        public string Text24 { get; set; }
        public string Text25 { get; set; }

        public string Settings_Title { get; set; }
        public string Settings_AutoStart { get; set; }
        public string Settings_AutoStart_Desc { get; set; }
        public string Settings_AutoLogin { get; set; }
        public string Settings_AutoLogin_Desc { get; set; }
        public string Settings_FullScreen { get; set; }
        public string Settings_FullScreen_Desc { get; set; }
        public string Settings_Language { get; set; }
        public string Settings_Language_Desc { get; set; }

        public string Login_Title { get; set; }
        public string Login_Subtitle { get; set; }
        public string Login_Button { get; set; }
        public string Login_ApiError { get; set; }
        public string Login_ConnectionError { get; set; }
        public string Login_Failed { get; set; }
        public string Login_UnknownError { get; set; }
        public string Login_DetailPrefix { get; set; }
        public string Login_HttpStatusSuffix { get; set; }

        public string Menu_PavoPosTest { get; set; }
        public string Form_SocketLogTitle { get; set; }
        public string Form_PavoPosTitle { get; set; }
        public string Dialog_WebView2Title { get; set; }

        public string Log_Label_System { get; set; }
        public string Log_Label_Error { get; set; }
        public string Log_Label_Debug { get; set; }
        public string Log_Marker_Empty { get; set; }
        public string Log_SocketNotStarted { get; set; }
        public string Log_SocketStarting { get; set; }
        public string Log_PrintRequestDispatch { get; set; }
        public string Log_PavoFormError { get; set; }
        public string Log_PrinterSkipped { get; set; }
        public string Log_PrinterRequestsCalling { get; set; }
        public string Log_Auth401Redirect { get; set; }
        public string Log_PrinterResponseInvalid { get; set; }
        public string Log_PrinterResponseSummary { get; set; }
        public string Log_UrlSkippedEmpty { get; set; }
        public string Log_UrlNavigating { get; set; }
        public string Log_UrlResponseChars { get; set; }
        public string Log_ReceiptDtoCount { get; set; }
        public string Log_NoReceiptFromUrl { get; set; }
        public string Log_JsonPreview { get; set; }
        public string Log_PrintUrlError { get; set; }
        public string Log_PrintedCount { get; set; }
    }

    public class Helpers
    {
        private void StartPutty(string PuttyPath)
        {
            Process winscp = new Process();
            winscp.StartInfo.FileName = PuttyPath;
            winscp.StartInfo.UseShellExecute = false;
            winscp.StartInfo.CreateNoWindow = true;
            winscp.Start();
        }

        /// <summary>HTTP isteği (opsiyonel LoginToken header ile). Status kodu gerekmezse bu overload kullanılır.</summary>
        public static string HttpHelper(string Url, string HttpMethod, ListDictionary parameters, string loginToken = null)
        {
            int? _;
            return HttpHelper(Url, HttpMethod, parameters, loginToken, out _);
        }

        /// <summary>HTTP isteği + sunucunun döndüğü status kodu (401 = auth hatası için MainForm'da kullanılır). HttpClient kullanır - harici bağımlılık yok.</summary>
        public static string HttpHelper(string Url, string HttpMethod, ListDictionary parameters, string loginToken, out int? httpStatusCode)
        {
            httpStatusCode = null;
            if (string.IsNullOrEmpty(Url))
                return "{}";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Qiox/3.0");
                    if (!string.IsNullOrEmpty(loginToken))
                        client.DefaultRequestHeaders.Add("LoginToken", loginToken);
                    client.Timeout = TimeSpan.FromSeconds(30);

                    HttpResponseMessage resp;
                    if (string.Equals(HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        resp = client.GetAsync(Url).GetAwaiter().GetResult();
                    }
                    else
                    {
                        HttpContent content = null;
                        if (parameters != null && parameters.Count > 0)
                        {
                            var formData = new List<KeyValuePair<string, string>>();
                            foreach (DictionaryEntry item in parameters)
                                formData.Add(new KeyValuePair<string, string>(item.Key.ToString(), item.Value?.ToString() ?? ""));
                            content = new FormUrlEncodedContent(formData);
                        }
                        resp = client.PostAsync(Url, content ?? new StringContent("")).GetAwaiter().GetResult();
                    }

                    if (resp != null)
                        httpStatusCode = (int)resp.StatusCode;
                    if (resp == null)
                        return "{}";
                    string body = resp.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
                    if (!resp.IsSuccessStatusCode)
                        return "{\"result\":\"error\",\"text1\":\"HTTP " + (int)resp.StatusCode + " " + (resp.ReasonPhrase ?? "") + "\"}";
                    return string.IsNullOrEmpty(body) ? "{}" : body;
                }
            }
            catch (Exception ex)
            {
                return "{\"result\":\"error\",\"text1\":\"" + (ex.Message ?? "İstek hatası") + "\"}";
            }
        }

        /// <summary>HTTP isteği (Asenkron). Status kodu gerekirse HttpResponseMessage içinden bakılır.</summary>
        public static async Task<string> HttpHelperAsync(string url, string httpMethod, ListDictionary parameters, string loginToken = null)
        {
            if (string.IsNullOrEmpty(url)) return "{}";
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Qiox/3.0");
                    if (!string.IsNullOrEmpty(loginToken))
                        client.DefaultRequestHeaders.Add("LoginToken", loginToken);
                    client.Timeout = TimeSpan.FromSeconds(30);

                    HttpResponseMessage resp;
                    if (string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        resp = await client.GetAsync(url).ConfigureAwait(false);
                    }
                    else
                    {
                        HttpContent content = null;
                        if (parameters != null && parameters.Count > 0)
                        {
                            var formData = new List<KeyValuePair<string, string>>();
                            foreach (DictionaryEntry item in parameters)
                                formData.Add(new KeyValuePair<string, string>(item.Key.ToString(), item.Value?.ToString() ?? ""));
                            content = new FormUrlEncodedContent(formData);
                        }
                        resp = await client.PostAsync(url, content ?? new StringContent("")).ConfigureAwait(false);
                    }

                    if (resp == null) return "{}";
                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                        return "{\"result\":\"error\",\"text1\":\"HTTP " + (int)resp.StatusCode + " " + (resp.ReasonPhrase ?? "") + "\"}";
                    return string.IsNullOrEmpty(body) ? "{}" : body;
                }
            }
            catch (Exception ex)
            {
                return "{\"result\":\"error\",\"text1\":\"" + (ex.Message ?? "İstek hatası") + "\"}";
            }
        }

        /// <summary>JSON body ile POST (exe-state, exe-log için). LoginToken opsiyonel.</summary>
        public static string HttpHelperJson(string Url, string jsonBody, string loginToken = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Qiox/3.0");
                    if (!string.IsNullOrEmpty(loginToken))
                        client.DefaultRequestHeaders.Add("LoginToken", loginToken);
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var content = new StringContent(jsonBody ?? "{}", Encoding.UTF8, "application/json");
                    var resp = client.PostAsync(Url, content).GetAwaiter().GetResult();
                    return resp.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>JSON body ile POST (Asenkron).</summary>
        public static async Task<string> HttpHelperJsonAsync(string url, string jsonBody, string loginToken = null)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Qiox/3.0");
                    if (!string.IsNullOrEmpty(loginToken))
                        client.DefaultRequestHeaders.Add("LoginToken", loginToken);
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var content = new StringContent(jsonBody ?? "{}", Encoding.UTF8, "application/json");
                    var resp = await client.PostAsync(url, content).ConfigureAwait(false);
                    return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>Şifreyi dokümantasyona uygun MD5 hash'e çevirir (login için).</summary>
        public static string PasswordToMd5(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            using (var md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static void StringToPdf(string b64)
        {

        }
    }

    /// <summary>
    /// app_data.json dosyasını yükler ve API URL'lerini üretir.
    /// </summary>
    public static class AppDataLoader
    {
        private static AppDataDTO _data;
        private static readonly object _lock = new object();

        public static AppDataDTO Data
        {
            get
            {
                if (_data == null)
                {
                    lock (_lock)
                    {
                        if (_data == null)
                        {
                            string lang = "TR";
                            try { lang = KioskSettingsStore.Load().UiLanguage; } catch { }
                            _data = LoadForLanguage(lang);
                        }
                    }
                }
                return _data;
            }
        }

        /// <summary>kiosk_settings dil koduna göre app_data_XX.json veya app_data.json yükler.</summary>
        public static void ReloadWithLanguage(string langCode)
        {
            lock (_lock)
            {
                _data = LoadForLanguage(langCode);
            }
        }

        private static AppDataDTO LoadForLanguage(string langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode)) langCode = "TR";
            langCode = langCode.Trim().ToUpperInvariant();
            if (langCode != "TR")
            {
                string localized = Path.Combine(ApplicationStartupPath(), "app_data_" + langCode + ".json");
                if (File.Exists(localized))
                {
                    try
                    {
                        var d = ParseAppDataFile(localized, langCode);
                        if (d != null) return d;
                    }
                    catch { }
                }
            }
            return LoadPrimaryOrDefaults(langCode);
        }

        private static string MapLangJsonKey(string uiLangUpper)
        {
            if (string.IsNullOrWhiteSpace(uiLangUpper)) return "tr";
            switch (uiLangUpper.Trim().ToUpperInvariant())
            {
                case "DE": return "de";
                case "FR": return "fr";
                case "EN": return "en";
                default: return "tr";
            }
        }

        private static AppDataDTO ParseAppDataFile(string path, string uiLangUpper)
        {
            string json = File.ReadAllText(path);
            return ParseAppDataJson(json, uiLangUpper);
        }

        /// <summary>Kökte Languages varsa UiLanguage ile blok seçilir; yoksa düz AppDataDTO.</summary>
        private static AppDataDTO ParseAppDataJson(string json, string uiLangUpper)
        {
            if (string.IsNullOrWhiteSpace(json)) return GetDefaults();
            try
            {
                var jo = JObject.Parse(json);
                var languages = jo["Languages"] as JObject;
                if (languages != null && languages.Properties().Any())
                {
                    string key = MapLangJsonKey(uiLangUpper);
                    JToken block = languages[key] ?? languages["tr"];
                    if (block == null || block.Type == JTokenType.Null)
                        block = languages.Properties().First().Value;
                    if (block is JObject)
                    {
                        var dto = block.ToObject<AppDataDTO>();
                        if (dto != null)
                        {
                            int rootInterval = jo["PrinterRequestIntervalSeconds"]?.Value<int?>() ?? 0;
                            if (rootInterval > 0)
                                dto.PrinterRequestIntervalSeconds = rootInterval;
                            else if (dto.PrinterRequestIntervalSeconds <= 0)
                                dto.PrinterRequestIntervalSeconds = 5;
                            return dto;
                        }
                    }
                }
                var flat = jo.ToObject<AppDataDTO>();
                if (flat != null)
                {
                    if (flat.PrinterRequestIntervalSeconds <= 0)
                        flat.PrinterRequestIntervalSeconds = 5;
                    return flat;
                }
            }
            catch
            {
                try
                {
                    var flat = JsonConvert.DeserializeObject<AppDataDTO>(json);
                    if (flat != null)
                    {
                        if (flat.PrinterRequestIntervalSeconds <= 0)
                            flat.PrinterRequestIntervalSeconds = 5;
                        return flat;
                    }
                }
                catch { }
            }
            return GetDefaults();
        }

        private static AppDataDTO LoadPrimaryOrDefaults(string uiLangUpper)
        {
            try
            {
                string path = Path.Combine(ApplicationStartupPath(), "app_data.json");
                if (File.Exists(path))
                {
                    var data = ParseAppDataFile(path, uiLangUpper);
                    if (data != null) return data;
                }
            }
            catch { }
            return GetDefaults();
        }

        public static AppDataDTO Load()
        {
            string lang = "TR";
            try { lang = KioskSettingsStore.Load().UiLanguage; } catch { }
            return LoadPrimaryOrDefaults(lang);
        }

        private static string ApplicationStartupPath()
        {
            try
            {
                return AppPaths.InstallDirectory;
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory ?? "";
            }
        }

        public static string GetApiBase()
        {
            string link = Data?.ApiLink ?? "";
            return (link ?? "").TrimEnd('/', ' ');
        }

        public static string GetApiUrl(string path)
        {
            string baseUrl = GetApiBase();
            if (string.IsNullOrEmpty(baseUrl)) return path;
            path = (path ?? "").TrimStart('/');
            return baseUrl + "/" + path;
        }

        private static AppDataDTO GetDefaults()
        {
            return new AppDataDTO
            {
                ApiLink = "https://qrmenue.com/tr/",
                PrinterRequestIntervalSeconds = 5,
                Text1 = "Firma Kodu",
                Text2 = "Kullanıcı Adı",
                Text3 = "Şifre",
                Text4 = "Beni hatırla",
                Text9 = "Lütfen tüm alanları doldurunuz!",
                Text10 = "Hata",
                Text11 = "Programın text datası bulunamadı!",
                Text12 = "Program Zaten Çalışıyor!",
                Text13 = "Personel Giriş",
                Text14 = "Giriş",
                Text15 = "Çalışıyor",
                Text16 = "Çıkış",
                Text17 = "Otomatik Başlat",
                Text18 = "İptal",
                Text19 = "Bağlanıyor, lütfen bekleyin...",
                Text20 = "Bağlantı Durumu",
                Text21 = "Qiox",
                Text22 = "Hiç Hata Yok.",
                Text23 = "Version: 2.0.0",
                Text24 = "Qiox",
                Text25 = "WebSocket sunucusu 22444 portunda çalışıyor",
                Settings_Title = "Ayarlar",
                Settings_AutoStart = "PC başladığında otomatik başla",
                Settings_AutoStart_Desc = "Windows açıldığında uygulama otomatik çalışır",
                Settings_AutoLogin = "Otomatik giriş yap",
                Settings_AutoLogin_Desc = "Kayıtlı bilgilerle otomatik giriş yapılır",
                Settings_FullScreen = "Tam ekran modu",
                Settings_FullScreen_Desc = "Login ve panel tam ekranda açılır",
                Settings_Language = "Dil",
                Settings_Language_Desc = "Uygulama dilini seçin",
                Login_Title = "Personel Giriş",
                Login_Subtitle = "Hesabınıza giriş yapın",
                Login_Button = "Giriş Yap",
                Login_ApiError = "API adresi tanımlı değil. app_data.json içinde ApiLink kontrol edin.",
                Login_ConnectionError = "Bağlantı yanıtı alınamadı.",
                Login_Failed = "Giriş başarısız.",
                Login_UnknownError = "Bilinmeyen hata",
                Login_DetailPrefix = "Detay: ",
                Login_HttpStatusSuffix = ", HTTP ",
                Menu_PavoPosTest = "Pavo POS Test",
                Form_SocketLogTitle = "Socket olayları (test)",
                Form_PavoPosTitle = "Pavo POS Log İzleyici",
                Dialog_WebView2Title = "WebView2",
                Log_Label_System = "Sistem",
                Log_Label_Error = "Hata",
                Log_Label_Debug = "Debug",
                Log_Marker_Empty = "(boş)",
                Log_SocketNotStarted = "Socket başlatılmadı: Login yanıtında SocketConnectUrl yok veya boş. API'nin DataList.SocketConnectUrl döndürdüğünden emin olun.",
                Log_SocketStarting = "Socket başlatılıyor: {0} FirmaCode: {1} KullaniciID: {2}",
                Log_PrintRequestDispatch = "PrintRequest alındı → printer-requests API tetikleniyor...",
                Log_PavoFormError = "PavoPosForm: {0}",
                Log_PrinterSkipped = "printer-requests atlandı: PrinterStart kapalı veya LoginToken yok.",
                Log_PrinterRequestsCalling = "printer-requests API çağrılıyor: {0}",
                Log_Auth401Redirect = "401 Auth hatası — WebView login sayfasına yönlendiriliyor.",
                Log_PrinterResponseInvalid = "printer-requests yanıtı geçersiz veya boş.",
                Log_PrinterResponseSummary = "printer-requests yanıt: result={0}, Url sayısı={1}{2}",
                Log_UrlSkippedEmpty = "URL {0} boş, atlanıyor.",
                Log_UrlNavigating = "Fiş URL'sine gidiliyor: {0}",
                Log_UrlResponseChars = "URL yanıtı: {0} karakter",
                Log_ReceiptDtoCount = "RecieptDto sayısı: {0}",
                Log_NoReceiptFromUrl = "Bu URL'den fiş verisi gelmedi (JSON formatı RecieptDto ile uyumlu olmayabilir).",
                Log_JsonPreview = "JSON önizleme: {0}",
                Log_PrintUrlError = "URL isteği/yazdırma: {0}",
                Log_PrintedCount = "Yazdırılan fiş sayısı: {0}"
            };
        }
    }
}
