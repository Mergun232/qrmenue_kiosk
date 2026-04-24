using System.Collections.Generic;

namespace PrintOptions.Dto
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 

    public class FontInfo
    {
        public string Align { get; set; }
        public string FontSize { get; set; }
        public string FontStyle { get; set; }
        public string FontName { get; set; }

        public string Color { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }

    }

    public class Column
    {
        public string Text { get; set; }
        public string Length { get; set; }

        public FontInfo FontInfo { get; set; }
    }

    public class RecieptInfo
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public FontInfo FontInfo { get; set; }
        public List<Column> Columns { get; set; }
    }

    public class PrinterData
    {
        public FontInfo DefaultFont { get; set; }
        public List<RecieptInfo> RecieptInfos { get; set; }
    }

    public class RecieptDto
    {
        public string PrinterID { get; set; }
        public string PrinterName { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        public int? FixedWidth { get; set; }
        public PrinterData PrinterData { get; set; }
        public string ReceiptImage { get; set; }
        public string ReceiptJSON { get; set; }

    }

    public class Root
    {

        public List<RecieptDto> RecieptDto { get; set; }
    }


}
