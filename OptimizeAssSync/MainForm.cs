using System.Diagnostics;
using System.Text;
using WebViewForm;

namespace Jamaker
{
    public partial class MainForm : WebForm
    {
        private readonly string[] args;

        public MainForm(string[] args)
        {
            InitializeAsync("AssSyncOptimizer", new Binder(this));
            try { Icon = new Icon("view/jamaker.ico"); } catch (Exception) { }

            int[] rect = [0, 0, 1280, 800];
            StreamReader? sr = null;
            try
            {   // 설정 파일 경로
                sr = new(Path.Combine(Application.StartupPath, "setting/AssSyncOptimizer.txt"), Encoding.UTF8);
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
            mainView.Source = new Uri(Path.Combine(Application.StartupPath, "view/AssSyncOptimizer.html"));
        }
        public override void InitAfterLoad(string title)
        {
            base.InitAfterLoad(title);
            LoadFiles(args);
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

                StreamWriter sw = new StreamWriter(Path.Combine(Application.StartupPath, "setting/AssSyncOptimizer.txt"), false, Encoding.UTF8);
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

        public void CheckAndMakeFkf(string path)
        {
            try
            {
                string selector = "#labelVideo";
                FileInfo info = new FileInfo(path);
                string fkfName = $"{info.Name.Substring(0, info.Name.Length - info.Extension.Length)}.{info.Length}.fkf";

                // 기존에 있으면 가져오기
                try
                {
                    DirectoryInfo di = new DirectoryInfo(Path.Combine(Application.StartupPath, "temp"));
                    if (di.Exists)
                    {
                        VideoInfo.FromFkfFile(Path.Combine(Application.StartupPath, "temp/fkf/" + fkfName));
                        Script("Progress.set", selector, 0);
                        Script("setFkfFile", fkfName);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if ((VideoInfo.CheckFFmpeg() & 3) == 3)
                {
                    // 없으면 새로 가져오기
                    new VideoInfo(path, (double ratio) =>
                    {
                        Script("Progress.set", selector, ratio);
                    }).RefreshInfo((VideoInfo videoInfo) =>
                    {
                        videoInfo.ReadKfs(true);
                        videoInfo.SaveFkf(Path.Combine(Application.StartupPath, "temp/fkf/" + fkfName));
                        Script("Progress.set", selector, 0);
                        Script("setFkfFile", fkfName);
                    });
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
        }

        #region 파일
        public void DropFiles()
        {
            LoadFiles(droppedFiles!);
        }
        public void LoadFiles(string[] files)
        {
            bool hasAss = false;
            bool hasVideo = false;
            foreach (string file in files)
            {
                string ext = file.ToLower();

                if (ext.EndsWith(".ass"))
                {
                    if (hasAss) continue;

                    StreamReader? sr = null;
                    try
                    {
                        sr = new StreamReader(file, DetectEncoding(file));
                        string text = sr.ReadToEnd();

                        hasAss = true;
                        Script("setAssFile", file, text);

                        string[] lines = text.Split('\n');
                        // TODO: 비디오 파일 정보 있을 때 처리
                    }
                    catch
                    {
                        return;
                    }
                    finally { sr?.Close(); }

                }
                else if (ext.EndsWith(".avi")
                      || ext.EndsWith(".mp4")
                      || ext.EndsWith(".mkv")
                      || ext.EndsWith(".ts")
                      || ext.EndsWith(".m2ts"))
                {
                    if (hasVideo) continue;
                    hasVideo = true;
                    Script("setVideoFile", file);
                    CheckAndMakeFkf(file);
                }
                else if (ext.EndsWith(".fkf"))
                {
                    if (hasVideo) continue;
                    hasVideo = true;
                    Script("setVideoFile", file);
                    Script("setFkfFile", file);
                }
            }
            droppedFiles = null;
        }
        public void Save(string path, string text)
        {
            StreamWriter? sw = null;
            try
            {   // 무조건 UTF-8로 저장
                (sw = new StreamWriter(path, false, Encoding.UTF8)).Write(text);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Script("alert", "저장되지 않았습니다.");
            }
            finally
            {
                sw?.Close();
            }
        }
        #endregion
    }
}