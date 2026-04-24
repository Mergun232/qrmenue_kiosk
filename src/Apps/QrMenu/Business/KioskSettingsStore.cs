using Microsoft.Win32;
using Qrmenue.DTO;
using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace QRMENUE
{
    public static class KioskSettingsStore
    {
        private const string FileName = "kiosk_settings.json";
        private const string RegistryValueName = "Qiox";

        public static string GetConfigPath()
        {
            return Path.Combine(AppPaths.WritableDataDirectory, FileName);
        }

        public static KioskAppSettingsDTO Load()
        {
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var s = new JavaScriptSerializer().Deserialize<KioskAppSettingsDTO>(json);
                    if (s != null)
                    {
                        if (string.IsNullOrWhiteSpace(s.UiLanguage))
                            s.UiLanguage = "TR";
                        else
                            s.UiLanguage = s.UiLanguage.Trim().ToUpperInvariant();
                        return s;
                    }
                }
            }
            catch { }
            return new KioskAppSettingsDTO
            {
                RunOnWindowsStartup = true,
                AutoLogin = false,
                UiLanguage = "TR"
            };
        }

        public static void Save(KioskAppSettingsDTO settings)
        {
            if (settings == null) return;
            if (string.IsNullOrWhiteSpace(settings.UiLanguage))
                settings.UiLanguage = "TR";
            else
                settings.UiLanguage = settings.UiLanguage.Trim().ToUpperInvariant();
            try
            {
                var ser = new JavaScriptSerializer();
                string json = ser.Serialize(settings);
                File.WriteAllText(GetConfigPath(), json);
            }
            catch { }
        }

        public static void ApplyRunOnStartup(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;
                    try { key.DeleteValue("QRMENUE", false); } catch { }
                    if (enabled)
                        key.SetValue(RegistryValueName, Application.ExecutablePath);
                    else
                        key.DeleteValue(RegistryValueName, false);
                }
            }
            catch { }
        }
    }
}
