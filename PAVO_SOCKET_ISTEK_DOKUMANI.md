# Pavo POS – Socket ile Tam JSON İstek Dökümanı

Bu döküman, sunucunuzun Socket.IO üzerinden Kiosk'a göndereceği istek formatını açıklar.

---

## Önerilen: Tek Event ile URL + Tam JSON (PavoRequest)

**Sunucuda tüm bilgiler varsa (POS IP, SerialNumber, Fingerprint, TransactionSequence vb.)** en temiz yol: **tek bir event** ile **tam URL** ve **tam JSON body** göndermek. Kiosk sadece relay yapar, hiçbir ayar yönetmez.

| Avantaj | Açıklama |
|---------|----------|
| Tek event | `PavoRequest` – tüm endpoint'ler için aynı |
| Sıfır config | Kiosk'ta pavopos.json yok, her şey sunucudan |
| Tam kontrol | Web'de kullanıcıdan alınan IP, SerialNumber, vb. doğrudan kullanılır |
| Basit | Kiosk sadece POST isteği iletir |
| Çift Yönlü İletişim | Kiosk, Pavo'dan aldığı sonucu `PavoRequestResult` eventiyle sunucuya ("P{KullaniciID}" odasına) geri bildirir. |

### PavoRequest – Payload Formatı

**Event:** `PavoRequest`  
**Payload (JSON string):**

```json
{
  "url": "https://192.168.1.150:4567/InitiateSale",
  "body": {
    "TransactionHandle": {
      "SerialNumber": "PAV200016754",
      "TransactionDate": "2024-03-04T12:00:00.0000000",
      "TransactionSequence": 2,
      "Fingerprint": "test1"
    },
    "Sale": {
      "RefererApp": "Kiosk Uygulama",
      "RefererAppVersion": "1.0.0",
      "OrderNo": "SIP20240304001",
      "MainDocumentType": 1,
      "GrossPrice": 50.00,
      "TotalPrice": 50.00,
      "CurrencyCode": "TRY",
      "ExchangeRate": 1.0,
      "ShowCreditCardMenu": false,
      "SelectedSlots": ["rf", "icc", "manual"],
      "AllowDismissCardRead": false,
      "CardReadTimeout": 60,
      "SkipAmountCash": false,
      "CancelPaymentLater": true,
      "AddedSaleItems": [
        {
          "Name": "Ürün Adı",
          "IsGeneric": false,
          "UnitCode": "KGM",
          "TaxGroupCode": "KDV18",
          "ItemQuantity": 1,
          "UnitPriceAmount": 50.00,
          "GrossPriceAmount": 50.00,
          "TotalPriceAmount": 50.00
        }
      ],
      "PaymentInformations": [
        {
          "Mediator": 2,
          "Amount": 50.00,
          "CurrencyCode": "TRY",
          "ExchangeRate": 1.0
        }
      ],
      "ReceiptInformation": {
        "ReceiptImageEnabled": false,
        "ReceiptWidth": "58mm",
        "PrintCustomerReceipt": true,
        "PrintMerchantReceipt": true
      }
    }
  }
}
```

### Zorunlu Alanlar

| Alan | Açıklama |
|------|----------|
| `url` | Tam URL, örn: `https://{POS_IP}:4567/InitiateSale` |
| `body` | POST ile gönderilecek tam JSON objesi |

### Sunucu Örneği (Node.js)

```javascript
// InitiateSale
io.to(room).emit('PavoRequest', JSON.stringify({
  url: `https://${posIp}:4567/InitiateSale`,
  body: {
    TransactionHandle: {
      SerialNumber: serialNumber,
      TransactionDate: new Date().toISOString(),
      TransactionSequence: nextSequence(),
      Fingerprint: fingerprint
    },
    Sale: { /* ... tam Sale objesi ... */ }
  }
}));

// Pairing
io.to(room).emit('PavoRequest', JSON.stringify({
  url: `https://${posIp}:4567/Pairing`,
  body: { TransactionHandle: { SerialNumber, TransactionDate, TransactionSequence, Fingerprint } }
}));

// GetSaleResult
io.to(room).emit('PavoRequest', JSON.stringify({
  url: `https://${posIp}:4567/GetSaleResult`,
  body: { TransactionHandle: {...}, Sale: { OrderNo: "SIP001" } }
}));
```

### Kiosk Davranışı

1. `PavoRequest` event'ini alır.
2. Payload'u parse eder: `url` ve `body`.
3. `body`'yi JSON olarak serialize edip `url`'e POST gönderir.
4. Yanıtı loglar.

### Tüm Endpoint URL Örnekleri

| İşlem | URL |
|-------|-----|
| Pairing | `https://{POS_IP}:4567/Pairing` |
| InitiateSale | `https://{POS_IP}:4567/InitiateSale` |
| GetSaleResult | `https://{POS_IP}:4567/GetSaleResult` |
| PrintOut | `https://{POS_IP}:4567/PrintOut` |
| PavoPaymentMediators | `https://{POS_IP}:4567/PaymentMediators` |
| PavoCompleteSale | `https://{POS_IP}:4567/CompleteSale` |

---

## Kiosk'tan Sunucuya Yanıt (PavoRequestResult)

Sunucu `PavoRequest` eventiyle kioska işlem gönderdiğinde, kiosk ilgili Pavo POS cihazına REST API üzerinden ulaşır. 
İşlem tamamlandığında veya hata oluştuğunda kiosk, sunucuya **PavoRequestResult** eventi ile geri bildirim yapar.

**Hedef Oda:** `P{KullaniciID}` (Örn: KullaniciID si 20 olan bağlantı için `P20` odasına)
**Event Adı:** `PavoRequestResult`
**Payload Formatı (JSON Obj):**

```json
{
  "Room": "P20",
  "Result": "{\"HasError\":false,\"Message\":\"...\"}" // Pavo REST API'sinden dönen tüm JSON stringi
}
```

### Sunucu Tarafı Dinleme Örneği (Node.js)

Sunucu tarafında, önceden P20 odasına kayıtlı olan istemcilere (örneğin web ekranına) bu JSON iletilebilir:

```javascript
// İstemci tarafında (Örn: Web Frontend) P20 odasına katıldıysanız:
socket.on('PavoRequestResult', (data) => {
    console.log("Kiosk'tan gelen Pavo Yanıtı: ", data.Result);
    
    // Gelen JSON stringini parse edip kullanabilirsiniz
    const pavoResult = JSON.parse(data.Result);
    
    if(pavoResult.HasError) {
        alert("Pavo Cihazı Hatası: " + pavoResult.Message);
    } else {
        console.log("İşlem Başarılı. Pavo verisi: ", pavoResult);
    }
});
```

Eğer bağlantıda sorun olursa veya cihaz hata fırlatırsa C# uygulaması kendiliğinden şu şekilde bir hata yapısı gönderir:
`{"HasError":true,"Message":"...hata açıklaması..."}`

### C# Uygulaması – Ödeme Durumu Kontrolü

Web tarafı PAVO cihazına doğrudan erişemez (farklı ağ). Bu yüzden **C# uygulaması** ödeme sonucunu kontrol eder ve `PavoRequestResult` ile web'e iletir:

1. **InitiateSale** sonrası PAVO senkron cevap verebilir (kart hemen okutulursa).
2. **Async senaryo**: InitiateSale "bekleniyor" dönerse, C# **2 saniyede bir** `GetSaleResult` ile PAVO'ya sorgu atar.
3. Sonuç (başarı veya hata) geldiğinde `PavoRequestResult` emit edilir.
4. Web `PavoRequestResult` alınca `pavo_odeme_onayla` API'sini çağırır (GetSaleResult yapmaz, C# zaten doğruladı).

### Önerilen: Minimal Payload (Bant Genişliği Tasarrufu)

C# uygulaması tam PAVO cevabı yerine **sadece gerekli alanları** gönderebilir. Web tarafı her iki formatı da destekler:

```json
{
  "Room": "P2",
  "Result": {
    "HasError": false,
    "ErrorCode": null,
    "Message": null,
    "StatusId": 8,
    "OrderNo": "75"
  }
}
```

| Alan | Zorunlu | Açıklama |
|------|---------|----------|
| Room | Evet | Hedef oda (P{KullaniciID}) |
| Result.HasError | Evet | true/false |
| Result.ErrorCode | Hata durumunda | 130 = işlem devam ediyor |
| Result.Message | Hata durumunda | Hata mesajı |
| Result.StatusId | HasError:false ise | Sale Status (6=tamamlandı, 7=vazgeçildi, 8=DocumentFailed vb.) |
| Result.OrderNo | Opsiyonel | Sipariş referansı (debug) |

**StatusId 130 (işlem devam ediyor):** `ErrorCode: 130` ve `HasError: true` → Web sessizce beklemeye devam eder.

---

## Alternatif: Event Bazlı (PavoPairing, PavoInitiateSale, vb.)

Eski yapı: Her endpoint için ayrı event. Kiosk `pavopos.json` ile BaseUrl, SerialNumber, Fingerprint kullanır; TransactionSequence Kiosk'ta yönetilir.

| Event Adı | Endpoint | Açıklama |
|-----------|----------|----------|
| `PavoPairing` | POST /Pairing | Cihaz eşleştirme |
| `PavoInitiateSale` | POST /InitiateSale | Ödeme isteği (kart okutma) |
| `PavoGetSaleResult` | POST /GetSaleResult | Satış sonucu sorgulama |
| `PavoPrintOut` | POST /PrintOut | Fiş yazdırma |
| `PavoPaymentMediators` | POST /PaymentMediators | Ödeme yöntemleri listesi |
| `PavoCompleteSale` | POST /CompleteSale | Nakit satış |

**Payload:** Sadece JSON body (string). Kiosk URL'i `pavopos.json`'dan alır.

---

## 1. PavoPairing

**Event:** `PavoPairing`  
**Endpoint:** `POST https://{POS_IP}:4567/Pairing`

### Gönderilecek JSON (tam)

```json
{
  "TransactionHandle": {
    "SerialNumber": "PAV200016754",
    "TransactionDate": "2024-03-04T12:00:00.0000000",
    "TransactionSequence": 1,
    "Fingerprint": "test1"
  }
}
```

**Not:** `TransactionHandle` Kiosk tarafından `pavopos.json` ve yerel sayaç ile güncellenir. Minimal gönderim için boş obje de kabul edilebilir:

```json
{
  "TransactionHandle": {}
}
```

---

## 2. PavoInitiateSale

**Event:** `PavoInitiateSale`  
**Endpoint:** `POST https://{POS_IP}:4567/InitiateSale`

### Gönderilecek JSON (tam)

```json
{
  "TransactionHandle": {
    "SerialNumber": "PAV200016754",
    "TransactionDate": "2024-03-04T12:00:00.0000000",
    "TransactionSequence": 2,
    "Fingerprint": "test1"
  },
  "Sale": {
    "RefererApp": "Kiosk Uygulama",
    "RefererAppVersion": "1.0.0",
    "OrderNo": "SIP20240304001",
    "MainDocumentType": 1,
    "GrossPrice": 50.00,
    "TotalPrice": 50.00,
    "CurrencyCode": "TRY",
    "ExchangeRate": 1.0,
    "ShowCreditCardMenu": false,
    "SelectedSlots": ["rf", "icc", "manual"],
    "AllowDismissCardRead": false,
    "CardReadTimeout": 60,
    "SkipAmountCash": false,
    "CancelPaymentLater": true,
    "SendPhoneNotification": false,
    "SendEMailNotification": false,
    "AddedSaleItems": [
      {
        "Name": "Ürün Adı",
        "IsGeneric": false,
        "UnitCode": "KGM",
        "TaxGroupCode": "KDV18",
        "ItemQuantity": 1,
        "UnitPriceAmount": 50.00,
        "GrossPriceAmount": 50.00,
        "TotalPriceAmount": 50.00
      }
    ],
    "PaymentInformations": [
      {
        "Mediator": 2,
        "Amount": 50.00,
        "CurrencyCode": "TRY",
        "ExchangeRate": 1.0
      }
    ],
    "ReceiptInformation": {
      "ReceiptImageEnabled": false,
      "ReceiptWidth": "58mm",
      "PrintCustomerReceipt": true,
      "PrintMerchantReceipt": true
    }
  }
}
```

### Zorunlu Alanlar (Sale)

| Alan | Tip | Açıklama |
|------|-----|----------|
| OrderNo | string | Benzersiz sipariş no (GetSaleResult'ta kullanılır) |
| GrossPrice | decimal | Brüt tutar |
| TotalPrice | decimal | Toplam tutar |
| AddedSaleItems | array | Satış kalemleri |
| PaymentInformations | array | Mediator: 2 = Kart |

### Mediator Değerleri

| Değer | Anlamı |
|-------|--------|
| 1 | Nakit |
| 2 | Kredi/Banka Kartı |
| 9 | Harici ödeme |

---

## 3. PavoGetSaleResult

**Event:** `PavoGetSaleResult`  
**Endpoint:** `POST https://{POS_IP}:4567/GetSaleResult`

### Gönderilecek JSON (tam)

```json
{
  "TransactionHandle": {
    "SerialNumber": "PAV200016754",
    "TransactionDate": "2024-03-04T12:00:00.0000000",
    "TransactionSequence": 3,
    "Fingerprint": "test1"
  },
  "Sale": {
    "OrderNo": "SIP20240304001"
  }
}
```

**Not:** `OrderNo` mutlaka InitiateSale'da kullandığınız değerle aynı olmalıdır.

---

## 4. PavoPrintOut

**Event:** `PavoPrintOut`  
**Endpoint:** `POST https://{POS_IP}:4567/PrintOut`

### Gönderilecek JSON (tam)

```json
{
  "TransactionHandle": {
    "SerialNumber": "PAV200016754",
    "TransactionDate": "2024-03-04T12:00:00.0000000",
    "TransactionSequence": 4,
    "Fingerprint": "test1"
  },
  "Print": {
    "Image": "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
  }
}
```

**Not:** `Image` Base64 kodlanmış PNG/JPEG görsel. Boş string gönderilirse fiş yazdırılmaz, sadece test amaçlı çağrı yapılır.

---

## 5. PavoPaymentMediators

**Event:** `PavoPaymentMediators`  
**Endpoint:** `POST https://{POS_IP}:4567/PaymentMediators`

### Gönderilecek JSON (tam)

```json
{
  "TransactionHandle": {
    "SerialNumber": "PAV200016754",
    "TransactionDate": "2024-03-04T12:00:00.0000000",
    "TransactionSequence": 5,
    "Fingerprint": "test1"
  }
}
```

---

## 6. PavoCompleteSale (Nakit Satış)

**Event:** `PavoCompleteSale`  
**Endpoint:** `POST https://{POS_IP}:4567/CompleteSale`

### Gönderilecek JSON (tam – basit)

```json
{
  "TransactionHandle": {
    "SerialNumber": "PAV200016754",
    "TransactionDate": "2024-03-04T12:00:00.0000000",
    "TransactionSequence": 6,
    "Fingerprint": "test1"
  },
  "Sale": {
    "RefererApp": "Kiosk Uygulama",
    "RefererAppVersion": "1.0.0",
    "MainDocumentType": 1,
    "GrossPrice": 25.00,
    "TotalPrice": 25.00,
    "SendPhoneNotification": false,
    "SendEMailNotification": false,
    "AddedSaleItems": [
      {
        "Name": "Nakit Ürün",
        "IsGeneric": false,
        "UnitCode": "KGM",
        "TaxGroupCode": "KDV18",
        "ItemQuantity": 1,
        "UnitPriceAmount": 25.00,
        "GrossPriceAmount": 25.00,
        "TotalPriceAmount": 25.00
      }
    ],
    "PaymentInformations": [
      {
        "Mediator": 1,
        "Amount": 25.00
      }
    ]
  }
}
```

---

## Sunucu Tarafı Örnek (Node.js)

```javascript
// PavoPairing tetikleme
io.to(room).emit('PavoPairing', JSON.stringify({
  TransactionHandle: {
    SerialNumber: "PAV200016754",
    TransactionDate: new Date().toISOString(),
    TransactionSequence: 1,
    Fingerprint: "test1"
  }
}));

// PavoInitiateSale tetikleme
io.to(room).emit('PavoInitiateSale', JSON.stringify({
  TransactionHandle: { SerialNumber: "...", TransactionDate: "...", TransactionSequence: 2, Fingerprint: "..." },
  Sale: {
    RefererApp: "Kiosk",
    RefererAppVersion: "1.0.0",
    OrderNo: "SIP" + Date.now(),
    MainDocumentType: 1,
    GrossPrice: 100,
    TotalPrice: 100,
    CurrencyCode: "TRY",
    ExchangeRate: 1,
    ShowCreditCardMenu: false,
    SelectedSlots: ["rf", "icc", "manual"],
    AllowDismissCardRead: false,
    CardReadTimeout: 60,
    SkipAmountCash: false,
    CancelPaymentLater: true,
    AddedSaleItems: [
      { Name: "Ürün", IsGeneric: false, UnitCode: "KGM", TaxGroupCode: "KDV18", ItemQuantity: 1, UnitPriceAmount: 100, GrossPriceAmount: 100, TotalPriceAmount: 100 }
    ],
    PaymentInformations: [{ Mediator: 2, Amount: 100, CurrencyCode: "TRY", ExchangeRate: 1 }],
    ReceiptInformation: { ReceiptImageEnabled: false, ReceiptWidth: "58mm", PrintCustomerReceipt: true, PrintMerchantReceipt: true }
  }
}));

// PavoGetSaleResult tetikleme
io.to(room).emit('PavoGetSaleResult', JSON.stringify({
  TransactionHandle: { SerialNumber: "...", TransactionDate: "...", TransactionSequence: 3, Fingerprint: "..." },
  Sale: { OrderNo: "SIP20240304001" }
}));
```

---

## Kiosk Davranışı

1. Socket'ten event + payload alır.
2. Payload'u JSON olarak parse eder.
3. `TransactionHandle` alanını `pavopos.json` ve yerel sayaç ile günceller.
4. İlgili endpoint'e POST isteği gönderir.
5. Yanıtı loglar (PavoPosForm açıksa forma, değilse arka planda).

---

## TransactionHandle – Kiosk Üzerine Yazımı

Sunucu `TransactionHandle` gönderse bile Kiosk şu alanları **mutlaka** günceller:

| Alan | Kaynak |
|------|--------|
| SerialNumber | pavopos.json |
| Fingerprint | pavopos.json |
| TransactionDate | DateTime.Now (ISO 8601) |
| TransactionSequence | Yerel artan sayaç |

Bu nedenle sunucu `TransactionHandle`'ı boş `{}` veya placeholder değerlerle gönderebilir.

---

## Özet Tablo

| Event | Zorunlu Alanlar (body içinde) |
|-------|-------------------------------|
| PavoPairing | TransactionHandle (boş olabilir) |
| PavoInitiateSale | Sale.OrderNo, Sale.TotalPrice, Sale.AddedSaleItems, Sale.PaymentInformations |
| PavoGetSaleResult | Sale.OrderNo |
| PavoPrintOut | Print.Image (Base64, boş olabilir) |
| PavoPaymentMediators | TransactionHandle (boş olabilir) |
| PavoCompleteSale | Sale.GrossPrice, Sale.TotalPrice, Sale.AddedSaleItems, Sale.PaymentInformations |

---

*Bu döküman PAVO_REST_API_ENTEGRASYON_DOKUMANI.md ve rest-demo-postman-collection.json.json referans alınarak hazırlanmıştır.*
