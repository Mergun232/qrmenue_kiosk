using QRMENUE;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace QrMenu
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static System.Threading.Mutex _mutex;
        
        [STAThread]
        static void Main()
        {
            // Global Mutex kullanarak uygulamanın sadece bir kez çalışmasını sağlıyoruz
            bool createdNew;
            _mutex = new System.Threading.Mutex(true, "Global\\QrmenueKioskApp_UniqueMutex", out createdNew);

            if (!createdNew)
            {
                // Uygulama zaten çalışıyorsa uyarı ver ve çık
                string msg = AppDataLoader.Data?.Text12 ?? "Program zaten çalışıyor!";
                MessageBox.Show(msg, AppDataLoader.Data?.Text10 ?? "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.ThreadException += Application_ThreadException;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppPaths.EnsureWritableDataAndMigrate();
            try
            {
                var ks = KioskSettingsStore.Load();
                KioskSettingsStore.ApplyRunOnStartup(ks.RunOnWindowsStartup);
            }
            catch { }

            Application.Run(new LoginForm());
            
            // Uygulama kapanırken Mutex'i bırak
            _mutex.ReleaseMutex();
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Exception ex = e?.Exception;
            var d = AppDataLoader.Data;
            string msg = ex?.Message ?? (d?.Login_UnknownError ?? "Bilinmeyen hata");
            string stack = ex?.StackTrace ?? "";

            try
            {
                using (var sw = new StreamWriter(AppPaths.ErrorLogPath, true))
                    sw.WriteLine(sender?.ToString() + "##  " + msg + " ## " + stack + " ## " + DateTime.Now.ToString());
            }
            catch { }

            System.Diagnostics.Debug.WriteLine("[EXCEPTION] " + msg + "\r\n" + stack);
            MessageBox.Show(msg, d?.Text10 ?? "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
