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
        [STAThread]
        static void Main()
        {
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

            if (IsInstanceRunning() != null)
            {
                string msg = AppDataLoader.Data?.Text12 ?? "Program zaten \u00e7al\u0131\u015f\u0131yor!";
                MessageBox.Show(msg, AppDataLoader.Data?.Text10 ?? "Hata", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.Run(new LoginForm());
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

        private static Process IsInstanceRunning()
        {
            Process curr = Process.GetCurrentProcess();
            Process[] procs = Process.GetProcessesByName(curr.ProcessName);
            foreach (Process p in procs)
            {
                if ((p.Id != curr.Id) &&
                    (p.MainModule.FileName == curr.MainModule.FileName))
                    return p;
            }
            return null;
        }
    }
}
