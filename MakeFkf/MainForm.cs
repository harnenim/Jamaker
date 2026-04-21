using Microsoft.WindowsAPICodePack.Taskbar;
using System.Diagnostics;
using System.Text;
using WebViewForm;

namespace Jamaker
{
    public partial class MainForm : WebForm
    {
        private readonly string settingJson = "{\"filters\":\"*.txt, *.smi, *.sami, *.ass, *.jmk\",\"replacers\":[{\"use\":true,\"from\":\"다시 한번\",\"to\":\"다시 한 번\"},{\"use\":true,\"from\":\"그리고 보니\",\"to\":\"그러고 보니\"},{\"use\":true,\"from\":\"뒤쳐\",\"to\":\"뒤처\"},{\"use\":true,\"from\":\"제 정신\",\"to\":\"제정신\"},{\"use\":true,\"from\":\"스탠드 얼론\",\"to\":\"스탠드얼론\"},{\"use\":true,\"from\":\"멘테넌스\",\"to\":\"메인터넌스\"},{\"use\":true,\"from\":\"뒷처리\",\"to\":\"뒤처리\"},{\"use\":true,\"from\":\"스탭도\",\"to\":\"스태프도\"},{\"use\":true,\"from\":\"등 져선\",\"to\":\"등져선\"},{\"use\":true,\"from\":\"타코이즈\",\"to\":\"터쿼이즈\"},{\"use\":true,\"from\":\"쓰레드\",\"to\":\"스레드\"},{\"use\":true,\"from\":\"져버리지\",\"to\":\"저버리지\"},{\"use\":true,\"from\":\"글러먹\",\"to\":\"글러 먹\"}]}";
        private readonly string[] args;

        public MainForm(string[] args)
        {
            InitializeAsync("MakeFkf", new Binder(this));
            try { Icon = new Icon("view/jamaker.ico"); } catch (Exception) { }

            int[] rect = [0, 0, 1280, 800];
            StreamReader? sr = null;
            try
            {   // 설정 파일 경로
                sr = new(Path.Combine(Application.StartupPath, "setting/MakeFkf.txt"), Encoding.UTF8);
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

        private async void InitWebView()
        {
            mainView.Source = new Uri(Path.Combine(Application.StartupPath, "view/MakeFkf.html"));
        }
        public override void InitAfterLoad(string title)
        {
            base.InitAfterLoad(title);
            CheckFFmpegWithAlert();
            Script("init", settingJson);
            AddFiles(args);
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

                StreamWriter sw = new(Path.Combine(Application.StartupPath, "setting/MakeFkf.txt"), false, Encoding.UTF8);
                sw.Write(offset.left + "," + offset.top + "," + (offset.right - offset.left) + "," + (offset.bottom - offset.top));
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Process.GetCurrentProcess().Kill();
        }

        protected override void Drop(int x, int y)
        {
            Script("drop", x, y);
        }

        string[] videos = [];
        public void MakeFkfs(string[] files)
        {
            Script("showProcessing");
            videos = files;
            Invoke(() => { TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, Handle); });
            new Thread(() => { MakeFkf(0); }).Start();
        }

        private void MakeFkf(int index)
        {
            if (index < videos.Length)
            {
                Invoke(() => { TaskbarManager.Instance.SetProgressValue(index, videos.Length, Handle); });
                try
                {
                    Script("scrollTo", index);

                    string selector = $"#listFiles li:eq({index})";
                    string path = videos[index];
                    FileInfo info = new(path);
#pragma warning disable IDE0057 // 범위 연산자 사용
                    string fkfName = $"{info.Name.Substring(0, info.Name.Length - info.Extension.Length)}.{info.Length}.fkf";
#pragma warning restore IDE0057 // 범위 연산자 사용

                    // 기존에 있으면 가져오기
                    try
                    {
                        DirectoryInfo di = new(Path.Combine(Application.StartupPath, "temp/fkf"));
                        if (di.Exists)
                        {
                            VideoInfo.FromFkfFile(Path.Combine(Application.StartupPath, "temp/fkf/" + fkfName));
                            Script("Progress.set", selector, 1);
                            MakeFkf(index + 1);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    // 없으면 새로 가져오기
                    if ((VideoInfo.CheckFFmpeg() & 3) == 3)
                    {
                        new VideoInfo(path, (ratio) =>
                        {
                            Script("Progress.set", selector, ratio);
                        }).RefreshInfo((videoInfo) =>
                        {
                            videoInfo.ReadKfs(true);
                            videoInfo.SaveFkf(Path.Combine(Application.StartupPath, "temp/fkf/" + fkfName));
                            Script("Progress.set", selector, 1);
                            MakeFkf(index + 1);
                        });
                    }
                    else
                    {
                        MakeFkf(index + 1);
                    }
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
            else
            {
                Invoke(() => { TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, Handle); });
                Script("hideProcessing");
            }
        }

        #region 파일
        public int CheckFFmpegWithAlert()
        {
            int status = VideoInfo.CheckFFmpeg();
            switch (status)
            {
                case 2: Script("alert", "ffmpeg.exe 파일이 없습니다."); break;
                case 1: Script("alert", "ffprobe.exe 파일이 없습니다."); break;
                case 0: Script("alert", "ffmpeg.exe, ffprobe.exe 파일이 없습니다."); break;
            }
            return status;
        }
        public void AddFilesByDrag()
        {
            AddFiles(droppedFiles!);
        }
        public void AddFiles(string[] files)
        {
            foreach (string file in files)
            {
                GetFilesWithSubDir(file);
            }
        }
        public void GetFilesWithSubDir(string file)
        {
            if (Directory.Exists(file))
            {
                DirectoryInfo dir = new(file);
                DirectoryInfo[] subDirs = dir.GetDirectories();
                foreach (DirectoryInfo subDir in subDirs)
                {
                    GetFilesWithSubDir(subDir.FullName);
                }
                FileInfo[] subFiles = dir.GetFiles();
                foreach (FileInfo subFile in subFiles)
                {
                    Script("addFile", subFile.FullName);
                }
            }
            else
            {
                Script("addFile", file);
            }
        }
        #endregion
    }
}