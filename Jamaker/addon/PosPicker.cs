namespace Jamaker.addon
{
    public partial class PosPicker : Form
    {
        private readonly MainForm _;
        private readonly int px, py, type;
        private readonly double ratio;
        
        public PosPicker(MainForm _, int px, int py, double ratio, int type)
        {
            InitializeComponent();
            this._ = _;
            this.px = px;
            this.py = py;
            this.ratio = ratio;
            this.type = type;
            MouseMove += OnMouseMoveForPosPicker;
            MouseClick += OnMouseClickForPosPicker;
            KeyDown += OnKeyDownForPosPicker;
        }
        public double GetVX(int x)
        {
            return Math.Round((x - px) * ratio, 2);
        }
        public double GetVY(int y)
        {
            return Math.Round((y - py) * ratio, 2);
        }
        public void OnMouseMoveForPosPicker(object? sender, MouseEventArgs e)
        {
            border.Top = e.Y + 4;
            border.Left = e.X + 4;
            labelPos.Top = e.Y + 5;
            labelPos.Left = e.X + 5;
            labelPos.Text = $"{GetVX(e.X)},{GetVY(e.Y)}";
        }
        public void OnMouseClickForPosPicker(object? sender, MouseEventArgs e)
        {
            _.InputText($"{GetVX(e.X)},{GetVY(e.Y)}");
            Close();
        }

        public void OnKeyDownForPosPicker(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }
    }
}
