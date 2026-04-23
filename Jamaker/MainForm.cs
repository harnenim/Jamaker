using Jamaker.addon;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using WebViewForm;

namespace Jamaker
{
    public partial class MainForm : WebForm
    {
        public PlayerBridge.PlayerBridge? player = null;
        private bool useMovePlayer = false;

        private string strSettingJson = "불러오기 실패 예제";
        private string strBridgeList = "NoPlayer: (없음)"; // 기본값
        private string strHighlights = "SyncOnly: 싱크 줄 구분\neclipse: 이클립스 스타일";
        private readonly Dictionary<string, string> bridgeDlls = [];
        private readonly string[] args;

        public MainForm(string[] args)
        {
            Opacity = 0;
            _ = WinAPI.MoveWindow(Handle.ToInt32(), -10000, -10000, Width, Height, true);

            InitializeAsync("Jamaker", new Binder(this));
            Icon = Properties.Resources.JamakerIcon;
            MaximizeBox = false;

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            AllowTransparency = true;

            LoadSetting();

            this.args = args;

            timer.Interval = 10;
            timer.Enabled = true;
            timer.Tick += FollowWindow;
            timer.Tick += RefreshPlayer;
            timer.Tick += SaveLogs;
            timer.Start();

            FormClosing += new FormClosingEventHandler(BeforeExit);
            FormClosed += new FormClosedEventHandler(WebFormClosed);

            // 마지막 섬네일 정보 가져오기
            new Thread(() => { LoadThumbnailInfo(); }).Start();

            InitWebView();
        }

        public override string GetTitle()
        {
            return "Jamaker";
        }

        private async void InitWebView()
        {
            mainView.Source = new Uri(Path.Combine(Application.StartupPath, "view/Jamaker.html"));
        }
        public override void InitAfterLoad(string title)
        {
            base.InitAfterLoad(title);
            try
            {
                Script("init", strSettingJson, false); // C#에서 객체 그대로 못 보내주므로 json string 만드는 걸로
                Script("setPlayerDlls", strBridgeList); // 플레이어 브리지 추가 가능토록
                Script("setHighlights", strHighlights);
                Script("setDroppable");

                _ = WinAPI.GetWindowRectWithoutShadow(windows["editor"], ref lastOffset);
            }
            catch { }
        }
        public override void AfterOpenPopup(string name, PopupForm popup, bool reuse)
        {
            switch (name)
            {
                case "viewer":
                    if (!reuse)
                    {
                        popup.ShowInTaskbar = false;
                        popup.MaximizeBox = false;
                        popup.MinimizeBox = false;
                        popup.fixedUrl = true;
                    }
                    break;
                case "finder":
                    if (!reuse)
                    {
                        popup.ShowInTaskbar = false;
                        popup.MaximizeBox = false;
                        popup.MinimizeBox = false;
                        popup.fixedUrl = true;
                        popup.TopMost = true;
                        popup.FormBorderStyle = FormBorderStyle.FixedSingle;
                        Deactivate += (sender, e) => { popup.Opacity = 0.9; };
                        Activated  += (sender, e) => { popup.Opacity = 0.5; };
                        popup.FormClosing += (sender, e) =>
                        {   // 찾기/바꾸기 창은 끄지 않고 숨기기만 함
                            e.Cancel = true;
                            popup.Hide();
                        };
                        popup.Opacity = 0;
                        new Thread(() =>
                        {
                            Thread.Sleep(1000);
                            Eval(popup, "window.close();");
                        }).Start();
                    }
                    else
                    {
                        popup.Opacity = 0.9;
                        Eval("SmiEditor.Finder.onload()");
                    }
                    break;
                case "addon":
                    popup.mainView.NavigationCompleted += AfterInitAddon;
                    break;
            }
        }
        public void AfterInitAddon(object? sender, EventArgs e)
        {
            if (afterInitAddon.Length > 0 &&  popups.TryGetValue("addon", out PopupForm? popup))
            {
                Eval(popup, afterInitAddon);
                afterInitAddon = "";
            }
        }

        private int refreshPlayerIndex = 0;
        private void RefreshPlayer(object? sender, EventArgs e)
        {
            if (player != null)
            {
                if (player.CheckAndRerfreshPlayer())
                {   // 플레이어 살아있음
                    if (player.initialOffset.top + 100 < player.initialOffset.bottom)
                    {   // 유효
                        int time = player.GetTime();
                        Script("refreshTime", time);
                        UpdateViewerTime(time);

                        if (++refreshPlayerIndex % 100 == 0)
                        {   // 1초마다 파일명 확인
                            refreshPlayerIndex = 0;
                            player.GetFileName();
                        }
                    }
                    else
                    {   // 실행 직후 초기 위치 가져옴
                        if (player.GetWindowInitialPosition() != null && useMovePlayer)
                        {   // 초기화 성공
                            player.MoveWindow(); // 설정 위치로 이동
                        }
                    }

                    // 저장 대화상자를 띄우기 위해 현재 영상 파일명 요청 후 대기
                    if (saveAfter > 0)
                    {
                        saveAfter--;
                        if (saveAfter == 0)
                        {   // 파일명 응답 대기 시간 만료
                            if (saveAfter > 0) SaveWithDialogAfterGetVideoFileName("-");
                        }
                    }
                }
                else
                {   // 플레이어 죽었으면 바로 저장 대화상자 띄우기
                    if (saveAfter > 0)
                    {
                        saveAfter = 0;
                        SaveWithDialogAfterGetVideoFileName("-");
                    }
                    // 열려있는 동영상 파일 없애기
                    Script("setVideo", "");
                }
            }
            else
            {   // 플레이어 죽었으면 바로 저장 대화상자 띄우기
                if (saveAfter > 0)
                {
                    saveAfter = 0;
                    SaveWithDialogAfterGetVideoFileName("-");
                }
                // 열려있는 동영상 파일 없애기
                Script("setVideo", "");
            }
        }

        private void BeforeExit(object? sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Script("beforeExit");
        }
        public void DoExit(bool resetPlayer, bool exitPlayer)
        {
            if (player != null && player.hwnd > 0)
            {
                if (resetPlayer)
                {
                    player!.ResetPosition();
                }
                if (exitPlayer)
                {
                    player!.DoExit();
                }
            }
            swLogs?.Close();
            Process.GetCurrentProcess().Kill();
        }

        private StreamWriter? swLogs = null;
        private long lastLog = long.MaxValue;
        public void Log(string msg)
        {
            Console.WriteLine(msg);

            if (swLogs == null)
            {
                try
                {
                    // 설정 폴더 없으면 생성
                    DirectoryInfo di = new(Path.Combine(Application.StartupPath, "temp"));
                    if (!di.Exists) { di.Create(); }
                    di = new DirectoryInfo(Path.Combine(Application.StartupPath, "temp/logs"));
                    if (!di.Exists) { di.Create(); }

                    swLogs = new StreamWriter(Path.Combine(Application.StartupPath, $"temp/logs/{lastLog = DateTime.Now.Ticks}.txt"), false, Encoding.UTF8);
                    swLogs.WriteLine(msg);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                swLogs.WriteLine(msg);
                lastLog = DateTime.Now.Ticks;
            }
        }
        public void PassiveLog(string msg)
        {
            if (swLogs == null) return;
            swLogs.WriteLine(msg);
        }
        private void SaveLogs(object? sender, EventArgs e)
        {
            if (swLogs != null && ((DateTime.Now.Ticks - lastLog) > 1000))
            {
                swLogs.Flush();
                lastLog = long.MaxValue;
            }
        }

        private void WebFormClosed(object? sender, FormClosedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        #region 창 조작

        protected override int GetHwnd(string target)
        {
            if (target == "player")
            {
                return player == null ? 0 : player.hwnd;
            }
            return base.GetHwnd(target);
        }
        public void MoveWindow(string target, int x, int y, int width, int height, bool resizable)
        {
            try
            {
                int hwnd = GetHwnd(target);
                if (!target.Equals("editor"))
                {   // follow window 동작 일시정지
                    _ = WinAPI.GetWindowRectWithoutShadow(windows["editor"], ref lastOffset);
                }
                if (!resizable)
                {
                    // TODO: 안 됨.............
                    WinAPI.DisableResize(hwnd);
                }
                if (target.Equals("player"))
                {
                    if (player != null && useMovePlayer)
                    {
                        player.currentOffset.top = y;
                        player.currentOffset.left = x;
                        player.currentOffset.right = x + width;
                        player.currentOffset.bottom = y + height;
                        if (hwnd > 0)
                        {
                            RECT shadow = WinAPI.GetWindowShadow(hwnd);
                            player.currentOffset.top += shadow.top;
                            player.currentOffset.left += shadow.left;
                            player.currentOffset.right += shadow.right;
                            player.currentOffset.bottom += shadow.bottom;
                            player.MoveWindow();
                        }
                    }
                }
                else
                {
                    if (hwnd > 0)
                    {   // 윈도우 그림자 여백 보정
                        RECT shadow = WinAPI.GetWindowShadow(Handle.ToInt32());

                        if (popups.TryGetValue(target, out PopupForm? popup))
                        {
                            popup.Location = new Point(x - shadow.left, y - shadow.top);
                            popup.Size = new Size(width + shadow.left - shadow.right, height + shadow.top - shadow.bottom);
                        }
                        else
                        {
                            if (target.Equals("editor"))
                            {
                                Location = new Point(x - shadow.left, y - shadow.top);
                                Size = new Size(width + shadow.left - shadow.right, height + shadow.top - shadow.bottom);
                                Script("setDpiBy", width);

                                //IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);
                                //IntPtr DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = new IntPtr(-2);
                                //WinAPI.SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                            }
                            else
                            {
                                _ = WinAPI.MoveWindow(hwnd, x - shadow.left, y - shadow.top, width + shadow.left - shadow.right, height + shadow.top - shadow.bottom, true);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public void OverrideFocusWindow(string target)
        {
            if (target.Equals("player"))
            {
                return;
            }
            int hwnd = GetHwnd(target);
            WinAPI.SetForegroundWindow(hwnd);

            // 에디터 활성화할 땐 커서까지 포커싱
            if (target.Equals("editor"))
            {
                delayFocusing = 10; // 창 전환 후 바로 호출하면 꼬임
                timer.Tick += FocusEditor;
            }
        }
        private int delayFocusing = 0;
        private void FocusEditor(object? sender, EventArgs e)
        {
            if (--delayFocusing == 0)
            {
                mainView.Focus();
                timer.Tick -= FocusEditor;
            }
        }
        public void SetFollowWindow(bool follow)
        {
            if (follow)
            {
                _ = WinAPI.GetWindowRectWithoutShadow(windows["editor"], ref lastOffset);
            }
            useFollowWindow = follow;
        }
        public void GetWindows(string[] targets)
        {
            foreach (string target in targets)
            {
                RECT shadow = WinAPI.GetWindowShadow(Handle.ToInt32());

                if (popups.TryGetValue(target, out PopupForm? popup))
                {
                    Script("afterGetWindow"
                        , target
                        , popup.Location.X + shadow.left
                        , popup.Location.Y + shadow.top
                        , popup.Size.Width - shadow.left + shadow.right
                        , popup.Size.Height - shadow.top + shadow.bottom
                    );
                }
                else
                {
                    int hwnd = GetHwnd(target);
                    if (hwnd > 0)
                    {
                        RECT targetOffset = new();
                        _ = WinAPI.GetWindowRectWithoutShadow(hwnd, ref targetOffset);
                        Script("afterGetWindow"
                            , target
                            , targetOffset.left
                            , targetOffset.top
                            , targetOffset.right - targetOffset.left
                            , targetOffset.bottom - targetOffset.top
                        );
                    }
                }
            }
        }

        bool useFollowWindow = false;
        RECT lastOffset, offset, viewerOffset;
        int saveSettingAfter = 0;
        private void FollowWindow(object? sender, EventArgs e)
        {
            if (!useFollowWindow)
            {
                return;
            }
            RECT shadow = WinAPI.GetWindowShadow(Handle.ToInt32());
            _ = WinAPI.GetWindowRectWithoutShadow(windows["editor"], ref offset);
            if ((   lastOffset.top != offset.top
                 || lastOffset.left != offset.left
                 || lastOffset.right != offset.right
                 || lastOffset.bottom != offset.bottom
                )
             && (offset.top > -32000) // 창 최소화 시 문제
            )
            {
                int moveX = offset.left - lastOffset.left;
                int moveY = offset.top - lastOffset.top;
            	
                try
                {
                    if (popups.TryGetValue("viewer", out PopupForm? popup))
                    {
                        viewerOffset.top = popup.Location.Y;
                        viewerOffset.left = popup.Location.X;
                        viewerOffset.right = popup.Location.X + popup.Size.Width;
                        viewerOffset.bottom = popup.Location.Y + popup.Size.Height;

                        int vMoveX = moveX;
                        int vMoveY = moveY;
                        if (viewerOffset.left - lastOffset.left > lastOffset.right - viewerOffset.left)
                        {   // 오른쪽 경계에 더 가까울 땐 오른쪽을 따라감
                            vMoveX = offset.right - lastOffset.right;
                        }
                        if (viewerOffset.top - lastOffset.top > lastOffset.top - viewerOffset.top)
                        {   // 아래쪽 경계에 더 가까울 땐 아래쪽을 따라감
                            vMoveY = offset.bottom - lastOffset.bottom;
                        }
                        viewerOffset.top += vMoveY;
                        viewerOffset.left += vMoveX;
                        viewerOffset.right += vMoveX;
                        viewerOffset.bottom += vMoveY;
                        popup.Location = new Point(viewerOffset.left, viewerOffset.top);
                    }
                    else
                    {
                        int viewer = windows["viewer"];
                        if (viewer > 0)
                        {
                            int vMoveX = moveX;
                            int vMoveY = moveY;
                            _ = WinAPI.GetWindowRect(viewer, ref viewerOffset);
                            if (viewerOffset.left - lastOffset.left > lastOffset.right - viewerOffset.left)
                            {   // 오른쪽 경계에 더 가까울 땐 오른쪽을 따라감
                                vMoveX = offset.right - lastOffset.right;
                            }
                            if (viewerOffset.top - lastOffset.top > lastOffset.top - viewerOffset.top)
                            {   // 아래쪽 경계에 더 가까울 땐 아래쪽을 따라감
                                vMoveY = offset.bottom - lastOffset.bottom;
                            }
                            WinAPI.MoveWindow(viewer, vMoveX, vMoveY, ref viewerOffset);
                        }
                    }
                }
                catch { }

                int player = this.player == null ? 0 : this.player.hwnd;
                if (player > 0)
                {
                    int pMoveX = moveX;
                    int pMoveY = moveY;
                    PlayerBridge.RECT playerOffset = this.player!.GetWindowPosition();
                    if (playerOffset.left - lastOffset.left > lastOffset.right - playerOffset.left)
                    {   // 오른쪽 경계에 더 가까울 땐 오른쪽을 따라감
                        pMoveX = offset.right - lastOffset.right;
                    }
                    if (playerOffset.top - lastOffset.top > playerOffset.top - viewerOffset.top)
                    {   // 아래쪽 경계에 더 가까울 땐 아래쪽을 따라감
                        pMoveY = offset.bottom - lastOffset.bottom;
                    }
                    this.player.MoveWindow(pMoveX, pMoveY);
                }

                lastOffset = offset;
                saveSettingAfter = 300; // 창 이동 후 3초간 변화 없으면 설정 저장

                Script("refreshPaddingBottom");
            }
            else if (saveSettingAfter > 0)
            {
                if (--saveSettingAfter == 0)
                {
                    _ = WinAPI.GetWindowRectWithoutShadow(windows["editor"], ref offset);
                    Script("eval",
                        $"setting.window.x = { offset.left };"
                    +   $"setting.window.y = { offset.top };"
                    +   $"setting.window.width = { offset.right - offset.left };"
                    +   $"setting.window.height = { offset.bottom - offset.top };"
                    );

                    if (popups.TryGetValue("viewer", out PopupForm? popup))
                    {
                        Script("eval",
                            $"setting.viewer.window.x = { popup.Location.X + shadow.left };"
                        +   $"setting.viewer.window.y = { popup.Location.Y + shadow.top };"
                        +   $"setting.viewer.window.width = { popup.Size.Width - shadow.left + shadow.right };"
                        +   $"setting.viewer.window.height = { popup.Size.Height - shadow.top + shadow.bottom };"
                        );
                    }
                    else
                    {
                        int viewer = windows["viewer"];
                        if (viewer > 0)
                        {
                            _ = WinAPI.GetWindowRect(viewer, ref viewerOffset);
                            Script("eval",
                                $"setting.viewer.window.x = { viewerOffset.left - shadow.left };"
                            +   $"setting.viewer.window.y = { viewerOffset.top - shadow.top };"
                            +   $"setting.viewer.window.width = { viewerOffset.right - viewerOffset.left + shadow.left - shadow.right };"
                            +   $"setting.viewer.window.height = { viewerOffset.bottom - viewerOffset.top + shadow.bottom - shadow.top };"
                            );
                        }
                    }

                    int player = this.player!.hwnd;
                    if (player > 0)
                    {
                        PlayerBridge.RECT playerOffset = this.player.GetWindowPosition();
                        Script("eval",
                            $"setting.player.window.x = { playerOffset.left };"
                        +   $"setting.player.window.y = { playerOffset.top };"
                        +   $"setting.player.window.width = { playerOffset.right - playerOffset.left };"
                        +   $"setting.player.window.height = { playerOffset.bottom - playerOffset.top };"
                        );
                    }
                    Script("saveSetting");
                }
            }
        }
        #endregion

        #region 브라우저 통신

        // Finder, Viewer는 팝업 형태 제한
        public void SendMsg(string target, string msg) { ScriptToPopup(target, "sendMsg", msg); }
        public void OnloadFinder() { Script("SmiEditor.Finder.onload"); }
        public void OnloadFinder (string last ) { ScriptToPopup("finder", "init", last); }
        public void RunFind      (string param) { Script("SmiEditor.Finder.runFind"      , param); }
        public void RunReplace   (string param) { Script("SmiEditor.Finder.runReplace"   , param); }
        public void RunReplaceAll(string param) { Script("SmiEditor.Finder.runReplaceAll", param); }

        private void UpdateViewerTime(int time)
        {
            ScriptToPopup("viewer", "refreshTime", time);
        }
        #endregion

        #region 설정
        private void LoadSetting()
        {
            StreamReader? sr = null;
            try
            {   // 설정 파일 경로
                sr = new StreamReader(Path.Combine(Application.StartupPath, "setting/Jamaker.json"), Encoding.UTF8);
                strSettingJson = sr.ReadToEnd();
            }
            catch (Exception e)
            {
                PassiveLog(e.ToString());
                try
                {   // 구버전 설정 파일 경로
                    sr = new StreamReader(Path.Combine(Application.StartupPath, "view/setting.json"), Encoding.UTF8);
                    strSettingJson = sr.ReadToEnd();
                }
                catch (Exception e2) { Console.WriteLine(e2); }
            }
            finally { sr?.Close(); }

            try
            {
                sr = new StreamReader(Path.Combine(Application.StartupPath, "bridge/list.txt"), Encoding.UTF8);
                strBridgeList = sr.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
            }
            finally { sr?.Close(); }

            try
            {
                sr = new StreamReader(Path.Combine(Application.StartupPath, "view/lib/highlight/list.txt"), Encoding.UTF8);
                strHighlights = sr.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
            }
            finally { sr?.Close(); }

            string[] bridgeList = strBridgeList.Split('\n');
            for (int i = 0; i < bridgeList.Length; i++)
            {
                string strBridge = bridgeList[i].Trim();
                int devider = strBridge.IndexOf(':');
                if (devider > 0)
                {
                    string[] bridge = [strBridge[..devider].Trim(), strBridge[(devider + 1)..].Trim()];
                    bridgeDlls.Add(bridge[0], bridge[1]);
                }
            }
        }

        public void RepairSetting()
        {
            // 백지 상태에서 시작
            strSettingJson = "";

            StreamReader? sr = null;
            try
            {
                // 설정 폴더 없으면 생성
                DirectoryInfo di = new(Path.Combine(Application.StartupPath, "setting"));
                if (!di.Exists)
                {
                    di.Create();
                }
                else
                {
                    if (File.Exists(Path.Combine(Application.StartupPath, "setting/Jamaker.json")))
                    {   // 설정파일 있는데 여기로 왔으면 설정파일이 깨졌다는 의미
                        File.Delete(Path.Combine(Application.StartupPath, "setting/Jamaker.json"));
                    }
                    if (File.Exists(Path.Combine(Application.StartupPath, "setting/Jamaker.json.bak")))
                    {   // 백업 파일 존재하면 가져오기
                        File.Move(Path.Combine(Application.StartupPath, "setting/Jamaker.json.bak"), Path.Combine(Application.StartupPath, "setting/Jamaker.json"));
                        sr = new StreamReader(Path.Combine(Application.StartupPath, "setting/Jamaker.json"), Encoding.UTF8);
                        strSettingJson = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
            }
            finally { sr?.Close(); }

            // 프로그램 다시 초기화
            Script("init", strSettingJson, true);
        }

        public void SaveSetting(string strSettingJson)
        {
            this.strSettingJson = strSettingJson;

            try
            {
                // 설정 폴더 없으면 생성
                DirectoryInfo di = new(Path.Combine(Application.StartupPath, "setting"));
                if (!di.Exists)
                {
                    di.Create();
                }
                else if (File.Exists(Path.Combine(Application.StartupPath, "setting/Jamaker.json")))
                {
                    // 기존 설정파일 백업 후 진행
                    if (File.Exists(Path.Combine(Application.StartupPath, "setting/Jamaker.json.bak")))
                    {
                        File.Delete(Path.Combine(Application.StartupPath, "setting/Jamaker.json.bak"));
                    }
                    File.Move(Path.Combine(Application.StartupPath, "setting/Jamaker.json"), Path.Combine(Application.StartupPath, "setting/Jamaker.json.bak"));
                }

                StreamWriter sw = new(Path.Combine(Application.StartupPath, "setting/Jamaker.json"), false, Encoding.UTF8);
                sw.Write(strSettingJson);
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
            }
        }

        private string[] videoExts = ["mkv", "mp4", "avi", "wmv", "m2ts", "ts"];
        public void SetVideoExts(string exts)
        {
            videoExts = exts.Split(',');
        }

        public void SetPlayer(string dll, string path, bool withRun, bool useMove)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { SetPlayer(dll, path, withRun, useMove); }));
                return;
            }

            // 플레이어 선택이 바뀌었으면 연결 끊기
            if (player != null && !dll.Equals(player.GetType().Name))
            {
                if (player != null && player.hwnd > 0)
                {   // 기존 플레이어 종료
                    player!.ResetPosition();
                    player!.DoExit();
                }
                player = null;
            }

            if (path.IndexOf(':') < 0)
            {   // 드라이브 문자 없을 경우 상대경로
                path = Path.Combine(Application.StartupPath, path);
            }
            string[] paths = path.Replace('\\', '/').Split('/');
            string exe = paths[^1]; // paths[paths.Length - 1]

            // 잔여 플레이어가 없으면 실행
            if (player == null)
            {
                try
                {   // DLL 파일 동적 호출
                    string dllPath = Path.Combine(Application.StartupPath, $"bridge/{dll}.dll");
                    Assembly asm = Assembly.LoadFile(dllPath);
                    Type[] types = asm.GetExportedTypes();
                    player = (PlayerBridge.PlayerBridge)Activator.CreateInstance(types[0])!;
                }
                catch
                {
                    Script("alert", "플레이어 브리지 라이브러리가 없습니다.");
                }
                player?.SetEditorHwnd(Handle.ToInt32());
            }

            // 플레이어 있으면 exe 파일 설정
            if (player != null)
            {
                player.FindPlayer(exe);

                if (withRun && player.hwnd == 0)
                {
                    /*
                    // TODO: 이 실행 부분을 PlayerBridge로 옮기는 게 맞을 듯함
                    new Thread(() =>
                    {
                        try
                        {
                            ProcessStartInfo startInfo = new()
                            {   FileName = path
                            ,   Arguments = null
                            };
                            Process.Start(startInfo);
                        }
                        catch
                        {
                            Script("alert", "플레이어를 실행하지 못했습니다.\n설정을 확인하시기 바랍니다.");
                        }
                    }).Start();
                    */
                    player.RunPlayer(path, () =>
                    {
                        Script("alert", "플레이어를 실행하지 못했습니다.\n설정을 확인하시기 바랍니다.");
                    });
                }
            }

            useMovePlayer = useMove;
        }
        
        public async void SelectPlayerPath()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { SelectPlayerPath(); }));
                return;
            }

            Form form = this;
            if (popups.TryGetValue("setting", out PopupForm? value))
            {
                form = value;
            }
            string? filename = await RunDialog(form, () =>
            {
                OpenFileDialog dialog = new() { Filter = "실행 파일|*.exe" };
                return (dialog.ShowDialog() == DialogResult.OK) ? dialog.FileName : null;
            });

            if (filename != null)
            {
                Script("afterSelectPlayerPath", filename);
            }
        }

        public string afterInitAddon = "";
        public void SetAfterInitAddon(string func)
        {
            afterInitAddon = func;
        }
        public void LoadAddonSetting(string path)
        {
            string setting = "";
            try
            {   // addon 설정 파일 경로
                StreamReader sr = new(Path.Combine(Application.StartupPath, $"setting/addon_{path}"), Encoding.UTF8);
                setting = sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
                try
                {   // 구버전 addon 설정 파일 경로
                    StreamReader sr = new(Path.Combine(Application.StartupPath, $"view/addon/{path}"), Encoding.UTF8);
                    setting = sr.ReadToEnd();
                    sr.Close();
                }
                catch (Exception e2)
                {
                    Console.WriteLine(e2);
                    PassiveLog(e2.ToString());
                }
            }
            Script("afterLoadAddonSetting", setting.Replace("\r\n", "\n"));
        }
        public void SaveAddonSetting(string path, string text)
        {
            try
            {
                // 설정 폴더 없으면 생성
                DirectoryInfo di = new(Path.Combine(Application.StartupPath, "setting"));
                if (!di.Exists)
                {
                    di.Create();
                }

                StreamWriter sw = new(Path.Combine(Application.StartupPath, $"setting/addon_{path}"), false, Encoding.UTF8);
                sw.Write(text);
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
            }
            Script("afterSaveAddonSetting");
        }

        public void GetSubDirs(string dir)
        {
            string dirs = "";
            DirectoryInfo di = new(dir);
            if (di.Exists)
            {
                DirectoryInfo[] subDirs = di.GetDirectories();
                foreach (DirectoryInfo subDir in subDirs)
                {
                    if (dirs.Length > 0)
                    {
                        dirs += "\n";
                    }
                    dirs += subDir.FullName;
                }
            }
            Script("afterGetSubDirs", dirs);
        }
        public void SearchFiles(string dir, string query)
        {
            DirectoryInfo di = new(dir);
            if (di.Exists)
            {
                FileInfo[] files = di.GetFiles();
                foreach (FileInfo file in files)
                {
                    if (file.Name.ToUpper().EndsWith(".SMI")
                     || file.Name.ToUpper().EndsWith(".SAMI")
                     || file.Name.ToUpper().EndsWith(".JMK"))
                    {
                        try
                        {
                            StreamReader sr = new(file.FullName, Encoding.UTF8);
                            string text = sr.ReadToEnd();
                            sr.Close();

                            if (text.Contains(query))
                            {
                                Script("afterFound", file.Name, text);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            PassiveLog(e.ToString());
                        }
                    }
                }
            }
        }
        #endregion

        #region 파일 드래그 관련
        protected override void Drop(int x, int y)
        {
            try
            {
                foreach (string strFile in droppedFiles!)
                {
                    if (strFile.ToUpper().EndsWith(".SMI")
                     || strFile.ToUpper().EndsWith(".SRT")
                     || strFile.ToUpper().EndsWith(".ASS")
                     || strFile.ToUpper().EndsWith(".JMK"))
                    {
                        LoadFile(strFile);
                        break;
                    }
                }
            }
            catch { }
        }
        #endregion

        #region 파일

        private delegate void AfterGetString(string str);
        private AfterGetString? afterGetFileName = null;

        private string receive = "";
        protected override void WndProc(ref Message m)
        {
            const int WM_COPYDATA = 0x004A;

            switch (m.Msg)
            {
                case WM_COPYDATA:
                    try
                    {
                        byte[] buff = new byte[Marshal.ReadInt32(m.LParam, IntPtr.Size)];
                        IntPtr dataPtr = Marshal.ReadIntPtr(m.LParam, IntPtr.Size * 2);
                        Marshal.Copy(dataPtr, buff, 0, buff.Length);
                        receive = Encoding.UTF8.GetString(buff);
                        if (receive.EndsWith(".smi")
                         || receive.EndsWith(".sami")
                         || receive.EndsWith(".jmk"))
                        {
                            LoadFile(receive);
                            return;
                        }
                    }
                    catch (Exception e) { Console.WriteLine(e); }
                    break;
            }
            if (player != null)
            {
                string path = player.AfterGetFileName(m);
                if (path != null)
                {
                    afterGetFileName?.Invoke(path);
                    Script("setVideo", path);
                    return;
                }
            }
            base.WndProc(ref m);
        }

        public async void OpenFile()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { OpenFile(); }));
                return;
            }

            string? filename = await RunDialog(() =>
            {
                OpenFileDialog dialog = new() { Filter = "지원되는 자막 파일|*.smi;*.sami;*.jmk;*.srt;*.ass" };
                return (dialog.ShowDialog() == DialogResult.OK) ? dialog.FileName : null;
            });

            if (filename != null)
            {
                LoadFile(filename, false, true);
            }
        }
        public void AfterInit(int limit)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { AfterInit(limit); }));
                return;
            }

            Opacity = 1;

            // CefSharp에선 안 그랬는데, WebView2에서 자꾸 스크롤이 내려감
            Eval("$('textarea').scrollTop(0);");

            // 찾기/바꾸기 최초 1회 실행 후 숨기기
            Eval("SmiEditor.Finder.open()");

            // 최초 실행 시 ffmpeg 존재 여부 확인인데, 창 위치 잡아준 후에 alert 돌아가도록 함
            int status = CheckFFmpegWithAlert();
            new Thread(() =>
            {
                string[] strings = VideoInfo.CheckFFmpegVersion(status);
                Script("setFFmpegVersion", strings[0], strings[1]);
            }).Start();

            int count = 0;
            foreach (string path in args)
            {
                if (path.ToUpper().EndsWith(".SMI")
                 || path.ToUpper().EndsWith(".SRT")
                 || path.ToUpper().EndsWith(".ASS")
                 || path.ToUpper().EndsWith(".JMK"))
                {
                    LoadFile(path);
                    count++;
                }
                if (count >= limit)
                {
                    break;
                }
            }
        }

        // 공통 웹폼에 VideoInfo 참조 넣기는 좀 미묘...
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
        public void OpenFileForVideo()
        {
            // 파일명 수신 시 동작 설정
            afterGetFileName = new AfterGetString(OpenFileAfterGetVideoFileName);
            // player에 현재 재생 중인 파일명 요청
            _ = (player?.GetFileName());
        }
        private void OpenFileAfterGetVideoFileName(string path)
        {
            afterGetFileName = null;
            if (path != null && path.Length > 0)
            {
                int index = path.LastIndexOf('.');
                if (index > 0)
                {
                    LoadFile(string.Concat(path.AsSpan(0, index), ".jmk"), true, true);
                }
            }
        }
        
        private void LoadFile(string path)
        {
            LoadFile(path, false, false);
        }
        private void LoadFile(string path, bool forVideo, bool confirmed)
        {
            StreamReader? sr = null;
            try
            {
                sr = new StreamReader(path, DetectEncoding(path));
                string text = sr.ReadToEnd();
                Script("openFile", path, text, forVideo, confirmed);
            }
            catch (Exception e)
            {
                if (path.EndsWith(".jmk"))
                {
                    // 프로젝트 파일 없었으면 smi 파일로 재시도
                    LoadFile(string.Concat(path.AsSpan(0, path.Length - 4), ".smi"), true, confirmed);
                }
                else
                {
                    Script("alert", "파일을 열지 못했습니다.");
                    PassiveLog(e.ToString());
                }
            }
            finally { sr?.Close(); }
        }

        private string? smiPath;
        public void SetPath(string path)
        {
            smiPath = path;
            string title = "Jamaker";
            if (path.Length > 0)
            {
                string[] paths = path.Replace('\\', '/').Split('/');
                title = $"{paths[^1]} - Jamaker"; // paths[paths.Length - 1]
            }
            SetTitle(title);
        }
        public void SetTitle(string title)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { SetTitle(title); }));
                return;
            }
            Text = title;
        }
        public void CheckLoadVideoFile()
        {
            // 파일명 수신 시 동작 설정
            afterGetFileName = new AfterGetString(CheckLoadVideoFileAfterGetVideoFileName);
            // player에 현재 재생 중인 파일명 요청
            player?.GetFileName();
        }
        private void CheckLoadVideoFileAfterGetVideoFileName(string path)
        {
            afterGetFileName = null;

            string smiWithoutExt = smiPath![..smiPath!.LastIndexOf('.')];

            if (path != null && path.Length > 0 && path.IndexOf('.') > 0) // 확장자가 존재해야 함
            {
                string withoutExt = path[..path.LastIndexOf('.')];
                if (withoutExt.Equals(smiWithoutExt))
                {
                    // 현재 열려있는 동영상 파일이 자막과 일치 -> 추가동작 없음
                    return;
                }
            }

            foreach (string ext in videoExts)
            {
                string videoPath = smiWithoutExt + "." + ext;
                if (File.Exists(videoPath))
                {
                    Script("confirmLoadVideo", videoPath);
                    return;
                }
            }
        }
        public void LoadVideoFile(string path)
        {
            player?.OpenFile(path);
        }
        private string? requestFramesPath = null;
        public void RequestFrames(string path)
        {
            // 아주 오래 걸리진 않는 작업
            // 키프레임 신뢰 버튼 쪽에 프로그레스
            requestFramesPath = path;

            if ((VideoInfo.CheckFFmpeg() & 1) == 0)
            {
                // ffmpeg 없을 경우 - 처음에 만들었던 플레이어 의존 방식
                // 해상도 없이 frame rate만 가져옴
                Script("setVideoInfo", 1920, 1080, player?.GetFps());

            }
            else
            {
                new Thread(() =>
                {
                    try
                    {
                        FileInfo info = new(path);

                        {
                            Process proc = VideoInfo.GetFFprobe(true);
                            proc.StartInfo.Arguments = $"-v error -select_streams v -show_entries stream=width,height,r_frame_rate \"{path}\"";
                            proc.Start();
                            proc.BeginErrorReadLine();

                            StreamReader sr = new(proc.StandardOutput.BaseStream);
                            string? line;
                            int width = 0;
                            int height = 0;
                            int fr = 0;
                            while ((line = sr.ReadLine()) != null)
                            {
                                try
                                {
                                    if (line.StartsWith("width="))
                                    {
                                        if (width > 0) continue;
                                        width = int.Parse(line[6..]);
                                    }
                                    else if (line.StartsWith("height="))
                                    {
                                        if (height > 0) continue;
                                        height = int.Parse(line[7..]);
                                    }
                                    else if (line.StartsWith("r_frame_rate="))
                                    {
                                        if (fr > 0) continue;
                                        string[] strFrs = line[13..].Split('/');
                                        if (strFrs.Length > 1)
                                        {
                                            fr = (int)Math.Round(double.Parse(strFrs[0]) / double.Parse(strFrs[1]) * 1000);
                                        }
                                        else
                                        {
                                            fr = (int)Math.Round(double.Parse(strFrs[0]));
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    PassiveLog(e.ToString());
                                }
                            }
                            proc.Close();

                            if (width == 0) width = 1920;
                            if (height == 0) height = 1080;

                            Console.WriteLine($"setVideoInfo: {width}, {height}, {fr}");
                            Script("setVideoInfo", width, height, fr);
                        }

                        string fkfName = $"{info.Name[..^info.Extension.Length]}.{info.Length}.fkf";
                        string fkfPath = Path.Combine(Application.StartupPath, "temp/fkf/" + fkfName);

                        // 기존에 있으면 가져오기
                        try
                        {
                            DirectoryInfo di = new(Path.Combine(Application.StartupPath, "temp/fkf"));
                            if (di.Exists)
                            {
                                VideoInfo.FromFkfFile(fkfPath);
                                Script("Progress.set", "#forFrameSync", 1);
                                Script("loadFkf", fkfName);
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            PassiveLog(e.ToString());
                        }

                        // 구버전에선 fkf 폴더 없이 그냥 temp 폴더에 있었음
                        try
                        {
                            DirectoryInfo di = new(Path.Combine(Application.StartupPath, "temp"));
                            if (di.Exists)
                            {
                                di = new DirectoryInfo(Path.Combine(Application.StartupPath, "temp/fkf"));
                                if (!di.Exists)
                                {
                                    di.Create();
                                }
                                File.Move(Path.Combine(Application.StartupPath, "temp/" + fkfName), fkfPath);
                                VideoInfo.FromFkfFile(fkfPath);
                                Script("Progress.set", "#forFrameSync", 1);
                                Script("loadFkf", fkfName);
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            PassiveLog(e.ToString());
                        }

                        // 없으면 새로 가져오기
                        Invoke(() => { TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, Handle); });
                        new VideoInfo(path, (ratio) =>
                        {
                            if (requestFramesPath == path)
                            {   // 중간에 다른 파일 불러왔을 수도 있음
                                Script("Progress.set", "#forFrameSync", ratio);
                                Invoke(() => { TaskbarManager.Instance.SetProgressValue((int)(ratio * 64), 64, Handle); });
                            }
                        }).RefreshInfo((videoInfo) =>
                        {
                            Script("Progress.set", "#forFrameSync", 1);
                            Invoke(() => { TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, Handle); });
                            videoInfo.ReadKfs(true);
                            videoInfo.SaveFkf(fkfPath);
                            if (requestFramesPath == path)
                            {   // 중간에 다른 파일 불러왔을 수도 있음
                                Script("loadFkf", fkfName);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        PassiveLog(e.ToString());
                    }
                }).Start();
            }
        }

        private string? lastThumbnailsPath = null;
        private int lastThumbnailsProcSeq = 0;
        private int lastThumbnailsFileSeq = 0;

        private static int TX = 96;
        private static int TY = 54;

        private static byte[] BitmapToByteArray(Bitmap bitmap)
        {
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, TX, TY), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] bytes = new byte[TX * TY * 3];
            Marshal.Copy(data.Scan0, bytes, 0, TX * TY * 3);
            bitmap.UnlockBits(data);
            return bytes;
        }
        private static Bitmap ByteArrayToBitmap(byte[] bytes)
        {
            Bitmap bitmap = new(TX, TY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, TX, TY), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            Marshal.Copy(bytes, 0, data.Scan0, TX * TY * 3);
            bitmap.UnlockBits(data);
            return bitmap;
        }

        public void SaveThumbnailInfo(string path)
        {
            lastThumbnailsPath = path;
            try
            {
                StreamWriter sw = new(Path.Combine(Application.StartupPath, "temp/thumbnails/_.txt"), false, Encoding.UTF8);
                sw.Write($"{path}\n{TX}\n{TY}");
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
            }
        }
        public void LoadThumbnailInfo()
        {
            StreamReader? sr = null;
            try
            {   // 설정 파일 경로
                sr = new(Path.Combine(Application.StartupPath, "temp/thumbnails/_.txt"), Encoding.UTF8);
                string[] info = sr.ReadToEnd().Split('\n');
                lastThumbnailsPath = info[0];
                TX = int.Parse(info[1]);
                TY = int.Parse(info[2]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
            }
            finally { sr?.Close(); }
        }
        public void SetThumbnailSize(int width, int height)
        {
            if (TX != width || TY != height)
            {
                ClearThumbnails();
                TX = width;
                TY = height;
            }
        }
        public void RenderThumbnails(string path, string paramsStr)
        {
            Console.WriteLine($"RenderThumbnails: {paramsStr}");
            int procSeq = ++lastThumbnailsProcSeq;
            int fileSeq = lastThumbnailsFileSeq;

            string[] list = paramsStr.Split('\n');
            nint hwnd = GetHwnd("addon");
            if (hwnd == 0) hwnd = Handle;
            Invoke(() => { TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, hwnd); });

            new Thread(() =>
            {
                string dir = Path.Combine(Application.StartupPath, "temp/thumbnails");
                DirectoryInfo di = new(dir);
                if (!di.Exists)
                {
                    di.Create();
                }

                // 섬네일 대상 파일이 바뀐 경후 초기화
                if (lastThumbnailsPath != path)
                {
                    ClearThumbnails();
                    SaveThumbnailInfo(path);
                    fileSeq = ++lastThumbnailsFileSeq;
                }
                Script("setThumbnailsFileSeq", fileSeq);

                int didread;
                int offset = 0;
                byte[] buffer = new byte[sizeof(float) * (1024 + 1)];

                for (int i = 0; i < list.Length; i++)
                {
                    Invoke(() => { TaskbarManager.Instance.SetProgressValue(i, list.Length, hwnd); });
                    string paramStr = list[i];
                    
                    // 중간에 작업 끊은 경우
                    if (procSeq != lastThumbnailsProcSeq) break;

                    string[] param = paramStr.Split(',');
                    int time = int.Parse(param[0]);
                    double length = double.Parse(param[1]) / 1000;
                    int begin = int.Parse(param[2]);
                    int end = int.Parse(param[3]);
                    string flag = param[4];

                    try
                    {
                        // 필요한 이미지들이 이미 존재하는지 확인
                        bool isCompleted = true;
                        for (int index = 0; index < (end - begin); index++)
                        {
                            Console.WriteLine($"index: {index}");
                            if (!File.Exists(Path.Combine(Application.StartupPath, $"{dir}/{fileSeq}_{begin + index}{flag}.jpg")))
                            {
                                Console.WriteLine("no img");
                                isCompleted = false;
                                break;
                            }
                            // 0일 땐 비교값 계산 없음
                            if (index == 0) continue;

                            if (!File.Exists(Path.Combine(Application.StartupPath, $"{dir}/{fileSeq}_{begin + index}{flag}_.jpg")))
                            {
                                Console.WriteLine("no img _");
                                isCompleted = false;
                                break;
                            }
                            if (!File.Exists(Path.Combine(Application.StartupPath, $"{dir}/{fileSeq}_{begin + index}{flag}~.jpg")))
                            {
                                Console.WriteLine("no img ~");
                                isCompleted = false;
                                break;
                            }
                        }
                        // 이미 존재하면 재활용
                        if (isCompleted)
                        {
                            Script("afterRenderThumbnails", begin, end, flag);
                            continue;
                        }

                        Script("startRenderThumbnails", begin, end, flag);

                        int ms = time % 60000;
                        int m = time / 60000;
                        int h = m / 60;
                        m %= 60;
                        string timeStr = ((100 + h) * 100 + m) * 100000 + ms + "";
                        timeStr = timeStr.Substring(1, 2) + ":" + timeStr.Substring(3, 2) + ":" + timeStr.Substring(5, 2) + "." + timeStr.Substring(7, 3);

                        string vf = "";
                        //if (flag == "b") vf = "-vf \"curves=r='0/0 0.1/0.9 1/1'\" ";
                        //else
                        //if (flag == "d") vf = "-vf \"curves=r='0/0 0.9/0.1 1/1'\" ";

                        Process proc = VideoInfo.GetFFmpeg(true);
                        proc.StartInfo.Arguments = $"-ss {timeStr} -t {length} -i \"{path}\" -s {TX}x{TY} -qscale:v 2 -fps_mode passthrough {vf}-f image2 \"{dir}/p{procSeq}_{begin}{flag}_%d.jpg\"";
                        proc.Start();
                        proc.BeginErrorReadLine();

                        Stream stream = proc.StandardOutput.BaseStream;

                        while ((didread = stream.Read(buffer, offset, sizeof(float) * 1024)) != 0)
                        {
                            Console.WriteLine(didread);
                        }

                        // 중간에 작업 끊었어도, 파일 자체가 바뀐 게 아니면 일단 진행
                        if (fileSeq != lastThumbnailsFileSeq) break;

                        new Thread(() =>
                        {
                            Script("startCompareThumbnails", begin, end, flag);

                            Bitmap? bLast = null;
                            for (int index = 0; index < (end - begin); index++)
                            {
                                // 중간에 작업 끊었어도, 파일 자체가 바뀐 게 아니면 일단 진행
                                if (fileSeq != lastThumbnailsFileSeq) break;

                                // 위에서 만든 이미지 경로
                                string img0 = Path.Combine(Application.StartupPath, $"{dir}/p{procSeq}_{begin}{flag}_{index + 1}.jpg");

                                // 실제 필요한 이미지 경로
                                string img1 = Path.Combine(Application.StartupPath, $"{dir}/{fileSeq}_{begin + index}{flag}.jpg");
                                try {
                                    if (File.Exists(img1)) File.Delete(img1);
                                    File.Move(img0, img1);
                                } catch (Exception e) { Console.WriteLine(e); }

                                // 프레임 간 차이 이미지 경로
                                string img2 = Path.Combine(Application.StartupPath, $"{dir}/{fileSeq}_{begin + index}{flag}_.jpg");
                                try {
                                    if (File.Exists(img2)) File.Delete(img2);
                                } catch (Exception e) { Console.WriteLine(e); }

                                // 밝기 변화 이미지 경로
                                string img3 = Path.Combine(Application.StartupPath, $"{dir}/{fileSeq}_{begin + index}{flag}~.jpg");
                                try {
                                    if (File.Exists(img3)) File.Delete(img3);
                                } catch (Exception e) { Console.WriteLine(e); }

                                if (bLast != null)
                                {
                                    bool isFade = (flag != "");
                                    double sum = 0;

                                    // 렌더링 중복 실행 시 다른 스레드에서 파일 삭제될 수 있어서 try-catch 문에 넣음
                                    try
                                    {
                                        // 라이브러리 써보려고 했는데 결과물이 별로임
                                        // 96x54 정도는 직접 돌릴 만한 크기
                                        Bitmap bPrev = bLast;
                                        Bitmap bTrgt = bLast = new Bitmap(img1);

                                        byte[] arrPrev = BitmapToByteArray(bPrev);
                                        byte[] arrTrgt = BitmapToByteArray(bTrgt);

                                        // 각 픽셀 비교
                                        int[] arrDiff = new int[TX * TY * 3];
                                        double[] sums = new double[TY];
                                        for (int y = 0; y < TY; y++)
                                        {
                                            // int y = get_global_id(0);
                                            int pixel;

                                            sums[y] = 0;
                                            for (int x = 0; x < TX; x++)
                                            {
                                                pixel = (TX * y + x) * 3;
                                                sums[y] += 0.299 * (arrDiff[pixel + 0] = arrPrev[pixel+0] - arrTrgt[pixel+0]);
                                                sums[y] += 0.587 * (arrDiff[pixel + 1] = arrPrev[pixel+1] - arrTrgt[pixel+1]);
                                                sums[y] += 0.114 * (arrDiff[pixel + 2] = arrPrev[pixel+2] - arrTrgt[pixel+2]);
                                            }
                                        }
                                        for (int y = 0; y < TY; y++)
                                        {
                                            sum += sums[y];
                                        }

                                        // 페이드 효과에 대해선 더 잘 보이도록
                                        int a = isFade ? 40 : 4;
                                        int avg = (int)(Math.Abs(sum) / (TX * TY));

                                        byte[] arr2 = new byte[TX * TY * 3];
                                        byte[] arr3 = new byte[TX * TY * 3];

                                        for (int y = 0; y < TY; y++)
                                        {
                                            // int y = get_global_id(0);
                                            int pixel, diffR, diffG, diffB, v;

                                            for (int x = 0; x < TX; x++)
                                            {
                                                pixel = (TX * y + x) * 3;

                                                // 이전 프레임과 차이
                                                v = arrDiff[pixel+0] * a; if (v < 0) v = -v; arr2[pixel+0] = (byte)(v < 255 ? v : 255);
                                                v = arrDiff[pixel+1] * a; if (v < 0) v = -v; arr2[pixel+1] = (byte)(v < 255 ? v : 255);
                                                v = arrDiff[pixel+2] * a; if (v < 0) v = -v; arr2[pixel+2] = (byte)(v < 255 ? v : 255);

                                                // 밝기 변화량
                                                //v = (int)((arrDiff[pixel+0] + arrDiff[pixel+1] + arrDiff[pixel+2]) * a);
                                                // RGB 중에 가장 조금 변화한 것만 확인
                                                diffR = arrDiff[pixel+0]; if (diffR < 0) diffR = -diffR; v = diffR;
                                                diffG = arrDiff[pixel+1]; if (diffG < 0) diffG = -diffG; v = (v < diffG ? v : diffG);
                                                diffB = arrDiff[pixel+2]; if (diffG < 0) diffG = -diffB; v = (v < diffB ? v : diffB);
                                                v *= a;

                                                if (v > 0) {
                                                    arr3[pixel+0] = (byte)(v < 255 ? v : 255);
                                                    arr3[pixel+2] = 0;
                                                } else {
                                                    v = -v;
                                                    arr3[pixel+0] = 0;
                                                    arr3[pixel+2] = (byte)(v < 255 ? v : 255);
                                                }
                                                arr3[pixel+1] = (byte)avg;
                                            }
                                        }

                                        ByteArrayToBitmap(arr2).Save(img2, ImageFormat.Jpeg);
                                        ByteArrayToBitmap(arr3).Save(img3, ImageFormat.Jpeg);

                                        Script("setDiff", $"{begin + index}{flag}", sum);

                                    } catch (Exception e) { Console.WriteLine(e); }
                                }
                                else
                                {
                                    // 앞 프레임이 없음 = 첫 번째 이미지, 그냥 복사
                                    // ... 조절 후 재렌더링 기능 추가해보니, 이건 비워두는 게 맞을 듯함
                                    try
                                    {
                                        /*
                                        File.Copy(img1, img2);
                                        File.Copy(img1, img3);
                                        */
                                        bLast = new Bitmap(img1);
                                    } catch (Exception e) { Console.WriteLine(e); }
                                }
                            }

                            // 중간에 작업 끊었어도, 파일 자체가 바뀐 게 아니면 갱신해줌
                            if (fileSeq != lastThumbnailsFileSeq) return;

                            //Console.WriteLine($"{renderingProcSeq}/{lastRenderingProcSeq}: {begin}, {end}, {flag}");
                            Script("afterRenderThumbnails", begin, end, flag);

                        }).Start();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        PassiveLog(e.ToString());
                    }
                }
                Invoke(() => { TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, hwnd); });

            }).Start();
        }

        public void CancelRenderThumbnails()
        {
            // 작업 중인 걸 끝낸 후에 섬네일 삭제해야 함
            // 스레드 종료 시점이 불분명해, 여기서 삭제 시키면 터질 수 있음
            lastThumbnailsProcSeq++;
        }
        public void ClearThumbnails()
        {
            string dir = Path.Combine(Application.StartupPath, "temp/thumbnails");
            DirectoryInfo di = new(dir);
            if (di.Exists)
            {
                FileInfo[] files = di.GetFiles();
                foreach (FileInfo file in files)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        PassiveLog(e.ToString());
                    }
                }
            }
        }

        private class SaveOrder(int tab, string text, string path, int type)
        {
            public int tab = tab;
            public string text = text;
            public string path = path;
            public int type = type;
        }
        private readonly List<SaveOrder> saveOrders = [];
        private int saveIndex = 0;
        public void Save(int tab, string text, string path, int type)
        {
            Console.WriteLine($"Save({tab}, {path}, {type}), count: {saveOrders.Count}");
            SaveOrder order = new(tab, text, path, type);

            if (saveOrders.Count == 0)
            {
                // 저장 대기열 없으면 그냥 진행
                saveOrders.Add(order);
                Save();
            }
            else
            {
                bool updated = false;
                for (int i = saveIndex + 1; i < saveOrders.Count; i++)
                {
                    SaveOrder item = saveOrders[i];
                    if (item.tab == tab && item.type == type)
                    {
                        // 저장 대기열 중복이면 내용물 교체
                        item.text = text;
                        item.path = path;
                        item.type = type;
                        updated = true;
                        break;
                    }
                }
                if (!updated)
                {
                    // 저장 대기열 추가
                    saveOrders.Add(order);
                }
            }
        }

        private int saveAfter = 0;
        private int saveTab = 0;
        private string textToSave = "";
        public void Save()
        {
            SaveOrder order = saveOrders[saveIndex];

            int tab = order.tab;
            string text = order.text;
            string path = order.path;
            int type = order.type;

            if (path == null || path.Length == 0)
            {
                // 파일명 수신 시 동작 설정
                saveAfter = 100;
                saveTab = tab;
                textToSave = text;
                afterGetFileName = new AfterGetString(SaveWithDialogAfterGetVideoFileName);
                // player에 현재 재생 중인 파일명 요청
                player?.GetFileName();

                //SaveWithDialog(text, null);
                return;
            }

            StreamWriter? sw = null;
            try
            {   // 무조건 UTF-8로 저장
                (sw = new StreamWriter(path, false, Encoding.UTF8)).Write(text);
                if (type == 0)
                {
                    SetPath(path);
                    Script("afterSaveFile", tab, path);
                }
                else
                {
                    // TODO: ASS 저장 후 작업은?
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
                Script("alert", "저장되지 않았습니다.");
            }
            finally
            {
                sw?.Close();
            }
            AfterSave();
        }
        private void AfterSave()
        {
            if (++saveIndex < saveOrders.Count)
            {
                // 저장 대기열 있으면 다음 순서 진행
                Save();
            }
            else
            {
                // 저장 끝났으면 대기열 초기화
                saveOrders.Clear();
                saveIndex = 0;
            }
        }
        public void SaveWithDialogAfterGetVideoFileName(string path)
        {
            afterGetFileName = null;
            if (path != null && path.Length > 0)
            {
                saveAfter = 0;
                SaveWithDialog(path);
            }
            else
            {
                AfterSave();
            }
        }
        public async void SaveWithDialog(string videoPath)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { SaveWithDialog(videoPath); }));
                return;
            }

            string? directory = null;
            string? filename = null;
            if (videoPath != null)
            {
                videoPath = videoPath.Replace('/', '\\');
                if (videoPath.IndexOf('\\') > 0)
                {
                    directory = videoPath[..videoPath.LastIndexOf('\\')];
                    filename = videoPath[(directory.Length + 1)..];
                    if (filename.Contains('.'))
                    {
                        filename = string.Concat(filename.AsSpan(0, filename.LastIndexOf('.')), ".smi");
                    }
                }
            }

            SaveOrder order = saveOrders[saveIndex];

            string filter = "";
            switch (order.type) {
                case 0:
                case 1: filter = "SAMI 자막|*.smi;*.sami"; break;
                case 2: filter = "ASS 자막|*.ass"; break;
                case 3: filter = "SRT 자막|*.srt"; break;
            }
            if (order.type == 0) {
                filter += "|Jamakaer 프로젝트|*.jmk";
            }

            filename = await RunDialog(() => {
                SaveFileDialog dialog = new()
                {   Filter = filter
                ,   InitialDirectory = directory
                ,   FileName = filename
                };
                return (dialog.ShowDialog() == DialogResult.OK) ? dialog.FileName : null;
            });
            if (filename != null)
            {
                order.path = filename;
                Save();
            }
            else
            {
                // 저장 취소
                AfterSave();
            }
        }
        public void SaveTemp(string text, string path)
        {   // 임시 파일 저장
            int index = path.LastIndexOf('\\');
            string filename = path[(index + 1)..];

            try
            {   // 임시 파일 폴더 없으면 생성
                DirectoryInfo di = new(Path.Combine(Application.StartupPath, "temp"));
                if (!di.Exists)
                {
                    di.Create();
                }

                // 임시 파일 저장
                long now = DateTime.Now.Ticks;
                StreamWriter sw = new(Path.Combine(Application.StartupPath, "temp/" + now + "_" + filename), false, Encoding.UTF8);
                sw.Write(text);
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PassiveLog(e.ToString());
            }
        }
        #endregion

        #region 부가기능
        public void RunColorPicker()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => { RunColorPicker(); }));
                return;
            }

            Thread thread = new(() =>
            {
                new ColorPicker(this).ShowDialog();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public void InputText(string text)
        {
            Script("SmiEditor.inputText", text);
        }

        public void AfterTransform(string text)
        {
            Script("SmiEditor.afterTransform", text);
        }
        #endregion
    }
}
