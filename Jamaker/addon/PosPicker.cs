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

            public Pos? b1 = null;
            public Pos? b2 = null;

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
        private readonly List<Pos> bezier1s = [], bezier2s = [];
        private int moving = -1, split = -1, bezier = -1;
        private int addX, addY;
        private bool isNew = false, isFirst = false;

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
                inputValue.Width = 300;
                inputValue.Height = ih;
                btnOk.Top = iy;
                btnOk.Left = ix + 300;
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
            char status = 'm';
            int d = 0;
            double x = 0;
            points.Clear();
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i].Trim();
                if (double.TryParse(value, out double v))
                {
                    if (status == 'b')
                    {
                        // 점 하나도 없이 bezier가 나올 순 없음
                        if (points.Count == 0) continue;

                        if ((i - d) % 2 == 0)
                        {
                            {   // x 좌표 차례
                                x = v;
                            }
                        }
                        else
                        {
                            if (i == d + 1)
                            {   // y1
                                points[^1].b1 = Pos.FromVideo(x, v);
                            }
                            else
                            {   // y2
                                points[^1].b2 = Pos.FromVideo(x, v);
                                // 좌표값 x1 y1 x2 y2 다 나왔으면 다시 l로 변경
                                status = 'l';
                                d = i + 1;
                            }
                        }
                    }
                    else
                    {
                        if ((i - d) % 2 == 0)
                        {   // x 좌표 차례
                            x = v;
                        }
                        else
                        {
                            points.Add(Pos.FromVideo(x, v));
                        }
                    }
                }
                else
                {
                    status = value.Length > 0 ? value[0] : '-'; // m, l, b만 나와야 함
                    d = i + 1; // 다음 값은 x 좌표
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
            if (bezier1s.Count > 0)
            {
                for (int i = 0; i < bezier1s.Count; i++)
                {
                    Pos point = bezier1s[i];
                    int dx = point.DX - e.X;
                    int dy = point.DY - e.Y;
                    if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                    {   // 곡선 편집 시작
                        bezier = i;
                        addX = dx;
                        addY = dy;
                        Render();
                        return;
                    }
                }
                for (int i = 0; i < bezier2s.Count; i++)
                {
                    Pos point = bezier2s[i];
                    int dx = point.DX - e.X;
                    int dy = point.DY - e.Y;
                    if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                    {   // 곡선 편집 시작
                        bezier = points.Count + i;
                        addX = dx;
                        addY = dy;
                        Render();
                        return;
                    }
                }
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
            else if (bezier1s.Count > 0)
            {
                for (int i = 0; i < bezier1s.Count; i++)
                {
                    Pos point = bezier1s[i];
                    int dx = point.DX - e.X;
                    int dy = point.DY - e.Y;
                    if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                    {   // 점 이동 시작 가능 영역
                        movable = true;
                        break;
                    }
                }
                if (!movable)
                {
                    for (int i = 0; i < bezier2s.Count; i++)
                    {
                        Pos point = bezier2s[i];
                        int dx = point.DX - e.X;
                        int dy = point.DY - e.Y;
                        if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                        {   // 점 이동 시작 가능 영역
                            movable = true;
                            break;
                        }
                    }
                }
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
            int count = points.Count;
            if (count == 0)
            {
                Invalidate();
                return;
            }

            if (withText)
            {
                string text = "m";
                for (int i = 0; i < count; i++)
                {
                    Pos p = points[i];
                    text += $"{(i != 0 && i == 1 ? "\r\nl" : "")}\r\n{(moving == i ? pointer : p).VX} {(moving == i ? pointer : p).VY}";
                    if (split == i)
                    {   // 선 나눌 땐 직선만 가능
                        text += $"{(i == 0 ? "\r\nl" : "")}\r\n{pointer.VX} {pointer.VY}";
                    }
                    else
                    {   // 곡선인 경우
                        Pos? b1 = p.b1, b2 = p.b2;
                        if (i == bezier)
                        {
                            b1 = pointer;
                            b2 = bezier2s[i];
                        }
                        else if (i + count == bezier)
                        {
                            b1 = bezier1s[i];
                            b2 = pointer;
                        }
                        if (b1 != null && b2 != null)
                        {
                            text += $"\r\nb {b1.VX} {b1.VY} {b2.VX} {b2.VY}";
                        }
                    }
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
                if (p.b1 != null && p.b2 != null)
                {
                    minX = Math.Min(minX, p.b1.DX);
                    minY = Math.Min(minY, p.b1.DY);
                    maxX = Math.Max(maxX, p.b1.DX);
                    maxY = Math.Max(maxY, p.b1.DY);
                    minX = Math.Min(minX, p.b2.DX);
                    minY = Math.Min(minY, p.b2.DY);
                    maxX = Math.Max(maxX, p.b2.DX);
                    maxY = Math.Max(maxY, p.b2.DY);
                }
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
                int count = points.Count;

                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias; // 선 부드럽게 처리

                Region screenRegion = new(ClientRectangle);
                SolidBrush outsideBrush = new(Color.FromArgb(127, Color.Black));
                if (count == 0)
                {   // 점 없으면 배경만 그려줌
                    g.FillRegion(outsideBrush, screenRegion);
                    return;
                }

                Pen line = new(Color.Red, 1);
                if (count >= 2)
                {
                    using GraphicsPath path = new();

                    for (int i = 0; i < count; i++)
                    {
                        Pos p1 = points[i];
                        Pos p2 = points[(i + 1) % count];
                        Pos? b1 = p1.b1, b2 = p1.b2;

                        // 곡선 편집 확인
                        if (i == bezier)
                        {
                            b1 = pointer;
                            b2 = bezier2s[i];
                        }
                        else if (i + count == bezier)
                        {
                            b1 = bezier1s[i];
                            b2 = pointer;
                        }

                        if (i == split)
                        {   // 분할 중이면 중간 점 추가
                            Pos pm = pointer;
                            path.AddLine(p1.DX, p1.DY, pm.DX, pm.DY);
                            path.AddLine(pm.DX, pm.DY, p2.DX, p2.DY);
                        }
                        else if (b1 == null || b2 == null)
                        {   // 직선
                            if (i == moving) p1 = pointer; // 점 이동 중이면 치환
                            if (moving >= 0 && i == (moving + count - 1) % count) p2 = pointer;
                            path.AddLine(p1.DX, p1.DY, p2.DX, p2.DY);
                        }
                        else
                        {   // 곡선
                            Pos p0 = (i == moving) ? pointer : p1; // 점 이동 중이면 치환
                            if (moving >= 0 && i == (moving + count - 1) % count) p2 = pointer;
                            path.AddBezier(p0.DX, p0.DY
                                , b1.DX, b1.DY
                                , b2.DX, b2.DY
                                , p2.DX, p2.DY);
                            if (bezier1s.Count > 0)
                            {   // Ctrl 누른 상태면 가이드라인 보여주기

                            }
                        }
                    }
                    screenRegion.Exclude(path);
                    e.Graphics.DrawPath(line, path);
                }
                g.FillRegion(outsideBrush, screenRegion);

                if (mode == 2
                 && splitPoints.Count == 0
                 && bezier1s.Count == 0
                 && !isFirst)
                {   // 다각형 편집 중이면 현재 마우스 위치로 이어지는 선을 추가로 그려줌
                    line.DashStyle = DashStyle.Custom;
                    line.DashPattern = [0.5f, 3f];
                    if (moving < 0)
                    {
                        e.Graphics.DrawLine(line, points[0].DX, points[0].DY, pointer.DX, pointer.DY);
                        if (count > 1)
                        {
                            e.Graphics.DrawLine(line, points[^1].DX, points[^1].DY, pointer.DX, pointer.DY);
                        }
                    }
                    else if (isNew && count == 2)
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
                    for (int i = 0; i < count; i++)
                    {
                        Pos p = points[i];
                        // 원이 그려질 바운딩 박스(사각형 영역) 계산
                        int x = p.DX - 2;
                        int y = p.DY - 2;
                        int size = 2 * 2;

                        // 원 내부 채우기
                        g.FillEllipse(Brushes.White, x, y, size, size);

                        // 원 테두리 그리기
                        g.DrawEllipse(Pens.Red, x, y, size, size);
                    }
                }
                else if (bezier1s.Count > 0)
                {
                    for (int i = 0; i < bezier1s.Count; i++)
                    {
                        Pos[] ps = [(i == bezier) ? pointer : bezier1s[i], (i + count == bezier) ? pointer : bezier2s[i]];
                        foreach (Pos p in ps)
                        {
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
                    for (int i = 0; i < count; i++)
                    {
                        Pos p = points[i];
                        // 원이 그려질 바운딩 박스(사각형 영역) 계산
                        int x = p.DX - 2;
                        int y = p.DY - 2;
                        int size = 2 * 2;

                        // 원 내부 채우기
                        g.FillEllipse(Brushes.White, x, y, size, size);

                        // 원 테두리 그리기
                        g.DrawEllipse(Pens.Red, x, y, size, size);
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
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
                // 해당 부분 bezier 제거
                points[split].b1 = null;
                points[split].b2 = null;
                split = -1;
                // 분할 후 새 분할점 그려야 함
                RefreshSplit();
                return;
            }
            if (bezier >= 0)
            {   // 곡선 편집 종료
                if (bezier < points.Count)
                {
                    bezier1s[bezier] = pointer.Clone();
                    points[bezier].b1 = pointer.Clone();
                    points[bezier].b2 = bezier2s[bezier].Clone();
                }
                else
                {
                    bezier -= points.Count;
                    bezier2s[bezier] = pointer.Clone();
                    points[bezier].b2 = pointer.Clone();
                    points[bezier].b1 = bezier1s[bezier].Clone();
                }
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
                    Pos p = points[i];
                    int dx = p.DX - e.X;
                    int dy = p.DY - e.Y;
                    if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                    {
                        moving = -1;
                        // bezier 전후 연결 혹은 삭제
                        Pos p0 = points[(i + points.Count - 1) % points.Count];
                        if (p0.b1 != null && p.b2 != null)
                        {
                            p0.b2 = p.b2;
                        }
                        else
                        {
                            p0.b1 = null;
                            p0.b2 = null;
                        }
                        points.RemoveAt(i);
                        Render();
                        return;
                    }
                }
                if (bezier1s.Count > 0)
                {
                    for (int i = 0; i < bezier1s.Count; i++)
                    {
                        Pos[] ps = [bezier1s[i], bezier2s[i]];
                        foreach(Pos p in ps)
                        {
                            int dx = p.DX - e.X;
                            int dy = p.DY - e.Y;
                            if (-PR < dx && dx < PR && -PR < dy && dy < PR)
                            {
                                // bezier 삭제
                                Pos p0 = points[i];
                                p0.b1 = null;
                                p0.b2 = null;
                                Render();
                                return;
                            }
                        }
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
                    RefreshSplit();
                    break;
                case Keys.ControlKey: // Ctrl 누르면 곡선 편집
                    RefreshBezier();
                    break;
                case Keys.A: // A 누르면 입력 처리
                    ClickBtnOk(this, e);
                    break;
            }
        }
        private void RefreshSplit()
        {
            splitPoints.Clear();
            int count = points.Count;
            if (count > 3)
            {
                for (int i = 0; i < count; i++)
                {
                    splitPoints.Add(Pos.FromDisplay(
                            (points[i].DX + points[(i + 1) % count].DX) / 2
                        ,   (points[i].DY + points[(i + 1) % count].DY) / 2
                    ));
                }
                Render();
            }
        }
        private void RefreshBezier()
        {
            bezier1s.Clear();
            bezier2s.Clear();
            int count = points.Count;
            if (count > 3)
            {
                for (int i = 0; i < count; i++)
                {
                    Pos p1 = points[i];
                    if (p1.b1 != null && p1.b2 != null)
                    {
                        bezier1s.Add(p1.b1);
                        bezier2s.Add(p1.b2);
                    }
                    else
                    {
                        Pos p2 = points[(i + 1) % count];
                        bezier1s.Add(Pos.FromDisplay((p1.DX * 2 + p2.DX) / 3, (p1.DY * 2 + p2.DY) / 3));
                        bezier2s.Add(Pos.FromDisplay((p1.DX + p2.DX * 2) / 3, (p1.DY + p2.DY * 2) / 3));
                    }
                }
                Render();
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
                    bezier1s.Clear();
                    bezier2s.Clear();
                    Render();
                    break;
            }
        }

        private void OnTextChanged(object? sender, EventArgs e)
        {
            if (moving < 0 && split < 0 && bezier < 0)
            {   // 텍스트를 직접 수정한 경우
                RefreshPoints(inputValue.Text);
            }
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
