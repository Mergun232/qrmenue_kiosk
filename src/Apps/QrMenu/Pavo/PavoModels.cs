using System.Collections.Generic;

namespace QRMENUE.Pavo
{
    /// <summary>Her API isteğinde gönderilmesi zorunlu işlem tanımlayıcısı.</summary>
    public class TransactionHandle
    {
        public string SerialNumber { get; set; } = "";
        public string TransactionDate { get; set; } = "";
        public int TransactionSequence { get; set; }
        public string Fingerprint { get; set; } = "";
    }

    /// <summary>Satış kalemi.</summary>
    public class SaleItem
    {
        public string Name { get; set; } = "";
        public bool IsGeneric { get; set; }
        public string UnitCode { get; set; } = "KGM";
        public string TaxGroupCode { get; set; } = "KDV18";
        public decimal ItemQuantity { get; set; }
        public decimal UnitPriceAmount { get; set; }
        public decimal GrossPriceAmount { get; set; }
        public decimal TotalPriceAmount { get; set; }
    }

    /// <summary>Ödeme bilgisi. Mediator: 1=Nakit, 2=Kredi Kartı, 9=Harici.</summary>
    public class PaymentInformation
    {
        public int Mediator { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "TRY";
        public decimal ExchangeRate { get; set; } = 1m;
    }

    /// <summary>PrintOut için yazdırma isteği.</summary>
    public class PrintRequest
    {
        public string Image { get; set; } = "";
    }
}
