using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
namespace TrafficSimulation
{
    public class Simulation
    {
        private struct Phase {
            public LightColor Hor;
            public LightColor Vert;
            public double Duration;
            public Phase(LightColor h, LightColor v, double d) { Hor = h; Vert = v; Duration = d; }
        }
        private readonly Phase[] _phases = {
            new Phase(LightColor.Green,  LightColor.Red,    10.0),
            new Phase(LightColor.Yellow, LightColor.Red,    3.0),
            new Phase(LightColor.Red,    LightColor.Yellow, 2.0),
            new Phase(LightColor.Red,    LightColor.Green,  10.0),
            new Phase(LightColor.Red,    LightColor.Yellow, 3.0),
            new Phase(LightColor.Red,    LightColor.Red,    6.0),
            new Phase(LightColor.Yellow, LightColor.Red,    2.0)
        };
        private int _currentPhaseIndex = 0;
        private double _phaseTimer = 0;
        public List<Road> Roads { get; } = new List<Road>();
        public List<Car> Cars { get; } = new List<Car>();
        public List<Pedestrian> Pedestrians { get; } = new List<Pedestrian>();
        // ИСПРАВЛЕНО: Добавлен начальный цвет (initialColor) согласно конструктору TrafficLight
        public TrafficLight HorizontalLight { get; } = new TrafficLight(new Point(320, 230), RoadOrientation.Horizontal, LightColor.Green);
        public TrafficLight VerticalLight { get; } = new TrafficLight(new Point(480, 370), RoadOrientation.Vertical, LightColor.Red);
        public Statistics Statistics { get; } = new Statistics();
        public double MinSpeedKmh { get; set; } = 40;
        public double MaxSpeedKmh { get; set; } = 90;
        public double CarSpawnInterval { get; set; } = 2.0;
        public int MaxCarsInSimulation { get; set; } = 30;
        private readonly Random _rand = new Random();
        private double _spawnTimer;
        private double _pedSpawnTimer;
        private int _nextId = 1;
        public void Initialize()
        {
            Roads.Clear(); Cars.Clear(); Pedestrians.Clear();
            Roads.Add(new Road(1, RoadOrientation.Horizontal, TrafficDirection.LeftToRight, new Point(400, 300)));
            Roads.Add(new Road(2, RoadOrientation.Horizontal, TrafficDirection.RightToLeft, new Point(400, 300)));
            Roads.Add(new Road(3, RoadOrientation.Vertical, TrafficDirection.TopToBottom, new Point(400, 300)));
            Roads.Add(new Road(4, RoadOrientation.Vertical, TrafficDirection.BottomToTop, new Point(400, 300)));
            _currentPhaseIndex = 0;
            _phaseTimer = 0;
        }
        public void Update(double dt)
        {
            _phaseTimer += dt;
            if (_phaseTimer >= _phases[_currentPhaseIndex].Duration)
            {
                _currentPhaseIndex = (_currentPhaseIndex + 1) % _phases.Length;
                _phaseTimer = 0;
            }
            HorizontalLight.CurrentColor = _phases[_currentPhaseIndex].Hor;
            VerticalLight.CurrentColor = _phases[_currentPhaseIndex].Vert;
            if (_currentPhaseIndex == 5)
            {
                _pedSpawnTimer += dt;
                if (_pedSpawnTimer >= 0.6 && Pedestrians.Count < 15)
                {
                    _pedSpawnTimer = 0;
                    SpawnPedestrianAlongZebra();
                }
            }
            _spawnTimer += dt;
            if (_spawnTimer >= CarSpawnInterval && Cars.Count < MaxCarsInSimulation)
            {
                _spawnTimer = 0;
                SpawnCar();
            }
            for (int i = Cars.Count - 1; i >= 0; i--)
            {
                var light = (Cars[i].CurrentRoad.Orientation == RoadOrientation.Horizontal) ? HorizontalLight : VerticalLight;
                Cars[i].Update(dt, light, Cars, Pedestrians);
                if (Cars[i].ShouldRemove())
                {
                    Statistics.RecordCarPassed(Cars[i].WaitTime);
                    Cars.RemoveAt(i);
                }
            }
            for (int i = Pedestrians.Count - 1; i >= 0; i--)
            {
                Pedestrians[i].Update(dt);
                if (Pedestrians[i].ShouldRemove()) Pedestrians.RemoveAt(i);
            }
        }
        public void Draw(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(35, 55, 35));
            using (var rb = new SolidBrush(Color.FromArgb(55, 55, 60)))
            {
                g.FillRectangle(rb, 0, 250, 800, 100);
                g.FillRectangle(rb, 350, 0, 100, 600);
            }
            DrawZebras(g);
            // Осевые линии прерываются перед перекрестком
            using (Pen dashPen = new Pen(Color.FromArgb(180, 255, 255, 255), 2) { DashPattern = new float[] { 10, 10 } })
            {
                g.DrawLine(dashPen, 0, 300, 330, 300);
                g.DrawLine(dashPen, 470, 300, 800, 300);
                g.DrawLine(dashPen, 400, 0, 400, 230);
                g.DrawLine(dashPen, 400, 370, 400, 600);
            }
            using (Pen stopPen = new Pen(Color.White, 5))
            {
                g.DrawLine(stopPen, 350, 255, 350, 345);
                g.DrawLine(stopPen, 450, 255, 450, 345);
                g.DrawLine(stopPen, 355, 250, 445, 250);
                g.DrawLine(stopPen, 355, 350, 445, 350);
            }
            HorizontalLight.Draw(g);
            VerticalLight.Draw(g);
            foreach (var p in Pedestrians) p.Draw(g);
            foreach (var car in Cars) car.Draw(g);
        }
        private void DrawZebras(Graphics g)
        {
            using (Pen p = new Pen(Color.FromArgb(160, 255, 255, 255), 4))
            {
                for (int i = 358; i < 445; i += 12) {
                    g.DrawLine(p, i, 235, i, 248);
                    g.DrawLine(p, i, 352, i, 365);
                }
                for (int i = 258; i < 345; i += 12) {
                    g.DrawLine(p, 335, i, 348, i);
                    g.DrawLine(p, 452, i, 465, i);
                }
            }
        }
        private void SpawnCar()
        {
            var road = Roads[_rand.Next(Roads.Count)];
            var spawnPoint = road.GetSpawnPoint();
            if (!Cars.Any(c => Math.Sqrt(Math.Pow(c.Position.X - spawnPoint.X, 2) + Math.Pow(c.Position.Y - spawnPoint.Y, 2)) < 75))
            {
                double speed = (MinSpeedKmh + _rand.NextDouble() * (MaxSpeedKmh - MinSpeedKmh)) * 3.0;
                Cars.Add(new Car(_nextId++, spawnPoint, speed, road));
            }
        }
        private void SpawnPedestrianAlongZebra()
        {
            Point[,] paths = {
                { new Point(360, 240), new Point(440, 240) },
                { new Point(360, 360), new Point(440, 360) },
                { new Point(340, 260), new Point(340, 340) },
                { new Point(460, 260), new Point(460, 340) }
            };
            int side = _rand.Next(4);
            bool rev = _rand.Next(2) == 0;
            var p = new Pedestrian(rev ? paths[side, 1] : paths[side, 0], rev ? paths[side, 0] : paths[side, 1]);
            p.StartCrossing();
            Pedestrians.Add(p);
        }
    }
}
