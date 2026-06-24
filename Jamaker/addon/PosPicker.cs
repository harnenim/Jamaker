using System.Drawing.Drawing2D;

namespace Jamaker.addon
{
    public partial class PosPicker : Form
    {
        private readonly MainForm _;
        private readonly int type;

        private class Pos
        {
            public static int px, py;
            public static double ratio;

            private int dx, dy, vx, vy;

            public int DX { get { return dx; } set { dx = value; vx = (int)Math.Round((dx - px) * ratio); } }
            public int DY { get { return dy; } set { dy = value; vy = (int)Math.Round((dy - py) * ratio); } }
            public double VX { get { return vx; } set { vx = (int)Math.Round(value); dx = (int)Math.Round(vx / ratio + px); } }
            public double VY { get { return vy; } set { vy = (int)Math.Round(value); dy = (int)Math.Round(vy / ratio + py); } }
            public Point DisplayPoint { get { return new(DX, DY); } }

            public static Pos FromDisplay(int x, int y)
            {
                return new() { DX = x, DY = y };
            }
            public static Pos FromVideo(double x, double y)
            {
                return new() { VX = x, VY = y };
            }
        }

        private readonly List<Pos> points = [];
        private readonly Pos pointer = new();
        private int moving = -1, addX, addY;

        private Rectangle? lastRenderRange = null;

        public PosPicker(MainForm _, int px, int py, double ratio
            , int type, string value
            , int ix, int iy, int iw, int ih)
        {
            InitializeComponent();
            this._ = _;
            Pos.px = px;
            Pos.py = py;
            Pos.ratio = ratio;
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
                inputValue.Width = 200;
                inputValue.Height = ih;
                btnOk.Top = iy;
                btnOk.Left = ix + 200;
            }
            MouseDown += OnMouseDownForPosPicker;
            MouseMove += OnMouseMoveForPosPicker;
            MouseUp += OnMouseUpForPosPicker;
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

            string[] values = value.Split(" ");
            int d = 0;
            double x = 0;
            string text = "";
            for (int i = 0; i < values.Length; i++) {
                value = values[i].Trim();
                if (double.TryParse(value, out double v))
                {
                    if (i % 2 == d)
                    { // x 좌표 차례
                        text += "\r\n" + value;
                        x = v;
                    } else {
                        text += " " + value;
                        points.Add(Pos.FromVideo(x, v));
                    }
                }
                else
                {
                    text += (i == 0 ? "" : "\r\n") + value;
                    d = (i + 1) % 2; // m이나 l 다음에 x 좌표가 나옴
                }
            }
            Render();
        }

        public void OnMouseDownForPosPicker(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Pos pos = points[i];
                int dx = pos.DX - e.X;
                int dy = pos.DY - e.Y;
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
                        points.Add(Pos.FromDisplay(e.X, e.Y));
                        points.Add(Pos.FromDisplay(e.X, e.Y));
                        points.Add(Pos.FromDisplay(e.X, e.Y));
                        points.Add(Pos.FromDisplay(e.X, e.Y));
                        moving = 2;
                        Render();
                        break;
                    }
                case 2: // 다각형
                    {
                        points.Add(Pos.FromDisplay(e.X, e.Y));
                        moving = points.Count - 1;
                        Render();
                        break;
                    }
            }
        }
        public void OnMouseMoveForPosPicker(object? sender, MouseEventArgs e)
        {
            pointer.DX = e.X;
            pointer.DY = e.Y;

            if (moving >= 0)
            {   // 점 이동
                Pos p = points[moving];
                int dx = p.DX = e.X + addX;
                int dy = p.DY = e.Y + addY;
                if (type == 1)
                {   // 사각형이면 이웃 모서리 함께 이동
                    int prev = (moving + 3) % 4;
                    int next = (moving + 1) % 4;
                    if (moving % 2 == 0)
                    {
                        points[prev].DX = dx;
                        points[next].DY = dy;
                    }
                    else
                    {
                        points[prev].DY = dy;
                        points[next].DX = dx;
                    }
                }
                Render();
                return;
            }
            bool movable = false;
            for (int i = 0; i < points.Count; i++)
            {
                Pos point = points[i];
                int dx = point.DX - e.X;
                int dy = point.DY - e.Y;
                if (-2 < dx && dx < 2 && -2 < dy && dy < 2)
                {   // 점 이동 시작 가능 영역
                    movable = true;
                    break;
                }
            }
            Cursor = movable ? Cursors.Hand : Cursors.Cross;

            switch (type)
            {
                case 1: // 사각형
                {
                    break;
                }
                case 2: // 다각형
                {
                    Render();
                    break;
                }
                default:
                {
                    border.Top = e.Y + 4;
                    border.Left = e.X + 4;
                    labelPos.Top = e.Y + 5;
                    labelPos.Left = e.X + 5;
                    labelPos.Text = $"{pointer.VX},{pointer.VY}";
                    break;
                }
            }
        }
        private void Render()
        {
            if (points.Count == 0) return;

            string text = $"m\r\n{points[0].VX} {points[0].VY}\r\nl";
            for (int i = 1; i < points.Count; i++) {
                text += $"\r\n{points[i].VX} {points[i].VY}";
            }
            inputValue.Text = text;

            int minX = pointer.DX, minY = pointer.DY, maxX = pointer.DX, maxY = pointer.DY;
            foreach (Pos p in points)
            {
                minX = Math.Min(minX, p.DX);
                minY = Math.Min(minY, p.DY);
                maxX = Math.Max(maxX, p.DX);
                maxY = Math.Max(maxY, p.DY);
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
            using Pen myPen = new(Color.FromArgb(127, Color.Red), 1);
            if (points.Count == 2)
            {
                e.Graphics.DrawLine(myPen, points[0].DX, points[0].DY, points[1].DX, points[1].DY);
            }
            else if (points.Count > 2)
            {
                using GraphicsPath polygonPath = new();
                Point[] arr = new Point[points.Count];
                for (int i = 0; i < points.Count; i++) {
                    arr[i] = points[i].DisplayPoint;
                }
                polygonPath.AddPolygon(arr);
                screenRegion.Exclude(polygonPath);
                e.Graphics.DrawPath(myPen, polygonPath);
            }
            using SolidBrush outsideBrush = new(Color.FromArgb(127, Color.Black));
            g.FillRegion(outsideBrush, screenRegion);

            if (type == 2 && moving < 0)
            {   // 다각형이면 현재 마우스 위치로 이어지는 선을 추가로 그려줌
                e.Graphics.DrawLine(myPen, points[0].DX, points[0].DY, pointer.DX, pointer.DY);
                if (points.Count > 1)
                {
                    e.Graphics.DrawLine(myPen, points[points.Count - 1].DX, points[points.Count - 1].DY, pointer.DX, pointer.DY);
                }
            }

            // [B] 각 꼭짓점에 선택할 수 있는 작은 사각형(핸들) 그리기
            foreach (Pos pt in points)
            {
                // 원이 그려질 바운딩 박스(사각형 영역) 계산
                int x = pt.DX - 3;
                int y = pt.DY - 3;
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
                default:
                    {
                        _.InputText($"{pointer.VX},{pointer.VY}");
                        Close();
                        return;
                    }
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
            _.InputText(inputValue.Text.Replace("\r\n", " "));
            Close();
        }
    }
}
