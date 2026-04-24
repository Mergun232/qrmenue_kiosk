namespace QRMENUE.Business
{
    class PrintService
    {
        //public PrintService()
        //{

        //}
        //public bool PrintPDF(
        //    string printer,
        //    string paperName,
        //     Stream stream,int pageHeight,int pageWidth)
        //{
        //    try
        //    {
        //        // Create the printer settings for our printer
        //        var printerSettings = new PrinterSettings
        //        {
        //            PrinterName = printer
        //        };

        //        //var paperSize = new PaperSize();
        //        //paperSize.Width = 80;
        //        // Create our page settings for the paper size selected
        //        var pageSettings = new PageSettings(printerSettings)
        //        {
        //            Margins = new Margins(0, 0, 0, 0)

        //        };
        //        foreach (PaperSize paperSize in printerSettings.PaperSizes)
        //        {
        //            if (paperSize.PaperName == paperName)
        //            {
        //                pageSettings.PaperSize = paperSize;
        //                pageSettings.PaperSize.Width = 3;
        //                break;
        //            }
        //        }
        //        var height = 1122;
        //        if (pageHeight !=0)
        //        {
        //            height = pageHeight;
        //        }
        //        var width = 300;
        //        if (pageWidth != 0)
        //        {
        //            width = pageWidth;
        //        }
        //        var psize = new PaperSize("custom", width, height);
        //        pageSettings.PaperSize = psize;
        //        // Now print the PDF document
        //        using (var document = PdfiumViewer.PdfDocument.Load(stream))
        //        {
        //            using (var printDocument = document.CreatePrintDocument())
        //            {
        //                printDocument.PrinterSettings = printerSettings;
        //                printDocument.DefaultPageSettings = pageSettings;
        //                printDocument.PrintController = new StandardPrintController();
        //                printDocument.Print();
        //            }
        //        }
        //        return true;
        //    }
        //    catch (System.Exception e)
        //    {
        //        return false;
        //    }
        //}
        //public void Print(string pdfB64, string printerName, int pageHeight,int pageWidth)
        //{
        //    byte[] pdf = Convert.FromBase64String(pdfB64);

        //    var stream = new MemoryStream(pdf);

        //    var path = /*DateTime.Now.ToString().Replace(" ","") +*/ "test_printer_file.pdf";
        //   // File.WriteAllBytes(path, pdf);

        //    PrintPDF(printerName, "reciept", stream,  pageHeight, pageWidth);
        //}
    }
}
