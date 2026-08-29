using System;
using System.Collections.Generic;
using System.Drawing;
namespace TrafficSimulation
{
    public enum RoadOrientation { Horizontal, Vertical }
    public enum TrafficDirection { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
    public class Road
    {
        public int Id { get; }
        public RoadOrientation Orientation { get; }
        public TrafficDirection Direction { get; }
        public Point IntersectionCenter { get; }
        public List<Car> Cars { get; } = new List<Car>();
        public Road(int id, RoadOrientation ori, TrafficDirection dir, Point center)
        {
            Id = id;
            Orientation = ori;
            Direction = dir;
            IntersectionCenter = center;
        }
        public Point GetSpawnPoint()
        {
            // ТОЧНЫЕ КООРДИНАТЫ ПОЛОС:
            // Горизонтальные:
            // - LeftToRight (→): y = 322 (НИЖНЯЯ полоса)
            // - RightToLeft (←): y = 278 (ВЕРХНЯЯ полоса)
            // Вертикальные:
            // - BottomToTop (↑): x = 422 (ПРАВАЯ полоса)
            // - TopToBottom (↓): x = 378 (ЛЕВАЯ полоса)
            return Orientation switch
            {
                RoadOrientation.Horizontal => Direction switch
                {
                    TrafficDirection.LeftToRight => new Point(-100, 322), // → из левого края
                    TrafficDirection.RightToLeft => new Point(900, 278),   // ← из правого края
                    _ => new Point(0, 300)
                },
                RoadOrientation.Vertical => Direction switch
                {
                    TrafficDirection.BottomToTop => new Point(422, 700),   // ↑ из нижнего края
                    TrafficDirection.TopToBottom => new Point(378, -100),  // ↓ из верхнего края
                    _ => new Point(400, 0)
                },
                _ => new Point(0, 0)
            };
        }
        public void Draw(Graphics g)
        {
            if (Orientation == RoadOrientation.Horizontal)
            {
                // Центральная линия
                g.DrawLine(new Pen(Color.White, 2), 0, 300, 800, 300);
                // Полосы движения
                g.DrawLine(new Pen(Color.Yellow, 1), 0, 278, 800, 278); // Верхняя (←)
                g.DrawLine(new Pen(Color.Yellow, 1), 0, 322, 800, 322); // Нижняя (→)
                // Разметка направления
                DrawDirectionArrows(g, true);
            }
            else // Vertical
            {
                // Центральная линия
                g.DrawLine(new Pen(Color.White, 2), 400, 0, 400, 600);
                // Полосы движения
                g.DrawLine(new Pen(Color.Yellow, 1), 378, 0, 378, 600); // Левая (↓)
                g.DrawLine(new Pen(Color.Yellow, 1), 422, 0, 422, 600); // Правая (↑)
                // Разметка направления
                DrawDirectionArrows(g, false);
            }
        }
        private void DrawDirectionArrows(Graphics g, bool horizontal)
        {
            Brush arrowBrush = Brushes.White;
            int arrowSize = 8;
            if (horizontal)
            {
                int y = Direction == TrafficDirection.LeftToRight ? 322 : 278;
                int dx = Direction == TrafficDirection.LeftToRight ? 1 : -1;
                for (int x = 100; x < 700; x += 80)
                {
                    Point[] arrow = dx > 0
                        ? new Point[] { new Point(x, y), new Point(x + arrowSize, y), new Point(x, y + arrowSize) }
                        : new Point[] { new Point(x, y), new Point(x - arrowSize, y), new Point(x, y + arrowSize) };
                    g.FillPolygon(arrowBrush, arrow);
                }
            }
            else
            {
                int x = Direction == TrafficDirection.BottomToTop ? 422 : 378;
                int dy = Direction == TrafficDirection.BottomToTop ? -1 : 1;
                for (int y = 100; y < 500; y += 80)
                {
                    Point[] arrow = dy > 0
                        ? new Point[] { new Point(x, y), new Point(x, y + arrowSize), new Point(x + arrowSize, y) }
                        : new Point[] { new Point(x, y), new Point(x, y - arrowSize), new Point(x + arrowSize, y) };
                    g.FillPolygon(arrowBrush, arrow);
                }
            }
        }
    }
}
