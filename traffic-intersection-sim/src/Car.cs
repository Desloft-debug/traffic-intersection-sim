using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
namespace TrafficSimulation
{
    public enum CarDirection { Straight, Left, Right }
    public class Car
    {
        public int Id { get; }
        public PointF Position { get; set; }
        public Color Color { get; }
        public double Speed { get; set; }
        public double MaxSpeed { get; }
        public CarDirection Direction { get; set; }
        public bool IsWaiting { get; set; }
        public double WaitTime { get; set; }
        public DateTime WaitStart { get; set; }
        public Road CurrentRoad { get; set; }
        private int _moveDx, _moveDy;
        private bool _hasEnteredIntersection = false;
        private bool _isTurning = false;
        private PointF _p0, _p1, _p2;
        private double _turnProgress = 0;
        private float _currentAngle = 0;
        // Для поворотников
        private double _blinkTimer;
        private bool _blinkOn;
        public Car(int id, Point start, double maxSpeed, Road road)
        {
            Id = id;
            Position = start;
            MaxSpeed = maxSpeed;
            Speed = maxSpeed;
            CurrentRoad = road;
            var rand = new Random(id);
            int r = rand.Next(100);
            if (r < 60) Direction = CarDirection.Straight;
            else if (r < 80) Direction = CarDirection.Left;
            else Direction = CarDirection.Right;
            Color = Direction == CarDirection.Straight ? Color.Blue : (Direction == CarDirection.Left ? Color.Red : Color.Green);
            SetDirectionVector(road.Direction);
            UpdateAngleFromVector();
        }
        private void SetDirectionVector(TrafficDirection dir)
        {
            _moveDx = dir == TrafficDirection.LeftToRight ? 1 : (dir == TrafficDirection.RightToLeft ? -1 : 0);
            _moveDy = dir == TrafficDirection.TopToBottom ? 1 : (dir == TrafficDirection.BottomToTop ? -1 : 0);
        }
        private void UpdateAngleFromVector()
        {
            if (_moveDx == 1) _currentAngle = 0;
            if (_moveDx == -1) _currentAngle = 180;
            if (_moveDy == 1) _currentAngle = 90;
            if (_moveDy == -1) _currentAngle = 270;
        }
        public void Update(double dt, TrafficLight light, List<Car> allCars, List<Pedestrian> allPedestrians)
        {
            // Таймер поворотника
            _blinkTimer += dt;
            if (_blinkTimer > 0.4) { _blinkTimer = 0; _blinkOn = !_blinkOn; }
            if (_isTurning)
            {
                UpdateTurn(dt);
                return;
            }
            bool shouldStop = false;
            // 1. Светофор
            if (!_hasEnteredIntersection && (light.CurrentColor == LightColor.Red || light.CurrentColor == LightColor.Yellow))
            {
                float dist = GetDistanceToStopLine();
                if (dist > 0 && dist < 60) shouldStop = true;
            }
            // 2. Машина впереди
            if (CheckCarInFront(allCars)) shouldStop = true;
            // 3. ПОМЕХА СПРАВА (Новое)
            if (!_hasEnteredIntersection && !shouldStop && IsNearIntersectionCenter())
            {
                if (CheckPriorityRight(allCars)) shouldStop = true;
            }
            if (shouldStop)
            {
                Speed = Math.Max(0, Speed - 300 * dt);
                if (!IsWaiting) { IsWaiting = true; WaitStart = DateTime.Now; }
            }
            else
            {
                Speed = Math.Min(MaxSpeed, Speed + 150 * dt);
                if (IsWaiting) { WaitTime += (DateTime.Now - WaitStart).TotalSeconds; IsWaiting = false; }
                MoveStraight(dt);
                CheckIntersectionLogic();
            }
        }
        private bool IsNearIntersectionCenter()
        {
            float distToCenter = (float)Math.Sqrt(Math.Pow(Position.X - 400, 2) + Math.Pow(Position.Y - 300, 2));
            return distToCenter < 100;
        }
        private bool CheckPriorityRight(List<Car> allCars)
        {
            // Если мы уже пересекли стоп-линию, мы НЕ останавливаемся, а заканчиваем маневр
            if (_hasEnteredIntersection) return false;
            foreach (var other in allCars)
            {
                if (other == this) continue;
                double dist = Math.Sqrt(Math.Pow(other.Position.X - Position.X, 2) + Math.Pow(other.Position.Y - Position.Y, 2));
                if (dist > 120) continue;
                // ПРОВЕРКА "МЕРТВОЙ ПЕТЛИ":
                // Если обе машины стоят, едет та, у которой ID меньше. Это разрывает круг.
                if (other.IsWaiting && other.Id < this.Id && dist < 100) return false;
                bool isRight = false;
                if (_moveDx > 0) isRight = (other.Position.Y > Position.Y && other._moveDy < 0);
                else if (_moveDx < 0) isRight = (other.Position.Y < Position.Y && other._moveDy > 0);
                else if (_moveDy > 0) isRight = (other.Position.X < Position.X && other._moveDx > 0);
                else if (_moveDy < 0) isRight = (other.Position.X > Position.X && other._moveDx < 0);
                // Уступаем, если машина справа уже начала движение или едет быстрее нас
                if (isRight && (other._hasEnteredIntersection || other.Speed > 15)) return true;
            }
            return false;
        }
        private void CheckIntersectionLogic()
        {
            if (_hasEnteredIntersection) return;
            bool readyToTurn = false;
            if (_moveDx == 1 && Position.X >= 360) readyToTurn = true;
            else if (_moveDx == -1 && Position.X <= 440) readyToTurn = true;
            else if (_moveDy == 1 && Position.Y >= 260) readyToTurn = true;
            else if (_moveDy == -1 && Position.Y <= 340) readyToTurn = true;
            if (readyToTurn)
            {
                _hasEnteredIntersection = true;
                if (Direction != CarDirection.Straight)
                {
                    CalculateTurnTrajectory();
                    _isTurning = true;
                }
            }
        }
        private void CalculateTurnTrajectory()
        {
            _p0 = Position;
            _turnProgress = 0;
            float targetX = 0, targetY = 0, controlX = 400, controlY = 300;
            if (CurrentRoad.Direction == TrafficDirection.LeftToRight)
            {
                if (Direction == CarDirection.Right) { targetX = 380; targetY = 360; controlX = 380; controlY = 300; }
                else if (Direction == CarDirection.Left) { targetX = 420; targetY = 240; controlX = 440; controlY = 300; }
            }
            else if (CurrentRoad.Direction == TrafficDirection.RightToLeft)
            {
                if (Direction == CarDirection.Right) { targetX = 420; targetY = 240; controlX = 420; controlY = 300; }
                else if (Direction == CarDirection.Left) { targetX = 380; targetY = 360; controlX = 360; controlY = 300; }
            }
            else if (CurrentRoad.Direction == TrafficDirection.TopToBottom)
            {
                if (Direction == CarDirection.Right) { targetX = 340; targetY = 280; controlX = 400; controlY = 280; }
                else if (Direction == CarDirection.Left) { targetX = 460; targetY = 320; controlX = 400; controlY = 340; }
            }
            else if (CurrentRoad.Direction == TrafficDirection.BottomToTop)
            {
                if (Direction == CarDirection.Right) { targetX = 460; targetY = 320; controlX = 400; controlY = 320; }
                else if (Direction == CarDirection.Left) { targetX = 340; targetY = 280; controlX = 400; controlY = 260; }
            }
            _p1 = new PointF(controlX, controlY);
            _p2 = new PointF(targetX, targetY);
        }
        private void UpdateTurn(double dt)
        {
            _turnProgress += (dt * Speed) / 100.0;
            if (_turnProgress >= 1.0)
            {
                _turnProgress = 1.0; _isTurning = false; Position = _p2;
                CalculateNewVectorAfterTurn();
                return;
            }
            float u = (float)(1 - _turnProgress);
            float tt = (float)(_turnProgress * _turnProgress);
            float uu = u * u;
            float x = (uu * _p0.X) + (2 * u * (float)_turnProgress * _p1.X) + (tt * _p2.X);
            float y = (uu * _p0.Y) + (2 * u * (float)_turnProgress * _p1.Y) + (tt * _p2.Y);
            double angleRad = Math.Atan2(y - Position.Y, x - Position.X);
            _currentAngle = (float)(angleRad * 180 / Math.PI);
            Position = new PointF(x, y);
        }
        private void CalculateNewVectorAfterTurn()
        {
            float ang = _currentAngle;
            while (ang < 0) ang += 360;
            while (ang >= 360) ang -= 360;
            if (ang >= 315 || ang < 45) { _moveDx = 1; _moveDy = 0; _currentAngle = 0; }
            else if (ang >= 45 && ang < 135) { _moveDx = 0; _moveDy = 1; _currentAngle = 90; }
            else if (ang >= 135 && ang < 225) { _moveDx = -1; _moveDy = 0; _currentAngle = 180; }
            else { _moveDx = 0; _moveDy = -1; _currentAngle = 270; }
        }
        private void MoveStraight(double dt)
        {
            Position = new PointF((float)(Position.X + _moveDx * Speed * dt), (float)(Position.Y + _moveDy * Speed * dt));
        }
        private float GetDistanceToStopLine()
        {
            return CurrentRoad.Direction switch {
                TrafficDirection.LeftToRight => 350 - Position.X,
                TrafficDirection.RightToLeft => Position.X - 450,
                TrafficDirection.TopToBottom => 250 - Position.Y,
                TrafficDirection.BottomToTop => Position.Y - 350,
                _ => -1
            };
        }
        private bool CheckCarInFront(List<Car> allCars)
        {
            float laneWidthThreshold = 25.0f;
            foreach (var other in allCars)
            {
                if (other == this) continue;
                double dist = Math.Sqrt(Math.Pow(other.Position.X - Position.X, 2) + Math.Pow(other.Position.Y - Position.Y, 2));
                if (dist < 60)
                {
                    bool inMyLane = (_moveDx != 0) ? Math.Abs(other.Position.Y - Position.Y) < laneWidthThreshold : Math.Abs(other.Position.X - Position.X) < laneWidthThreshold;
                    if (!inMyLane) continue;
                    bool isForward = false;
                    if (_moveDx == 1) isForward = other.Position.X > Position.X;
                    else if (_moveDx == -1) isForward = other.Position.X < Position.X;
                    else if (_moveDy == 1) isForward = other.Position.Y > Position.Y;
                    else if (_moveDy == -1) isForward = other.Position.Y < Position.Y;
                    if (isForward) return true;
                }
            }
            return false;
        }
        public bool ShouldRemove() => Position.X < -150 || Position.X > 950 || Position.Y < -150 || Position.Y > 750;
        public void Draw(Graphics g)
        {
            var state = g.Save();
            g.TranslateTransform(Position.X, Position.Y);
            g.RotateTransform(_currentAngle);
            Rectangle rect = new Rectangle(-12, -7, 24, 14);
            g.FillRectangle(new SolidBrush(Color), rect);
            g.DrawRectangle(Pens.Black, rect);
            // Фары
            g.FillRectangle(Brushes.White, 8, -6, 4, 3);
            g.FillRectangle(Brushes.White, 8, 3, 4, 3);
            // Поворотник (Мигает оранжевым)
            if (_blinkOn && Direction != CarDirection.Straight)
            {
                // Если поворот направо - мигает правый борт, налево - левый
                int blinkY = (Direction == CarDirection.Right) ? 3 : -6;
                g.FillRectangle(Brushes.Orange, 8, blinkY, 4, 3);
            }
            g.Restore(state);
        }
    }
}
