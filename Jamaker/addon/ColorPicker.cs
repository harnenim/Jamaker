namespace Jamaker.addon
{
    public partial class ColorPicker : Form
    {
        private readonly MainForm _;

        public Bitmap buffer = new(1, 1);
        public Graphics pixel;
        public string? code;

        public ColorPicker(MainForm _)
        {
            InitializeComponent();
            this._ = _;
            pixel = Graphics.FromImage(buffer);
            MouseMove += OnMouseMoveForColorPicker;
            MouseClick += OnMouseClickForColorPicker;
            KeyDown += OnKeyDownForColorPicker;
        }
        public void OnMouseMoveForColorPicker(object? sender, MouseEventArgs e)
        {
            // 창 이동
            // TODO: 듀얼 모니터로, 서브모니터를 좌측에 두는 경우는??
            //WinAPI.MoveWindow(instance.Handle.ToInt32(), e.X - 4, e.Y - 4, 68, 26, true);

            // 색상 뽑기
            pixel.CopyFromScreen(e.X, e.Y, 0, 0, new Size(1, 1));
            Color color = buffer.GetPixel(0, 0);
            code = "#" + Convert.ToHexString([color.R, color.G, color.B]);

            border.Top = e.Y + 4;
            border.Left = e.X + 4;
            labelColor.Top = e.Y + 5;
            labelColor.Left = e.X + 5;
            labelColor.BackColor = color;
            labelColor.Text = code;
            labelColor.ForeColor = border.BackColor = (color.R + color.G + color.B < 256) ? Color.White : Color.Black;
        }
        public void OnMouseClickForColorPicker(object? sender, MouseEventArgs e)
        {
            _.InputText(code!);
            Close();
        }

        public void OnKeyDownForColorPicker(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }
    }
}
