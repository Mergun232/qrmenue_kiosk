# QR Menu Kiosk – Proje Yapısı ve Dokümantasyon

Bu belge, QR Menu Kiosk uygulamasının klasör yapısını, bileşenlerini ve çalışma akışını açıklar.

---

## 1. Çözüm (Solution) Yapısı

```
kiosk/
├── QrMenu.sln                 # Visual Studio çözüm dosyası
├── PROJE_YAPISI.md            # Bu doküman
├── app_data.json              # (Opsiyonel) Kök kopya; asıl kullanılan src/Apps/QrMenu/app_data.json
├── packages/                  # NuGet paketleri (CefSharp, RestSharp, vb.)
└── src/
    ├── Apps/                  # Uygulama projeleri
    │   └── QrMenu/            # Ana kiosk uygulaması
    └── Layers/                # Katman kütüphaneleri
        └── PrintOptions/       # Yazdırma servisi katmanı
```

**Projeler:**
- **QrMenu** – Ana Windows Forms uygulaması (giriş, ana form, tarayıcı, yazdırma tetikleyicisi).
- **PrintOptions** – Yazdırma tasarımı ve fiş/fatura çıktısı için kütüphane (QrMenu buna referans verir).

**Apps / Layers ayrımı (problem yok):**  
Solution Explorer’da **QrMenu** ile **PrintOptions** farklı yerlerde görünür: QrMenu `Apps` altında, PrintOptions `Layers` altındadır. Bu tasarım gereği böyledir; bir hata değildir. **Apps** = çalıştırılabilir uygulamalar (exe), **Layers** = paylaşılan kütüphaneler (dll). QrMenu, yazdırma işini PrintOptions projesine referans vererek kullanır; ESC/POS yazıcı çıktısı bu katmanda üretilir. **İleride çıktı işlemlerini özelleştirmek** (fiş/fatura layout’u, yazıcı komutları, yeni çıktı türleri vb.) için değişiklik yapılacak yer **src/Layers/PrintOptions** projesidir; QrMenu tarafında sadece bu kütüphaneyi çağıran kod (ör. Business/PrintService.cs) vardır.

---

## 2. QrMenu Projesi (src/Apps/QrMenu)

### 2.1 Klasör Yapısı

```
QrMenu/
├── Program.cs                 # Uygulama giriş noktası
├── app_data.json              # Metinler ve API base URL (çıktıya kopyalanır)
├── App.config
├── packages.config
├── Business/                  # İş mantığı
│   ├── Helpers.cs             # HTTP istekleri, AppDataDTO, AppDataLoader
│   ├── PrintService.cs        # Yazdırma servisi
│   └── PrinterOperations.cs   # Yazıcı işlemleri
├── DTO/                       # Veri transfer nesneleri
│   ├── ConfigDTO.cs           # config.json (kullanıcı/şifre/firma kodu)
│   ├── LoginResultDTO.cs      # Giriş API yanıtı
│   ├── PrintRequestDTO.cs     # Yazdırma isteği API yanıtı
│   ├── InformStateDTO.cs      # Durum bilgisi API yanıtı
│   ├── FicheDTO.cs
│   └── ...
├── Forms/                     # Ekranlar
│   ├── LoginForm.cs           # Giriş ekranı
│   ├── LoginForm.Designer.cs
│   ├── MainForm.cs            # Arka plan formu (tray, timer’lar)
│   ├── MainForm.designer.cs
│   ├── Chromium.cs            # CefSharp tam ekran web görünümü
│   ├── Chromium.Designer.cs
│   └── ...
└── Properties/
    ├── Resources.resx
    ├── Settings.settings
    └── app.manifest
```

### 2.2 Ana Bileşenler

| Dosya / Sınıf | Açıklama |
|---------------|----------|
| **Program.cs** | Tek instance kontrolü, CefSharp başlatma, `Application.Run(LoginForm)`. |
| **Helpers.cs** | `HttpHelper` (GET/POST), `AppDataDTO`, `AppDataLoader` (app_data.json okuma, API base URL). |
| **LoginForm** | Firma kodu, kullanıcı adı, şifre; API ile giriş; başarıda MainForm + isteğe bağlı Chromium açılır. |
| **MainForm** | Pencere gizli; system tray; timer ile yazdırma isteği ve durum API’leri çağrılır; hata durumunda Chromium’a login sayfası yüklenir. |
| **Chromium** | CefSharp ile tam ekran web sayfası; URL `LoginResultDTO.Url` ile gelir. |

### 2.3 Yapılandırma Dosyaları

- **app_data.json** (proje kökünde, çıktıya kopyalanır)  
  - `ApiLink`: API base URL (örn. `https://qrmenue.com/tr/`).  
  - `Text1`–`Text25`: Arayüz metinleri (etiketler, mesajlar, başlıklar).  
  - Uygulama tüm API URL’lerini `AppDataLoader.GetApiUrl("api/v1/...")` ile bu base üzerinden üretir.

- **config.json** (çalışma klasöründe, uygulama tarafından yazılır)  
  - "Beni hatırla" ile kaydedilen: `Username`, `Password`, `CompanyCode`.  
  - LoginForm açılışta bu dosyadan alanları doldurur.

---

## 3. Uygulama Akışı

```
1. Program.Main()
   ├── Tek instance mi? → Hayır devam, evet ise mesaj göster ve çık.
   ├── Cef.Initialize() (tek sefer)
   └── Application.Run(new LoginForm())

2. LoginForm
   ├── Load: app_data.json’dan metinler (ApplyAppDataTexts), config.json’dan kullanıcı bilgisi.
   ├── Giriş Yap: API (AppDataLoader.GetApiUrl("api/v1/staff-login-kiosk")) → LoginResultDTO.
   ├── Başarılı ise:
   │   ├── MainForm(firmaId, loginResult) oluştur, göster (form gizli, tray’de).
   │   └── loginResult.Browser && loginResult.Url dolu ise Chromium(loginResult.Url) aç, tam ekran.
   └── Hata ise MessageBox.

3. MainForm (arka planda)
   ├── Timer: printer-requests API (aralık app_data.json → PrinterRequestIntervalSeconds, varsayılan 5 sn) → yeni fiş varsa PrintOptions ile yazdır; Socket PrintRequest olayında da anında tetiklenir.
   ├── Timer: exe/state API → Restart/Close/ErrorLog/interval güncelleme.
   └── Hata sonrası toparlanma: "Chromium" formunu bul → Login() + ManuelFocus() (login sayfasına git).

4. Chromium
   ├── CefSharp ile loginResult.Url tam ekran gösterilir.
   ├── Kapanınca Cef.Shutdown() + Application.Exit().
   └── MainForm’dan Login() / ChangeUrl() ile URL değiştirilebilir.
```

---

## 4. API Kullanımı

Uygulama **api/exe** API’sini kullanır (detay: **API_EXE_DOCUMENTATION.md**). Base URL **app_data.json** → **ApiLink**; tüm isteklerde giriş sonrası **LoginToken** header kullanılır (login hariç).

### 4.1 Kullanılan Endpoint'ler (api/exe)

| Endpoint | Metot | Dosya | Açıklama |
|----------|--------|------|----------|
| `api/exe/login` | POST | **LoginForm.cs** | Giriş; body: company_code, nick_name, password (MD5), remember_me, platform=5. Yanıt: LoginToken, WebviewLink, Browser, Printer vb. |
| `api/exe/printer-requests` | GET | **MainForm.cs** → `CheckPrintables()` | Header: LoginToken. Bekleyen yazdırma URL listesi (PrintRequestDTO). |
| `api/exe/printer-content/{token}` | GET | **MainForm.cs** → `CheckPrintables()` | printer-requests’ten gelen her URL; fiş JSON (Root/RecieptDto), PrintOptions ile yazdırılır. |
| `api/exe/exe-state` | POST | **MainForm.cs** → `InformSystem()` | Header: LoginToken. Body: state, printer. Cihaz durumu sunucuya iletilir. |
| `api/exe/exe-log` | POST | **MainForm.cs** → `SendExeLog()` | Header: LoginToken. Body: type, log. Hata/bilgi logu gönderimi. |

Özet: **LoginToken** login yanıtından alınır; printer-requests, exe-state, exe-log bu token ile çağrılır. Base URL **AppDataLoader.GetApiUrl("api/exe/...")** ile üretilir.

### 4.2 Yapılandırma Gerekli mi?

Şu an API kullanımı **iki yerde** dağınık: **LoginForm.cs** (1 çağrı) ve **MainForm.cs** (4 farklı çağrı). Endpoint path’leri bu formların içinde string olarak yazılı; tek bir listede toplanmıyor.

- **Zorunlu değil:** Proje küçük, endpoint sayısı az; mevcut haliyle çalışıyor ve dokümantasyon (bu tablo) hangi endpoint’in nerede olduğunu gösteriyor.
- **İstersen toplayabilirsin:** Tüm API işlemlerini tek bir servis altında toplamak (ör. **Business/ApiService.cs**) şunları sağlar:
  - Hangi endpoint’lerin kullanıldığı tek dosyada görünür.
  - Yeni endpoint veya base URL değişikliği tek yerden yönetilir.
  - LoginForm ve MainForm sadece `ApiService.Login(...)`, `ApiService.GetPrinterRequests(...)` gibi metodları çağırır.

Öneri: Önce bu dokümantasyonla “nerede ne var” netleşsin. İleride endpoint sayısı artarsa veya API tarafında sık değişiklik olursa **ApiService** benzeri bir katman eklemek mantıklı olur.

---

## 5. PrintOptions Katmanı (src/Layers/PrintOptions)

- **Amaç:** Fiş/fatura içeriğini alıp yazıcıya (ESC/POS vb.) göndermek.
- **PrinterDesignService** – Fiş yerleşimi ve çizim; asıl çıktı burada üretilir.
- **FontInfoService** – Font bilgisi.
- **Dto (Root, Printer, vb.)** – API’den gelen yazdırma verisi.
- **RecieptInfoTypeEnum** – Fiş satır türleri.

QrMenu, yazdırma isteği API’den gelen URL’leri çağırır, gelen veriyi PrintOptions DTO’larına deserialize eder ve **PrinterDesignService.PrintSlip** ile yazdırır.

**İleride çıktı özelleştirmesi:** Fiş/fatura tasarımı, yazıcı komutları, kağıt genişliği, yeni çıktı türleri eklemek veya mevcut çıktıyı değiştirmek için tüm değişiklikler **PrintOptions** projesinde yapılmalıdır. QrMenu yalnızca bu katmanı referans edip çağırır; çıktı mantığı burada toplanmıştır.

---

## 6. Önemli Namespace’ler

- **QrMenu** – Program.cs.
- **QRMENUE** – LoginForm, Helpers, AppDataLoader, AppDataDTO.
- **Qrmenue** – MainForm.
- **QRMENUE.Business** – PrintService (QrMenu içi).
- **WebBrowser** – Chromium formu.
- **Qrmenue.DTO** – ConfigDTO, LoginResultDTO, vb.
- **PrintOptions** / **PrintOptions.Dto** – Yazdırma katmanı.

---

## 7. Bağımlılıklar (Özet)

- **.NET Framework 4.8**
- **CefSharp.WinForms** – Tam ekran web görünümü.
- **RestSharp** – HTTP istekleri.
- **System.Web.Script.Serialization** – JSON (JavaScriptSerializer); app_data ve API yanıtları.
- **PrintOptions** – Proje referansı ile yazdırma katmanı.

### 7.1 Bilinen Uyarılar ve Güvenlik Açıkları

**Derleme uyarısı: "yanlış şekilde dosya olarak belirtildi" (CefSharp)**  
- CefSharp’ın `CefSharp.BrowserSubprocess.Core.dll` ve `CefSharp.BrowserSubprocess.exe` dosyaları, **BuildAction=Content** ile işaretlendiğinde MSBuild tarafından bazen derlenecek dosya gibi görülüp bu uyarı verilir.  
- **Çözüm:** Projede `CefSharpBuildAction` değeri **None** olmalı (packages.config kullanan projeler için varsayılan budur). `QrMenu.csproj` içinde `<CefSharpBuildAction>None</CefSharpBuildAction>` kullanılıyor; **Content** yapmayın.

**Güvenlik uyarıları (NuGet / GitHub Advisory):**  
- **RestSharp 106.11.7:** [GHSA-9pq7-rcxv-47vq](https://github.com/advisories/GHSA-9pq7-rcxv-47vq) (ReDoS, high). Düzeltme: RestSharp’ı **112.0.0** veya üzeri (tercihen güncel stabil sürüm) yükseltin. 110+ sürümlerde API değişiklikleri olabilir; yükseltme sonrası kodu gözden geçirin.  
- **CefSharp 90.6.50 (Common / WinForms):** Birden fazla advisory (GHSA-vv6j-ww6x-54gx, GHSA-j646-gj5p-p45g, GHSA-f87w-3j5w-v58p, GHSA-4c29-gfrp-g6x9). Bu sürüm eski Chromium/CEF tabanlıdır. **Çözüm:** CefSharp’ı güncel bir sürüme (ör. 120+, NuGet’teki en güncel sürüm) yükseltmek gerekir. Major sürüm atlayacağınız için dokümantasyona göre API değişikliklerini kontrol edip projeyi uyarlayın.

Bu uyarılar, projede eski paket sürümleri kullanıldığı için sürekli görünür; yukarıdaki güncellemeler yapıldığında azalır veya kalkar.

---

## 8. Kısa Özet

- **Giriş:** LoginForm; metinler ve API base URL **app_data.json**’dan okunur.
- **Arka plan:** MainForm tray’de çalışır; periyodik API ile yazdırma istekleri ve durum kontrolü yapar.
- **Kullanıcı arayüzü:** API’den gelen URL, CefSharp (Chromium) ile tam ekran gösterilir.
- **Yazdırma:** API’den gelen fiş verisi PrintOptions kütüphanesi ile yazdırılır.
- **Dil / sunucu:** Arayüz metinleri ve API adresi **app_data.json** (özellikle ApiLink ve Text1–Text25) ile tek yerden yönetilir.

Bu doküman, projenin yapısını ve akışını anlamak için kullanılabilir. Güncellemek istediğiniz bölümleri not alırsanız doküman buna göre genişletilebilir.


------------------------------
1. Dokunmatik klavye
txtCompanyCode, txtUserName, txtPassword alanlarına focus olduğunda tabtip.exe (Windows dokunmatik klavye) otomatik açılıyor.
Önce CommonProgramFiles ve CommonProgramFilesX86 altındaki yollar deneniyor, bulunamazsa tabtip.exe doğrudan çalıştırılıyor.

2. Form sabit (Release modda)
LoginForm_MouseDown ve LoginForm_MouseMove sadece Debug modda çalışıyor (#if DEBUG).
Release modda form sürüklenemez; kiosk dokunmatik ekranda sabit kalır.