# WebView2 IPC (postMessage) Entegrasyon Dokümantasyonu

Bu doküman, web tabanlı arayüzünüz (Frontend/JS) ile Kiosk uygulamanızın çalıştığı ana makine (C# Backend) arasındaki haberleşmeyi **Socket sunucusu kullanmadan**, doğrudan **WebView2 IPC (Süreçler Arası İletişim)** altyapısıyla nasıl gerçekleştireceğinizi açıklar.

Bu mimari, ileride yapacağınız refactoring veya yeni projelerinizde bağlantı (socket) kopmalarını önlemek, hızı artırmak ve ekstra sunucu yükünden kurtulmak için tasarlanmıştır.

---

## 1. Mimari Karşılaştırma: Socket vs WebView2 IPC

### Eski Mimari (Socket Tabanlı)
```text
[ Tarayıcı (JS) ] <---(Ağ / WebSocket)---> [ NodeJS Sunucu ] <---(Ağ / WebSocket)---> [ C# Uygulaması ] ---> [ Pavo POS vs. ]
```
* **Dezavantajlar:** Ağ katmanına bağımlıdır. Port çakışmaları, güvenlik duvarı engelleri veya anlık bağlantı kopmaları tüm sistemi durdurabilir. Gecikme (latency) yüksektir.

### Yeni Mimari (WebView2 IPC - postMessage)
```text
[ Tarayıcı (JS) ] <===(RAM / Dahili İletişim)===> [ C# Uygulaması (WebView2) ] ---> [ Pavo POS vs. ]
```
* **Avantajlar:** Aracı sunucu yoktur. İletişim tamamen uygulamanın kendi RAM'i (hafızası) üzerinden gerçekleşir. Ağ tabanlı bir kopma ihtimali **sıfırdır**. Hız anlıktır.

---

## 2. İletişim Standartı (JSON Formatı)

İki tarafın (JS ve C#) birbirini sorunsuz anlaması için sabit bir JSON protokolü belirlemek en doğrusudur.

**İstek Formatı (JS -> C#):**
```json
{
  "action": "KULLANILACAK_KOMUT_ADI",
  "data": { 
      "parametre1": "deger", 
      "parametre2": "deger" 
  }
}
```

**Cevap Formatı (C# -> JS):**
```json
{
  "action": "KULLANILACAK_KOMUT_ADI_RESPONSE",
  "success": true,
  "data": { 
      "sonuc": "basarili" 
  },
  "error": null
}
```

### 2.1 Desteklenen İstekler (Actions) ve Beklenen Veri Formatları

Sistemdeki `pavopos.json` bağımlılığını tamamen ortadan kaldırmak için cihaz ile olan tüm iletişiminizi tek bir komut olan **`PavoRequest`** üzerinden gerçekleştirebilirsiniz. Bu sayede cihazın IP adresi ve güvenlik bilgileri (Seri No vb.) tamamen Frontend (Web) tarafında yönetilmiş olur.

#### PavoRequest (Genel POS İsteği)
- **Açıklama:** Belirtilen URL'ye (`https://192.168.x.x:4567/...`) Pavo'nun beklediği JSON formatındaki veriyi doğrudan POST eder. C#'ın otomatik sorgulama (polling) özellikleri (`InitiateSale`, `CompleteSale` ve `CancelSale` işlemleri için) bu kullanımda da aktif olarak arka planda çalışmaya devam eder.
- **Önemli:** İsteği doğrudan siz attığınız için cihazın beklediği `TransactionHandle` objesini (`SerialNumber` ve `Fingerprint` dahil) JS tarafında `body` içerisine manuel olarak eklemeniz gerekmektedir.
- **Gönderilecek `data` Formatı (Örnek InitiateSale isteği):**
```json
{
  "url": "https://192.168.1.147:4567/InitiateSale",
  "body": {
    "TransactionHandle": {
      "SerialNumber": "Cihaz_Seri_No",
      "Fingerprint": "Sizin_Urettiginiz_Fingerprint"
    },
    "Sale": {
      "OrderNo": "SIPARIS123",
      "TotalPrice": 50.50
    }
  }
}
```
- **Dönen Cevap (Response Action):** `PavoRequest_Response`

---

## 3. JavaScript (Frontend) Tarafı Uygulaması

Frontend kodunuzda (Vue, React veya Vanilla JS fark etmez), `window.chrome.webview` objesini kullanarak C# ile konuşabilirsiniz.

### 3.1. C#'a Mesaj Gönderme
```javascript
function sendCommandToHost(actionName, payloadData) {
    const requestMessage = {
        action: actionName,
        data: payloadData
    };
    
    // Uygulamanın gerçekten WebView2 içinde çalışıp çalışmadığını kontrol et
    if (window.chrome && window.chrome.webview) {
        // C#'a JSON formatında string gönderiyoruz
        window.chrome.webview.postMessage(JSON.stringify(requestMessage));
        console.log("C#'a komut gönderildi:", actionName);
    } else {
        console.warn("Bu tarayıcı WebView2 değil. Komut gönderilemedi:", actionName);
        // Geliştirme ortamında (normal tarayıcıda) test yapabilmek için 
        // buraya mock (sahte) cevaplar ekleyebilirsiniz.
    }
}

// KULLANIM ÖRNEĞİ: Satış Başlatma
function pavoInitiateSale(orderNo, price) {
    sendCommandToHost("PavoInitiateSale", { 
        orderNo: orderNo, 
        totalPrice: price 
    });
}
```

### 3.2. C#'tan Gelen Cevapları Dinleme
Uygulamanız ilk yüklendiğinde (örneğin Vue `mounted` içinde veya global scope'ta) bir dinleyici eklemelisiniz.

```javascript
function setupHostListener() {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.addEventListener('message', function(event) {
            // C#'tan gelen JSON stringini objeye çevir
            const response = JSON.parse(event.data);
            
            console.log("C#'tan cevap geldi:", response.action);
            
            // Gelen işleme göre yönlendirme yap
            switch(response.action) {
                case "PavoInitiateSale_Response":
                    if(response.success) {
                        alert("Satış Başarıyla Tamamlandı: " + response.data.OrderNo);
                    } else {
                        alert("Satış Hatası: " + response.error);
                    }
                    break;
                    
                case "PavoPrintOut_Response":
                    // Yazdırma işlemi sonuçları...
                    break;
            }
        });
    }
}

// Uygulama başlarken dinleyiciyi aktif et
setupHostListener();
```

---

## 4. C# (Backend) Tarafı Uygulaması

C# tarafında WebView2 kontrolünün olaylarını (event) kullanarak bu mesajları yakalar ve cevap döneriz.

### 4.1. WebView2 Başlatma ve Event Bağlama

Form yüklendiğinde WebView2 çekirdeğinin hazır olmasını beklemeli ve ardından `WebMessageReceived` eventini bağlamalısınız.

```csharp
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        InitializeAsync();
    }

    async void InitializeAsync()
    {
        // WebView2 çekirdeğinin yüklenmesini bekle
        await webView21.EnsureCoreWebView2Async(null);
        
        // Mesaj dinleyiciyi bağla
        webView21.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        
        // Frontend uygulamanızı yükleyin
        webView21.Source = new Uri("http://localhost:8080"); // Veya yerel bir HTML dosyası
    }
```

### 4.2. JS'ten Gelen Mesajları Yakalama ve İşleme

Bu metod, JS'ten her `postMessage` atıldığında tetiklenir. **ÖNEMLİ:** Arayüzün (UI) donmaması için uzun süren Pavo işlemlerini `Task.Run` ile arka planda yapmalısınız.

```csharp
    private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            // 1. JS'ten gelen JSON stringini al
            string jsonString = e.TryGetWebMessageAsString();
            JObject request = JObject.Parse(jsonString);
            
            string action = request["action"]?.ToString();
            JToken data = request["data"];

            // 2. Aksiyona göre yönlendir
            if (action == "PavoInitiateSale")
            {
                string orderNo = data["orderNo"]?.ToString();
                decimal totalPrice = Convert.ToDecimal(data["totalPrice"]);

                // UI donmasın diye arka planda çalıştırıyoruz
                Task.Run(async () => 
                {
                    try
                    {
                        // Pavo işlemlerini yap (Soket Bridge yerine doğrudan Pavo API'yi çağırın)
                        // Örn: var result = await PavoApiClient.InitiateSaleAsync(...);
                        
                        // Simülasyon:
                        await Task.Delay(2000); 
                        string fakeResult = "{\"OrderNo\":\"" + orderNo + "\", \"Status\":\"Paid\"}";

                        // Başarılı cevabı hazırla
                        SendResponseToJs("PavoInitiateSale_Response", true, JObject.Parse(fakeResult), null);
                    }
                    catch (Exception ex)
                    {
                        // Hata durumunda hata cevabı gönder
                        SendResponseToJs("PavoInitiateSale_Response", false, null, ex.Message);
                    }
                });
            }
            else if (action == "OtherCommand")
            {
                // Diğer işlemler...
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Mesaj işlenirken hata: " + ex.Message);
        }
    }
```

### 4.3. C#'tan JS'e Cevap (Mesaj) Gönderme

Arka planda çalışan `Task`, işini bitirdiğinde sonucu Frontend'e gönderir. WebView2'de UI güncellemeleri veya mesaj gönderimleri **mutlaka Ana (UI) Thread üzerinde** yapılmalıdır. Bu yüzden `Invoke` kullanılır.

```csharp
    private void SendResponseToJs(string action, bool success, object data, string errorMessage)
    {
        // JSON formatına uygun obje oluştur
        var responseObj = new
        {
            action = action,
            success = success,
            data = data,
            error = errorMessage
        };

        // Obje'yi JSON String'e çevir
        string responseJson = JsonConvert.SerializeObject(responseObj);

        // WebView2'ye mesajı Main (UI) Thread üzerinden gönder
        if (webView21.InvokeRequired)
        {
            webView21.Invoke((Action)(() =>
            {
                webView21.CoreWebView2.PostWebMessageAsString(responseJson);
            }));
        }
        else
        {
            webView21.CoreWebView2.PostWebMessageAsString(responseJson);
        }
    }
}
```

---

## 5. İpuçları ve En İyi Pratikler

1. **Callback (Promise) Yapısı Kurmak:** Javascript tarafında her `postMessage` attığınızda bir cevap bekliyorsanız, basit bir Promise sarmalayıcı (wrapper) yazabilirsiniz. Böylece kodunuz `const sonuc = await sendCommand("PavoInitiateSale", veriler);` şeklinde son derece temiz yazılabilir.
2. **Güvenlik:** Kiosk projesi dışarıya kapalı olduğu için büyük bir risk yoktur, ancak WebView2'nin sadece güvendiğiniz domainleri (veya localhost'u) yüklediğinden emin olun.
3. **Loglama:** Hem JS hem de C# tarafında gönderilen/alınan JSON paketlerini konsola yazdırmak, debug yaparken hayat kurtarır.
4. **Offline Çalışma:** Bu yapı sayesinde C# backend'iniz çalıştığı sürece, ağ veya internet tamamen gitse bile UI ve Backend sorunsuz iletişim kurmaya devam eder. Soket koptu, yeniden bağlandı dertleri ortadan kalkar.
