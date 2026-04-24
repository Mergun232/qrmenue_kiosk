# Pavo POS REST API Entegrasyon Dokümantasyonu

Bu doküman, Pavo/OverPay POS cihazları ile REST API üzerinden iletişim kurmak için C# projenize entegrasyon yapmanızda rehber niteliğindedir.

---

## 📋 İçindekiler

1. [Proje Özeti](#1-proje-özeti)
2. [Ön Hazırlık ve Gereksinimler](#2-ön-hazırlık-ve-gereksinimler)
3. [İzlemeniz Gereken Yol Haritası](#3-izlemeniz-gereken-yol-haritası)
4. [Temel Kavramlar](#4-temel-kavramlar)
5. [API Endpoint'leri](#5-api-endpointleri)
6. [Modeller ve Veri Yapıları](#6-modeller-ve-veri-yapıları)
7. [Kendi Projenize Entegrasyon](#7-kendi-projenize-entegrasyon)
8. [Örnek Kod Yapısı](#8-örnek-kod-yapısı)
9. [Test ve Hata Ayıklama](#9-test-ve-hata-ayıklama)
10. [Kiosk için Ödeme Alma İşlemi (InitiateSale)](#10-kiosk-için-ödeme-alma-işlemi-initiatesale)
11. [Kendi C# Projenize Ekstra Ne Gerekir?](#11-kendi-c-projenize-ekstra-ne-gerekir)

---

## 1. Proje Özeti

**OverPay.Samples.RestIntegration** projesi:
- **Platform:** .NET 8.0 Console uygulaması
- **Amaç:** Pavo POS cihazına REST API ile bağlanıp satış, eşleştirme (pairing) ve ödeme mediator bilgilerini almak
- **Bağlantı:** HTTPS üzerinden JSON tabanlı REST API
- **Port:** Varsayılan `4567`

### Proje Yapısı

```
OverPay.Samples.RestIntegration/
├── Program.cs              # Ana uygulama ve API çağrıları
├── RestClientApp.csproj    # Proje dosyası
├── rest-demo-postman-collection.json.json  # Postman test koleksiyonu
└── RestClientApp.sln      # Solution dosyası
```

---

## 2. Ön Hazırlık ve Gereksinimler

### Yazılım Gereksinimleri

| Gereksinim | Açıklama |
|------------|----------|
| .NET 8.0 SDK | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) adresinden indirin |
| Visual Studio 2022 veya VS Code | C# geliştirme için |
| Pavo POS Cihazı | Aynı ağda (LAN) erişilebilir olmalı |
| Postman (Opsiyonel) | API testleri için |

### NuGet Paketleri

```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="System.Net.Http" Version="4.3.4" />
```

> **Not:** .NET 8 projelerinde `System.Net.Http` genellikle framework ile gelir; sadece `Newtonsoft.Json` eklemeniz yeterli olabilir.

### Ağ Gereksinimleri

- POS cihazı ve bilgisayarınız **aynı yerel ağda** olmalı
- POS cihazının IP adresi ve port bilgisi (örn: `https://192.168.85.242:4567`)
- Geliştirme ortamında SSL sertifika doğrulaması genellikle devre dışı bırakılır (sadece test için)

---

## 3. İzlemeniz Gereken Yol Haritası

### Başlangıç Seviyesi İçin Adım Adım Plan

```
┌─────────────────────────────────────────────────────────────────┐
│  ADIM 1: Temel C# ve REST API Bilgisi                           │
│  • HttpClient kullanımı                                           │
│  • JSON serialization (Newtonsoft.Json)                          │
│  • async/await kavramı                                           │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  ADIM 2: Örnek Projeyi Çalıştırma                                │
│  • Projeyi Visual Studio'da açın                                 │
│  • Base URL'i kendi POS IP'nize göre değiştirin                   │
│  • dotnet run ile çalıştırın                                     │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  ADIM 3: Postman ile API Testi                                   │
│  • Postman collection'ı import edin                               │
│  • url değişkenini POS IP'nize ayarlayın                          │
│  • Pairing, CompleteSale, PaymentMediators deneyin                │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  ADIM 4: Kendi Projenize Entegrasyon                             │
│  • Model sınıfları oluşturun (DTO)                               │
│  • PavoApiClient servisi yazın                                   │
│  • appsettings.json ile yapılandırma                              │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  ADIM 5: İleri Seviye (Opsiyonel)                                │
│  • Dependency Injection kullanımı                                │
│  • Retry politikaları                                            │
│  • Logging ekleme                                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. Temel Kavramlar

### TransactionHandle (İşlem Tanımlayıcı)

**Her API isteğinde** gönderilmesi zorunlu olan nesne. İşlemi benzersiz şekilde tanımlar.

| Alan | Tip | Açıklama |
|------|-----|----------|
| SerialNumber | string | POS cihazının seri numarası (örn: "N500Z000036") |
| TransactionDate | string | ISO 8601 formatında tarih (örn: "2024-04-01T15:20:30.3107733") |
| TransactionSequence | int | Her işlem için artan sıra numarası (benzersiz olmalı) |
| Fingerprint | string | Uygulama/cihaz parmak izi (örn: "test1") |

**Önemli:** `TransactionSequence` her yeni işlemde **artmalıdır**. Aynı değer tekrar kullanılırsa hata alabilirsiniz.

### Base URL Formatı

```
https://<POS_IP_ADRESI>:4567
```

Örnek: `https://192.168.85.242:4567`

### İstek Formatı

- **Method:** POST
- **Content-Type:** application/json
- **Body:** JSON formatında istek gövdesi

---

## 5. API Endpoint'leri

### Örnek Projede Kullanılan Endpoint'ler

| # | Endpoint | Açıklama |
|---|----------|----------|
| 1 | POST /Pairing | Cihaz eşleştirme (ilk bağlantıda) |
| 2 | POST /CompleteSale | Satışı tamamla (nakit – ödeme zaten alındı) |
| 3 | POST /InitiateSale | Kiosk satış başlat (müşteri kart okutur) |
| 4 | POST /PaymentMediators | Ödeme yöntemlerini listele (nakit, kredi kartı vb.) |

### Postman Koleksiyonundaki Diğer Endpoint'ler

| Kategori | Endpoint | Açıklama |
|----------|----------|----------|
| **Satış** | InitiateSale | Satış başlat (kart okutma bekler) |
| | JewellerySale | Kuyumcu satışı |
| | AdvanceSale | Avans satışı |
| | CurrentSale | Cari satış |
| | RestaurantSale | Restoran adisyon satışı |
| | StartMultiPaymentSale | Çoklu ödeme satışı başlat |
| | AddPayment | Ödeme ekle |
| | CancelSale | Satış iptali |
| | GetSaleResult | Satış sonucunu al |
| **Auth** | Login | Kullanıcı girişi |
| | LoginWithPin | PIN ile giriş |
| | Logout | Çıkış |
| **Operasyon** | PerformEOD | Gün sonu işlemi |
| | SendOfflineSales | Offline satışları gönder |
| **Cihaz** | GetDeviceInfo | Cihaz bilgisi |
| | PaymentMediators | Ödeme mediator listesi |
| | PrintOut | Yazdırma |

---

## 6. Modeller ve Veri Yapıları

### TransactionHandle

```csharp
public class TransactionHandle
{
    public string SerialNumber { get; set; } = "";
    public string TransactionDate { get; set; } = "";
    public int TransactionSequence { get; set; }
    public string Fingerprint { get; set; } = "";
}
```

### CompleteSale İçin Sale Nesnesi (Basit)

```csharp
public class SaleItem
{
    public string Name { get; set; } = "";
    public bool IsGeneric { get; set; }
    public string UnitCode { get; set; } = "KGM";      // Kilogram
    public string TaxGroupCode { get; set; } = "KDV18"; // KDV oranı
    public decimal ItemQuantity { get; set; }
    public decimal UnitPriceAmount { get; set; }
    public decimal GrossPriceAmount { get; set; }
    public decimal TotalPriceAmount { get; set; }
}

public class PaymentInformation
{
    public int Mediator { get; set; }   // 1=Nakit, 2=Kredi Kartı, vb.
    public decimal Amount { get; set; }
}

public class Sale
{
    public string RefererApp { get; set; } = "Harici Uygulama";
    public string RefererAppVersion { get; set; } = "1.0.0";
    public int MainDocumentType { get; set; } = 1;  // 1=Satış fişi
    public decimal GrossPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool SendPhoneNotification { get; set; }
    public bool SendEMailNotification { get; set; }
    public List<SaleItem> AddedSaleItems { get; set; } = new();
    public List<PaymentInformation> PaymentInformations { get; set; } = new();
}
```

### Mediator Değerleri (Ödeme Türleri)

| Değer | Anlamı |
|-------|--------|
| 1 | Nakit |
| 2 | Kredi/Banka Kartı |
| 9 | Harici ödeme (dışarıda ödenmiş) |

> **Not:** Tam liste için `PaymentMediators` endpoint'ini çağırıp cihazdaki tanımlı mediator'ları alabilirsiniz.

---

## 7. Kendi Projenize Entegrasyon

### 7.1 Yeni Bir C# Projesi Oluşturma

```bash
# Konsol uygulaması
dotnet new console -n MyPosIntegration -f net8.0

# Web API projesi (ASP.NET Core)
dotnet new webapi -n MyPosIntegration -f net8.0
```

### 7.2 NuGet Paketi Ekleme

```bash
cd MyPosIntegration
dotnet add package Newtonsoft.Json
```

### 7.3 Klasör Yapısı Önerisi

```
MyPosIntegration/
├── Models/
│   ├── TransactionHandle.cs
│   ├── Sale.cs
│   ├── SaleItem.cs
│   └── PaymentInformation.cs
├── Services/
│   └── PavoApiClient.cs
├── appsettings.json
└── Program.cs
```

### 7.4 appsettings.json ile Yapılandırma

```json
{
  "PavoPos": {
    "BaseUrl": "https://192.168.85.242:4567",
    "SerialNumber": "N500Z000036",
    "Fingerprint": "test1",
    "BypassSslValidation": true
  }
}
```

> **Uyarı:** `BypassSslValidation: true` sadece geliştirme ortamında kullanın. Production'da `false` yapın ve geçerli sertifika kullanın.

---

## 8. Örnek Kod Yapısı

### PavoApiClient Servisi

```csharp
using System.Net;
using System.Text;
using Newtonsoft.Json;

public class PavoApiClient
{
    private readonly string _baseUrl;
    private readonly string _serialNumber;
    private readonly string _fingerprint;
    private int _transactionSequence = 0;
    private readonly bool _bypassSsl;

    public PavoApiClient(string baseUrl, string serialNumber, string fingerprint, bool bypassSsl = false)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _serialNumber = serialNumber;
        _fingerprint = fingerprint;
        _bypassSsl = bypassSsl;
    }

    private object CreateTransactionHandle()
    {
        _transactionSequence++;
        return new
        {
            SerialNumber = _serialNumber,
            TransactionDate = DateTime.Now.ToString("o"),
            TransactionSequence = _transactionSequence,
            Fingerprint = _fingerprint
        };
    }

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler();
        if (_bypassSsl)
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        return new HttpClient(handler);
    }

    public async Task<string> PairingAsync()
    {
        var payload = new { TransactionHandle = CreateTransactionHandle() };
        return await PostAsync("/Pairing", payload);
    }

    public async Task<string> CompleteSaleAsync(decimal totalPrice, List<SaleItem> items, List<PaymentInformation> payments)
    {
        var payload = new
        {
            TransactionHandle = CreateTransactionHandle(),
            Sale = new
            {
                RefererApp = "Harici Uygulama",
                RefererAppVersion = "1.0.0",
                MainDocumentType = 1,
                GrossPrice = totalPrice,
                TotalPrice = totalPrice,
                SendPhoneNotification = false,
                SendEMailNotification = false,
                AddedSaleItems = items,
                PaymentInformations = payments
            }
        };
        return await PostAsync("/CompleteSale", payload);
    }

    public async Task<string> GetPaymentMediatorsAsync()
    {
        var payload = new { TransactionHandle = CreateTransactionHandle() };
        return await PostAsync("/PaymentMediators", payload);
    }

    // Kiosk için: Müşteri kart okutacak
    public async Task<string> InitiateSaleAsync(string orderNo, decimal totalPrice, List<SaleItem> items)
    {
        var payload = new
        {
            TransactionHandle = CreateTransactionHandle(),
            Sale = new
            {
                RefererApp = "Kiosk Uygulama",
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
                AddedSaleItems = items,
                PaymentInformations = new[] { new { Mediator = 2, Amount = totalPrice, CurrencyCode = "TRY", ExchangeRate = 1m } },
                ReceiptInformation = new { ReceiptImageEnabled = false, ReceiptWidth = "58mm", PrintCustomerReceipt = true, PrintMerchantReceipt = true }
            }
        };
        return await PostAsync("/InitiateSale", payload);
    }

    // InitiateSale sonucunu sorgula
    public async Task<string> GetSaleResultAsync(string orderNo)
    {
        var payload = new { TransactionHandle = CreateTransactionHandle(), Sale = new { OrderNo = orderNo } };
        return await PostAsync("/GetSaleResult", payload);
    }

    private async Task<string> PostAsync(string path, object payload)
    {
        var url = _baseUrl + path;
        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = CreateHttpClient();
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Kullanım Örneği

```csharp
var client = new PavoApiClient(
    baseUrl: "https://192.168.85.242:4567",
    serialNumber: "N500Z000036",
    fingerprint: "test1",
    bypassSsl: true
);

// 1. Eşleştirme
var pairingResult = await client.PairingAsync();
Console.WriteLine(pairingResult);

// 2. Satış
var items = new List<SaleItem>
{
    new() { Name = "Gofret", IsGeneric = false, UnitCode = "KGM", TaxGroupCode = "KDV18",
            ItemQuantity = 1, UnitPriceAmount = 20, GrossPriceAmount = 20, TotalPriceAmount = 20 }
};
var payments = new List<PaymentInformation> { new() { Mediator = 1, Amount = 20 } };
var saleResult = await client.CompleteSaleAsync(20, items, payments);
Console.WriteLine(saleResult);

// 3. Ödeme yöntemleri
var mediators = await client.GetPaymentMediatorsAsync();
Console.WriteLine(mediators);

// 4. Kiosk - InitiateSale (müşteri kart okutacak)
var orderNo = $"KIOK{DateTime.Now:yyyyMMddHHmmss}";
var items = new List<SaleItem> { new() { Name = "Ürün", IsGeneric = false, UnitCode = "KGM", TaxGroupCode = "KDV18", ItemQuantity = 1, UnitPriceAmount = 20, GrossPriceAmount = 20, TotalPriceAmount = 20 } };
await client.InitiateSaleAsync(orderNo, 20, items);
// Sonucu almak için periyodik GetSaleResult çağrısı:
var result = await client.GetSaleResultAsync(orderNo);
```

---

## 9. Test ve Hata Ayıklama

### Postman ile Test

1. `rest-demo-postman-collection.json.json` dosyasını Postman'a import edin
2. Collection Variables'da `url` değerini POS IP'nize ayarlayın: `https://192.168.1.XXX:4567`
3. Sırayla **Pairing** → **PaymentMediators** → **CompleteSale Simple** isteklerini deneyin

### Yaygın Hatalar

| Hata | Olası Sebep | Çözüm |
|------|--------------|-------|
| Connection refused | POS kapalı veya farklı ağda | IP ve portu kontrol edin |
| SSL/TLS hatası | Self-signed sertifika | Geliştirmede bypass kullanın |
| 400 Bad Request | Geçersiz JSON veya eksik alan | TransactionHandle ve Sale alanlarını kontrol edin |
| TransactionSequence hatası | Aynı sıra numarası tekrar kullanıldı | Her istekte artan değer kullanın |

### TransactionSequence Yönetimi

Örnek projede her istekte sabit değer kullanılıyor. **Gerçek uygulamada** şunlardan birini yapın:

1. **Bellekte sayaç:** Uygulama açık kaldığı sürece artan
2. **Veritabanı:** Kalıcı sıra numarası
3. **Zaman tabanlı:** `DateTime.Now.Ticks` veya benzeri (çakışma riski var)

---

## 10. Kiosk için Ödeme Alma İşlemi (InitiateSale)

Kiosk cihazlarında müşteri kartını okutması gerektiğinde **InitiateSale** kullanılır. CompleteSale'dan farkı: ödeme anında alınmaz, POS ekranında tutar gösterilir ve müşteri kart okutur.

### Akış Diyagramı

```
┌─────────────┐     InitiateSale      ┌─────────────┐     Kart okutma     ┌─────────────┐
│  Sizin App  │ ──────────────────►  │  POS Cihazı  │ ◄────────────────  │  Müşteri    │
│  (Kiosk)    │                      │  (Tutar      │                    │  (Kart      │
│             │                      │   gösterir)  │                    │   okutur)   │
└─────────────┘                      └─────────────┘                    └─────────────┘
       │                                      │
       │         GetSaleResult (OrderNo)       │
       │ ◄────────────────────────────────────│
       │         (Sonuç: Başarılı/İptal)      │
       ▼
  Sonucu işle
```

### InitiateSale vs CompleteSale

| Özellik | InitiateSale | CompleteSale |
|---------|--------------|--------------|
| **Kullanım** | Kiosk – müşteri kart okutur | Kasiyer – ödeme zaten alındı |
| **Mediator** | 2 (Kart) | 1 (Nakit) veya 2 (Kart) |
| **Davranış** | Asenkron – POS bekler | Senkron – anında tamamlanır |
| **Sonuç** | GetSaleResult ile alınır | Yanıtta hemen döner |

### InitiateSale İstek Örneği

```csharp
// OrderNo benzersiz olmalı - sonuç sorgulamasında kullanılacak
var orderNo = $"KIOK{DateTime.Now:yyyyMMddHHmmss}";

var payload = new
{
    TransactionHandle = new
    {
        SerialNumber = "PAV200016754",
        TransactionDate = DateTime.Now.ToString("o"),
        TransactionSequence = ++sequence,
        Fingerprint = "test1"
    },
    Sale = new
    {
        RefererApp = "Kiosk Uygulama",
        RefererAppVersion = "1.0.0",
        OrderNo = orderNo,
        MainDocumentType = 1,
        GrossPrice = 20m,
        TotalPrice = 20m,
        CurrencyCode = "TRY",
        ExchangeRate = 1m,
        ShowCreditCardMenu = false,
        SelectedSlots = new[] { "rf", "icc", "manual" },  // rf=temassız, icc=chip, manual=manuel
        AllowDismissCardRead = false,
        CardReadTimeout = 60,
        SkipAmountCash = false,
        CancelPaymentLater = true,
        AddedSaleItems = new[] { /* ürünler */ },
        PaymentInformations = new[]
        {
            new { Mediator = 2, Amount = 20m, CurrencyCode = "TRY", ExchangeRate = 1m }
        },
        ReceiptInformation = new
        {
            ReceiptImageEnabled = false,
            ReceiptWidth = "58mm",
            PrintCustomerReceipt = true,
            PrintMerchantReceipt = true
        }
    }
};
```

### Önemli Alanlar (Kiosk)

| Alan | Değer | Açıklama |
|------|-------|----------|
| OrderNo | Benzersiz string | GetSaleResult'ta sonuç sorgulamak için kullanılır |
| Mediator | 2 | Kredi/banka kartı (müşteri okutacak) |
| SelectedSlots | ["rf","icc","manual"] | rf=temassız, icc=chip, manual=manuel giriş |
| CardReadTimeout | 60 | Kart okutma bekleme süresi (saniye) |
| SkipAmountCash | false | Nakit seçeneği gösterilsin mi |

### Sonuç Sorgulama (GetSaleResult)

InitiateSale gönderdikten sonra ödeme sonucunu almak için:

```csharp
var payload = new
{
    TransactionHandle = CreateTransactionHandle(),
    Sale = new { OrderNo = orderNo }  // InitiateSale'da kullandığınız OrderNo
};
// POST /GetSaleResult
```

**Önerilen akış:** InitiateSale sonrası 2–3 saniye aralıklarla GetSaleResult çağırın; müşteri kart okutana kadar "beklemede" dönecektir.

---

## 11. Kendi C# Projenize Ekstra Ne Gerekir?

Bu örnek projeden **referans almanız gerekmez**. Kendi projenize sadece şunları eklemeniz yeterli:

### Gerekli Olanlar

| Gereksinim | Açıklama |
|------------|----------|
| **Newtonsoft.Json** | NuGet paketi – `dotnet add package Newtonsoft.Json` |
| **HttpClient** | .NET ile gelir, ek paket gerekmez |
| **async/await** | C# dil özelliği |

### Bu Projeden Kopyalamanız Gerekenler

| Ne | Nereden |
|----|---------|
| API çağrı mantığı | `Program.cs` içindeki `PostAsync`, `CreateTransactionHandle` benzeri yapı |
| InitiateSale payload yapısı | `CallInitiateSaleAsync` metodu |
| SSL bypass (sadece geliştirme) | `HttpClientHandler.ServerCertificateCustomValidationCallback` |

### Bu Projeden Almamanız Gerekenler

- Proje referansı (Project Reference) – **gerekmez**
- DLL kopyalama – **gerekmez**
- Tüm solution'ı dahil etme – **gerekmez**

### Özet: Kendi Projenize Ekleme Adımları

1. **NuGet:** `Newtonsoft.Json` paketini ekleyin
2. **Kod:** Dokümandaki `PavoApiClient` benzeri bir servis sınıfı yazın veya `Program.cs`'deki mantığı kopyalayın
3. **Yapılandırma:** BaseUrl, SerialNumber, Fingerprint değerlerini `appsettings.json` veya sabitlerde tutun
4. **InitiateSale:** Kiosk için yukarıdaki InitiateSale örneğini kullanın
5. **GetSaleResult:** Sonucu almak için OrderNo ile periyodik sorgulama yapın

### Minimal Örnek (Kendi Projenizde)

```csharp
// Sadece bu 3 şey yeterli:
// 1. Newtonsoft.Json paketi
// 2. HttpClient + JSON serialize
// 3. Doğru payload yapısı

using System.Text;
using Newtonsoft.Json;

var payload = new { TransactionHandle = new { ... }, Sale = new { ... } };
var json = JsonConvert.SerializeObject(payload);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await httpClient.PostAsync("https://192.168.1.165:4567/InitiateSale", content);
var result = await response.Content.ReadAsStringAsync();
```

---

## 📚 Ek Kaynaklar

- **REST API Temelleri:** [MDN - REST](https://developer.mozilla.org/en-US/docs/Glossary/REST)
- **C# HttpClient:** [Microsoft Docs](https://learn.microsoft.com/tr-tr/dotnet/fundamentals/networking/http/httpclient)
- **Newtonsoft.Json:** [Json.NET Documentation](https://www.newtonsoft.com/json/documentation)

---

## ✅ Kontrol Listesi

Entegrasyona başlamadan önce:

- [ ] .NET 8 SDK yüklü
- [ ] POS cihazı aynı ağda ve erişilebilir
- [ ] POS IP adresi ve port bilgisi
- [ ] SerialNumber ve Fingerprint değerleri (cihazdan veya dokümantasyondan)
- [ ] Postman ile en az Pairing isteğinin başarılı olduğu doğrulandı

Kiosk entegrasyonu için:

- [ ] InitiateSale ile satış başlatıldı, POS'ta tutar görünüyor
- [ ] Müşteri kart okuttuktan sonra GetSaleResult ile sonuç alınabiliyor

---

*Bu doküman OverPay.Samples.RestIntegration örnek projesi temel alınarak hazırlanmıştır. Resmi API dokümantasyonu için OverPay/Pavo yetkilileriyle iletişime geçin.*
