using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace QRMENUE
{
    /// <summary>PavoRequestResult emit hedefi; sunucu tarafındaki oda adlarıyla (P… / C…) eşleşmeli.</summary>
    public enum PavoRequestResultRoom
    {
        Personel,
        Firma
    }

    /// <summary>Login sonrası SocketConnectUrl + FirmaCode ile Socket.IO sunucusuna bağlanır; AllRequest, PrintRequest, DisSiparisRequest2, TumBildirimler dinler.
    /// HttpClient tabanlı minimal client kullanır. Kopma durumunda otomatik yeniden bağlanır.</summary>
    public class SocketService
    {
        private SocketIoHttpClient _client;
        private readonly string _socketUrl;
        private readonly string _firmaCode;
        private readonly string _kullaniciId;
        private readonly Action<string, string> _onMessage;
        private volatile bool _disconnectRequested;
        private const int ReconnectDelaySeconds = 5;

        /// <summary>Socket şu an bağlı mı? MainForm timer'da buna bakıyor: true ise printer-requests sadece PrintRequest ile atılır, false ise timer tetikler.</summary>
        public bool IsConnected => _client?.Connected ?? false;

        public SocketService(string socketConnectUrl, string firmaCode, Action<string, string> onMessage)
            : this(socketConnectUrl, firmaCode, null, onMessage)
        {
        }

        /// <summary>
        /// FirmaCode + kullaniciId ile bağlanmak için overload.
        /// </summary>
        public SocketService(string socketConnectUrl, string firmaCode, string kullaniciId, Action<string, string> onMessage)
        {
            _socketUrl = NormalizeSocketUrl((socketConnectUrl ?? "").Trim());
            _firmaCode = (firmaCode ?? "").Trim();
            _kullaniciId = (kullaniciId ?? "").Trim();
            _onMessage = onMessage ?? ((_, __) => { });
            _client = null;
        }

        /// <summary>HttpClient ile Socket.IO bağlantısı kurar. Kopunca otomatik yeniden bağlanır (Disconnect çağrılana kadar).</summary>
        public void Connect()
        {
            if (string.IsNullOrEmpty(_socketUrl))
                return;

            _disconnectRequested = false;
            SafeLog("Sistem", "Socket bağlanıyor (HttpClient): " + _socketUrl);

            Task.Run(() => TryConnectOnceAsync());
        }

        private async Task TryConnectOnceAsync()
        {
            if (_disconnectRequested) return;
            try
            {
                var uri = new Uri(_socketUrl);
                if (!await CanReachServerAsync(uri).ConfigureAwait(false))
                {
                    SafeLog("Hata", "Sunucuya erişilemiyor: " + _socketUrl);
                    ScheduleReconnect();
                    return;
                }

                SafeLog("Sistem", "Socket.IO polling URL erişilebilir — bağlantı kuruluyor.");

                var client = new SocketIoHttpClient(_socketUrl, OnEvent, OnConnectionLost);
                await client.ConnectAsync().ConfigureAwait(false);

                if (_disconnectRequested) { client.Dispose(); return; }

                _client = client;
                SafeLog("Sistem", "Socket bağlantısı BAŞARILI!");
                if (!string.IsNullOrEmpty(_firmaCode))
                {
                    var kanal = new
                    {
                        FirmaCode = _firmaCode,
                        KullaniciID = _kullaniciId ?? string.Empty
                    };

                    await _client.EmitAsync("channelfixer", new object[] { kanal }).ConfigureAwait(false);
                    SafeLog("Sistem", "channelfixer emit edildi, FirmaCode: " + _firmaCode + " | KullaniciID: " + (_kullaniciId ?? ""));
                }
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? " | İç: " + ex.InnerException.Message : "";
                SafeLog("Hata", "Socket bağlantısı: " + ex.Message + inner);
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[Socket] Hata: " + ex.Message + inner);
#endif
                ScheduleReconnect();
            }
        }

        private void OnEvent(string eventName, string payload)
        {
            SafeLog(eventName, payload ?? "");
        }

        /// <summary>Pavo cihazından gelen cevabı sunucuya PavoRequestResult eventi ile iletir.</summary>
        /// <param name="room">Personel: P{KullaniciID}, Firma: C{FirmaCode} (SOCKET dokümantasyonu ile uyumlu).</param>
        public async Task EmitPavoResultAsync(string resultJson, PavoRequestResultRoom room = PavoRequestResultRoom.Personel)
        {
            if (_client == null || !IsConnected)
            {
                SafeLog("Uyarı", "Socket bağlı değil, Pavo sonucu sunucuya iletilemedi.");
                return;
            }

            try
            {
                string targetRoom;
                if (room == PavoRequestResultRoom.Firma && !string.IsNullOrEmpty(_firmaCode))
                    targetRoom = "C" + _firmaCode;
                else
                {
                    if (room == PavoRequestResultRoom.Firma)
                        SafeLog("Uyarı", "Pavo sonucu firma odasına istenmiş ama FirmaCode boş; personel odası kullanılıyor.");
                    targetRoom = "P" + (_kullaniciId ?? "");
                }
                object parsedResult = resultJson;
                try
                {
                    parsedResult = Newtonsoft.Json.Linq.JObject.Parse(resultJson);
                }
                catch { }

                var payload = new
                {
                    Room = targetRoom,
                    Result = parsedResult
                };

                // Yerel log için Newtonsoft kullanıyoruz, JavaScriptSerializer JObject'leri doğru serialize edemez.
                string logStr = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                await _client.EmitAsync("PavoRequestResult", new object[] { payload }).ConfigureAwait(false);
                SafeLog("Sistem", $"PavoRequestResult ({targetRoom}) emit edildi: {logStr}");
            }
            catch (Exception ex)
            {
                SafeLog("Hata", "EmitPavoResultAsync: " + ex.Message);
            }
        }

        /// <summary>Bağlantı koptuğunda çağrılır. IsConnected false olur; MainForm artık timer ile printer-requests atar. 5 sn sonra otomatik yeniden bağlanma denenir.</summary>
        private void OnConnectionLost()
        {
            var c = _client;
            _client = null;
            try { c?.Dispose(); } catch { }
            SafeLog("Sistem", "Socket bağlantısı koptu. " + ReconnectDelaySeconds + " saniye sonra yeniden denenecek.");
            ScheduleReconnect();
        }

        private void ScheduleReconnect()
        {
            if (_disconnectRequested) return;
            Task.Run(async () =>
            {
                await Task.Delay(ReconnectDelaySeconds * 1000).ConfigureAwait(false);
                if (_disconnectRequested) return;
                SafeLog("Sistem", "Yeniden bağlanılıyor...");
                await TryConnectOnceAsync().ConfigureAwait(false);
            });
        }

        private static string NormalizeSocketUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            return url.TrimEnd('/', ' ');
        }

        private static async Task<bool> CanReachServerAsync(Uri baseUri)
        {
            try
            {
                using (var handler = new HttpClientHandler { UseProxy = false })
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) })
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "SocketIOHttpClient/1 (QRMenue)");
                    var socketIoUri = new Uri(baseUri, "socket.io/?EIO=4&transport=polling");
                    var response = await client.GetAsync(socketIoUri).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode) return true;
                }
            }
            catch { }
            try
            {
                using (var handler = new HttpClientHandler { UseProxy = false })
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(6) })
                {
                    var response = await client.GetAsync(baseUri).ConfigureAwait(false);
                    return response.IsSuccessStatusCode;
                }
            }
            catch { }
            return false;
        }

        private void SafeLog(string eventName, string payload)
        {
            try
            {
                _onMessage?.Invoke(eventName, payload ?? "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[SocketService.SafeLog] " + eventName + ": " + (payload ?? "") + " | " + ex.Message);
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[Socket] " + eventName + ": " + (payload ?? ""));
#endif
        }

        public void Disconnect()
        {
            _disconnectRequested = true;
            try
            {
                _client?.Disconnect();
                _client?.Dispose();
            }
            catch { }
            _client = null;
        }
    }
}
