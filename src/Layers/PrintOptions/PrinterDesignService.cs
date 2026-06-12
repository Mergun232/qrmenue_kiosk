using PrintOptions.Dto;
using PrintOptions.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace PrintOptions
{
    public class PrinterDesignService
    {
        FontInfoService fis { get; set; } //= new FontInfoService();
        public readonly List<string> allowedHosts = new List<string>() { "qrmenue.com" };

        /// <summary>Boşsa StartupPath/Receipts. Uygulama başında AppPaths.ReceiptsDirectory ile set edilmeli.</summary>
        public static string ReceiptsDirectory { get; set; }

        private static string GetReceiptsFolder()
        {
            return !string.IsNullOrWhiteSpace(ReceiptsDirectory)
                ? ReceiptsDirectory
                : Path.Combine(Application.StartupPath, "Receipts");
        }

        /// <summary>ReceiptJSON doluysa dosya mevcut ve okunabilir olmalı; aksi halde fiş basılmamalı.</summary>
        public static bool CanPrintReceipt(RecieptDto dto)
        {
            if (dto == null) return false;
            if (string.IsNullOrWhiteSpace(dto.ReceiptJSON)) return true;
            return TryLoadReceiptJsonLines(dto.ReceiptJSON, out _);
        }

        private static bool TryLoadReceiptJsonLines(string receiptJsonFileName, out List<RecieptInfo> lines)
        {
            lines = null;
            if (string.IsNullOrWhiteSpace(receiptJsonFileName)) return false;

            string jsonPath = Path.Combine(GetReceiptsFolder(), receiptJsonFileName.Trim());
            if (!File.Exists(jsonPath)) return false;

            try
            {
                string jsonContent = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                var settings = new JsonSerializerSettings { Error = (s, e) => e.ErrorContext.Handled = true };
                lines = JsonConvert.DeserializeObject<List<RecieptInfo>>(jsonContent, settings);
                return lines != null && lines.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public void PrintSlip(RecieptDto RecieptDto)
        {
            if (!CanPrintReceipt(RecieptDto))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[PrinterDesign] ReceiptJSON dosyası yok veya boş, fiş atlandı: {RecieptDto?.ReceiptJSON}");
                return;
            }

            fis = new FontInfoService(RecieptDto.PrinterData.DefaultFont);
            
            // JSON üzerinden değer gönderilirse onu kullanır, gönderilmezse varsayılan 280px olur
            int fixedWidth = RecieptDto.FixedWidth ?? 280;

            float startX = 0.0f;
            //  args.Graphics.PageUnit =  GraphicsUnit.Millimeter;

            float x = 0.0f;
            float y = 0.0f;

            var slip = RecieptDto.PrinterData;// data.PrinterData;
            PrintDocument pd = new PrintDocument()
            {
                PrinterSettings = new PrinterSettings()
                {
                    // set the printer to 'Microsoft Print to PDF'
                    PrinterName = "Microsoft Print to PDF",

                    // tell the object this document will print to file
                    PrintToFile = true,

                    // set the filename to whatever you like (full path)
                    PrintFileName = "D://newPdf.pdf",
                }
            };
            var sourceLines = slip.RecieptInfos;
            if (sourceLines == null) sourceLines = new List<RecieptInfo>();

            List<RecieptInfo> pavoInfosFromFile = null;
            string receiptJsonPlaceholder = RecieptInfoTypeEnum.ReceiptJSON.ToString();
            if (!string.IsNullOrEmpty(RecieptDto.ReceiptJSON))
                TryLoadReceiptJsonLines(RecieptDto.ReceiptJSON, out pavoInfosFromFile);

            // ReceiptJSON yer tutucusu: PrinterData.RecieptInfos içinde Type "ReceiptJSON" olan satırın yerine dosyadaki satırlar konur.
            // Yer tutucu yoksa (eski davranış) dosya içeriği listenin sonuna eklenir.
            var lines = new List<RecieptInfo>();
            bool hasReceiptJsonPlaceholder = false;
            foreach (var line in sourceLines)
            {
                if (string.Equals(line.Type, receiptJsonPlaceholder, StringComparison.OrdinalIgnoreCase))
                {
                    hasReceiptJsonPlaceholder = true;
                    if (pavoInfosFromFile != null && pavoInfosFromFile.Count > 0)
                        lines.AddRange(pavoInfosFromFile);
                    continue;
                }
                lines.Add(line);
            }

            if (pavoInfosFromFile != null && pavoInfosFromFile.Count > 0 && !hasReceiptJsonPlaceholder)
                lines.AddRange(pavoInfosFromFile);

            pd.PrintController = new StandardPrintController();
            float height = 0;
            Font LineDataFont = fis.font;//.getFont(options);
            pd.PrintPage += (object sp, PrintPageEventArgs args) =>
             {
                 float width = fixedWidth;

                 var defaults = slip.DefaultFont;
                 SolidBrush drawBrush = new SolidBrush(fis.color);

                 StringFormat SlipStringFormat = fis.stringFormat;//.getStringFormat(defaults);
                 var options = slip.DefaultFont;// data.FontInfo;


                 height = LineDataFont.Height;

                 foreach (var line in lines)
                 {
                     fis.SetFontAndFormat(line.FontInfo);
                     drawBrush = new SolidBrush(fis.color);




                     if (line.Type == RecieptInfoTypeEnum.Row.ToString())
                     {
                         var tempHeight = y;
                         float colX = x;
                         float maxColumnHeight = 0;

                         foreach (var data in line.Columns)
                         {
                             fis.SetFontAndFormat(data.FontInfo);
                             // Asıl kağıt boyutu üzerinden net yüzdelik hesaplıyoruz (gereksiz boşluk kalmaması için)
                             float sizeOfColumn = (fixedWidth / 100f * Convert.ToSingle(data.Length));
                             
                             var sizeF = new SizeF(sizeOfColumn, fis.font.Height);

                             var columnLines = GetLines(data.Text, fis.font, sizeF, fis.stringFormat, (int)sizeOfColumn, args);
                             
                             float localY = tempHeight;
                             foreach (var item in columnLines)
                             {
                                 args.Graphics.DrawString(item, fis.font, drawBrush, new RectangleF(new PointF(colX, localY), sizeF), fis.stringFormat);
                                 localY += sizeF.Height;
                             }
                             // En uzun sütunun yüksekliğini kaydediyoruz
                             float currentHeight = localY - tempHeight;
                             if (currentHeight > maxColumnHeight)
                                 maxColumnHeight = currentHeight;

                             colX += sizeOfColumn;
                         }

                         y += maxColumnHeight;
                         
                         // Alt boşluk kontrolü (isteğe bağlı)
                         if (line.FontInfo != null && int.TryParse(line.FontInfo.Height, out int rowExtraH) && rowExtraH > 0)
                             y += rowExtraH;

                         x = startX;
                         fis.Reset();
                     }


                     else if (line.Type == RecieptInfoTypeEnum.Image.ToString())
                     {
                         try
                         {
                             fis.SetFontAndFormat(line.FontInfo);

                             byte[] imageBytes;
                             if (line.Text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                             {
                                 using (WebClient webClient = new WebClient())
                                 {
                                     imageBytes = webClient.DownloadData(line.Text);
                                 }
                             }
                             else
                             {
                                 string base64 = line.Text;
                                 if (base64.Contains(",")) base64 = base64.Substring(base64.IndexOf(",") + 1);
                                 imageBytes = Convert.FromBase64String(base64);
                             }
                             //byte[] imageBytes = Convert.FromBase64String(base64);
                             var ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                             Image image = Image.FromStream(ms, true);

                             int ImgHeight = Convert.ToInt32(line.FontInfo.Height);
                             int ImgWidth = Convert.ToInt32(line.FontInfo.Width);

                             float ImgX = x;
                             float ImgY = y;

                             if (fis.stringFormat.Alignment == StringAlignment.Center)
                             {
                                 float centerX = startX + (fixedWidth / 2f);
                                 ImgX = centerX - (ImgWidth / 2f);
                             }
                             else if (fis.stringFormat.Alignment == StringAlignment.Far)
                             {
                                 ImgX = startX + fixedWidth - ImgWidth;
                             }
                             DrawThermalOptimizedImage(args.Graphics, image, ImgX, ImgY, ImgWidth, ImgHeight);

                             y += ImgHeight;
                         }
                         catch (Exception imgEx)
                         {
                             System.Diagnostics.Debug.WriteLine("[PrinterDesign] Resim atland\u0131: " + imgEx.Message);
                         }
                         finally
                         {
                             fis.Reset();
                         }
                     }


                     else if (line.Type == RecieptInfoTypeEnum.Line.ToString())
                     {
                         fis.SetFontAndFormat(line.FontInfo);
                         drawBrush = new SolidBrush(fis.color);

                         var sizeF = new SizeF(fixedWidth, fis.font.Height);

                         float localY = y;
                         var columnLines = GetLines(line.Text, fis.font, sizeF, fis.stringFormat, fixedWidth, args);
                         foreach (var item in columnLines)
                         {
                             args.Graphics.DrawString(item, fis.font, drawBrush, new RectangleF(x, localY, fixedWidth, fis.font.Height), fis.stringFormat);
                             localY += sizeF.Height;
                         }
                         
                         y = localY;

                         // Alt boşluk kontrolü (isteğe bağlı)
                         if (line.FontInfo != null && int.TryParse(line.FontInfo.Height, out int lineExtraH) && lineExtraH > 0)
                             y += lineExtraH;

                         fis.Reset();
                         x = startX;
                     }
                     else if (line.Type == RecieptInfoTypeEnum.Seperate.ToString())
                     {
                         fis.SetFontAndFormat(line.FontInfo);

                         var pen = new Pen(fis.color);
                         // Çizginin y=0 noktasından sedikit aşağı çizilip sonra alt boşluk bırakılması:
                         float currentY = y + 2f; 
                         var p1 = new PointF(x, currentY);
                         var p2 = new PointF(x + fixedWidth, currentY);

                         args.Graphics.DrawLine(pen, p1, p2);

                         fis.Reset();
                         
                         // Varsayılan olarak çok az yer kaplamalı ancak JSON'dan ayarlanabilmeli
                         float sepSpace = 6f; 
                         if (line.FontInfo != null && int.TryParse(line.FontInfo.Height, out int sepH) && sepH > 0)
                             sepSpace = sepH;
                             
                         y += 2f + sepSpace; // Üstüne ve altına küçük boşluk
                         x = startX;
                     }

                 }


                 // Pavo çizimleri artık yukarıda 'lines' içerisine dahil edildiğinden 
                 // burada tekrardan işlenmesine gerek kalmadı. Sadece resim işleme kısmına devam ediyoruz.

                 // Pavo'dan gelen ReceiptImage'ı (varsa) fişin en altına ekle
                 if (!string.IsNullOrEmpty(RecieptDto.ReceiptImage))
                 {
                     try
                     {
                         string receiptsFolder = GetReceiptsFolder();
                         string imagePath = Path.Combine(receiptsFolder, RecieptDto.ReceiptImage);
                         
                         if (File.Exists(imagePath))
                         {
                             // Alt kısma biraz boşluk bırak
                             y += 10f; 

                             using (Image image = Image.FromFile(imagePath))
                             {
                                 // Genişliği fiş genişliğine sabitleyip, oranını bozmadan boyunu ayarlıyoruz
                                 int imgWidth = fixedWidth;
                                 float ratio = (float)imgWidth / image.Width;
                                 int imgHeight = (int)(image.Height * ratio);
                                 
                                 DrawThermalOptimizedImage(args.Graphics, image, startX, y, imgWidth, imgHeight);
                                 y += imgHeight;
                             }
                         }
                     }
                     catch { }
                 }
             };
            // Fişte yazıcı adı varsa: cihazda bu yazıcı var mı kontrol et; yoksa varsayılan yazıcıya düş (hata fırlatma, WebView login'e atılmasın)
            if (string.IsNullOrEmpty(RecieptDto.PrinterName) == false)
            {
                bool printerExists = false;
                try
                {
                    foreach (string name in PrinterSettings.InstalledPrinters)
                    {
                        if (string.Equals(name, RecieptDto.PrinterName, StringComparison.OrdinalIgnoreCase))
                        {
                            printerExists = true;
                            break;
                        }
                    }
                }
                catch { }

                if (printerExists)
                {
                    var printerSettings = new PrinterSettings { PrinterName = RecieptDto.PrinterName };
                    var pageSettings = new PageSettings(printerSettings) { Margins = new Margins(0, 0, 0, 0) };
                    pd.PrinterSettings = printerSettings;
                    pd.DefaultPageSettings = pageSettings;
                    pd.PrinterSettings.PrintToFile = false;
                }
                else
                {
                    // Yazıcı cihazda yok → varsayılan yazıcı kullan (InvalidPrinterException önlenir)
                    var defaultSettings = new PrinterSettings();
                    pd.PrinterSettings = defaultSettings;
                    pd.DefaultPageSettings = new PageSettings(defaultSettings) { Margins = new Margins(0, 0, 0, 0) };
                    pd.PrinterSettings.PrintToFile = false;
                }
            }

            pd.Print();
        }


        public List<string> GetLines(string text, Font font, SizeF layoutArea, StringFormat stringFormat, int length, PrintPageEventArgs args)
        {
            var newLines = new List<string>();
            if (string.IsNullOrEmpty(text)) return newLines;

            string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string currentLine = "";

            foreach (var word in words)
            {
                int wordWidth = (int)args.Graphics.MeasureString(word, font, 9999, stringFormat).Width;
                
                if (wordWidth > length)
                {
                    string remainingWord = word;
                    while (remainingWord.Length > 0)
                    {
                        string testLine = string.IsNullOrEmpty(currentLine) ? "" : currentLine + " ";
                        int charsToTake = 1;

                        while (charsToTake <= remainingWord.Length)
                        {
                            string chunk = remainingWord.Substring(0, charsToTake);
                            if ((int)args.Graphics.MeasureString(testLine + chunk, font, 9999, stringFormat).Width > length)
                            {
                                charsToTake--;
                                break;
                            }
                            charsToTake++;
                        }

                        if (charsToTake > remainingWord.Length) charsToTake = remainingWord.Length;

                        if (charsToTake <= 0) 
                        {
                            if (!string.IsNullOrEmpty(currentLine)) 
                            {
                                newLines.Add(currentLine);
                                currentLine = "";
                                continue; 
                            }
                            else 
                            {
                                charsToTake = 1;
                            }
                        }
                        
                        string fitChunk = remainingWord.Substring(0, charsToTake);
                        currentLine = testLine + fitChunk;
                        
                        if (charsToTake < remainingWord.Length)
                        {
                            newLines.Add(currentLine);
                            currentLine = "";
                        }
                        
                        remainingWord = remainingWord.Substring(charsToTake);
                    }
                }
                else
                {
                    string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                    if ((int)args.Graphics.MeasureString(testLine, font, 9999, stringFormat).Width <= length)
                    {
                        currentLine = testLine;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                            newLines.Add(currentLine);
                        currentLine = word;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                newLines.Add(currentLine);

            return newLines;
        }

        private void DrawThermalOptimizedImage(Graphics g, Image originalImage, float x, float y, int width, int height)
        {
            var prevInterp = g.InterpolationMode;
            var prevSmoothing = g.SmoothingMode;
            var prevCompositing = g.CompositingQuality;
            var prevOffset = g.PixelOffsetMode;

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            using (Bitmap safeImage = new Bitmap(originalImage.Width, originalImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gSafe = Graphics.FromImage(safeImage))
                {
                    gSafe.Clear(Color.White);
                    gSafe.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);
                }

                using (var imageAttr = new System.Drawing.Imaging.ImageAttributes())
                {
                    // Daha net ve kalın çizgiler için eşik değerini 0.65'e çektik. 
                    // Bu sayede gri tonda kalan (kaybolmaya yüz tutmuş) ince çizgiler de tama yakın siyaha dönüşür.
                    imageAttr.SetThreshold(0.65f);
                    Rectangle destRect = new Rectangle((int)Math.Round(x), (int)Math.Round(y), width, height);
                    g.DrawImage(safeImage, destRect, 0, 0, safeImage.Width, safeImage.Height, GraphicsUnit.Pixel, imageAttr);
                }
            }

            g.InterpolationMode = prevInterp;
            g.SmoothingMode = prevSmoothing;
            g.CompositingQuality = prevCompositing;
            g.PixelOffsetMode = prevOffset;
        }
    }
}
