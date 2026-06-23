using System.Drawing.Drawing2D;

namespace Jamaker.addon
{
    public partial class PosPicker : Form
    {
        private readonly MainForm _;
        private readonly int px, py, type;
        private readonly double ratio;

        private readonly List<Point> points = [];
        private int moving = -1, addX, addY;

        private Rectangle? lastRenderRange = null;

        public PosPicker(MainForm _, int px, int py, double ratio, int type, int ix, int iy, int iw, int ih)
        {
            InitializeComponent();
            this._ = _;
            this.px = px;
            this.py = py;
            this.ratio = ratio;
            if ((this.type = type) == 0)
            {
                inputValue.Visible = false;
                btnOk.Visible = false;
            }
            else
            {
                labelPos.Visible = false;
                border.Visible = false;
                inputValue.Left = ix;
                inputValue.Top = iy;
                inputValue.Width = iw;
                inputValue.Height = ih;
                btnOk.Top = iy;
                btnOk.Left = ix + iw;
            }
            MouseDown += OnMouseDownForPosPicker;
            MouseMove += OnMouseMoveForPosPicker;
            MouseUp += OnMouseUpForPosPicker;
            MouseClick += OnMouseClickForPosPicker;
            KeyDown += OnKeyDownForPosPicker;
            DoubleBuffered = true;

            {
                if (Screen.PrimaryScreen == null) return;

                Size screenSize = Screen.PrimaryScreen.Bounds.Size;
                Bitmap screenCapture = new(screenSize.Width, screenSize.Height);

                using (Graphics g = Graphics.FromImage(screenCapture))
                {
                    // 모니터 화면(0,0)부터 전체 크기만큼 내 비트맵으로 복사
                    g.CopyFromScreen(0, 0, 0, 0, screenSize);
                }
                BackgroundImage = screenCapture;
            }
        }

        private double GetVX(int x)
        {
            return GetV(x, px);
        }
        private double GetVY(int y)
        {
            return GetV(y, py);
        }
        private double GetV(int v, int p)
        {
            return Math.Round((v - p) * ratio, 1);
        }
        public void OnMouseDownForPosPicker(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Point point = points[i];
                int dx = point.X - e.X;
                int dy = point.Y - e.Y;
                if (-2 < dx && dx < 2 && -2 < dy && dy < 2)
                {   // 점 이동 시작
                    moving = i;
                    addX = dx;
                    addY = dy;
                    Render();
                    return;
                }
            }

            switch (type)
            {
                case 1: // 사각형
                    {
                        if (points.Count > 0)
                        {
                            points.Clear();
                        }
                        points.Add(new Point(e.X, e.Y));
                        points.Add(new Point(e.X, e.Y));
                        points.Add(new Point(e.X, e.Y));
                        points.Add(new Point(e.X, e.Y));
                        moving = 2;
                        Render();
                        break;
                    }
                case 2: // 다각형
                    {
                        points.Add(new Point(e.X, e.Y));
                        moving = 0;
                        Render();
                        break;
                    }
            }
        }
        public void OnMouseMoveForPosPicker(object? sender, MouseEventArgs e)
        {
            if (moving >= 0)
            {   // 점 이동
                Point p = points[moving] = new Point(e.X + addX, e.Y + addY);
                if (type == 1)
                {   // 사각형이면 이웃 모서리 함께 이동
                    int prev = (moving + 3) % 4;
                    int next = (moving + 1) % 4;
                    if (moving % 2 == 0)
                    {
                        points[prev] = new Point(p.X, points[prev].Y);
                        points[next] = new Point(points[next].X, p.Y);
                    }
                    else
                    {
                        points[prev] = new Point(points[prev].X, p.Y);
                        points[next] = new Point(p.X, points[next].Y);
                    }
                }
                Render();
                return;
            }
            bool movable = false;
            for (int i = 0; i < points.Count; i++)
            {
                Point point = points[i];
                int dx = point.X - e.X;
                int dy = point.Y - e.Y;
                if (-2 < dx && dx < 2 && -2 < dy && dy < 2)
                {   // 점 이동 시작 가능 영역
                    movable = true;
                    break;
                }
            }
            Cursor = movable ? Cursors.Default : Cursors.Cross;

            switch (type)
            {
                case 1: // 사각형
                {
                    break;
                }
                case 2: // 다각형
                {
                    break;
                }
                default:
                {
                    border.Top = e.Y + 4;
                    border.Left = e.X + 4;
                    labelPos.Top = e.Y + 5;
                    labelPos.Left = e.X + 5;
                    labelPos.Text = $"{GetVX(e.X)},{GetVY(e.Y)}";
                    break;
                }
            }
        }
        private void Render()
        {
            string text = $"m {GetVX(points[0].X)} {GetVY(points[0].Y)} l";
            for (int i = 1; i < points.Count; i++) {
                text += $" {GetVX(points[i].X)} {GetVY(points[i].Y)}";
            }
            inputValue.Text = text;

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (Point p in points)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }
            Rectangle last = lastRenderRange == null ? ClientRectangle : lastRenderRange.Value;
            Rectangle curr = new(minX, minY, maxX - minX, maxY - minY);
            minX = Math.Min(minX, last.X);
            minY = Math.Min(minY, last.Y);
            maxX = Math.Max(maxX, last.X + last.Width);
            maxY = Math.Max(maxY, last.Y + last.Height);
            Invalidate(new Rectangle(minX - 4, minY - 4, maxX - minX + 8, maxY - minY + 8));
            lastRenderRange = curr;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (points.Count == 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // 선 부드럽게 처리

            // [2] 화면 전체를 칠할 거대한 영역(Region) 생성 (폼 전체 크기)
            using Region screenRegion = new(ClientRectangle);
            if (points.Count > 2)
            {
                // [1] 다각형의 형태를 담을 그래픽 패스(Path) 생성
                using (GraphicsPath polygonPath = new())
                {
                    // 패스에 다각형 좌표 배열 추가
                    polygonPath.AddPolygon(points.ToArray());

                    // 전체 영역에서 다각형 영역을 '도려내기(제외)'
                    screenRegion.Exclude(polygonPath);
                }
            }
            // [3] 도려내고 남은 '바깥쪽 영역'에만 색상 채우기 (반투명 검은색)
            using SolidBrush outsideBrush = new(Color.FromArgb(150, Color.Black));
            g.FillRegion(outsideBrush, screenRegion);

            // [B] 각 꼭짓점에 선택할 수 있는 작은 사각형(핸들) 그리기
            foreach (Point pt in points)
            {
                // 원이 그려질 바운딩 박스(사각형 영역) 계산
                int x = pt.X - 3;
                int y = pt.Y - 3;
                int size = 2 * 3;

                // 원 내부 채우기
                g.FillEllipse(Brushes.White, x, y, size, size);

                // 원 테두리 그리기
                g.DrawEllipse(Pens.Black, x, y, size, size);
            }
        }
        public void OnMouseUpForPosPicker(object? sender, MouseEventArgs e)
        {
            if (moving >= 0)
            {   // 점 이동 종료
                moving = -1;
                return;
            }

            switch (type)
            {
                case 1: // 사각형
                    {
                        break;
                    }
                case 2: // 다각형
                    {
                        break;
                    }
            }
        }
        public void OnMouseClickForPosPicker(object? sender, MouseEventArgs e)
        {
            if (type == 0)
            {
                _.InputText($"{GetVX(e.X)},{GetVY(e.Y)}");
                Close();
            }
        }

        public void OnKeyDownForPosPicker(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void ClickBtnOk(object sender, EventArgs e)
        {
            _.InputText(inputValue.Text);
            Close();
        }
    }
}
