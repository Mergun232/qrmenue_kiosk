# QRMenu API exe Dokümantasyonu

## Genel Bilgiler

API exe, QRMenu sisteminin Windows Kiosk ve PC exesi için hazırlanan versiyonudur. LoginToken tabanlı kimlik doğrulama sistemi kullanılmaktadır.

**Base URL:** `{SITE_URL}/{lang}/api/exe`
*(Örnek: `http://localhost:8007/tr/api/exe`)*

**Desteklenen Diller:**
- `tr` (Türkçe)
- `en` (İngilizce)
- `de` (Almanca)
- `fr` (Fransızca)
- `ru` (Rusça)

## Kimlik Doğrulama (Authentication)

API exe'te kimlik doğrulama LoginToken ile yapılır. Login endpoint'i hariç tüm endpoint'ler için LoginToken gereklidir.

### LoginToken Kullanımı

Login endpoint'inden dönen `LoginToken` değeri, diğer tüm isteklerde HTTP Header olarak gönderilmelidir:

```
Header: LoginToken: {token_değeri}
```

Alternatif olarak şu formatlar da kabul edilir:
- `logintoken` (küçük harf)
- `HTTP_LOGINTOKEN` (server variable)

### Authentication Gerektirmeyen Endpoint'ler

- `login` - Giriş yapmak için kullanılır
- `printer-content` - Çıktı token'ı ile çalışır, LoginToken gerektirmez

## Endpoint'ler
  - login: `POST /api/exe/login`
  - printer-requests: `GET /api/exe/printer-requests`
  - printer-content: `GET /api/exe/printer-content/{token}`
  - exe-state: `POST /api/exe/exe-state`
  - exe-log: `POST /api/exe/exe-log`



### 1. Login

Personel girişi yapar ve LoginToken döndürür.

**Endpoint:** `POST /{lang}/api/exe/login`

**Authentication:** Gerektirmez

**Request Body:**
```json
{
  "company_code": "FIRMA123",
  "nick_name": "kullanici_adi",
  "password": "sifre",
  "remember_me": 1,
  "platform": 5
}
```

**Parametreler:**
- `company_code` (required): Firma kodu
- `nick_name` (required): Kullanıcı adı
- `password` (required): Şifre (MD5 hash'e çevrilir)
- `remember_me` (optional): Beni hatırla (default: 1)
- `platform` (optional): Platform bilgisi (kiosk için 5, PC Exe için 4)

**Başarılı Response (200 OK):**
```json
{
  "status": 200,
  "result": "success",
  "DataList": {
    "ApiUrl": "http://localhost:8007/tr/api/exe/",
    "Lang": "tr",
    "FirmaName": "Firma Adı",
    "FirmaID": 123,
    "FirmaCode": "CFIRMA123",
    "LoginToken": "abc123def456...",
    "WebviewLink": "http://localhost:8007/tr/c/firma-adi-CFIRMA123/kiosk?login_token=abc123...",
    "SocketConnectUrl": "http://localhost:8000",
    "KullaniciID": 123,
    "Browser": true,
    "Printer": true,
    "CallerID": false,
    "Personel": {
      "NameSurname": "Ahmet Yılmaz",
      "KullaniciNick": "kullanici_adi",
      "Durum": 1,
      "KayitTarihi": "2024-01-15 10:30:00",
      "SonGiris": "2024-12-10 14:25:00"
    }
  }
}
```

**Hata Response:**
```json
{
  "status": 404,
  "result": "error",
  "btn": "Tamam",
  "title": "Firma bulunamadi",
  "text1": "Firma bulunamadi",
  "text2": "FIRMA123"
}
```

**Örnek CURL:**
```bash
curl -X POST http://localhost:8007/tr/api/exe/login \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "company_code=FIRMA123&nick_name=kullanici&password=sifre&remember_me=1&platform=4"
```

---

### 2. Printer Requests

Yazdırılacak çıktıların listesini döndürür.

**Endpoint:** `GET /{lang}/api/exe/printer-requests`

**Authentication:** Gereklidir (LoginToken header)

**Başarılı Response (200 OK):**
```json
{
  "status": 200,
  "result": "success",
  "Url": [
    "http://localhost:8007/tr/api/exe/printer-content/token123",
    "http://localhost:8007/tr/api/exe/printer-content/token456"
  ]
}
```

**Hata Response:**
```json
{
  "status": 404,
  "result": "failure"
}
```

**Notlar:**
- Personel ayarlarına (PSetting) göre farklı filtreleme yapılıp URL dizisi dönderilir:
  - `all`: Tüm çıktılar
  - `dissiparis`: Dış siparişler
  - `table`: Masa siparişleri
  - `dissiparis_takeaway`: Dış siparişler ve takeaway
  - `table_takeaway`: Masa siparişleri ve takeaway

**Örnek CURL:**
```bash
curl -X GET "http://localhost:8007/tr/api/exe/printer-requests" \
  -H "LoginToken: abc123def456..."
```

**Exe tarafı (Kiosk):**
- Bu endpoint, exe arka planda periyodik olarak çağrılır. Aralık **app_data.json** içindeki `PrinterRequestIntervalSeconds` ile ayarlanır (varsayılan **5** saniye). Örnek: `"PrinterRequestIntervalSeconds": 5`
- Socket üzerinden **PrintRequest** olayı geldiğinde exe bu endpoint'i **anında** bir kez daha tetikler; böylece yazdırma gecikmesi azalır.

---

### 3. Printer Content

Tek bir çıktı token'ı ile yazdırma JSON verilerini döndürür. Bu veri genelde yazıcı cihazları tarafından doğrudan işlenir.

**Endpoint:** `GET /{lang}/api/exe/printer-content/{token}`

**Authentication:** Gerektirmez (çıktı token'ı ile çalışır)

**URL Parametreleri:**
- `{token}` (required): Çıktı token'ı (printer-requests endpoint'inden URL içerisinde alınır)

**Başarılı Response (İşlenmemiş JSON Metni):**
```json
{
  "RecieptDto": [
    {
      "WhichPrinter": "Mutfak",
      "PrinterData": "...",
      ...
    }
  ]
}
```

**Hata Response:**
```json
{
  "status": 404,
  "result": "error_token"
}
```

**Notlar:**
- Bu endpoint çıktı token'ı ile çalışır.
- Token kullanıldıktan sonra yazdırılmış sayılarak silinir (tek kullanımlık).

**Örnek CURL:**
```bash
curl -X GET "http://localhost:8007/tr/api/exe/printer-content/token123"
```

---

### 4. Exe State Güncelleme

Kiosk veya PC Exe cihazının anlık durumunu, versiyon bilgisini vb. sisteme göndermek için kullanılır. İstek atıldığında veriler sunucuda saklanır. Eğer POST parametresi içerisinde `exe_token` gönderilirse, personelin `TokenExe` alanına kaydedilir.

**Endpoint:** `POST /{lang}/api/exe/exe-state` VEYA `POST /{lang}/api/exe/exe-sate`

**Authentication:** Gereklidir (LoginToken header)

**Request Body (Örnek):**
```json
{
  "state": "online",
  "cpu": "15%",
  "ram": "2GB",
  "printer": "ready",
  "exe_token": "abcde_push_token_or_unique_id"
}
```

**Başarılı Response (200 OK):**
```json
{
  "status": 200,
  "result": "success",
  "message": "State updated"
}
```

---

### 5. Exe Log Gönderme

Uygulamanın çalışırken karşılaştığı hataları (error) veya bilgi/uyarı mesajlarını (info/warning) sisteme iletmesi için kullanılır.

**Endpoint:** `POST /{lang}/api/exe/exe-log`

**Authentication:** Gereklidir (LoginToken header)

**Request Body:**
```json
{
  "type": "error",
  "log": "Printer connection failed on port USB001"
}
```

**Parametreler:**
- `log` (optional): Hata veya log mesajı metni
- `type` (optional): Log tipi (örneğin: `error`, `info`, `warning`) (Varsayılan: `info`)
Not: Dilerseniz doğrudan istediğiniz herhangi bir JSON formatında parametre gönderebilirsiniz, payload olduğu gibi kayıt edilecektir.

**Başarılı Response (200 OK):**
```json
{
  "status": 200,
  "result": "success",
  "message": "Log saved"
}
```

---



## Hata Kodları

### HTTP Status Codes
*API standart olarak JSON response body içerisinde kendi `status` kodlarını da döndürür:*
- `200`: İşlem başarılı
- `400`: Geçersiz istek / Kötü istek / Eksik Token
- `401`: Kimlik doğrulama hatası (LoginToken geçersiz veya bulunamadı)
- `403`: Yetkilendirme hatası (Personel hesabı pasif)
- `404`: Kayıt bulunamadı (Firma, personel, çıktı işlemi bulunamadı)

## Örnek Kullanım Senaryosu

### 1. Kullanıcı Girişi
```bash
# Login yap
POST /tr/api/exe/login
Body: {
  "company_code": "FIRMA123",
  "nick_name": "kullanici",
  "password": "sifre",
  "remember_me": 1,
  "platform": 4
}

# Response'dan LoginToken'ı al
LoginToken: "abc123def456..."
```

### 2. Yazdırma İstekleri
```bash
# Yazdırılacak çıktıların listesini al
GET /tr/api/exe/printer-requests
Headers: LoginToken: abc123def456...

# Gelen URL Listesi: ["http://localhost:8007/tr/api/exe/printer-content/token123"]
```

### 3. Çıktı Verisi
```bash
# Çıktı token'ı ile içeriği al ve yazdır (URL'e GET isteği atılır)
GET /tr/api/exe/printer-content/token123
```

## Önemli Notlar

1. **LoginToken Güvenliği:**
   - HTTPS kullanılmalıdır
   - Token'lar tek kullanımlık değildir, logout yapılmadığı sürece geçerlidir

2. **Şifre Hash:**
   - Login endpoint'inde gönderilen şifre MD5 hash'e çevrilir
   - Veritabanında MD5 hash saklanmalıdır

3. **Firma Kodu Formatı:**
   - Eğer firma kodu "C" ile başlamıyorsa ve "CFirmaCode" formatında bir firma varsa, otomatik olarak "C" eklenir
   - Örnek: "FIRMA123" → "CFIRMA123" (eğer bu şekilde varsa)

4. **Platform Kodları:**
   - `1`: Android
   - `2`: iOS
   - `4`: PC EXE
   - `5`: Kiosk
   - `9`: Diğer/Bilinmeyen (default)

5. **Yazıcı Tipleri:**
   - `Mutfak`: Mutfak yazıcısı
   - `Adisyon`: Adisyon yazıcısı

## Destek

Sorularınız için:
- Dokümantasyon: Bu dokümantasyon dosyası
- API Versiyonu: exe

---

**Son Güncelleme:** 2026
**API Versiyonu:** EXE (v1)
