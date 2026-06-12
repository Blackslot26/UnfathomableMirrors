using System;
using System.Windows;

namespace UnfathomableMirrors.Models
{
    public class RefractionBlock : IOpticSurface
    {
        public Point Position { get; set; }
        public double Angle { get; set; } = 0;
        public double Length { get; private set; }
        public double Thickness { get; private set; }
        public bool IsRefractive => true;
        public double RefractiveIndex { get; private set; }
        public Point[] Corners { get; private set; } = new Point[4];

        public RefractionBlock(double length, double thickness, double refractionIndex)
        {
            Position = new Point(550, 350);
            Length = Math.Max(50, Math.Min(length, 1000));
            Thickness = Math.Max(10, Math.Min(thickness, 1000));
            RefractiveIndex = Math.Max(1.0, refractionIndex);
        }

        public void MoveTo(Point newPos) => Position = newPos;
        public void AimAwayFrom(Point target) => Angle = Math.Atan2(Position.Y - target.Y, Position.X - target.X);

        public bool IsMouseOver(Point mousePos)
        {
            double dx = mousePos.X - Position.X;
            double dy = mousePos.Y - Position.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= 25;
        }

        public void UpdateDimensions(double canvasWidth, double canvasHeight)
        {
            double cosA = Math.Cos(Angle), sinA = Math.Sin(Angle);
            double hl = Length / 2.0, ht = Thickness / 2.0;

            Corners[0] = new Point(Position.X + hl * cosA - ht * sinA, Position.Y + hl * sinA + ht * cosA);
            Corners[1] = new Point(Position.X - hl * cosA - ht * sinA, Position.Y - hl * sinA + ht * cosA);
            Corners[2] = new Point(Position.X - hl * cosA + ht * sinA, Position.Y - hl * sinA - ht * cosA);
            Corners[3] = new Point(Position.X + hl * cosA + ht * sinA, Position.Y + hl * sinA - ht * cosA);
        }

        public bool TryIntersect(Point rayOrigin, double rayDx, double rayDy, out double t, out double nx, out double ny)
        {
            t = double.MaxValue; nx = 0; ny = 0;
            bool hit = false;

            for (int i = 0; i < 4; i++)
            {
                Point p1 = Corners[i], p2 = Corners[(i + 1) % 4];
                double den = (p1.X - p2.X) * rayDy - (p1.Y - p2.Y) * rayDx;
                if (Math.Abs(den) < 0.0001) continue;

                double tSurf = ((p1.X - rayOrigin.X) * rayDy - (p1.Y - rayOrigin.Y) * rayDx) / den;
                double tRay = ((p1.X - p2.X) * (p1.Y - rayOrigin.Y) - (p1.Y - p2.Y) * (p1.X - rayOrigin.X)) / den;

                if (tSurf >= -0.001 && tSurf <= 1.001 && tRay > 0.001 && tRay < t)
                {
                    t = tRay;
                    nx = p2.Y - p1.Y;
                    ny = -(p2.X - p1.X);
                    double len = Math.Sqrt(nx * nx + ny * ny);
                    nx /= len; ny /= len;
                    hit = true;
                }
            }
            return hit;
        }
    }
}