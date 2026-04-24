using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QRMENUE
{
    /// <summary>HttpClient tabanlı minimal Socket.IO (Engine.IO v4 polling) istemcisi. SocketIOClient 3.1.2 bağlanamadığı ortamlarda kullanılır.</summary>
    internal sealed class SocketIoHttpClient : IDisposable
    {
        private const char PacketSeparator = '\x1e';
        private const int MaxConsecutiveFailures = 3;
        private const int MaxConsecutiveTimeouts = 2;
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly Action<string, string> _onEvent;
        private readonly Action _onConnectionLost;
        private string _sid;
        private volatile bool _running;
        private CancellationTokenSource _pollCts;
        private int _consecutiveFailures;
        private int _consecutiveTimeouts;

        public bool Connected { get; private set; }

        public SocketIoHttpClient(string baseUrl, Action<string, string> onEvent, Action onConnectionLost = null)
        {
            _baseUrl = (baseUrl ?? "").TrimEnd('/', ' ');
            _onEvent = onEvent ?? ((_, __) => { });
            _onConnectionLost = onConnectionLost;
            _http = new HttpClient(new HttpClientHandler { UseProxy = false })
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "SocketIOHttpClient/1 (QRMenue)");
        }

        public async Task ConnectAsync()
        {
            var uri = new Uri(_baseUrl + "/socket.io/?EIO=4&transport=polling");
            var resp = await _http.GetAsync(uri).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(body) || body[0] != '0')
                throw new InvalidOperationException("Engine.IO handshake failed: " + (body.Length > 0 ? body.Substring(0, Math.Min(100, body.Length)) : "empty"));
            var sidMatch = Regex.Match(body, @"\""sid\""\s*:\s*\""([^\""]+)\""");
            if (!sidMatch.Success)
                throw new InvalidOperationException("Engine.IO handshake: sid not found");
            _sid = sidMatch.Groups[1].Value;

            var postUri = new Uri(_baseUrl + "/socket.io/?EIO=4&transport=polling&sid=" + Uri.EscapeDataString(_sid));
            var postContent = new StringContent("40", Encoding.UTF8, "text/plain");
            var postResp = await _http.PostAsync(postUri, postContent).ConfigureAwait(false);
            postResp.EnsureSuccessStatusCode();

            Connected = true;
            _running = true;
            _pollCts = new CancellationTokenSource();
            _ = PollLoopAsync(_pollCts.Token);
        }

        public async Task EmitAsync(string eventName, object[] args)
        {
            if (!Connected || string.IsNullOrEmpty(_sid)) return;
            var payload = new StringBuilder();
            payload.Append("42[\"").Append(eventName.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\"");
            if (args != null)
            {
                foreach (var a in args)
                {
                    payload.Append(",");
                    if (a == null) payload.Append("null");
                    else if (a is string s) payload.Append("\"").Append(s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n")).Append("\"");
                    else payload.Append(JsonConvert.SerializeObject(a));
                }
            }
            payload.Append("]");
            var uri = new Uri(_baseUrl + "/socket.io/?EIO=4&transport=polling&sid=" + Uri.EscapeDataString(_sid));
            var content = new StringContent(payload.ToString(), Encoding.UTF8, "text/plain");
            await _http.PostAsync(uri, content).ConfigureAwait(false);
        }

        private void NotifyConnectionLost()
        {
            _running = false;
            Connected = false;
            try { _onConnectionLost?.Invoke(); } catch { }
        }

        private async Task PollLoopAsync(CancellationToken ct)
        {
            while (_running && !ct.IsCancellationRequested)
            {
                try
                {
                    var uri = new Uri(_baseUrl + "/socket.io/?EIO=4&transport=polling&sid=" + Uri.EscapeDataString(_sid));
                    using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
                        var resp = await _http.GetAsync(uri, timeoutCts.Token).ConfigureAwait(false);
                        if (resp.StatusCode == HttpStatusCode.BadRequest || resp.StatusCode == HttpStatusCode.NotFound)
                        {
                            NotifyConnectionLost();
                            return;
                        }
                        resp.EnsureSuccessStatusCode();
                        _consecutiveFailures = 0;
                        _consecutiveTimeouts = 0;
                        var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var packets = body.Split(PacketSeparator);
                        var toPost = new List<string>();
                        foreach (var p in packets)
                        {
                            if (string.IsNullOrEmpty(p)) continue;
                            var type = p[0];
                            var data = p.Length > 1 ? p.Substring(1) : "";
                            if (type == '2') toPost.Add("3");
                            else if (type == '4' && data.Length > 0)
                            {
                                var soType = data[0];
                                var soPayload = data.Length > 1 ? data.Substring(1) : "";
                                if (soType == '0') { }
                                else if (soType == '2')
                                {
                                    try
                                    {
                                        var arr = JArray.Parse(soPayload);
                                        var evt = arr.Count > 0 ? arr[0].ToString() : "";
                                        var payloadStr = arr.Count > 1 ? arr[1].ToString() : "";
                                        if (!string.IsNullOrEmpty(evt)) _onEvent(evt, payloadStr ?? "");
                                    }
                                    catch { }
                                }
                            }
                        }
                        if (toPost.Count > 0)
                        {
                            var postUri = new Uri(_baseUrl + "/socket.io/?EIO=4&transport=polling&sid=" + Uri.EscapeDataString(_sid));
                            var postBody = string.Join(PacketSeparator.ToString(), toPost);
                            var postResp = await _http.PostAsync(postUri, new StringContent(postBody, Encoding.UTF8, "text/plain")).ConfigureAwait(false);
                            if (postResp.StatusCode == HttpStatusCode.BadRequest || postResp.StatusCode == HttpStatusCode.NotFound)
                            {
                                NotifyConnectionLost();
                                return;
                            }
                            postResp.EnsureSuccessStatusCode();
                            _consecutiveFailures = 0;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (ct.IsCancellationRequested) break;
                    if (++_consecutiveTimeouts >= MaxConsecutiveTimeouts)
                    {
                        NotifyConnectionLost();
                        return;
                    }
                    await Task.Delay(500, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!_running) break;
                    if (ex is OperationCanceledException || ex is TaskCanceledException || ex.InnerException is OperationCanceledException || ex.InnerException is TaskCanceledException)
                    {
                        await Task.Delay(500, ct).ConfigureAwait(false);
                        continue;
                    }
                    if (++_consecutiveFailures >= MaxConsecutiveFailures)
                    {
                        NotifyConnectionLost();
                        return;
                    }
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                }
            }
        }

        public void Disconnect()
        {
            _running = false;
            _pollCts?.Cancel();
            Connected = false;
            _sid = null;
        }

        public void Dispose()
        {
            Disconnect();
            _http?.Dispose();
        }
    }
}
