using System.Runtime.InteropServices;

namespace WebViewForm
{
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

#pragma warning disable CA1416 // 플랫폼 호환성 유효성 검사
    public class WinAPI
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(int hwnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowRect(int hwnd, ref RECT lpRect);
        // TODO: DwmGetWindowAttribute

        [DllImport("user32.dll")]
        public static extern int MoveWindow(int hwnd, int x, int y, int nWidth, int nHeight, bool bRepaint);
        // TODO: DwmSetWindowAttribute

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(int hwnd, int attr, out RECT rect, int size);
        public static int GetWindowRectWithoutShadow(int hwnd, ref RECT rect)
        {
            return DwmGetWindowAttribute(hwnd, 9/*DWMWA_EXTENDED_FRAME_BOUNDS*/, out rect, Marshal.SizeOf(typeof(RECT)));
        }
        private static RECT shadow = new();
        public static double ratio = 1.0;
        public static RECT GetWindowShadow(int hwnd)
        {
            RECT defaults = new();
            _ = GetWindowRect(hwnd, ref defaults);
            _ = GetWindowRectWithoutShadow(hwnd, ref shadow);
            // top은 그림자가 0이라고 가정
            if (defaults.top > 0)
            {
                ratio = (double)shadow.top / defaults.top;
            }
            defaults.top = (int)(defaults.top * ratio);
            defaults.left = (int)(defaults.left * ratio);
            defaults.right = (int)(defaults.right * ratio);
            defaults.bottom = (int)(defaults.bottom * ratio);

            //shadow.top -= defaults.top;
            shadow.top = 0;
            shadow.left -= defaults.left;
            shadow.right -= defaults.right;
            shadow.bottom -= defaults.bottom;
            return shadow;
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern UIntPtr GetWindowLongPtr(int hwnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        public static extern UIntPtr SetWindowLongPtr32(int hwnd, int nIndex, UIntPtr dwNewLong);

        public static void SetTaskbarHide(int hwnd)
        {
            uint style = GetWindowLongPtr(hwnd, -20).ToUInt32();
            style |= 0x00000080;
            style |= 0x80880000;
            SetWindowLongPtr32(hwnd, -20, new UIntPtr(style));
            /*
            */
        }

        public static void DisableResize(int hwnd)
        {
            uint style = GetWindowLongPtr(hwnd, -20).ToUInt32();
            style &= ~(uint)0x00040000;
            SetWindowLongPtr32(hwnd, -20, new UIntPtr(style));
            /*
            */
        }

        public static void MoveWindow(int hwnd, int x, int y, ref RECT offset)
        {
            GetWindowRect(hwnd, ref offset);
            offset.left += x;
            offset.top += y;
            offset.right += x;
            offset.bottom += y;
            MoveWindow(hwnd
                , offset.left
                , offset.top
                , offset.right - offset.left
                , offset.bottom - offset.top
                , true
            );
        }

        [DllImport("user32.dll")]
        public static extern bool EnableWindow(IntPtr hWnd, bool enable);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // WM_DPICHANGED = 0x02E0 (이 메시지는 실제로 창에 전달될 때 OS가 처리하는 내부 메시지입니다.)
        // 개발자가 직접 이 메시지를 보내는 것이 아니라,
        // OS가 DPI 변경을 감지하도록 다른 방법을 사용해야 합니다.

        // WM_WINDOWPOSCHANGED = 0x0046 를 트리거하여 OS가 위치 변경을 인식하게 할 수 있습니다.
        // 하지만 가장 확실한 방법은 OS가 모니터 간 이동을 '감지'하도록 하는 것입니다.

        // SendMessage로 DPI 변경을 강제하는 것은 까다롭고 위험할 수 있습니다.
        // 대신 WinForms의 공용 메서드를 활용하는 것이 안전합니다.

        // WinAPI.SendMessage(hwnd, WM_WINDOWPOSCHANGED, 0, WINDOWPOS);
    }

    public struct tagWINDOWPOS
    {
        IntPtr hwnd;
        IntPtr hwndInsertAfter;
        int x;
        int y;
        int cx;
        int cy;
        uint flags;
    }
}
