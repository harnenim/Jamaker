using System.Runtime.InteropServices;

namespace DpiGetter
{
    public partial class Form : System.Windows.Forms.Form
    {
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);

        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

        const int MONITOR_DEFAULTTONEAREST = 2;
        const int MDT_EFFECTIVE_DPI = 0; // 실제 적용되는 DPI

        public Form(string[] args)
        {
            InitializeComponent();

            if (args.Length < 2) {
                args = ["500", "500"];
            }
            if (int.TryParse(args[0], out int x) && int.TryParse(args[1], out int y))
            {
                Point p = new Point(x, y);
                IntPtr hMonitor = MonitorFromPoint(p, MONITOR_DEFAULTTONEAREST);

                if (hMonitor != IntPtr.Zero)
                {
                    uint dpiX, dpiY;
                    if (GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out dpiX, out dpiY) == 0)
                    {
                        // 성공 시 X DPI 값을 출력합니다. (일반적으로 X, Y DPI는 동일)
                        Console.WriteLine(dpiX);
                    }
                }
            }

            Load += (object? sender, EventArgs e) =>
            {
                Close();
            };
        }
    }
}
