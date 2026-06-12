using System;
using System.Windows;

namespace UnfathomableMirrors.Models
{
    public class CurvedMirror : IOpticSurface
    {
        public Point Position { get; set; }
        public double Angle { get; set; } = 0;
        public double Radius { get; private set; }
        public double Length { get; private set; }
        public bool IsRefractive => false;
        public double RefractiveIndex => 1.0;
        public double MaxAngle { get; private set; }
        public Point Center { get; private set; }

        public CurvedMirror(double initialRadius, double length)
        {
            Position = new Point(550, 350);
            Radius = Math.Max(150, Math.Min(initialRadius, 2000));
            Length = Math.Max(50, Math.Min(length, 1000));
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
            Center = new Point(Position.X + Radius * Math.Cos(Angle), Position.Y + Radius * Math.Sin(Angle));
            double cutDistanceY = Length / 2.0;
            MaxAngle = Math.Asin(Math.Max(0, Math.Min(cutDistanceY / Radius, 1)));
        }

        public bool TryIntersect(Point rayOrigin, double rayDx, double rayDy, out double t, out double nx, out double ny)
        {
            t = -1; nx = 0; ny = 0;
            double dx = rayOrigin.X - Center.X, dy = rayOrigin.Y - Center.Y;
            double a = rayDx * rayDx + rayDy * rayDy;
            double b = 2.0 * (dx * rayDx + dy * rayDy);
            double c = (dx * dx + dy * dy) - (Radius * Radius);

            double discriminant = b * b - 4.0 * a * c;
            if (discriminant < 0) return false;

            double sqDisc = Math.Sqrt(discriminant);
            double t1 = (-b - sqDisc) / (2.0 * a);
            double t2 = (-b + sqDisc) / (2.0 * a);

            double minValidT = double.MaxValue;
            bool hit = false;

            if (t1 > 0.001) CheckHit(t1, rayOrigin, rayDx, rayDy, ref minValidT, ref hit);
            if (t2 > 0.001) CheckHit(t2, rayOrigin, rayDx, rayDy, ref minValidT, ref hit);

            if (hit)
            {
                t = minValidT;
                double hitX = rayOrigin.X + t * rayDx, hitY = rayOrigin.Y + t * rayDy;
                nx = hitX - Center.X; ny = hitY - Center.Y;
                double len = Math.Sqrt(nx * nx + ny * ny);
                nx /= len; ny /= len;
            }
            return hit;
        }

        private void CheckHit(double testT, Point rayOrigin, double rayDx, double rayDy, ref double minValidT, ref bool hit)
        {
            double hitX = rayOrigin.X + testT * rayDx;
            double hitY = rayOrigin.Y + testT * rayDy;
            double hitAngle = Math.Atan2(hitY - Center.Y, hitX - Center.X);

            double expectedAngle = Angle + Math.PI;
            double angleDiff = hitAngle - expectedAngle;
            while (angleDiff < -Math.PI) angleDiff += 2 * Math.PI;
            while (angleDiff > Math.PI) angleDiff -= 2 * Math.PI;

            if (Math.Abs(angleDiff) <= MaxAngle && testT < minValidT)
            {
                minValidT = testT;
                hit = true;
            }
        }
    }
}