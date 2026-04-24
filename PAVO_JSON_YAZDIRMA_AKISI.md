# Pavo JSON Fiş (Receipt) Yazdırma Entegrasyonu

Bu dosyada, Pavo POS sisteminden alınan `CustomerReceiptJson` verisinin otomatik olarak QrMenue sistemine dönüştürülüp, kağıda çizilmesi için gerçekleştirilen teknik süreç özetlenmektedir.

## 1. Veri Alımı ve Kaydedilmesi
`PavoPosSocketBridge.cs` içerisindeki `TryExtractAndSaveReceiptImage` metodu süreci yönetir:

1. **Ham Veri**: Pavo'dan gelen orijinal JSON verisi, denetim amaçlı **`ReceiptJSON_{SiparişNo}_Original.json`** olarak kaydedilir.
2. **Dönüştürme**: `ConvertPavoToRecieptInfos` fonksiyonu ile Pavo'nun karmaşık yapısı, projenin anladığı `List<RecieptInfo>` formatına çevrilir.
   - **Metinler**: Tipi boş gelen veya `text` olan öğeler metin olarak işlenir. Yazı boyutu varsayılan olarak **7**, "large" olanlar için **8** olarak atanır.
   - **QR Kod**: Google API yerine sistemin kendi servisi (`https://qrmenue.com/tr/qrcreate?Code=...`) kullanılır. Boyut **100x100** olarak sabitlenmiştir.
   - **Görseller**: `https://qrmenue.com/images/gib.png` gibi sabit yollar veya Base64 verileri desteklenir.
3. **Kalıcı Kayıt**: Dönüştürülen liste, `Newtonsoft.Json` kullanılarak **`ReceiptJSON_{SiparişNo}.json`** dosyasına kaydedilir.

## 2. DTO ve Tip Güvenliği Altyapısı
JSON serileştirme ve yazdırma sırasında oluşabilecek tip uyuşmazlıklarını (int vs string) önlemek için altyapı güçlendirilmiştir:

- **`FontInfo` DTO**: `FontSize`, `Width` ve `Height` alanları `int?` tipinden `string` tipine çekilmiştir. Bu, farklı JSON kütüphanelerinin (JavaScriptSerializer vs Newtonsoft) uyumlu çalışmasını sağlar.
- **`FontInfoService`**: String olarak gelen boyut verileri, çizim sırasında `int.TryParse` ile güvenli bir şekilde sayıya dönüştürülür.
- **Serileştirme**: Tüm projede `Newtonsoft.Json` kullanımı standartlaştırılmıştır. `SafeDeserialize` metodu sayıları otomatik olarak string'e çevirecek şekilde yapılandırılmıştır.

## 3. PrinterDesignService Yazdırma Akışı
`src\Layers\PrintOptions\PrinterDesignService.cs` dosyası fişi fiziksel olarak basar:

1. **JSON Yükleme**: Eğer `RecieptDto.ReceiptJSON` doluysa, `Receipts/` klasöründeki dönüştürülmüş dosya `Newtonsoft.Json` ile okunur.
2. **Koleksiyon Birleştirme**: Yüklenen Pavo satırları, ana fişin `Lines` koleksiyonuna (listenin sonuna) eklenir.
3. **Dayanıklı Resim Yazdırma**: Resim indirme (`WebClient`) ve Base64 işleme blokları `try-catch` ile sarmalanmıştır. Bir logo veya QR kodu 404 hatası verse bile, sistem hata fırlatmaz ve fişin geri kalanını basmaya devam eder.
4. **Grafik Motoru**: Pavo'dan gelen `space`, `line`, `image`, `qrCode` ve `text` elementleri, projenin mevcut GDI+ (System.Drawing) metotları kullanılarak yüksek kalitede çizilir.

Bu akış sayesinde, Pavo'dan gelen dış veriler sistemin iç yapısına tam uyumlu hale getirilmiş, hata toleransı artırılmış ve görsel standartlar (punto boyutları vb.) korunmuştur.
