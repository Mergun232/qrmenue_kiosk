namespace Qrmenue.DTO
{
    /// <summary>Giriş sonrası MainForm/Chromium için kullanılan model. LoginToken yeni exe API için zorunludur.</summary>
    public class LoginResultDTO
    {
        public string result { get; set; }
        public string title { get; set; }
        public string FirmaID { get; set; }
        public string btn { get; set; }
        public string Token { get; set; }
        public bool Browser { get; set; }
        public string Url { get; set; }
        public string SocketConnectUrl { get; set; }
        public string FirmaCode { get; set; }
        public bool PrinterStart { get; set; }
        public string KullaniciID { get; set; }
        /// <summary>api/exe için tüm isteklerde header'da gönderilir.</summary>
        public string LoginToken { get; set; }
    }
}
