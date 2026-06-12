# Socket ve Yazdırma Akışı — QrMenu Exe

Bu doküman, QrMenu uygulamasında **socket bağlantısı**, **yazdırma tetiklemesi** ve **login yönlendirmesi** mantığını C# bilmeyen biri için adım adım anlatır.

**Proje kökünde:** `SOCKET_VE_YAZDIRMA_AKISI.md`  
**Kodda açıklamalar:** `MainForm.cs`, `SocketService.cs`, `Helpers.cs`, `PrinterDesignService.cs` içindeki `///` ve `//` yorumlar.

---

## 1. Genel Mantık: Ne Zaman Yazdırma İsteği Atılır?

Uygulama yazdırma için **iki yol** kullanır:

| Durum | Ne olur? |
|--------|----------|
| **Socket bağlı** | Sadece sunucu **PrintRequest** olayı gönderdiğinde `printer-requests` API’si çağrılır. Timer ile sürekli istek **atılmaz**. |
| **Socket yok veya kopuk** | Belirli aralıklarla (ör. 5 saniye) **timer** devreye girer ve `printer-requests` API’si otomatik çağrılır. Böylece socket olmasa da yazdırma çalışır. |

**Özet:** Socket varsa tetikleme socket’ten; yoksa veya bağlantı kopuksa tetikleme timer’dan gelir.

---

## 2. Socket Bağlantısı

- **Dosya:** `MainForm.cs` → `StartSocketIfConfigured()`
- Login sonrası API’den `SocketConnectUrl` gelirse socket başlatılır.
- Bağlantı kurulunca `SocketService.IsConnected` true olur.
- Bağlantı kopunca (sunucu kapandı, ağ kesildi vb.) `IsConnected` false olur ve 5 saniye sonra otomatik yeniden bağlanma denenir.

---

## 3. Timer (Periyodik Kontrol)

- **Dosya:** `MainForm.cs` → `timer1_Tick` (tmrFicheCheck)
- **Aralık:** `app_data.json` içindeki `PrinterRequestIntervalSeconds` (varsayılan 5 saniye).

**Mantık:**

```
Eğer socket YOK veya socket BAĞLI DEĞİLSE
  → CheckPrintables() çağrılır (yani printer-requests API’si tetiklenir)

Eğer socket BAĞLI ise
  → Timer hiçbir şey yapmaz (tetikleme sadece PrintRequest ile gelir)
```

Böylece socket varken gereksiz istek atılmaz; socket yokken veya koptuğunda yazdırma timer ile devam eder.

---

## 4. PrintRequest Olayı (Socket’ten Gelen Tetikleme)

- **Dosya:** `MainForm.cs` → `OnSocketEvent()`
- Sunucu “yazdır” demek için **PrintRequest** olayı gönderir.
- Bu olay gelince `CheckPrintables()` çağrılır; yani `printer-requests` API’si hemen tetiklenir.

---

## 5. CheckPrintables() — Yazdırma İşinin Yapıldığı Yer

- **Dosya:** `MainForm.cs` → `CheckPrintables()`

**Adımlar (sırayla):**

1. **Ön koşul:** `PrinterStart` açık ve `LoginToken` dolu olmalı; değilse işlem yapılmaz.
2. **printer-requests API çağrısı:** `GET api/exe/printer-requests` + LoginToken header.
3. **HTTP 401 kontrolü:**  
   - Yanıt **401** ise = “Kimlik doğrulama hatası” (token geçersiz veya bulunamadı).  
   - Bu durumda WebView (tarayıcı penceresi) login sayfasına yönlendirilir; başka bir işlem yapılmaz.
4. **Başarılı yanıt (result = success):** API, yazdırılacak fişler için **URL listesi** döner.
5. **Her URL için:** Bu URL’ye istek atılır, gelen JSON’dan fiş verisi (RecieptDto) parse edilir.
6. **Yazdırma:** Her fiş için `PrinterDesignService.PrintSlip()` çağrılır.

**Özet:** 401 = oturum bitti → login ekranı. Success = URL’ler alınır, fişler indirilir ve yazdırılır.

---

## 6. Sadece 401’de Login’e Yönlendirme

- **Neden sadece 401?**  
  `printer-requests` bazen **failure** veya **error** da dönebilir (yazdırılacak iş yok, sunucu meşgul, ağ hatası vb.). Bunlar oturum hatası değildir.
- **401** ise API’nin döndüğü “Kimlik doğrulama hatası (LoginToken geçersiz veya bulunamadı)” anlamına gelir.
- Bu yüzden **sadece HTTP 401** geldiğinde WebView kapatılıp login ekranına yönlendirme yapılır; diğer hata kodlarında yönlendirme yok.

---

## 7. Yazıcı Cihazda Yoksa Ne Olur?

- **Dosya:** `PrinterDesignService.cs` → `PrintSlip()` içinde, yazıcı adı kullanılmadan önce
- Fişteki **PrinterName** (yazıcı adı), bilgisayardaki **yüklü yazıcılar listesinde** aranır.
- **Bulunursa:** O yazıcıya yazdırılır.
- **Bulunmazsa:** Hata vermek yerine **varsayılan (default) yazıcı** kullanılır; böylece uygulama çökmez ve WebView login’e atılmaz.

---

## 8. Pavo POS İşlemleri ve Banka Fişi Yazdırma

Qrmenu Kiosk uygulaması artık Pavo Pos entegrasyonundan (örneğin kartlı ödemelerden) dönen sanal banka fişlerinin resimlerini de uygulamanın sipariş fişine dahil edebilmektedir.
- **Resmin Kaydedilmesi:** `PavoPosSocketBridge.cs` içinde yürütülen `CompleteSale` veya `GetSaleResult` işlemleri başarılı olduğunda Pavo'dan dönen JSON içindeki `CustomerReceiptImage` (Base64) verisi yakalanır. Bu veri anında `uygulama_dizini\Receipts` klasörüne PNG dosyası (örneğin `ReceiptImage_130.png`) olarak kaydedilir. (Diski doldurmaması için sadece son 10 fiş resmi saklanır, eskiler otomatik silinir).
- **Yazdırılması:** `printer-requests` üzerinden fiş bilgileri dönerken, oluşturulan `RecieptDto` JSON nesnesinin içerisindeki `"ReceiptImage": "ReceiptImage_130.png"` bilgisi gelir.
- `PrinterDesignService.cs`, fişin diğer tüm satırlarını basmayı bitirdikten sonra (fişin en alt kısmında), ilgili resmi `Receipts` klasöründen okur ve fiş genişliğine oranlayarak çizdirir. (Resmin boyutlandırılması sırasında cihazlar arası uyumsuzluk olmaması için `Graphics.DrawImage` kullanılır).

---

## 9. Kısa Akış Özeti

```
[Uygulama açıldı]
  → Socket başlatılır (SocketConnectUrl varsa)
  → Timer başlar (her X saniye)

[Her timer vuruşunda]
  → Socket bağlı mı?
      EVET → Hiçbir şey yapma (sadece PrintRequest beklenir)
      HAYIR → CheckPrintables() çağır (printer-requests at)

[Socket’ten PrintRequest geldiğinde]
  → CheckPrintables() çağır (printer-requests at)

[CheckPrintables içinde]
  → printer-requests API’yi çağır
  → 401 mi? → EVET: WebView’ı login’e yönlendir, çık
  → result = success ve URL listesi var mı? → EVET: Her URL’den fiş al
    > Pavo Png resmi (ReceiptImage) varsa, fişin en sonuna o grafiği de ekle ve yazdır.
  → Yazıcı adı cihazda yoksa → Varsayılan yazıcıya yazdır
```

---

## 10. İlgili Dosyalar

| Dosya | Ne yapar? |
|-------|-----------|
| `MainForm.cs` | Socket/timer mantığı, CheckPrintables, 401 kontrolü, login yönlendirmesi |
| `SocketService.cs` | Socket bağlantısı, PrintRequest dinleme, kopunca yeniden bağlanma |
| `Helpers.cs` | HTTP istekleri; printer-requests için HTTP status (401) dönüyor |
| `PrinterDesignService.cs` | Fiş çizimi ve yazdırma; yazıcı yoksa varsayılana düşme, Pavo Png resmi fişe yazdırma |
| `PavoPosSocketBridge.cs` | Pavo ödeme adımlarını tetikleme, sonuç takibi yapma ve Base64 fiş resmini Receipts klasörüne kaydetme |

Bu doküman, bu akışları kodda takip ederken rehber olarak kullanılabilir.
