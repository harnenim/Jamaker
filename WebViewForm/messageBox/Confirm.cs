using System.Runtime.Versioning;

namespace WebViewForm
{
    [SupportedOSPlatform("windows")]
    public partial class Confirm : Form
    {
        private readonly Form form;
        private readonly string msg;

        public Confirm(Form form, string msg, string title)
        {
            InitializeComponent();

            this.form = form;
            labelMsg.Text = this.msg = msg;
            Text = title;
            ActiveControl = btnYes;

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

            Font font = new("맑은 고딕", 8);
            Size size = TextRenderer.MeasureText(msg, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.WordBreak);
            int width = size.Width + 16;
            Width = (width < 202 ? 202 : (width < 400 ? width : 400)) + w;

            size = TextRenderer.MeasureText(msg, font, new Size(384, int.MaxValue), TextFormatFlags.WordBreak);
            Height = size.Height + 120 + h;

            Location = new Point(
                form.Location.X + (form.Width  / 2) - (Width  / 2)
            ,   form.Location.Y + (form.Height / 2) - (Height / 2)
            );
        }

        private void OnClick(object sender, EventArgs e)
        {
            if (sender == btnYes)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (sender == btnNo)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    if (sender == btnYes)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else if (sender == btnNo)
                    {
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }
                    break;
                case Keys.Escape:
                    DialogResult = DialogResult.Cancel;
                    Close();
                    break;
            }
        }
    }
}
