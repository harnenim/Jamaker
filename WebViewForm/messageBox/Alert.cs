using System.Runtime.Versioning;

namespace WebViewForm
{
    [SupportedOSPlatform("windows")]
    public partial class Alert : Form
    {
        private readonly Form form;
        private readonly string msg;

        public Alert(Form form, string msg, string title)
        {
            InitializeComponent();

            this.form = form;
            labelMsg.Text = this.msg = msg.Replace("&", "&&");
            Text = title;

            StartPosition = FormStartPosition.Manual;
            Location = new Point(
                form.Location.X + (form.Width  / 2) - (Width  / 2)
            ,   form.Location.Y + (form.Height / 2) - (Height / 2)
            );
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            BeginInvoke(new Action(() => { ResizeForm(); }));
        }
        private void ResizeForm()
        {
            RECT shadow = WinAPI.GetWindowShadow(Handle.ToInt32());
            int w = shadow.left - shadow.right;
            int h = shadow.top - shadow.bottom;

            float scale = DeviceDpi / 96f;
            int minWidth  = (int)(131 * scale);
            int maxWidth  = (int)(400 * scale);
            int maxTWidth = (int)(384 * scale);
            int minHeight = (int)(120 * scale);

            Font font = new("맑은 고딕", 8);
            Size size = TextRenderer.MeasureText(msg, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine);
            int width = size.Width + (int)(20 * scale);
            Width = (width < minWidth ? minWidth : (width < maxWidth ? width : maxWidth)) + w;

            size = TextRenderer.MeasureText(msg, font, new Size(maxTWidth, int.MaxValue), TextFormatFlags.WordBreak);
            Height = size.Height + minHeight + h;

            Location = new Point(
                form.Location.X + (form.Width  / 2) - (Width  / 2)
            ,   form.Location.Y + (form.Height / 2) - (Height / 2)
            );
        }

        private void OnClick(object sender, EventArgs e)
        {
            Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                case Keys.Escape:
                    Close();
                    break;
            }
        }
    }
}
