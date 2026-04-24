namespace Qrmenue.DTO
{
    /// <summary>kiosk_settings.json — giriş ekranı ayarları.</summary>
    public class KioskAppSettingsDTO
    {
        public bool RunOnWindowsStartup { get; set; }
        public bool AutoLogin { get; set; }
        /// <summary>TR, DE, FR, EN — app_data_XX.json varsa yüklenir.</summary>
        public string UiLanguage { get; set; }
    }
}
