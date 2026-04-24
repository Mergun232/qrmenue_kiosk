# Socket.IO Olay Entegrasyonu – Proje Dokümantasyonu

Bu dosya, C# Kiosk projesine eklenen **Socket.IO** dinleme yapısını özetler. Unutmamak ve ileride genişletmek için referans olarak kullanılabilir.

---

## 1. Ne İşe Yarıyor?

- Login sonrası, sunucunun döndüğü **SocketConnectUrl** ve **SocketRoom** ile bir **Socket.IO** sunucusuna bağlanıyoruz.
- Bu socket üzerinden **anlık olaylar** dinleniyor (yeni sipariş, yazdırma isteği vb.).
- Şu an sadece **dinleme** ve **test için loglama** var; ileride her olay için ayrı iş mantığı (bildirim, yazdırma tetikleme vb.) eklenebilir.

---

## 2. Yapılandırma Nereden Geliyor?

Socket adresi ve oda bilgisi **login API yanıtından** gelir. Login başarılı olduğunda dönen JSON içindeki `DataList` alanları kullanılır:

| Alan               | Örnek                    | Açıklama                          |
|--------------------|--------------------------|-----------------------------------|
| `SocketConnectUrl` | `http://localhost:8000` | Socket.IO sunucusunun base URL’i  |
| `SocketRoom`       | `C32463`                 | Bu kioskun bağlanacağı oda (room) |

Örnek login response (ilgili kısım):

```json
"DataList": {
  "SocketConnectUrl": "http://localhost:8000",
  "SocketRoom": "C32463",
  ...
}
```

- Bu alanlar **LoginResponseDataList** ve **LoginResultDTO** içinde tutuluyor.
- **MainForm** açılırken `SocketConnectUrl` doluysa socket bağlantısı otomatik başlatılıyor.

---

## 3. Projedeki İlgili Dosyalar

| Dosya | Rol |
|-------|-----|
| **Business/SocketService.cs** | Socket.IO bağlantısı, room query parametresi, olay dinleme, gelen veriyi callback ile iletme. |
| **Forms/SocketLogForm.cs** | Test için: Dinlenen olayları ve payload’ları listeler (sonradan kaldırılabilir). |
| **Forms/MainForm.cs** | Login sonrası `SocketConnectUrl` / `SocketRoom` varsa `SocketService` oluşturur, `SocketLogForm` açar, bağlantıyı başlatır; kapanışta `Disconnect` ve log formunu kapatır. |
| **DTO/LoginResultDTO.cs** | `SocketConnectUrl`, `SocketRoom` alanları. |
| **DTO/LoginResponseExeDTO.cs** | Login API yanıtı: `LoginResponseDataList` içinde `SocketConnectUrl`, `SocketRoom`. |
| **Forms/LoginForm.cs** | Login başarılı olunca `DataList.SocketConnectUrl` ve `DataList.SocketRoom` değerlerini `LoginResultDTO` içine yazar. |

---

## 4. Kullanılan Kütüphane

- **SocketIOClient** (NuGet) sürüm **3.1.2**
- Bağımlılıklar: `SocketIO.Serializer.Core`, `SocketIO.Serializer.SystemTextJson`, `System.Text.Json`

---

## 5. Bağlantı Nasıl Kuruluyor?

Web tarafındaki JavaScript ile uyumlu:

1. **URL**: İstemci **yalnızca** API’den gelen `SocketConnectUrl`’e bağlanır (örn. `http://localhost:8000`). Başka bir adres veya yol eklenmez; sunucuda ayrıca `/socket.io` route’u tanımlamanız gerekmez.
2. **Path**: Bu, bağlandığınız sunucunun **içindeki** Socket.IO el sıkışma yoludur. Node’da `new Server(httpServer)` kullanıyorsanız varsayılan path zaten `/socket.io`’dur; C# tarafında da `Path = "/socket.io"` kullanıyoruz. Sunucuda `path: "/"` gibi farklı bir path kullandıysanız, `SocketService.cs` içinde aynı path’i (örn. `"/"`) ayarlayın.
3. **Seçenekler**: `Reconnection = true` (transports: önce polling, gerekirse WebSocket).
4. **Bağlantı**: Arka planda (`Task.Run` içinde) `SocketIO` client oluşturulur ve `ConnectAsync()` çağrılır.
5. **Oda (room)**: Bağlantı kurulduktan hemen sonra sunucuya **`channelfixer`** event’i emit edilir; veri olarak `SocketRoom` (oda adı) gönderilir. Sunucu bu event’i dinleyip ilgili socket’i o odaya join etmelidir. Olaylar bu oda üzerinden gönderilir.
6. **Zaman aşımı**: Bağlantı 15 saniye içinde tamamlanmazsa logda "Bağlantı zaman aşımı" yazar. Sunucu yanıt vermiyorsa Node.js tarafında port, CORS ve socket.io/Engine.IO sürüm uyumluluğunu kontrol edin (Socket.IO v4 sunucuda bazen `allowEIO3: true` gerekebilir).

---

## 6. Dinlenen Olaylar

Şu an dört olay dinleniyor; gelen veri `SocketService` içinde işlenip dışarıya **event adı + payload** olarak callback ile veriliyor:

| Olay adı | Amaç (özet) |
|----------|--------------|
| **AllRequest** | Genel istek olayı |
| **PrintRequest** | Yazdırma isteği |
| **DisSiparisRequest2** | Dis sipariş ile ilgili istek |
| **TumBildirimler** | Tüm bildirimler |

### Pavo POS Olayları (Test Formu)

Pavo POS REST API entegrasyonu için socket üzerinden tetiklenen olaylar. Tüm istekler **arka planda** (Task.Run) çalışır. **Tam JSON** veya minimal payload desteklenir. Detaylı format için `PAVO_SOCKET_ISTEK_DOKUMANI.md` dosyasına bakın.

| Olay adı | Amaç | Payload |
|----------|------|---------|
| **PavoPairing** | Cihaz eşleştirme | Tam JSON veya `{}` |
| **PavoInitiateSale** | Ödeme isteği (kart okutma) | Tam JSON veya `{"orderNo":"x","totalPrice":20}` |
| **PavoGetSaleResult** | Satış sonucu sorgula | Tam JSON veya `{"orderNo":"x"}` |
| **PavoPrintOut** | Fiş yazdır | Tam JSON veya `{"image":"base64"}` |
| **PavoPaymentMediators** | Ödeme yöntemleri listesi | Tam JSON veya `{}` |
| **PavoCompleteSale** | Nakit satış | Tam JSON (Sale zorunlu) |

Yapılandırma `pavopos.json` dosyasından okunur. TransactionHandle (SerialNumber, Fingerprint, TransactionSequence, TransactionDate) Kiosk tarafından otomatik eklenir.

Yeni bir olay eklemek için `SocketService.cs` içinde aynı kalıpla bir `_client.On("YeniOlayAdi", ctx => HandleEvent("YeniOlayAdi", ctx));` satırı eklemeniz yeterli.

---

## 7. Test Formu (SocketLogForm)

- **Amaç**: Socket’ten gelen olayları görmek (konsol olmadığı için formda listeleme).
- **Davranış**: Her olay için `[saat] [OlayAdi] payload` formatında bir satır eklenir; en fazla 500 satır tutulur.
- **Ne zaman açılır**: Login sonrası `SocketConnectUrl` doluysa MainForm tarafından otomatik açılır.
- **İleride**: Sadece test için kullanılıyorsa formu kapatıp, `SocketService` callback’ine doğrudan kendi iş mantığınızı (bildirim, yazdırma vb.) bağlayabilirsiniz.

---

## 8. Akış Özeti

1. Kullanıcı login olur.
2. API yanıtında `DataList.SocketConnectUrl` ve `DataList.SocketRoom` gelir.
3. MainForm açılır; bu alanlar doluysa:
   - SocketLogForm gösterilir,
   - SocketService oluşturulur, `Connect()` çağrılır,
   - Gelen olaylar SocketLogForm’a yazdırılır.
4. Uygulama kapanırken (güvenli çıkış) `SocketService.Disconnect()` ve SocketLogForm kapatılır.

---

## 9. Sunucu (Node.js) Tarafında Beklenenler

- Socket.IO sunucusu çalışır durumda olmalı (`SocketConnectUrl` ile erişilebilir).
- Bağlantıda **query**’den `room` okunup `socket.join(room)` yapılmalı.
- İlgili olaylar (`AllRequest`, `PrintRequest`, `DisSiparisRequest2`) bu oda için emit edilmeli (örn. `io.to(room).emit("PrintRequest", data)`).

---

## 10. WSL veya Docker Kullanıyorsanız

Kiosk uygulaması **Windows** üzerinde çalışıyor; socket sunucusu **WSL** veya **Docker** içindeyse bağlantı zaman aşımı genelde ağ yapılandırmasından kaynaklanır.

### Yapmanız gerekenler

1. **SocketConnectUrl Windows'tan erişilebilir olmalı**  
   API'nin döndüğü `SocketConnectUrl`, **Windows makinesinden** (kiosk uygulamasının çalıştığı yer) açılabilir bir adres olmalı.  
   - **Docker:** Port host'a yayınlanmış olmalı, örn. `docker run -p 8008:8008 ...`. API'nin döndüğü URL `http://localhost:8008` gibi olmalı (Windows'taki localhost = host makinesi).  
   - **WSL2:** Genelde `http://localhost:PORT` Windows'tan WSL'e yönlenir; yine de sunucunun `0.0.0.0` dinlediğinden emin olun.

2. **Sunucu tüm arayüzleri dinlemeli**  
   Node'da socket/http sunucusu **127.0.0.1** yerine **0.0.0.0** üzerinde dinlemeli ki container/WSL dışından gelen bağlantılar kabul edilsin.  
   Örnek: `app.listen(8008, '0.0.0.0', ...)` veya `server.listen(8008, '0.0.0.0')`.

3. **API'nin döndüğü URL'yi kontrol edin**  
   Login API'si `SocketConnectUrl` olarak container iç IP'si (örn. `172.17.0.2`) veya sadece container içinde çözülen bir hostname dönüyorsa, Windows'taki kiosk bu adrese ulaşamaz. Backend'de bu alanın, **istemcinin (Windows) kullanacağı adres** olacak şekilde set edilmesi gerekir (örn. `http://localhost:8008` veya host'un gerçek IP'si).

4. **Hızlı test**  
   Windows'ta tarayıcıdan `http://[SocketConnectUrl]/socket.io/?EIO=4&transport=polling` adresini açın. Yanıt (ör. `0{...}`) alıyorsanız sunucu Windows'tan erişilebilir; yine de bağlanamıyorsanız CORS veya firewall'ı kontrol edin.

Bu doküman, projede eklenen socket olay yapısını unutmamak ve geliştirmeye devam etmek için kullanılabilir.
