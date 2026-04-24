# QrMenue Deployment Kılavuzu

## Sunucuya Yüklemeden Önce

Release derlemesi yaptıktan sonra, **tüm** `bin\Release` klasörünü zipleyin. Özellikle şunların dahil olduğundan emin olun:

### Zorunlu Dosya ve Klasörler
- `QrMenue.exe`
- `QrMenue.exe.config`
- `app_data.json`
- **`images` klasörü** (içinde `favicon_multi.ico`) – **İkonlar bunun yokluğunda görünmez**
- `app.publish` klasörü (ClickOnce kullanıyorsanız)
- `QrMenue.exe.WebView2` klasörü
- `runtimes` klasörü
- Tüm `.dll` dosyaları (RestSharp artık gerekmez – yerleşik HttpClient kullanılıyor)

### İkon Sorunu
Uygulama simgesi ve form ikonları `images\favicon_multi.ico` dosyasından yüklenir. Bu klasör zip'e eklenmezse ikonlar varsayılan (boş) görünür.
