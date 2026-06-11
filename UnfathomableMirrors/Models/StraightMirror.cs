using System;
using System.Windows;

namespace UnfathomableMirrors.Models
{
    public class StraightMirror : IOpticSurface
    {
        public Point Position { get; set; }
        public double Angle { get; set; } = 0;
        public double Length { get; private set; }
        public bool IsRefractive => false;
        public double RefractiveIndex => 1.0;
        public Point StartPoint { get; private set; }
        public Point EndPoint { get; private set; }

        public StraightMirror(double length)
        {
            Position = new Point(550, 350);
            Length = Math.Max(50, Math.Min(length, 1000));
        }

        public void MoveTo(Point newPos) => Position = newPos;
        public void AimAwayFrom(Point target) => Angle = Math.Atan2(Position.Y - target.Y, Position.X - target.X);

        public bool IsMouseOver(Point mousePos)
        {
            return Math.Sqrt(Math.Pow(mousePos.X - Position.X, 2) + Math.Pow(mousePos.Y - Position.Y, 2)) <= 20;
        }

        public void UpdateDimensions(double canvasWidth, double canvasHeight)
        {
            double halfLength = Length / 2.0;
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

            if (tSurf >= -0.001 && tSurf <= 1.001 && tRay > 0.001)
            {
                t = tRay;
                nx = -(y2 - y1); ny = x2 - x1;
                double len = Math.Sqrt(nx * nx + ny * ny);
                nx /= len; ny /= len;
                return true;
            }
            return false;
        }
    }
}