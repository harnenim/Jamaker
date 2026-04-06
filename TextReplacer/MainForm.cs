using System.Diagnostics;
using System.Text;
using WebViewForm;

namespace Jamaker
{
    public partial class MainForm : WebForm
    {
        private string settingJson = "{\"filters\":\"*.txt, *.smi, *.sami, *.ass, *.jmk\",\"replacers\":[{\"use\":true,\"from\":\"다시 한번\",\"to\":\"다시 한 번\"},{\"use\":true,\"from\":\"그리고 보니\",\"to\":\"그러고 보니\"},{\"use\":true,\"from\":\"뒤쳐\",\"to\":\"뒤처\"},{\"use\":true,\"from\":\"제 정신\",\"to\":\"제정신\"},{\"use\":true,\"from\":\"스탠드 얼론\",\"to\":\"스탠드얼론\"},{\"use\":true,\"from\":\"멘테넌스\",\"to\":\"메인터넌스\"},{\"use\":true,\"from\":\"뒷처리\",\"to\":\"뒤처리\"},{\"use\":true,\"from\":\"스탭도\",\"to\":\"스태프도\"},{\"use\":true,\"from\":\"등 져선\",\"to\":\"등져선\"},{\"use\":true,\"from\":\"타코이즈\",\"to\":\"터쿼이즈\"},{\"use\":true,\"from\":\"쓰레드\",\"to\":\"스레드\"},{\"use\":true,\"from\":\"져버리지\",\"to\":\"저버리지\"},{\"use\":true,\"from\":\"글러먹\",\"to\":\"글러 먹\"}]}";
        private readonly string[] args;

        public MainForm(string[] args)
        {
            InitializeAsync("TextReplacer", new Binder(this));
            try { Icon = new Icon("view/jamaker.ico"); } catch (Exception) { }

            int[] rect = [0, 0, 1280, 800];
            StreamReader? sr = null;
            try
            {   // 설정 파일 경로
                sr = new(Path.Combine(Application.StartupPath, "setting/TextReplacer.txt"), Encoding.UTF8);
                string strSetting = sr.ReadToEnd();
                string[] strRect = strSetting.Split(',');
                if (strRect.Length >= 4)
                {
                    rect[0] = Convert.ToInt32(strRect[0]);
                    rect[1] = Convert.ToInt32(strRect[1]);
                    rect[2] = Convert.ToInt32(strRect[2]);
                    rect[3] = Convert.ToInt32(strRect[3]);
                }
                if (strSetting.IndexOf('\n') > 0)
                {
                    settingJson = strSetting[(strSetting.IndexOf('\n') + 1)..];
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

            FormClosing += new FormClosingEventHandler(BeforeExit);
            FormClosed += new FormClosedEventHandler(WebFormClosed);

            InitWebView();
        }

        private async void InitWebView()
        {
            mainView.Source = new Uri(Path.Combine(Application.StartupPath, "view/TextReplacer.html"));
        }
        public override void InitAfterLoad(string title)
        {
            base.InitAfterLoad(title);
            Script("init", settingJson);
            AddFiles(args);
        }

        private void BeforeExit(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Script("beforeExit");
        }

        public void ExitAfterSaveSetting(string setting)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { ExitAfterSaveSetting(setting); }));
                return;
            }

            StreamWriter? sw = null;
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

                sw = new StreamWriter(Path.Combine(Application.StartupPath, "setting/TextReplacer.txt"), false, Encoding.UTF8);
                sw.Write(offset.left + "," + offset.top + "," + (offset.right - offset.left) + "," + (offset.bottom - offset.top) + ",\n" + setting);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                sw?.Close();
            }

            Process.GetCurrentProcess().Kill();
        }

        private void WebFormClosed(object? sender, FormClosedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        protected override void Drop(int x, int y)
        {
            Script("drop", x, y);
        }

        public void Compare(string file, string[] froms, string[] tos)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { Compare(file, froms, tos); }));
                return;
            }

            string? text = null;
            StreamReader? sr = null;
            try
            {
                sr = new StreamReader(file, DetectEncoding(file));
                text = sr.ReadToEnd();
            }
            catch
            {
                Script("alert", "파일을 열지 못했습니다.");
                return;
            }
            finally { sr?.Close(); }

            Replaced result = new(text, froms, tos);
            if (result == null) return;

            Script("showPreview", result.PreviewOrigin(), result.PreviewResult());
            if (result.count == 0)
            {
                Script("alert", "치환한 문자열이 없습니다.");
            }

        }
        public void Replace(string[] files, string[] froms, string[] tos)
        {
            new Thread(() =>
            {
                Script("showDragging");
                Script("progress.set", 0, files.Length);

                string? text = null;
                StreamReader? sr = null;
                StreamWriter? sw = null;
                int fileCount = 0, count = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    Encoding encoding = DetectEncoding(file);
                    try
                    {
                        sr = new StreamReader(file, encoding);
                        text = sr.ReadToEnd();
                    }
                    catch
                    {
                        continue;
                    }
                    finally { sr?.Close(); }

                    Replaced result = new(text, froms, tos);
                    if (result == null) continue;

                    if (result.count == 0) continue;

                    try
                    {
                        // 원본 파일의 인코딩대로 저장
                        sw = new StreamWriter(file, false, encoding);
                        sw.Write(result.result);

                        // 성공 후 카운트
                        count += result.count;
                        fileCount++;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        sw?.Close();
                    }
                    Script("progress.set", i, files.Length);
                }

                Script("progress.hide");
                Script("hideDragging");

                string msg = "변환된 파일이 없습니다.";
                if (count > 0)
                {
                    msg = $"{files.Length}개 파일 중 {fileCount}개 파일에 대해\n{count}곳을 치환했습니다.";
                }
                Script("alert", msg);
            }).Start();
        }

        #region 설정
        public void ImportSetting()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { ImportSetting(); }));
                return;
            }

            OpenFileDialog dialog = new() { Filter = "JSON 파일|*.json" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string path = dialog.FileName;
                string text = "";
                StreamReader? sr = null;
                try
                {
                    sr = new StreamReader(path, Encoding.UTF8);
                    text = sr.ReadToEnd();
                }
                catch
                {
                    Script("alert", "파일을 열지 못했습니다.");
                }
                finally { sr?.Close(); }

                Script("init", settingJson = text);
            }
        }

        public void ExportSetting(string setting)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { ExportSetting(setting); }));
                return;
            }

            SaveFileDialog dialog = new() { Filter = "JSON 파일|*.json" };
            if (dialog.ShowDialog() != DialogResult.OK)
            {   // 저장 취소
                return;
            }
            string path = dialog.FileName;

            StreamWriter? sw = null;
            try
            {   // 무조건 UTF-8로 저장
                (sw = new StreamWriter(path, false, Encoding.UTF8)).Write(setting);
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

        #region 파일
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
        public void LoadSettingByDrag()
        {
            StreamReader? sr = null;
            try
            {
                sr = new StreamReader(droppedFiles![0], Encoding.UTF8);
                Script("init", sr.ReadToEnd());
            }
            catch
            {
                Script("alert", "파일을 열지 못했습니다.");
            }
            finally { sr?.Close(); }
        }

        public void Save(string dir, string name, string text)
        {
            StreamWriter? sw = null;
            try
            {   // 무조건 UTF-8로 저장
                (sw = new StreamWriter(dir + "/" + name, false, Encoding.UTF8)).Write(text);
                Script("alert", "저장했습니다.");
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
    public class Replaced
    {
        public readonly string origin;
        private readonly List<int[]> originReplaced = [];
        public readonly string result;
        private readonly List<int[]> resultReplaced = [];
        public readonly int count = 0;

        public Replaced(string text, string[] froms, string[] tos)
        {
            origin = result = text;

            for (int i = 0; i < froms.Length; i++)
            {
                int pos = 0, rPos;
                while ((rPos = result.IndexOf(froms[i], pos, StringComparison.Ordinal)) >= 0)
                {
                    int rIndex = 0;
                    for (; rIndex < resultReplaced.Count; rIndex++)
                    {
                        if (rPos <= resultReplaced[rIndex][0])
                        {
                            break;
                        }
                    }
                    int before = rIndex - 1;
                    int addLength;
                    if (before >= 0 && rPos <= resultReplaced[before][1])
                    { // 앞이랑 겹칠 때
                        int oEnd = rPos - resultReplaced[before][1] + originReplaced[before][1] + froms[i].Length;
                        if (oEnd < originReplaced[before][1])
                        {
                            resultReplaced[before][1] += tos[i].Length - froms[i].Length;
                        }
                        else
                        {
                            resultReplaced[before][1] = rPos + tos[i].Length;
                            originReplaced[before][1] = oEnd;
                        }
                        rIndex--;
                    }
                    else
                    {
                        addLength = (before >= 0) ? originReplaced[before][1] - resultReplaced[before][1] : 0;
                        originReplaced.Insert(rIndex, [rPos + addLength, rPos + froms[i].Length + addLength]);
                        resultReplaced.Insert(rIndex, [rPos, rPos + tos[i].Length]);
                    }
                    addLength = tos[i].Length - froms[i].Length;
                    for (int j = rIndex + 1; j < resultReplaced.Count; j++)
                    {
                        resultReplaced[j][0] += addLength;
                        resultReplaced[j][1] += addLength;
                    }
                    if (rIndex + 1 < resultReplaced.Count)
                    {
                        if (originReplaced[rIndex][1] >= originReplaced[rIndex + 1][0])
                        { // 뒤랑 겹칠 때
                            originReplaced[rIndex + 1][0] = originReplaced[rIndex][0];
                            resultReplaced[rIndex + 1][0] = resultReplaced[rIndex][0];
                            originReplaced.RemoveAt(rIndex);
                            resultReplaced.RemoveAt(rIndex);
                        }
                    }
                    result = string.Concat(result.AsSpan(0, rPos), tos[i], result.AsSpan(rPos + froms[i].Length));
                    pos = rPos + tos[i].Length;
                    count++;
                }
            }
        }
        public string PreviewOrigin()
        {
            return Preview(origin, originReplaced);
        }
        public string PreviewResult()
        {
            return Preview(result, resultReplaced);
        }
        private static string Preview(string text, List<int[]> replacedList)
        {
            string preview = "";
            int last = 0;
            for (int i = 0; i < replacedList.Count; i++)
            {
                int[] replaced = replacedList[i];
                if (last < replaced[0])
                {
                    preview += (EscapeHtml(text[last..replaced[0]]));
                }
                preview += ("<span class='highlight'>" + EscapeHtml(text[replaced[0]..replaced[1]]) + "</span>");
                last = replaced[1];
            }
            if (last < text.Length)
            {
                preview += (EscapeHtml(text[last..]));
            }
            return preview;
        }
        private static string EscapeHtml(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}