using Newtonsoft.Json.Linq;
using System.Drawing.Drawing2D;

namespace Jamaker.addon
{
    public partial class PosPicker : Form
    {
        private readonly MainForm _;
        private readonly int mode;

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
            public Pos Clone() {
                return new() { dx = dx, dy = dy, vx = vx, vy = vy };
            }
        }

        private readonly int PR = 3;

        private readonly Pos pointer = new();
        private readonly List<Pos> points = [];
        private readonly List<Pos> splitPoints = [];
        private readonly List<Pos[]> bezierPoints = [];
        private int moving = -1, split = -1, bezier = -1;
        private int addX, addY;
        private bool isNew = false, isFirst = true;

        private Rectangle? lastRenderRange = null;

#pragma warning disable IDE0060
        public PosPicker(MainForm _, int mode, int ox, int oy, string value
            , int px, int py, double ratio
            , int ix, int iy, int iw, int ih) // width도 필요할까 해서 iw를 넣었는데, 당장은 안 쓰는 중
#pragma warning restore IDE0060
        {
            InitializeComponent();
            this._ = _;
            Pos.px = px;
            Pos.py = py;
            Pos.ratio = ratio;
            if ((this.mode = mode) == 0)
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
            DoubleBuffered = true;

            Rectangle virtualScreen = SystemInformation.VirtualScreen;
            Bitmap screenCapture = new(virtualScreen.Width, virtualScreen.Height);
            Graphics g = Graphics.FromImage(screenCapture);
            g.CopyFromScreen(virtualScreen.Left, virtualScreen.Top, 0, 0, virtualScreen.Size);
            BackgroundImage = screenCapture;

            RefreshPoints(value);
        }

        private void AfterShown(object? sender, EventArgs e)
        {   // 가끔 포커스가 돌아가 버리는 경우가 있어서 재활성화
            Activate();
            Focus();
        }

        private void RefreshPoints(string input)
        {
            string[] values = input.Replace("\n", " ").Split(" ");
            int d = 0;
            double x = 0;
            points.Clear();
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i].Trim();
                if (double.TryParse(value, out double v))
                {
                    if (i % 2 == d)
                    { // x 좌표 차례
                        x = v;
                    }
                    else
                    {
                        points.Add(Pos.FromVideo(x, v));
                    }
                }
                else
                {
                    d = (i + 1) % 2; // m이나 l 다음에 x 좌표가 나옴
                }
            }
            if (points.Count > 0) {
                pointer.DX = points[0].DX;
                pointer.DY = points[0].DY;
            }
            Render(input != inputValue.Text);
        }

        public void OnMouseDownForPosPicker(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Pos pos = points[i];
                int dx = pos.DX - e.X;
                int dy = pos.DY - e.Y;
                if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                {   // 점 이동 시작
                    isFirst = false;
                    isNew = false;
                    moving = i;
                    addX = dx;
                    addY = dy;
                    Render();
                    return;
                }
            }
            if (splitPoints.Count > 0)
            {
                for (int i = 0; i < splitPoints.Count; i++)
                {
                    Pos pos = splitPoints[i];
                    int dx = pos.DX - e.X;
                    int dy = pos.DY - e.Y;
                    if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                    {   // 점 나누기 시작
                        split = i;
                        addX = dx;
                        addY = dy;
                        Render();
                        return;
                    }
                }
                return;
            }

            switch (mode)
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
                        if (points.Count == 0)
                        {   // 최초 드래그는 사각형 그리기로 시작
                            points.Add(Pos.FromDisplay(e.X, e.Y));
                            points.Add(Pos.FromDisplay(e.X, e.Y));
                            points.Add(Pos.FromDisplay(e.X, e.Y));
                            points.Add(Pos.FromDisplay(e.X, e.Y));
                            isFirst = true;
                            moving = 2;
                        }
                        else
                        {   // 새 점 추가
                            points.Add(Pos.FromDisplay(e.X, e.Y));
                            isNew = true;
                            moving = points.Count - 1;
                        }
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
                int dx = pointer.DX = e.X + addX;
                int dy = pointer.DY = e.Y + addY;
                if (mode == 1 || isFirst)
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
            if (split >= 0 || bezier >= 0)
            {   // 점 분할 / 곡선 작업 중
                pointer.DX = e.X + addX;
                pointer.DY = e.Y + addY;
                Render();
                return;
            }

            bool movable = false;
            if (splitPoints.Count > 0)
            {
                for (int i = 0; i < splitPoints.Count; i++)
                {
                    Pos point = splitPoints[i];
                    int dx = point.DX - e.X;
                    int dy = point.DY - e.Y;
                    if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                    {   // 점 이동 시작 가능 영역
                        movable = true;
                        break;
                    }
                }
            }
            else if (bezierPoints.Count > 0)
            {
            }
            else
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Pos point = points[i];
                    int dx = point.DX - e.X;
                    int dy = point.DY - e.Y;
                    if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                    {   // 점 이동 시작 가능 영역
                        movable = true;
                        break;
                    }
                }
            }
            Cursor = movable ? Cursors.Hand : Cursors.Cross;

            switch (mode)
            {
                case 1: // 사각형
                    break;
                case 2: // 다각형
                    Render(); // 새 점 예상 위치 렌더링 필요
                    break;
                default:
                    border.Top = e.Y + 4;
                    border.Left = e.X + 4;
                    labelPos.Top = e.Y + 5;
                    labelPos.Left = e.X + 5;
                    labelPos.Text = $"{pointer.VX},{pointer.VY}";
                    break;
            }
        }
        private void Render() { Render(true); }
        private void Render(bool withText)
        {
            if (points.Count == 0)
            {
                Invalidate();
                return;
            }

            if (withText)
            {
                string text = $"m\r\n{(moving == 0 ? pointer : points[0]).VX} {(moving == 0 ? pointer : points[0]).VY}\r\nl";
                if (split == 0) text += $"\r\n{pointer.VX} {pointer.VY}";
                for (int i = 1; i < points.Count; i++)
                {
                    text += $"\r\n{(moving == i ? pointer : points[i]).VX} {(moving == i ? pointer : points[i]).VY}";
                    if (split == i) text += $"\r\n{pointer.VX} {pointer.VY}";
                }
                inputValue.Text = text;
            }

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
            try
            {

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias; // 선 부드럽게 처리

                Region screenRegion = new(ClientRectangle);
                SolidBrush outsideBrush = new(Color.FromArgb(127, Color.Black));
                if (points.Count == 0)
                {   // 점 없으면 배경만 그려줌
                    g.FillRegion(outsideBrush, screenRegion);
                    return;
                }

                Pen line = new(Color.Red, 1);
                if (points.Count == 2 && !isNew)
                {
                    e.Graphics.DrawLine(line, points[0].DX, points[0].DY, points[1].DX, points[1].DY);
                }
                else if (points.Count > 2)
                {
                    using GraphicsPath polygonPath = new();

                    if (splitPoints.Count > 0)
                    {   // Shift 누르고 분할 중
                        Point[] arr = new Point[points.Count * 2];
                        for (int i = 0; i < points.Count; i++)
                        {
                            arr[2 * i] = points[i].DisplayPoint;
                            arr[2 * i + 1] = ((i == split) ? pointer : splitPoints[i]).DisplayPoint;
                        }
                        polygonPath.AddPolygon(arr);
                    }
                    else
                    {
                        Point[] arr = new Point[points.Count];
                        for (int i = 0; i < points.Count; i++)
                        {
                            arr[i] = ((i == moving) ? pointer : points[i]).DisplayPoint;
                        }
                        polygonPath.AddPolygon(arr);
                    }
                    screenRegion.Exclude(polygonPath);
                    e.Graphics.DrawPath(line, polygonPath);
                }
                g.FillRegion(outsideBrush, screenRegion);

                if (mode == 2
                 && splitPoints.Count == 0
                 && bezierPoints.Count == 0
                 && !isFirst)
                {   // 다각형 편집 중이면 현재 마우스 위치로 이어지는 선을 추가로 그려줌
                    line.DashStyle = DashStyle.Custom;
                    line.DashPattern = [0.5f, 3f];
                    if (moving < 0)
                    {
                        e.Graphics.DrawLine(line, points[0].DX, points[0].DY, pointer.DX, pointer.DY);
                        if (points.Count > 1)
                        {
                            e.Graphics.DrawLine(line, points[^1].DX, points[^1].DY, pointer.DX, pointer.DY);
                        }
                    }
                    else if (isNew && points.Count == 2)
                    {
                        e.Graphics.DrawLine(line, points[0].DX, points[0].DY, pointer.DX, pointer.DY);
                    }
                }

                // 점 표시
                if (splitPoints.Count > 0)
                {
                    for (int i = 0; i < splitPoints.Count; i++)
                    {
                        Pos p = (i == split) ? pointer : splitPoints[i];
                        // 원이 그려질 바운딩 박스(사각형 영역) 계산
                        int x = p.DX - 3;
                        int y = p.DY - 3;
                        int size = 2 * 3;

                        // 원 내부 채우기
                        g.FillEllipse(Brushes.White, x, y, size, size);

                        // 원 테두리 그리기
                        g.DrawEllipse(Pens.Black, x, y, size, size);
                    }
                    for (int i = 0; i < points.Count; i++)
                    {
                        Pos p = (i == moving) ? pointer : points[i];
                        // 원이 그려질 바운딩 박스(사각형 영역) 계산
                        int x = p.DX - 2;
                        int y = p.DY - 2;
                        int size = 2 * 2;

                        // 원 내부 채우기
                        g.FillEllipse(Brushes.White, x, y, size, size);

                        // 원 테두리 그리기
                        g.DrawEllipse(Pens.Black, x, y, size, size);
                    }
                }
                else if (bezierPoints.Count > 0)
                {
                    for (int i = 0; i < bezierPoints.Count; i++)
                    {
                        Pos p = (i == bezier) ? pointer : points[i];
                        // 원이 그려질 바운딩 박스(사각형 영역) 계산
                        int x = p.DX - 3;
                        int y = p.DY - 3;
                        int size = 2 * 3;

                        // 원 내부 채우기
                        g.FillEllipse(Brushes.White, x, y, size, size);

                        // 원 테두리 그리기
                        g.DrawEllipse(Pens.Black, x, y, size, size);
                    }
                    for (int i = 0; i < points.Count; i++)
                    {
                        Pos p = (i == moving) ? pointer : points[i];
                        // 원이 그려질 바운딩 박스(사각형 영역) 계산
                        int x = p.DX - 2;
                        int y = p.DY - 2;
                        int size = 2 * 2;

                        // 원 내부 채우기
                        g.FillEllipse(Brushes.White, x, y, size, size);

                        // 원 테두리 그리기
                        g.DrawEllipse(Pens.Black, x, y, size, size);
                    }
                }
                else
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        Pos p = (i == moving) ? pointer : points[i];
                        // 원이 그려질 바운딩 박스(사각형 영역) 계산
                        int x = p.DX - 3;
                        int y = p.DY - 3;
                        int size = 2 * 3;

                        // 원 내부 채우기
                        g.FillEllipse(Brushes.White, x, y, size, size);

                        // 원 테두리 그리기
                        g.DrawEllipse(Pens.Black, x, y, size, size);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }
        public void OnMouseUpForPosPicker(object? sender, MouseEventArgs e)
        {
            if (moving >= 0)
            {   // 점 이동 종료
                if (isFirst)
                {   // 다각형 최초 그리기일 때
                    if (points[moving].DX == pointer.DX && points[moving].DY == pointer.DY)
                    {   // 드래그로 사각형 만들지 않고 점만 찍음
                        points.RemoveRange(1, 3);
                    }
                    else
                    {
                        points[moving].DX = pointer.DX;
                        points[moving].DY = pointer.DY;
                    }
                    isFirst = false;
                }
                else
                {
                    points[moving].DX = pointer.DX;
                    points[moving].DY = pointer.DY;
                }
                moving = -1;
                isNew = false;
                return;
            }
            if (split >= 0)
            {   // 점 분할 종료
                points.Insert(split + 1, pointer.Clone());
                split = -1;
                return;
            }
            if (bezier >= 0)
            {   // 곡선 편집 종료
                bezier = -1;
                return;
            }

            switch (mode)
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
        public void OnClickForPosPicker(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {   // 우클릭 시 삭제
                for (int i = 0; i < points.Count; i++)
                {
                    Pos pos = points[i];
                    int dx = pos.DX - e.X;
                    int dy = pos.DY - e.Y;
                    if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                    {
                        moving = -1;
                        points.RemoveAt(i);
                        Render();
                        return;
                    }
                }

            }
        }

        public void OnKeyDownForPosPicker(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode) {
                case Keys.Escape:
                    if (moving >= 0)
                    {   // 이동 중이었으면 중지
                        if (mode == 1)
                        {   // 사각형이면 이웃 모서리 함께 복구
                            int prev = (moving + 3) % 4;
                            int next = (moving + 1) % 4;
                            if (moving % 2 == 0)
                            {
                                points[prev].DX = points[moving].DX;
                                points[next].DY = points[moving].DY;
                            }
                            else
                            {
                                points[prev].DY = points[moving].DY;
                                points[next].DX = points[moving].DX;
                            }
                        }
                        else if (isNew)
                        {   // 새로 만드는 걸 취소했으면 없애야 함
                            points.RemoveAt(moving);
                        }
                        moving = -1;
                        Render();
                    }
                    else
                    {   // 기본 상태였으면 종료
                        Close();
                    }
                    break;
                case Keys.ShiftKey: // Shift 누르면 선 분할
                    splitPoints.Clear();
                    if (points.Count > 3) {
                        for (int i = 0; i < points.Count - 1; i++)
                        {
                            splitPoints.Add(Pos.FromDisplay(
                                    (points[i].DX + points[i + 1].DX) / 2
                                ,   (points[i].DY + points[i + 1].DY) / 2
                            ));
                        }
                        splitPoints.Add(Pos.FromDisplay(
                                (points[0].DX + points[^1].DX) / 2
                            ,   (points[0].DY + points[^1].DY) / 2
                        ));
                        Render();
                    }
                    break;
                case Keys.ControlKey: // Ctrl 누르면 곡선
                    /*
                    if (points.Count > 0) {
                        pointer.DX = points[0].DX;
                        pointer.DY = points[0].DY;
                        Render();
                    }
                    */
                    break;
                case Keys.A: // A 누르면 입력 처리
                    ClickBtnOk(this, e);
                    break;
            }
        }
        public void OnKeyUpForPosPicker(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.ShiftKey: // 선 분할 취소
                    splitPoints.Clear();
                    Render();
                    break;
                case Keys.ControlKey: // 곡선 작업 취소
                    bezierPoints.Clear();
                    Render();
                    break;
            }
        }

        private void OnTextChanged(object? sender, EventArgs e)
        {
            if (moving + split + bezier >= 0) return; // 드래그로 인한 변화
            RefreshPoints(inputValue.Text);
        }

        private void ClickBtnOk(object sender, EventArgs e)
        {
            if (mode == 1)
            {   // 사각형
                if (points.Count == 4)
                {
                    _.InputText($"{points[0].VX},{points[0].VY},{points[2].VX},{points[2].VY}");
                }
            }
            else
            {   // 다각형
                _.InputText(inputValue.Text.Replace("\r\n", " "));
            }
            Close();
        }
    }
}
