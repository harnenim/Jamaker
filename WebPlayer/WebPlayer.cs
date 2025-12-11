using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media.Media3D;
using WebViewForm;
using static System.Net.WebRequestMethods;

namespace Jamaker
{
    public partial class WebPlayer : WebForm
    {
        private readonly string[] args;
        private string path = "?";

        public WebPlayer(string[] args)
        {
            InitializeAsync("WebPlayer", new Binder(this));
            FormBorderStyle = FormBorderStyle.None;
            try { Icon = new Icon("view/jamaker.ico"); } catch (Exception) { }

            int[] rect = [0, 0, 1280, 800];
            StreamReader? sr = null;
            try
            {   // 설정 파일 경로
                sr = new(Path.Combine(Application.StartupPath, "setting/WebPlayer.txt"), Encoding.UTF8);
                string strSetting = sr.ReadToEnd();
                string[] strRect = strSetting.Split(',');
                if (strRect.Length >= 4)
                {
                    rect[0] = Convert.ToInt32(strRect[0]);
                    rect[1] = Convert.ToInt32(strRect[1]);
                    rect[2] = Convert.ToInt32(strRect[2]);
                    rect[3] = Convert.ToInt32(strRect[3]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                rect[0] = (SystemInformation.VirtualScreen.Width - 1280) / 2;
                rect[1] = (SystemInformation.VirtualScreen.Height - 800) / 2;
            }
            finally { sr?.Close(); }

            StartPosition = FormStartPosition.Manual;
            Location = new Point(rect[0], rect[1]);
            Size = new Size(rect[2], rect[3]);

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            AllowTransparency = true;

            this.args = args;

            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            KeyDown += OnKeyDown;

            FormClosed += WebFormClosed;

            InitWebView();
        }

        public override string GetTitle()
        {
            return "WebPlayer";
        }
        private async void InitWebView()
        {
            mainView.Source = new Uri(Path.Combine(Application.StartupPath, "view/WebPlayer.html"));
        }
        public override void InitAfterLoad(string title)
        {
            base.InitAfterLoad(title);
            Script("setDroppable");
            foreach (string path in args)
            {
                if (path.ToUpper().EndsWith(".AVI")
                 || path.ToUpper().EndsWith(".MKV")
                 || path.ToUpper().EndsWith(".MP4")
                 || path.ToUpper().EndsWith(".WMV"))
                {
                    OpenFile(path);
                    return;
                }
            }
        }

        private void WebFormClosed(object? sender, FormClosedEventArgs e)
        {
            try
            {
                RECT offset = new();
                _ = WinAPI.GetWindowRect(Handle.ToInt32(), ref offset);

                // 설정 폴더 없으면 생성
                DirectoryInfo di = new(Path.Combine(Application.StartupPath, "setting"));
                if (!di.Exists)
                {
                    di.Create();
                }

                StreamWriter sw = new(Path.Combine(Application.StartupPath, "setting/WebPlayer.txt"), false, Encoding.UTF8);
                sw.Write(offset.left + "," + offset.top + "," + (offset.right - offset.left) + "," + (offset.bottom - offset.top));
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Process.GetCurrentProcess().Kill();
        }

        private RECT moveFrom = new();
        private Point mouseFrom = new();
        private int moveFlag;

        #region 리사이즈
        public void StartResizeWindow(int x, int y, string direction)
        {
            Console.WriteLine("StartResizeWindow");
            moveFlag = 0;
            foreach(char c in direction) {
                switch (c)
                {
                    case 'n':
                        moveFlag |= 0b1000;
                        break;
                    case 's':
                        moveFlag |= 0b0001;
                        break;
                    case 'w':
                        moveFlag |= 0b0100;
                        break;
                    case 'e':
                        moveFlag |= 0b0010;
                        break;
                }
            }

            /*
            if (top && left) m.Result = (IntPtr)HTTOPLEFT;
            else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
            else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
            else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
            else if (top) m.Result = (IntPtr)HTTOP;
            else if (bottom) m.Result = (IntPtr)HTBOTTOM;
            else if (left) m.Result = (IntPtr)HTLEFT;
            else if (right) m.Result = (IntPtr)HTRIGHT;
            */

            moveFrom.top = Top;
            moveFrom.left = Left;
            moveFrom.right = Left + Width;
            moveFrom.bottom = Top + Height;
            mouseFrom.X = x; mouseFrom.Y = y;

            StartMove(x, y, 0);
        }

        public void StartMoveWindow(int x, int y)
        {
            Console.WriteLine("StartMoveWindow");
            moveFlag = 0b1111;

            StartMove(x, y, 0);
        }

        private void StartMove(int x, int y, int type)
        {
            moveFrom.top = Top;
            moveFrom.left = Left;
            moveFrom.right = Left + Width;
            moveFrom.bottom = Top + Height;
            mouseFrom.X = x; mouseFrom.Y = y;
            ShowDragging();
            //SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        public void OnMouseMove(object? sender, MouseEventArgs e)
        {
            Console.WriteLine("OnMouseMove");
            if (moveFlag == 0) return;

            int x = e.X - mouseFrom.X;
            int y = e.Y - mouseFrom.Y;

            if ((moveFlag & 0x1000) > 0)
            {   // top
                Top = moveFrom.top + y;
            }
            if ((moveFlag & 0x0100) > 0)
            {   // left
                Left = moveFrom.left + x;
            }
            if ((moveFlag & 0x0010) > 0)
            {   // right
                Width = moveFrom.right + x - Left;
            }
            if ((moveFlag & 0x0001) > 0)
            {   // bottom
                Height = moveFrom.bottom + y - Top;
            }
        }

        public void OnMouseUp(object? sender, MouseEventArgs e)
        {
            Console.WriteLine("OnMouseUp");
            moveFlag = 0;
            HideDragging();
        }

        public void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (moveFlag == 0) return;

            if (e.KeyCode == Keys.Escape)
            {
                Height = moveFrom.bottom - moveFrom.top;
                Width = moveFrom.right - moveFrom.left;
                Left = moveFrom.left;
                Top = moveFrom.top;
                moveFlag = 0;
            }
        }
        #endregion

        #region 메시지 처리
        // 팟플레이어 SDK 값 가져옴
        const int WM_USER = 0x0400;
        const int WM_COPYDATA = 0x004A;
        const int POT_COMMAND = WM_USER;
        const int POT_GET_CURRENT_TIME = 0x5004;
        const int POT_SET_CURRENT_TIME = 0x5005;
        const int POT_SET_PLAY_STATUS  = 0x5007;
        const int POT_SET_PLAY_CLOSE   = 0x5009;
        const int POT_GET_VIDEO_FPS    = 0x6032;
        const int POT_GET_PLAYFILE_NAME = 0x6020;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == POT_COMMAND)
            {
                //Console.WriteLine($"WndProc: {m.Msg} / {m.WParam} / {m.LParam}");

                switch (m.WParam)
                {
                    case POT_SET_PLAY_STATUS:
                        Script("setStatus", (int)m.LParam);
                        return;
                    case POT_SET_PLAY_CLOSE:
                        Script("stop");
                        return;
                    case POT_GET_CURRENT_TIME:
                        m.Result = time;
                        return;
                    case POT_SET_CURRENT_TIME:
                        Script("moveTo", (int)m.LParam);
                        return;
                    case POT_GET_VIDEO_FPS:
                        Script("getFps");
                        return;
                    case POT_GET_PLAYFILE_NAME:
                        try
                        {
                            COPYUTF8STRUCT cds = new()
                            {   dwData = new IntPtr(POT_GET_PLAYFILE_NAME)
                            ,   cbData = Encoding.UTF8.GetBytes(path).Length
                            ,   lpData = path
                            };
                            int hwnd = (int)m.LParam;
                            _ = WinAPI.SendMessage(hwnd, WM_COPYDATA, hwnd, ref cds);
                        }
                        catch (Exception e) { Console.WriteLine(e); }
                        finally { }
                        return;
                    case 0x0010: // 종료는 return 하지 말고 base.WndProc 받아야 함
                        Close();
                        break;
                    /*
                    case WM_NCHITTEST:
                    {
                        // 마우스 포인터의 스크린 좌표 가져오기
                        Point originalPoint = new Point(m.LParam.ToInt32());

                        // 좌표를 폼 기준 클라이언트 좌표로 변환
                        Point clientPoint = PointToClient(originalPoint);

                        // 크기 조절을 위한 경계선 감지 범위 (예: 10 픽셀)
                        int resizeBorderWidth = 10;

                        bool top = clientPoint.Y <= resizeBorderWidth;
                        bool bottom = clientPoint.Y >= this.ClientSize.Height - resizeBorderWidth;
                        bool left = clientPoint.X <= resizeBorderWidth;
                        bool right = clientPoint.X >= this.ClientSize.Width - resizeBorderWidth;

                        // 마우스 위치에 따라 반환할 메시지 결정
                        if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                        else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                        else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                        else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                        else if (top) m.Result = (IntPtr)HTTOP;
                        else if (bottom) m.Result = (IntPtr)HTBOTTOM;
                        else if (left) m.Result = (IntPtr)HTLEFT;
                        else if (right) m.Result = (IntPtr)HTRIGHT;

                        // 만약 특정 영역(예: 상단 패널)에서만 이동을 원한다면
                        // 이 로직에 추가적인 조건문이 필요할 수 있습니다.
                        break;
                    }
                    */
                }
            }
            else if (m.Msg == WM_COPYDATA)
            {
                try
                {
                    byte[] buff = new byte[Marshal.ReadInt32(m.LParam, IntPtr.Size)];
                    IntPtr dataPtr = Marshal.ReadIntPtr(m.LParam, IntPtr.Size * 2);
                    Marshal.Copy(dataPtr, buff, 0, buff.Length);
                    string receive = Encoding.Unicode.GetString(buff);
                    OpenFile(receive);
                }
                catch (Exception e) { Console.WriteLine(e); }
                finally { }
                return;
            }
            base.WndProc(ref m);
        }

        private int time;
        public void SetTime(int time) {  this.time = time; }
        #endregion

        #region 파일
        protected override void Drop(int x, int y)
        {
            try
            {
                foreach (string strFile in droppedFiles!)
                {
                    if (strFile.ToUpper().EndsWith(".AVI")
                     || strFile.ToUpper().EndsWith(".MKV")
                     || strFile.ToUpper().EndsWith(".MP4")
                     || strFile.ToUpper().EndsWith(".WMV"))
                    {
                        OpenFile(strFile);
                        break;
                    }
                }
            }
            catch { }
        }

        public void OpenFileDialog()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { OpenFileDialog(); }));
                return;
            }

            string? filename = null;

            RunDialog(() =>
            {
                OpenFileDialog dialog = new() { Filter = "지원되는 동영상 파일|*.avi;*.mkv;*.mp4;*.wmv" };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    filename = dialog.FileName;
                }
            });

            if (filename != null)
            {
                OpenFile(filename);
            }
        }

        public void OpenFile(string path)
        {
            Script("openFile", this.path = path);
        }
        #endregion
    }
}