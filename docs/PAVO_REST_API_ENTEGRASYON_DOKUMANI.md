# Pavo POS REST API ve Socket Entegrasyon Dokümantasyonu

Bu doküman, QRMENUE projenizdeki Pavo/OverPay POS cihazı entegrasyonunun teknik yapısını, çalışma prensiplerini ve mimarisini detaylı olarak açıklamaktadır.

---

## 📋 İçindekiler

1. [Mimari Özet](#1-mimari-özet)
2. [Teknik Altyapı ve İletişim Protokolü](#2-teknik-altyapı-ve-iletişim-protokolü)
3. [Çekirdek İstemci: PavoApiClient](#3-çekirdek-i̇stemci-pavoapiclient)
4. [Socket Köprüsü: PavoPosSocketBridge](#4-socket-köprüsü-pavopossocketbridge)
5. [Otomatik Takip (Polling) Mekanizması](#5-otomatik-takip-polling-mekanizması)
6. [Fiş İşlemleri (Resim ve JSON Loglama)](#6-fiş-i̇şlemleri-resim-ve-json-loglama)
7. [Desteklenen Socket Olayları (Eventler)](#7-desteklenen-socket-olayları-eventler)
8. [Hata Yönetimi ve Optimizasyon](#8-hata-yönetimi-ve-optimizasyon)

---

## 1. Mimari Özet

Projedeki Pavo POS entegrasyonu, **NodeJS/Frontend üzerinden gelen Socket isteklerini yakalayarak Pavo POS cihazına REST API üzerinden ileten ve sonuçları tekrar Socket'e döndüren çift katmanlı bir yapıya sahiptir.**

1. **`PavoApiClient.cs`**: Cihazla doğrudan ve düşük seviyeli HTTP(S) iletişimini sağlayan asıl REST istemcisi.
2. **`PavoPosSocketBridge.cs`**: Frontend/Socket'ten gelen komutları alıp `PavoApiClient`'a ileten, asenkron işlemleri, otomatik durum sorgulamalarını (polling) ve loglamaları yöneten köprü katmanı.

---

## 2. Teknik Altyapı ve İletişim Protokolü

Cihaz ile iletişim tamamen **REST API** mantığı ile gerçekleşmektedir. 

- **Protokol:** HTTP veya HTTPS. Projenizde varsayılan olarak `BypassSsl = true` ayarı kullanılarak HTTPS sertifika doğrulama hataları (self-signed sertifikalar vb.) es geçilmektedir (`ServerCertificateCustomValidationCallback = true`).
- **Veri Formatı:** İstekler ve cevaplar **JSON** formatındadır (`application/json`).
- **İstemci Sınıfı:** .NET `HttpClient` sınıfı kullanılmaktadır. İşlemlerin uzun sürebileceği ihtimaline karşı (kredi kartı şifre girme vb.) **Timeout süresi 90 saniye** olarak yapılandırılmıştır.
- **Asenkron İletişim:** Tüm Socket istekleri ve API çağrıları UI thread'ini (veya ana thread'i) dondurmamak için `Task.Run` ve `async/await` ile **arka planda (background thread)** çalıştırılır.

---

## 3. Çekirdek İstemci: PavoApiClient

`PavoApiClient`, REST API ile konuşan doğrudan sınıftır.

### Temel Özellikleri
- **TransactionHandle Yönetimi:** Pavo API'si her istekte bir `TransactionHandle` bekler. Bu nesne cihazın seri numarasını, `TransactionDate` ve eşsiz, sürekli artan bir `TransactionSequence` değerini içerir. Sınıf içerisinde `_globalTransactionSequence` thread-safe (`lock`) bir şekilde artırılarak yönetilir.
- **Retry (Yeniden Deneme) Mekanizması:** Ağ hataları (`HttpRequestException` veya `TaskCanceledException`) durumunda otomatik olarak maksimum **3 kez** (3 saniye aralıklarla) tekrar deneme yapar.
- **Doğrudan Payload Desteği (`PostRawAsync`):** Frontend eğer Pavo'nun beklediği tam JSON yapısını (`{ Sale: {...} }`) oluşturup gönderirse, istemci bu JSON'u parse edip üzerine sadece `TransactionHandle` bilgisini enjekte ederek cihaza gönderir.

---

## 4. Socket Köprüsü: PavoPosSocketBridge

Socket üzerinden gelen istekleri (Örn: "PavoInitiateSale") karşılar, gerekli validasyonları yapar ve `PavoApiClient` üzerinden asıl isteği atar.

### Esnek İstek Yapısı (Tam JSON vs. Basit JSON)
Bridge yapısı oldukça esnektir. Eğer socket'ten gelen payload:
- **Tam JSON ise:** (Örn: `{"Sale": { ... }}`) doğrudan API'ye paslanır.
- **Basit JSON ise:** (Örn: `{"orderNo": "123", "totalPrice": 20}`) bridge sınıfı otomatik olarak Pavo'nun anlayacağı `Sale` ve `AddedSaleItems` nesnelerini oluşturarak işlemi gerçekleştirir.

---

## 5. Otomatik Takip (Polling) Mekanizması

Özellikle Kiosk gibi self-servis senaryolarında müşteri kart okuturken veya iptal işlemleri uzun sürdüğünde uygulanan çok kritik bir asenkron polling (durum sorgulama) mekanizması vardır.

**Nasıl Çalışır?**
1. Eğer socket'ten gelen istek bir `/InitiateSale`, `/CompleteSale` veya `/CancelSale` işlemiyse, ana HTTP isteği gönderilirken **paralel bir arka plan görevi başlatılır** (`PollGetSaleResultAsync`).
2. Bu görev cihaza **1.5 saniye** avans verdikten sonra çalışmaya başlar.
3. Her **3 saniyede bir**, maksimum **60 kez** (yaklaşık 3 dakika) cihazdaki ilgili işlemin sonucunu sorar (`GetSaleResult` veya `GetCancellationResult`).
4. **Durum Kodları (StatusId / ErrorCode):** 
   - `ErrorCode: 130`: İşlem cihazda devam ediyor demektir (müşteri şifre giriyor vb.), polling devam eder.
   - `ErrorCode: 73`: Sıra numarası çakışması hatasıdır, otomatik olarak yeni bir sequence üretilip tekrar denenir.
   - `StatusId`: 1, 2, 3, 4, 9, 12, 15, 17, 19, 22 durumlarında işlem henüz bitmemiştir, polling devam eder. Diğer durumlarda işlem bitmiş sayılır ve polling durdurulur.

Bu sayede ön yüz, uzun süren bir işlemi sormak zorunda kalmaz. İşlem bittiğinde sonuç Socket'e (örneğin Personel veya Firma odasına) anında düşer.

---

## 6. Fiş İşlemleri (Resim ve JSON Loglama)

Pavo cihazı işlem sonrasında fişi JSON yapısı ve/veya Base64 formatında PNG resmi olarak döner. Bridge üzerindeki `TryExtractAndSaveReceiptImage` fonksiyonu bu süreci otomatik yönetir:

- Cihazdan dönen sonucun içerisinden `CustomerReceiptImage` ve `CustomerReceiptJson` verileri çekilir.
- Resim Base64'ten çözülerek PNG dosyası olarak diske kaydedilir (`ReceiptImage_OrderNo.png`).
- Fiş JSON verisi hem orjinal haliyle (`..._Original.json`) hem de QRMENUE'nün anladığı formata (`RecieptInfo` nesnelerine) çevrilerek (`..._OrderNo.json`) diske kaydedilir.
- **Disk Optimizasyonu:** Dizinlerin gereksiz şişmesini önlemek için, her iki dosya türünden de dizinde sadece en yeni **10 dosya** tutulur, eski olanlar otomatik silinir.

---

## 7. Desteklenen Socket Olayları (Eventler)

`PavoPosSocketBridge` içerisinde frontend tarafından tetiklenebilen başlıca olaylar şunlardır:

| Olay Adı | Beklenen Payload | Açıklama |
|----------|------------------|----------|
| `TriggerPairing` | `Boş` veya `JSON` | Cihaz ile bilgisayarın eşleşme (pairing) işlemini başlatır. |
| `TriggerPaymentMediators`| `Boş` veya `JSON` | Cihazdaki ödeme yöntemlerini (Nakit, Kart vs.) getirir. |
| `TriggerInitiateSale` | `{orderNo, totalPrice}` veya Tam JSON | Kiosk satışı başlatır. (Müşterinin kart okutması beklenir). Polling başlatır. |
| `TriggerCompleteSale` | Tam JSON (Sale objeli) | Kasiyer satışını (zaten alınmış ödemeyi) tamamlar. Polling başlatır. |
| `TriggerGetSaleResult`| `{orderNo}` veya Tam JSON | Belirli bir sipariş numarasının (OrderNo) son durumunu cihazdan sorgular. |
| `TriggerPrintOut` | `{image: "base64"}` veya Tam JSON | Cihaza base64 formatında bir resim göndererek fiş/belge yazdırır. |
| `TriggerPavoRequest` | `{url: "...", body: {...}}` | Genel İstek. URL ve Body verilerek cihaza doğrudan her türlü işlemi (CancelSale vb.) yaptırmayı sağlar. İşlem Initiate/Complete/Cancel ise polling otomatik başlar. |

---

## 8. Hata Yönetimi ve Optimizasyon

- **JSON Optimizasyonu (Minimization):** Pavo cihazı çok uzun JSON verileri dönebilmektedir (Özellikle base64 fiş resimleri içeren `GetSaleResult` cevapları). Socket'in veya NodeJS'in tıkanmaması için, işlem bittiğinde `MinimizePavoResult` fonksiyonu çağrılır. Bu fonksiyon cevabın içindeki gereksiz detayları (ve büyük verileri) atarak sadece `{HasError, ErrorCode, Message, StatusId, OrderNo}` bilgilerini Frontend'e iletir.
- **Bağlantı Kopması (`ErrorCode: 999`):** Cihaz kapalıysa, aynı ağda değilse veya `HttpClient` isteği başarısız olursa yakalanan exception `ErrorCode: 999` ve `DeviceOffline: true` şeklinde standartlaştırılarak Frontend'e haber verilir.
- **Socket Odaları (Rooms):** Özellikle İptal (`CancelSale`) işlemlerinin sonucu Firma (Yönetici) yetkisini ilgilendirdiği için sonuçlar **Firma** odasına, standart satış işlemleri ise **Personel** odasına Socket üzerinden yönlendirilir. 

---
*Bu doküman, projede fiilen çalışan kod yapısı (PavoPosSocketBridge.cs ve PavoApiClient.cs) baz alınarak otomatik olarak oluşturulmuştur.*
