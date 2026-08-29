using System;
using System.Drawing;
namespace TrafficSimulation
{
    public enum LightColor { Red, Yellow, Green }
    public class TrafficLight
    {
        public Point Position { get; }
        public LightColor CurrentColor { get; set; }
        public RoadOrientation ControlledRoad { get; }
        public TrafficLight(Point pos, RoadOrientation road, LightColor initialColor)
        {
            Position = pos;
            ControlledRoad = road;
            CurrentColor = initialColor;
        }
        public void Draw(Graphics g)
        {
            g.FillRectangle(Brushes.Black, Position.X - 10, Position.Y - 25, 20, 50);
            int y = Position.Y - 20;
            DrawLight(g, Position.X, y, CurrentColor == LightColor.Red, Color.Red);
            DrawLight(g, Position.X, y + 15, CurrentColor == LightColor.Yellow, Color.Yellow);
            DrawLight(g, Position.X, y + 30, CurrentColor == LightColor.Green, Color.LimeGreen);
        }
        private void DrawLight(Graphics g, int x, int y, bool on, Color color)
        {
            using (var brush = new SolidBrush(on ? color : Color.FromArgb(40, 40, 40)))
            {
                g.FillEllipse(brush, x - 6, y - 6, 12, 12);
            }
        }
    }
}
