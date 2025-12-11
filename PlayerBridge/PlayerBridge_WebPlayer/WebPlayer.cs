using Jamaker;
using System.Windows.Forms;

namespace PlayerBridge
{
    public class WebPlayer : PlayerBridge
    {
        #region 초기화

        const int WM_USER = 0x0400;
        const int WM_COPYDATA = 0x004A;

        // 플레이어 명령
        /*
        const int POT_COMMAND = WM_USER;
        const int POT_GET_VOLUME = 0x5000; // 0 ~ 100
        const int POT_SET_VOLUME = 0x5001; // 0 ~ 100
        const int POT_GET_TOTAL_TIME = 0x5002; // ms unit
        const int POT_GET_PROGRESS_TIME = 0x5003; // ms unit
        const int POT_GET_CURRENT_TIME = 0x5004; // ms unit
        const int POT_SET_CURRENT_TIME = 0x5005; // ms unit
        const int POT_GET_PLAY_STATUS = 0x5006; // 0:Stopped, 1:Paused, 2:Running
        const int POT_SET_PLAY_STATUS = 0x5007; // 0:Toggle, 1:Paused, 2:Running
        const int POT_SET_PLAY_ORDER = 0x5008; // 0:Prev, 1:Next
        const int POT_SET_PLAY_CLOSE = 0x5009;
        const int POT_SEND_VIRTUAL_KEY = 0x5010; // Virtual Key(VK_UP, VK_DOWN....)

        const int POT_GET_AVISYNTH_USE = 0x6000;
        const int POT_SET_AVISYNTH_USE = 0x6001; // 0: off, 1:on
        const int POT_GET_VAPOURSYNTH_USE = 0x6010;
        const int POT_SET_VAPOURSYNTH_USE = 0x6011; // 0: off, 1:on
        const int POT_GET_VIDEO_WIDTH = 0x6030;
        const int POT_GET_VIDEO_HEIGHT = 0x6031;
        const int POT_GET_VIDEO_FPS = 0x6032; // scale by 1000

        // String getting
        const int POT_GET_AVISYNTH_SCRIPT = 0x6002;
        const int POT_GET_VAPOURSYNTH_SCRIPT = 0x6012;
        const int POT_GET_PLAYFILE_NAME = 0x6020;

        // String setting... Using WM_COPYDATA
        const int POT_SET_AVISYNTH_SCRIPT = 0x6003;
        const int POT_SET_VAPOURSYNTH_SCRIPT = 0x6013;
        const int POT_SET_SHOW_MESSAGE = 0x6040;
        const int POT_SET_PLAYFILE = 1000; // UTF-16 Unicode
        */
        const int POT_COMMAND = WM_USER;
        const int POT_GET_CURRENT_TIME = 0x5004; // ms unit
        const int POT_SET_CURRENT_TIME = 0x5005; // ms unit
        const int POT_SET_PLAY_STATUS = 0x5007; // 0:Toggle, 1:Paused, 2:Running
        const int POT_SET_PLAY_CLOSE = 0x5009;

        const int POT_GET_VIDEO_FPS = 0x6032; // scale by 1000

        const int POT_GET_PLAYFILE_NAME = 0x6020;
        // String setting... Using WM_COPYDATA
        const int POT_SET_PLAYFILE = 1000; // UTF-16 Unicode

        #endregion

        MainForm? playerForm;

        protected new int FindPlayer()
        {
            return hwnd;
        }

        public new void RunPlayer(string _, Action err)
        {
            try
            {
                playerForm = new MainForm();
                playerForm.Show();
                hwnd = (int)playerForm.Handle;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            if (hwnd == 0) { err(); }
        }

        public new void DoExit()
        {
            playerForm?.Close();
        }

        public override int OpenFile(string path)
        {
            playerForm?.OpenFile(path);
            return 0;
        }
        private string? fileName;
        public override int GetFileName()
        {
            fileName = playerForm?.GetFileName();
            return WinAPI.SendMessage(ownerHwnd, POT_COMMAND, POT_GET_PLAYFILE_NAME, hwnd); //가라로 메시지 보내서 동작시킴
        }
        public override string? AfterGetFileName(Message m)
        {
            return fileName;
        }

        public override int GetFps() { return (playerForm == null) ? 23976 : playerForm.GetFps(); }
        public override int PlayOrPause() { return (playerForm == null) ? 0 : playerForm.PlayOrPause(); }
        public override int Pause() { return (playerForm == null) ? 0 : playerForm.Pause(); }
        public override int Play() { return (playerForm == null) ? 0 : playerForm.Play(); }
        public override int Stop() { return (playerForm == null) ? 0 : playerForm.Stop(); }
        public override int GetTime() { return (playerForm == null) ? 0 : playerForm.GetTime(); }
        public override int MoveTo(int time) { return (playerForm == null) ? 0 : playerForm.MoveTo(); }
    }
}
