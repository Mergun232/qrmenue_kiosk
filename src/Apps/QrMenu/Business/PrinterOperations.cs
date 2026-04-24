namespace Qrmenue
{
    public static class PrinterOperations
    {

        //public static List<string> cachedPrinters = new List<string>();

        //public static void PrintPDf(string pdfB64, string printerName)
        //{


        //    byte[] pdf = Convert.FromBase64String(pdfB64);
        //    Image[] img = null;
        //    string path = Application.StartupPath + "/tempFile.pdf", systemPrinterName = "";
        //    File.WriteAllBytes(path, pdf);
        //    GetAllPrinterList();

        //    using (GhostscriptRasterizer rasterizer = new GhostscriptRasterizer())
        //    {
        //        rasterizer.Open(Application.StartupPath + "/tempFile.pdf");
        //        img = new Image[rasterizer.PageCount];
        //        int dpi_x = 512;
        //        int dpi_y = 512;
        //        for (int i = 1; i <= rasterizer.PageCount; i++)
        //        {
        //            img[i - 1] = rasterizer.GetPage(dpi_x, dpi_y, i);
        //            img[i - 1].Save(Application.StartupPath + "/tempImage" + i + ".jpg");
        //        }

        //    }

        //    foreach (var item in cachedPrinters)
        //    {
        //        if (item.Contains(printerName))
        //        {
        //            systemPrinterName = item;
        //            break;
        //        }
        //    }
        //    if (systemPrinterName != "")
        //    {
        //        PrintDocument pd = new PrintDocument();
        //        var count = 0;
        //        pd.PrintPage += (object sp, PrintPageEventArgs args) =>
        //        {
        //            args.Graphics.DrawImage(img[count], new Rectangle { Width = 285, Height = 745 });
        //            args.HasMorePages = ++count != img.Length;
        //        };
        //        pd.PrinterSettings.PrinterName = systemPrinterName;
        //        pd.Print();
        //    }

        //}

        //public static List<string> GetAllPrinterList()
        //{
        //    if (cachedPrinters.Count > 0)
        //        return cachedPrinters;

        //    foreach (string item in PrinterSettings.InstalledPrinters)
        //    {
        //        cachedPrinters.Add(item);
        //    }

        //    return cachedPrinters;
        //}
    }

}
