using System;
using System.Drawing;
namespace TrafficSimulation
{
    public class Pedestrian
    {
        public Point Start { get; }
        public Point End { get; }
        public Point Position { get; set; }
        public bool IsCrossing { get; set; }
        private DateTime _startCross;
        public Pedestrian(Point s, Point e)
        {
            Start = s;
            End = e;
            Position = s;
        }
        public void StartCrossing() { IsCrossing = true; Position = Start; _startCross = DateTime.Now; }
        public void Update(double dt)
        {
            if (!IsCrossing) return;
            double dx = End.X - Start.X, dy = End.Y - Start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;
            double move = 40 * dt;
            double t = Math.Min(1, move / len);
            Position = new Point((int)(Position.X + t * dx), (int)(Position.Y + t * dy));
        }
        public bool ShouldRemove() => IsCrossing && (DateTime.Now - _startCross).TotalSeconds > 8;
        public void Draw(Graphics g)
        {
            if (IsCrossing) g.FillEllipse(Brushes.Brown, Position.X - 3, Position.Y - 3, 6, 6);
        }
    }
}
