using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WebViewForm;

namespace Jamaker
{
    public partial class WebPlayer : WebForm
    {
        private readonly string[] args;
        private string? path = null;

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

            FormClosed += new FormClosedEventHandler(WebFormClosed);

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
                RECT offset = new RECT();
                WinAPI.GetWindowRect(Handle.ToInt32(), ref offset);

                // 설정 폴더 없으면 생성
                DirectoryInfo di = new DirectoryInfo(Path.Combine(Application.StartupPath, "setting"));
                if (!di.Exists)
                {
                    di.Create();
                }

                StreamWriter sw = new StreamWriter(Path.Combine(Application.StartupPath, "setting/WebPlayer.txt"), false, Encoding.UTF8);
                sw.Write(offset.left + "," + offset.top + "," + (offset.right - offset.left) + "," + (offset.bottom - offset.top));
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Process.GetCurrentProcess().Kill();
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
        const int POT_SET_PLAYFILE = 1000; // UTF-16 Unicode
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == POT_COMMAND)
            {
                //Console.WriteLine($"WndProc: {m.Msg} / {m.WParam} / {m.LParam}");

                switch (m.WParam)
                {
                    case POT_SET_PLAY_STATUS:
                        Script("setStatus", (int)m.LParam);
                        break;
                    case POT_SET_PLAY_CLOSE:
                        Script("stop");
                        break;
                    case POT_GET_CURRENT_TIME:
                        m.Result = time;
                        break;
                    case POT_SET_CURRENT_TIME:
                        Script("moveTo", (int)m.LParam);
                        break;
                    case POT_GET_VIDEO_FPS:
                        Script("getFps");
                        break;
                    case POT_GET_PLAYFILE_NAME:
                        try
                        {
                            string path = (this.path == null) ? "" : this.path;
                            COPYUTF8STRUCT cds = new COPYUTF8STRUCT
                            {   dwData = new IntPtr(POT_GET_PLAYFILE_NAME)
                            ,   cbData = Encoding.UTF8.GetBytes(path).Length
                            ,   lpData = path
                            };
                            int hwnd = (int)m.LParam;
                            WinAPI.SendMessage(hwnd, WM_COPYDATA, hwnd, ref cds);
                        }
                        catch (Exception e) { Console.WriteLine(e); }
                        finally { }
                        break;
                    case POT_SET_PLAYFILE:
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
                        break;
                    case 0x0010:
                        Close();
                        break;
                }
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