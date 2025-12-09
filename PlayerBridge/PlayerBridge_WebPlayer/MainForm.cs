using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WebViewForm;

namespace Jamaker
{
    public partial class MainForm : WebForm
    {
        public MainForm()
        {
            InitializeAsync("WebPlayer", new Binder(this));
            try { Icon = new Icon("view/jamaker.ico"); } catch (Exception) { }

            int[] rect = [0, 0, 1280, 710];

            StartPosition = FormStartPosition.Manual;
            Location = new Point(rect[0], rect[1]);
            Size = new Size(rect[2], rect[3]);

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            AllowTransparency = true;

            FormClosed += new FormClosedEventHandler(WebFormClosed);

            InitWebView();
        }

        private async void InitWebView()
        {
            mainView.Source = new Uri(Path.Combine(Application.StartupPath, "view/player.html"));
        }
        public override void InitAfterLoad(string title)
        {
            base.InitAfterLoad(title);
        }

        private void WebFormClosed(object? sender, FormClosedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        protected override void Drop(int x, int y)
        {
            Script("drop", x, y);
        }

        public void OpenFile(string path)
        {

        }

        public string GetFileName()
        {
            return "";
        }

        public int GetFps()
        {
            return 23976;
        }

        public int PlayOrPause()
        {
            return 23976;
        }

        public int Pause()
        {
            return 23976;
        }

        public int Play()
        {
            return 23976;
        }

        public int Stop()
        {
            return 23976;
        }

        public int GetTime()
        {
            return 23976;
        }

        public int MoveTo()
        {
            return 23976;
        }

        public void openFileByDrag()
        {
            
        }
        public void openFileDialog()
        {
            
        }
        public void close()
        {
            
        }
    }
}