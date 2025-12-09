using System.Runtime.InteropServices;

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