using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WebViewForm;

namespace Jamaker
{
    internal static class Program
    {
        // kernel32.dll에서 AllocConsole 및 FreeConsole 함수 임포트
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string ProcName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(ProcName);
            if (processes.Length > 1)
            {
                if (args.Length > 0)
                {
                    string path = args[0];
                    COPYUTF8STRUCT cds = new()
                    {   dwData = new IntPtr(1000)
                    ,   cbData = Encoding.UTF8.GetBytes(path).Length
                    ,   lpData = path
                    };
                    int hwnd = (int)processes[0].MainWindowHandle;
                    _ = WinAPI.SendMessage(hwnd, 0x004A/*WM_COPYDATA*/, hwnd, ref cds);
                }
                else
                {
                    MessageBox.Show($"{ProcName}는 이미 실행 중입니다.", "Jamaker");
                }
                Process.GetCurrentProcess().Kill();
            }

            bool showConsole = File.Exists(Path.Combine(Application.StartupPath, $"setting/.ShowConsole"));
            if (showConsole) AllocConsole();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(args));

            if (showConsole) FreeConsole();
        }
    }
}