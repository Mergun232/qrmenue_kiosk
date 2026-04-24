namespace PrintOptions.Enums
{
    public enum RecieptInfoTypeEnum
    {
        Line = 1,
        Row = 2,
        Seperate = 3,
        Image = 4,
        /// <summary>Fiş JSON'unda bu satırın olduğu yere, RecieptDto.ReceiptJSON dosyasından yüklenen satırlar eklenir.</summary>
        ReceiptJSON = 5
    }
}
