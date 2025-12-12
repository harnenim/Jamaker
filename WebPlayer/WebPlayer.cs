using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WebViewForm;

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
                string[] strSetting = sr.ReadToEnd().Split('\n');
                string[] strRect = strSetting[0].Split(',');
                if (strRect.Length >= 4)
                {
                    rect[0] = Convert.ToInt32(strRect[0]);
                    rect[1] = Convert.ToInt32(strRect[1]);
                    rect[2] = Convert.ToInt32(strRect[2]);
                    rect[3] = Convert.ToInt32(strRect[3]);
                }
                if (strSetting.Length > 1)
                {
                    path = strSetting[1];
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

            ResizeEnd += OnResizeEnd;
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
                    OpenFile(path, false);
                    return;
                }
            }
            OpenFile(path, false);
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
                sw.Write($"{offset.left},{offset.top},{offset.right - offset.left},{offset.bottom - offset.top}");
                sw.Write($"\n{path}");
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Process.GetCurrentProcess().Kill();
        }

        public void StartResizeWindow(int type)
        {
            WinAPI.ReleaseCapture();
            _ = WinAPI.SendMessage((int)Handle, 0xA1/*WM_NCLBUTTONDOWN*/, type, 0);
        }

        private void OnResizeEnd(object? sender, EventArgs e)
        {
            Script("endResize");
        }

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
                    OpenFile(receive, true);
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
                        OpenFile(strFile, true);
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
                OpenFile(filename, true);
            }
        }

        public void OpenFile(string path, bool withPlay)
        {
            Script("openFile", this.path = path, withPlay);
        }
        #endregion
    }
}