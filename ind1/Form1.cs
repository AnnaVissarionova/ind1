using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ind1
{
    public partial class Form1 : Form
    {
        private LinkedList<PointF> polygon1 = new LinkedList<PointF>();
        private LinkedList<PointF> polygon2 = new LinkedList<PointF>();
        private List<PointF> finalPolygon = new List<PointF>();
        private bool isFirstClosed = false;
        private bool isSecondClosed = false;
        private bool fillResult = false;

        private Button btnUnion;
        private Button btnClear;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.Text = "Объединение выпуклых полигонов (двусвязный список)";
            this.ClientSize = new Size(800, 600);

            btnUnion = new Button
            {
                Text = "Union",
                Location = new Point(10, 10),
                Size = new Size(100, 30)
            };
            btnUnion.Click += BtnUnion_Click;
            Controls.Add(btnUnion);

            btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(120, 10),
                Size = new Size(100, 30)
            };
            btnClear.Click += (s, e) => ClearAll();
            Controls.Add(btnClear);

            this.MouseClick += Form1_MouseClick;
            this.Paint += Form1_Paint;
        }

        private void ClearAll()
        {
            polygon1.Clear();
            polygon2.Clear();
            finalPolygon.Clear();
            isFirstClosed = false;
            isSecondClosed = false;
            Invalidate();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!isFirstClosed)
                {
                    polygon1.AddLast(e.Location);
                }
                else if (!isSecondClosed)
                {
                    polygon2.AddLast(e.Location);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (!isFirstClosed && polygon1.Count >= 3)
                {
                    if (!IsConvex(polygon1.ToList()))
                    {
                        MessageBox.Show("Первый полигон не является выпуклым! Нарисуйте заново.");
                        polygon1.Clear();
                        Invalidate();
                        return;
                    }
                    isFirstClosed = true;
                }
                else if (!isSecondClosed && polygon2.Count >= 3)
                {
                    if (!IsConvex(polygon2.ToList()))
                    {
                        MessageBox.Show("Второй полигон не является выпуклым! Нарисуйте заново.");
                        polygon2.Clear();
                        Invalidate();
                        return;
                    }
                    isSecondClosed = true;
                }
            }

            Invalidate();
        }

        private void BtnUnion_Click(object sender, EventArgs e)
        {
            if (isFirstClosed && isSecondClosed)
            {
                finalPolygon = ConvexPolygonsUnion(polygon1, polygon2);
                Invalidate();
            }
            else
            {
                MessageBox.Show("Сначала нарисуйте два выпуклых полигона и замкните их правой кнопкой мыши.");
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DrawPolygon(g, polygon1, Pens.Blue, Brushes.Blue, isFirstClosed);
            DrawPolygon(g, polygon2, Pens.Red, Brushes.Red, isSecondClosed);

            if (finalPolygon.Count > 2)
            {
                if (fillResult)
                    g.FillPolygon(new SolidBrush(Color.FromArgb(100, Color.Green)), finalPolygon.ToArray());
                g.DrawPolygon(new Pen(Color.Green, 2), finalPolygon.ToArray());
            }
        }

        private void DrawPolygon(Graphics g, LinkedList<PointF> poly, Pen pen, Brush brush, bool closed)
        {
            if (poly.Count > 1)
                g.DrawLines(pen, poly.ToArray());
            if (closed && poly.Count > 2)
                g.DrawPolygon(pen, poly.ToArray());

            foreach (var p in poly)
                g.FillEllipse(brush, p.X - 3, p.Y - 3, 6, 6);
        }

        // === Проверка выпуклости ===
        private bool IsConvex(List<PointF> poly)
        {
            if (poly.Count < 3)
                return false;

            int n = poly.Count;
            bool? sign = null;

            for (int i = 0; i < n; i++)
            {
                PointF a = poly[i];
                PointF b = poly[(i + 1) % n];
                PointF c = poly[(i + 2) % n];

                float cross = CrossProduct(a, b, c);
                if (Math.Abs(cross) < 1e-6) continue;

                bool currentSign = cross > 0;
                if (sign == null)
                    sign = currentSign;
                else if (sign != currentSign)
                    return false;
            }

            return true;
        }

        private float CrossProduct(PointF a, PointF b, PointF c)
        {
            float abx = b.X - a.X;
            float aby = b.Y - a.Y;
            float bcx = c.X - b.X;
            float bcy = c.Y - b.Y;
            return abx * bcy - aby * bcx;
        }

        // === Объединение выпуклых полигонов ===
        private List<PointF> ConvexPolygonsUnion(LinkedList<PointF> poly1, LinkedList<PointF> poly2)
        {
            var allPoints = new List<PointF>();
            allPoints.AddRange(poly1);
            allPoints.AddRange(poly2);
            return ConvexHull(allPoints);
        }

        // Построение выпуклой оболочки методом Джарвиса
        private List<PointF> ConvexHull(List<PointF> points)
        {
            if (points.Count < 3)
                return new List<PointF>(points);

            var hull = new List<PointF>();
            int leftmost = 0;
            for (int i = 1; i < points.Count; i++)
                if (points[i].X < points[leftmost].X)
                    leftmost = i;

            int p = leftmost, q;
            do
            {
                hull.Add(points[p]);
                q = (p + 1) % points.Count;

                for (int i = 0; i < points.Count; i++)
                {
                    if (Orientation(points[p], points[i], points[q]) == -1)
                        q = i;
                }

                p = q;
            } while (p != leftmost);

            return hull;
        }

        private int Orientation(PointF a, PointF b, PointF c)
        {
            float val = (b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y);
            if (Math.Abs(val) < 1e-6) return 0;
            return (val > 0) ? 1 : -1;
        }
    }
}
