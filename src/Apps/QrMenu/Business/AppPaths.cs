using System;
using System.IO;
using System.Windows.Forms;

namespace QRMENUE
{
    /// <summary>
    /// Kurulum dizini (ör. Program Files\QRMENUE) salt okunur; yazılabilir dosyalar LocalAppData altında.
    /// </summary>
    public static class AppPaths
    {
        private const string LocalAppFolderName = "Qiox";

        /// <summary>Setup ile kopyalanan exe ve images, app_data.json burada.</summary>
        public static string InstallDirectory
        {
            get
            {
                string p = Application.StartupPath ?? AppDomain.CurrentDomain.BaseDirectory ?? "";
                return p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
        }

        /// <summary>Kullanıcı yazabilir kök: %LocalAppData%\Qiox</summary>
        public static string WritableDataDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LocalAppFolderName);

        public static string ConfigJsonPath => Path.Combine(WritableDataDirectory, "config.json");

        public static string KioskSettingsPath => Path.Combine(WritableDataDirectory, "kiosk_settings.json");

        public static string WebView2UserDataFolder => Path.Combine(WritableDataDirectory, "WebView2");

        public static string ErrorLogPath => Path.Combine(WritableDataDirectory, "errorLog.txt");

        public static string PavoPosConfigPath => Path.Combine(WritableDataDirectory, "pavopos.json");

        public static string ReceiptsDirectory => Path.Combine(WritableDataDirectory, "Receipts");

        /// <summary>Uygulama başında çağrılır: klasörleri oluşturur, eski portable dosyaları bir kez taşır.</summary>
        public static void EnsureWritableDataAndMigrate()
        {
            try
            {
                Directory.CreateDirectory(WritableDataDirectory);
                Directory.CreateDirectory(WebView2UserDataFolder);
                Directory.CreateDirectory(ReceiptsDirectory);

                string inst = InstallDirectory;
                if (string.IsNullOrEmpty(inst)) return;

                TryMigrateFile(Path.Combine(inst, "config.json"), ConfigJsonPath);
                TryMigrateFile(Path.Combine(inst, "kiosk_settings.json"), KioskSettingsPath);
                TryMigrateFile(Path.Combine(inst, "pavopos.json"), PavoPosConfigPath);
                TryMigrateFile(Path.Combine(inst, "errorLog.txt"), ErrorLogPath);
            }
            catch { }
        }

        private static void TryMigrateFile(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return;
            if (string.Equals(Path.GetFullPath(from), Path.GetFullPath(to), StringComparison.OrdinalIgnoreCase)) return;
            if (!File.Exists(from) || File.Exists(to)) return;
            try { File.Copy(from, to, false); } catch { }
        }
    }
}
