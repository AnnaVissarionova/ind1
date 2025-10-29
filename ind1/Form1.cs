using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

public class Point
{
    public double x { get; set; }
    public double y { get; set; }
    public Point next { get; set; }
    public Point prev { get; set; }

    public Point(double x, double y, Point next = null, Point prev = null)
    {
        this.x = x;
        this.y = y;
        this.next = next;
        this.prev = prev;
    }

    public void Print()
    {
        Console.WriteLine($"({x}, {y}) next: {next?.x},{next?.y} prev: {prev?.x},{prev?.y}");
    }

    public override bool Equals(object obj)
    {
        if (obj is Point p)
            return Math.Abs(p.x - x) < 1e-9 && Math.Abs(p.y - y) < 1e-9;
        return false;
    }

    public override int GetHashCode()
    {
        return (x + y).GetHashCode();
    }
}

public partial class MainForm : Form
{
    private const int SIZE = 700;
    private bool fill = false;
    private bool fullFigure = false;
    private bool fullFigure2 = false;
    private List<Point> polygon1 = new List<Point>();
    private List<Point> polygon2 = new List<Point>();
    private List<PointF> finalPoints = new List<PointF>();
    private List<PointF> points = new List<PointF>();
    private List<PointF> points2 = new List<PointF>();
    private PictureBox canvas;
    private Button clearButton;
    private Button goButton;

    public MainForm()
    {
        InitializeComponent();
        SetupUI();
    }

    private void InitializeComponent()
    {
        this.Text = "Объединение выпуклых оболочек";
        this.Size = new Size(SIZE + 200, SIZE + 100);
        this.StartPosition = FormStartPosition.CenterScreen;
    }

    private void SetupUI()
    {
        // Canvas
        canvas = new PictureBox
        {
            Width = SIZE,
            Height = SIZE,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new System.Drawing.Point(10, 10)
        };
        canvas.Paint += Canvas_Paint;
        canvas.MouseClick += Canvas_MouseClick;
        this.Controls.Add(canvas);

        // Clear button
        clearButton = new Button
        {
            Text = "Clear",
            Location = new System.Drawing.Point(SIZE + 20, 20),
            Size = new Size(80, 30)
        };
        clearButton.Click += ClearButton_Click;
        this.Controls.Add(clearButton);

        // Go button
        goButton = new Button
        {
            Text = "Go",
            Location = new System.Drawing.Point(SIZE + 20, 60),
            Size = new Size(80, 30)
        };
        goButton.Click += GoButton_Click;
        this.Controls.Add(goButton);

        // Label with instructions
        var label = new Label
        {
            Text = "ЛКМ - добавить точку, ПКМ - закрыть полигон. Вершины по часовой стрелке!",
            Location = new System.Drawing.Point(SIZE + 20, 100),
            Size = new Size(170, 40),
            AutoSize = true
        };
        this.Controls.Add(label);
    }

    private void Canvas_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            LeftButtonClick(e.X, e.Y);
        }
        else if (e.Button == MouseButtons.Right)
        {
            RightButtonClick();
        }
        canvas.Invalidate();
    }

    private void LeftButtonClick(int x, int y)
    {
        Point point = new Point(x, y);

        if (!fullFigure)
        {
            if (points.Count == 0 && polygon1.Count == 0)
            {
                points.Add(new PointF((float)x, (float)y));
                polygon1.Add(point);
            }
            else
            {
                var lastPoint = points.Last();
                points.Add(new PointF((float)x, (float)y));
                point = new Point(x, y, null, polygon1.Last());
                polygon1.Last().next = point;
                polygon1.Add(point);
            }
        }
        else if (!fullFigure2)
        {
            if (points2.Count == 0 && polygon2.Count == 0)
            {
                points2.Add(new PointF((float)x, (float)y));
                polygon2.Add(point);
            }
            else
            {
                var lastPoint = points2.Last();
                points2.Add(new PointF((float)x, (float)y));
                point = new Point(x, y, null, polygon2.Last());
                polygon2.Last().next = point;
                polygon2.Add(point);
            }
        }
    }

    private void RightButtonClick()
    {
        if (!fullFigure)
        {
            if (points.Count > 2 && polygon1.Count > 2)
            {
                var lastPoint = points.Last();
                var firstPoint = points.First();
                polygon1.Last().next = polygon1.First();
                polygon1.First().prev = polygon1.Last();
                fullFigure = true;

                Console.WriteLine("Polygon 1:");
                foreach (var p in polygon1)
                    p.Print();
            }
        }
        else if (!fullFigure2)
        {
            if (points2.Count > 2 && polygon2.Count > 2)
            {
                var lastPoint = points2.Last();
                var firstPoint = points2.First();
                polygon2.Last().next = polygon2.First();
                polygon2.First().prev = polygon2.Last();
                fullFigure2 = true;

                Console.WriteLine("Polygon 2:");
                foreach (var p in polygon2)
                    p.Print();
            }
        }
        canvas.Invalidate();
    }

    private void ClearButton_Click(object sender, EventArgs e)
    {
        ClearWindow();
    }

    private void ClearWindow()
    {
        canvas.Invalidate();
        fullFigure = false;
        fullFigure2 = false;
        points.Clear();
        points2.Clear();
        polygon1.Clear();
        polygon2.Clear();
        finalPoints.Clear();
    }

    private void GoButton_Click(object sender, EventArgs e)
    {
        StartAlgorithm();
    }

    private void StartAlgorithm()
    {
        Union();
        if (fill)
        {
            FillPolygon();
        }
        canvas.Invalidate();
    }

    private void Canvas_Paint(object sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (fullFigure && polygon1.Count > 0)
        {
            DrawPolygon(g, polygon1, Pens.Black, Brushes.Transparent);
        }

        if (fullFigure2 && polygon2.Count > 0)
        {
            DrawPolygon(g, polygon2, Pens.Black, Brushes.Transparent);
        }

        if (finalPoints.Count > 0)
        {
            DrawFinalResult(g);
        }

        foreach (var p in points)
        {
            g.FillEllipse(Brushes.Red, p.X - 2, p.Y - 2, 4, 4);
        }
        foreach (var p in points2)
        {
            g.FillEllipse(Brushes.Blue, p.X - 2, p.Y - 2, 4, 4);
        }
    }

    private void DrawPolygon(Graphics g, List<Point> polygon, Pen linePen, Brush fillBrush)
    {
        if (polygon.Count < 3) return;

        PointF[] pointsArray = polygon.Select(p => new PointF((float)p.x, (float)p.y)).ToArray();
        g.DrawPolygon(linePen, pointsArray);
        if (fillBrush != Brushes.Transparent)
        {
            g.FillPolygon(fillBrush, pointsArray);
        }
    }

    private void DrawFinalResult(Graphics g)
    {
        if (finalPoints.Count < 3) return;

        Pen greenPen = new Pen(Color.Green, 2);
        Brush fillBrush = fill ? Brushes.SkyBlue : Brushes.Transparent;

        g.DrawPolygon(greenPen, finalPoints.ToArray());
        if (fill)
        {
            g.FillPolygon(fillBrush, finalPoints.ToArray());
        }

        foreach (var p in finalPoints)
        {
            g.FillEllipse(Brushes.Red, p.X - 3, p.Y - 3, 6, 6);
        }

        greenPen.Dispose();
    }

    private void FillPolygon()
    {
    }

    private double Rotate(Point lineFrom, Point lineTo, Point point)
    {
        return (lineTo.x - lineFrom.x) * (point.y - lineTo.y) - (lineTo.y - lineFrom.y) * (point.x - lineTo.x);
    }

    private PointF? Intersection(Point p1, Point p2, Point p3, Point p4)
    {
        double x1 = p1.x, y1 = p1.y, x2 = p2.x, y2 = p2.y;
        double x3 = p3.x, y3 = p3.y, x4 = p4.x, y4 = p4.y;

        double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
        if (Math.Abs(denom) < 1e-9) return null;

        double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
        double u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            double interX = x1 + t * (x2 - x1);
            double interY = y1 + t * (y2 - y1);
            return new PointF((float)interX, (float)interY);
        }
        return null;
    }

    private bool FindSide(Point p1, Point p2, double x, double y)
    {
        double xa = p2.x - p1.x;
        double ya = p2.y - p1.y;
        x -= p1.x;
        y -= p1.y;
        return (y * xa - x * ya) > 0;
    }

    private Point FindLeftestPoint(List<Point> curPolygon)
    {
        Point leftestPoint = new Point(1000000, -1000000);
        foreach (var point in curPolygon)
        {
            if (point.x < leftestPoint.x ||
                (Math.Abs(point.x - leftestPoint.x) < 1e-9 && point.y > leftestPoint.y))
            {
                leftestPoint = point;
            }
        }
        return leftestPoint;
    }

    private void Union()
    {
        Point left1 = FindLeftestPoint(polygon1);
        Point left2 = FindLeftestPoint(polygon2);

        List<Point> curPolygon = left1.x < left2.x ? polygon1 : polygon2;
        List<Point> otherPolygon = left1.x < left2.x ? polygon2 : polygon1;

        Point startPoint = FindLeftestPoint(curPolygon);
        HashSet<Point> used = new HashSet<Point>();
        int i = 0;
        int steps = 0;

        Point curPoint = null;
        Point nextPoint = null;
        bool start = false;

        while (steps < polygon1.Count + polygon2.Count + 1)
        {
            steps++;

            if (start && curPoint.Equals(startPoint)) break;

            if (!start)
            {
                curPoint = FindLeftestPoint(curPolygon);
                nextPoint = curPoint.next;
                start = true;
            }

            double[] line = { curPoint.x, curPoint.y, nextPoint.x, nextPoint.y };

            if (!used.Contains(curPoint))
            {
                finalPoints.Add(new PointF((float)curPoint.x, (float)curPoint.y));
                used.Add(curPoint);
                Console.WriteLine($"{i} - point: ({curPoint.x}, {curPoint.y})");
                i++;
            }

            List<(Point[] line, PointF intersection)> intersections = new List<(Point[], PointF)>();

            Point otherPoint = FindLeftestPoint(otherPolygon);
            Point otherNextPoint = otherPoint.next;
            int j = 0;

            do
            {
                PointF? inter = Intersection(curPoint, nextPoint, otherPoint, otherNextPoint);
                if (inter.HasValue)
                {
                    intersections.Add((new[] { otherPoint, otherNextPoint }, inter.Value));
                }

                otherPoint = otherNextPoint;
                otherNextPoint = otherPoint.next;
                j++;
            } while (otherPoint != FindLeftestPoint(otherPolygon) && j < otherPolygon.Count);

            if (intersections.Count == 0)
            {
                curPoint = nextPoint;
                nextPoint = curPoint.next;
                continue;
            }

            var closestIntersection = intersections.OrderBy(inter =>
                Math.Sqrt(Math.Pow(curPoint.x - inter.intersection.X, 2) + Math.Pow(curPoint.y - inter.intersection.Y, 2))).First();

            Point intersectionPoint = new Point(closestIntersection.intersection.X, closestIntersection.intersection.Y);
            if (!used.Contains(intersectionPoint))
            {
                finalPoints.Add(closestIntersection.intersection);
                used.Add(intersectionPoint);
                Console.WriteLine($"{i} - intersection: ({closestIntersection.intersection.X}, {closestIntersection.intersection.Y})");
                i++;
            }
            else
            {
                continue;
            }

            foreach (var pointInLine in closestIntersection.line)
            {
                if (!FindSide(curPoint, nextPoint, pointInLine.x, pointInLine.y) &&
                    !used.Contains(pointInLine))
                {
                    finalPoints.Add(new PointF((float)pointInLine.x, (float)pointInLine.y));
                    used.Add(pointInLine);
                    Console.WriteLine($"{i} - left point: ({pointInLine.x}, {pointInLine.y})");
                    i++;

                    curPoint = pointInLine;
                    nextPoint = curPoint.next;
                    var temp = curPolygon;
                    curPolygon = otherPolygon;
                    otherPolygon = temp;
                    break;
                }
            }
        }

        canvas.Invalidate();
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}