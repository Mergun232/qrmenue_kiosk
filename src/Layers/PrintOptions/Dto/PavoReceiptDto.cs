using System.Collections.Generic;

namespace PrintOptions.Dto
{
    public class PavoReceiptData
    {
        public List<PavoReceiptItem> customerReceipt1 { get; set; }
    }

    public class PavoReceiptItem
    {
        public string field { get; set; }
        public string type { get; set; }
        public string fontSize { get; set; }
        public double? leftMargin { get; set; }
        public double? rightMargin { get; set; }
        public string leftText { get; set; }
        public string centerText { get; set; }
        public string rightText { get; set; }
        public bool? isBold { get; set; }
        public string imageData { get; set; }
        public string qrData { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
    }
}
