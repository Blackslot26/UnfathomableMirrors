using System;
using System.Windows;

namespace UnfathomableMirrors.Models
{
    public class CurvedMirror : IOpticSurface
    {
        public Point Position { get; set; }
        public double Angle { get; set; } = 0;
        public double Radius { get; private set; }
        public double MaxAngle { get; private set; }
        public Point Center { get; private set; }

        public CurvedMirror(double initialRadius)
        {
            Position = new Point(550, 350);
            Radius = Math.Max(150, Math.Min(initialRadius, 2000));
        }

        public void MoveTo(Point newPos) => Position = newPos;
        public void AimAwayFrom(Point target) => Angle = Math.Atan2(Position.Y - target.Y, Position.X - target.X);

        public bool IsMouseOver(Point mousePos)
        {
            double dx = mousePos.X - Position.X, dy = mousePos.Y - Position.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= 25;
        }

        public void UpdateDimensions(double canvasWidth, double canvasHeight)
        {
            Center = new Point(Position.X + Radius * Math.Cos(Angle), Position.Y + Radius * Math.Sin(Angle));
            double cutDistanceY = 120;
            MaxAngle = Math.Asin(Math.Max(0, Math.Min(cutDistanceY / Radius, 1)));
        }

        public bool TryIntersect(Point rayOrigin, double rayAngleRad, out double t, out double nx, out double ny)
        {
            t = -1; nx = 0; ny = 0;
            double cosA = Math.Cos(rayAngleRad), sinA = Math.Sin(rayAngleRad);
            double dx = rayOrigin.X - Center.X, dy = rayOrigin.Y - Center.Y;

            double a = cosA * cosA + sinA * sinA;
            double b = 2.0 * (dx * cosA + dy * sinA);
            double c = (dx * dx + dy * dy) - (Radius * Radius);

            double discriminant = b * b - 4.0 * a * c;
            if (discriminant < 0) return false;

            double[] possibleTs = { (-b - Math.Sqrt(discriminant)) / (2.0 * a), (-b + Math.Sqrt(discriminant)) / (2.0 * a) };
            double minValidT = double.MaxValue;
            bool hit = false;

            for (int i = 0; i < 2; i++)
            {
                if (possibleTs[i] > 0.1)
                {
                    double hitX = rayOrigin.X + possibleTs[i] * cosA;
                    double hitY = rayOrigin.Y + possibleTs[i] * sinA;
                    double hitAngle = Math.Atan2(hitY - Center.Y, hitX - Center.X);

                    double expectedAngle = Angle + Math.PI;
                    double angleDiff = hitAngle - expectedAngle;
                    while (angleDiff < -Math.PI) angleDiff += 2 * Math.PI;
                    while (angleDiff > Math.PI) angleDiff -= 2 * Math.PI;

                    if (Math.Abs(angleDiff) <= MaxAngle && possibleTs[i] < minValidT)
                    {
                        minValidT = possibleTs[i];
                        hit = true;
                    }
                }
            }

            if (hit)
            {
                t = minValidT;
                double hitX = rayOrigin.X + t * cosA, hitY = rayOrigin.Y + t * sinA;
                nx = hitX - Center.X; ny = hitY - Center.Y;
                double len = Math.Sqrt(nx * nx + ny * ny);
                nx /= len; ny /= len;
            }
            return hit;
        }
    }
}