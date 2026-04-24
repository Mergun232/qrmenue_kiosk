namespace Qrmenue.DTO
{
    /// <summary>POST /api/exe/login yanıtı (API_EXE_DOCUMENTATION.md).</summary>
    public class LoginResponseExeDTO
    {
        public int status { get; set; }
        public string result { get; set; }
        public string btn { get; set; }
        public string title { get; set; }
        public string text1 { get; set; }
        public string text2 { get; set; }
        public LoginResponseDataList DataList { get; set; }
    }

    public class LoginResponseDataList
    {
        public string ApiUrl { get; set; }
        public string Lang { get; set; }
        public string FirmaName { get; set; }
        public int FirmaID { get; set; }
        public string FirmaCode { get; set; }
        public string LoginToken { get; set; }
        public string WebviewLink { get; set; }
        public string SocketConnectUrl { get; set; }
        public int KullaniciID { get; set; }
        public bool Browser { get; set; }
        public bool Printer { get; set; }
        public bool CallerID { get; set; }
        public LoginResponsePersonel Personel { get; set; }
    }

    public class LoginResponsePersonel
    {
        public string NameSurname { get; set; }
        public string KullaniciNick { get; set; }
        public int Durum { get; set; }
        public string KayitTarihi { get; set; }
        public string SonGiris { get; set; }
    }
}
