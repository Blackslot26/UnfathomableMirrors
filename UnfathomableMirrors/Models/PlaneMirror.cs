using System;
using System.Windows;

namespace UnfathomableMirrors.Models
{
    public class PlaneMirror : IOpticSurface
    {
        public Point Position { get; set; }
        public double Angle { get; set; } = 0;
        public Point StartPoint { get; private set; }
        public Point EndPoint { get; private set; }

        public PlaneMirror() => Position = new Point(550, 350);

        public void MoveTo(Point newPos) => Position = newPos;
        public void AimAwayFrom(Point target) => Angle = Math.Atan2(Position.Y - target.Y, Position.X - target.X);

        public bool IsMouseOver(Point mousePos)
        {
            double dx = mousePos.X - Position.X, dy = mousePos.Y - Position.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= 20;
        }

        public void UpdateDimensions(double canvasWidth, double canvasHeight)
        {
            double halfLength = 100;
            StartPoint = new Point(Position.X + halfLength * Math.Cos(Angle - Math.PI / 2), Position.Y + halfLength * Math.Sin(Angle - Math.PI / 2));
            EndPoint = new Point(Position.X + halfLength * Math.Cos(Angle + Math.PI / 2), Position.Y + halfLength * Math.Sin(Angle + Math.PI / 2));
        }

        public bool TryIntersect(Point rayOrigin, double rayAngleRad, out double t, out double nx, out double ny)
        {
            t = -1; nx = 0; ny = 0;

            double x1 = StartPoint.X, y1 = StartPoint.Y;
            double x2 = EndPoint.X, y2 = EndPoint.Y;
            double x3 = rayOrigin.X, y3 = rayOrigin.Y;

            double dx = Math.Cos(rayAngleRad), dy = Math.Sin(rayAngleRad);
            double den = (x1 - x2) * dy - (y1 - y2) * dx;

            if (Math.Abs(den) < 0.0001) return false;

            double tSurf = ((x1 - x3) * dy - (y1 - y3) * dx) / den;
            double tRay = ((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / den;

            if (tSurf >= 0.0 && tSurf <= 1.0 && tRay > 0.1)
            {
                t = tRay;
                nx = -(y2 - y1);
                ny = x2 - x1;
                double len = Math.Sqrt(nx * nx + ny * ny);
                nx /= len; ny /= len;
                return true;
            }
            return false;
        }
    }
}