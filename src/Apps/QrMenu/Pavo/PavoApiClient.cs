using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QRMENUE.Pavo
{
    /// <summary>Pavo/OverPay POS cihazına REST API ile bağlanıp işlem yapan istemci.</summary>
    public class PavoApiClient
    {
        private static int _globalTransactionSequence = Math.Max(100, Environment.TickCount % 10000);
        private static readonly object _seqLock = new object();
        private static readonly HttpClient _httpClient;

        static PavoApiClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true
            };
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(90) };
        }

        private readonly string _baseUrl;
        private readonly string _serialNumber;
        private readonly string _fingerprint;
        private readonly bool _bypassSsl;

        public PavoApiClient(string baseUrl, string serialNumber, string fingerprint, bool bypassSsl = true)
        {
            _baseUrl = (baseUrl ?? "").TrimEnd('/');
            _serialNumber = serialNumber ?? "";
            _fingerprint = fingerprint ?? "test1";
            _bypassSsl = bypassSsl;
        }

        private object CreateTransactionHandle()
        {
            lock (_seqLock)
            {
                _globalTransactionSequence++;
                return new
                {
                    SerialNumber = _serialNumber,
                    TransactionDate = DateTime.Now.ToString("o"),
                    TransactionSequence = _globalTransactionSequence,
                    Fingerprint = _fingerprint
                };
            }
        }



        /// <summary>Pairing - Cihaz eşleştirme (ilk bağlantıda).</summary>
        public async Task<string> PairingAsync()
        {
            var payload = new { TransactionHandle = CreateTransactionHandle() };
            return await PostAsync("/Pairing", payload);
        }

        /// <summary>PaymentMediators - Ödeme yöntemlerini listele.</summary>
        public async Task<string> GetPaymentMediatorsAsync()
        {
            var payload = new { TransactionHandle = CreateTransactionHandle() };
            return await PostAsync("/PaymentMediators", payload);
        }

        /// <summary>InitiateSale - Kiosk satış başlat (müşteri kart okutur).</summary>
        public async Task<string> InitiateSaleAsync(string orderNo, decimal totalPrice, List<SaleItem> items)
        {
            var payload = new
            {
                TransactionHandle = CreateTransactionHandle(),
                Sale = new
                {
                    RefererApp = "Kiosk Test Uygulama",
                    RefererAppVersion = "1.0.0",
                    OrderNo = orderNo,
                    MainDocumentType = 1,
                    GrossPrice = totalPrice,
                    TotalPrice = totalPrice,
                    CurrencyCode = "TRY",
                    ExchangeRate = 1m,
                    ShowCreditCardMenu = false,
                    SelectedSlots = new[] { "rf", "icc", "manual" },
                    AllowDismissCardRead = false,
                    CardReadTimeout = 60,
                    SkipAmountCash = false,
                    CancelPaymentLater = true,
                    AddedSaleItems = items ?? new List<SaleItem>(),
                    PaymentInformations = new[] { new { Mediator = 2, Amount = totalPrice, CurrencyCode = "TRY", ExchangeRate = 1m } },
                    ReceiptInformation = new { ReceiptImageEnabled = false, ReceiptWidth = "58mm", PrintCustomerReceipt = true, PrintMerchantReceipt = true }
                }
            };
            return await PostAsync("/InitiateSale", payload);
        }

        /// <summary>GetSaleResult - InitiateSale sonucunu sorgula.</summary>
        public async Task<string> GetSaleResultAsync(string orderNo)
        {
            var payload = new { TransactionHandle = CreateTransactionHandle(), Sale = new { OrderNo = orderNo } };
            return await PostAsync("/GetSaleResult", payload);
        }

        /// <summary>CompleteSale - Nakit satış (ödeme zaten alındı).</summary>
        public async Task<string> CompleteSaleAsync(decimal totalPrice, List<SaleItem> items, List<PaymentInformation> payments)
        {
            var payload = new
            {
                TransactionHandle = CreateTransactionHandle(),
                Sale = new
                {
                    RefererApp = "Kiosk Test Uygulama",
                    RefererAppVersion = "1.0.0",
                    OrderNo = "PAVO" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    MainDocumentType = 1,
                    GrossPrice = totalPrice,
                    TotalPrice = totalPrice,
                    SendPhoneNotification = false,
                    SendEMailNotification = false,
                    AddedSaleItems = items ?? new List<SaleItem>(),
                    PaymentInformations = payments ?? new List<PaymentInformation>()
                }
            };
            return await PostAsync("/CompleteSale", payload);
        }

        /// <summary>PrintOut - Fiş yazdır (Base64 görsel).</summary>
        public async Task<string> PrintOutAsync(string base64Image)
        {
            var payload = new
            {
                TransactionHandle = CreateTransactionHandle(),
                Print = new { Image = base64Image ?? "" }
            };
            return await PostAsync("/PrintOut", payload);
        }

        /// <summary>GetDeviceInfo - Cihaz bilgisi.</summary>
        public async Task<string> GetDeviceInfoAsync()
        {
            var payload = new
            {
                TransactionHandle = CreateTransactionHandle(),
                DeviceInfo = new { AdditionalInfo = new { serialNumber = true, fingerPrint = true } }
            };
            return await PostAsync("/GetDeviceInfo", payload);
        }

        /// <summary>Socket'ten gelen tam JSON'u TransactionHandle ile birleştirip endpoint'e gönderir.</summary>
        public async Task<string> PostRawAsync(string path, string bodyJson)
        {
            var handle = CreateTransactionHandle();
            object payload;
            try
            {
                var jo = Newtonsoft.Json.Linq.JObject.Parse(bodyJson ?? "{}");
                jo["TransactionHandle"] = Newtonsoft.Json.Linq.JObject.FromObject(handle);
                payload = jo;
            }
            catch
            {
                payload = new { TransactionHandle = handle };
            }
            return await PostAsync(path, payload);
        }

        private async Task<string> PostAsync(string path, object payload)
        {
            var url = _baseUrl + path;
            var json = JsonConvert.SerializeObject(payload);

            const int maxRetries = 3;
            Exception lastEx = null;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    try
                    {
                        var uri = new Uri(url);
                        var servicePoint = System.Net.ServicePointManager.FindServicePoint(uri);
                        if (servicePoint != null)
                        {
                            servicePoint.ConnectionLeaseTimeout = 5000; // 5 saniye
                        }
                    }
                    catch { }

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);
                    return await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
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

                    lastEx = ex;
                    if (attempt < maxRetries &&
                        (ex is HttpRequestException || ex is TaskCanceledException || ex.InnerException is HttpRequestException))
                    {
                        await Task.Delay(3000);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            throw lastEx ?? new InvalidOperationException("Beklenmeyen durum");
        }
    }
}
