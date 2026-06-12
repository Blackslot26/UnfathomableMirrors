using System;
using System.Windows;

namespace UnfathomableMirrors.Models
{
    public class BiconvexLens : IOpticSurface
    {
        public Point Position { get; set; }
        public double Angle { get; set; } = 0;
        public double Radius { get; private set; }
        public double Thickness { get; private set; }
        public bool IsRefractive => true;
        public double RefractiveIndex { get; private set; }
        public Point C1 { get; private set; }
        public Point C2 { get; private set; }

        public BiconvexLens(double radius, double thickness, double refractionIndex)
        {
            Position = new Point(550, 350);
            Radius = Math.Max(50, Math.Min(radius, 2000));
            Thickness = Math.Max(10, Math.Min(thickness, Radius * 1.9));
            RefractiveIndex = Math.Max(1.0, refractionIndex);
        }

        public void MoveTo(Point newPos) => Position = newPos;
        public void AimAwayFrom(Point target) => Angle = Math.Atan2(Position.Y - target.Y, Position.X - target.X);

        public bool IsMouseOver(Point mousePos)
        {
            // Optimización: Multiplicación directa en lugar de Math.Pow
            double dx = mousePos.X - Position.X;
            double dy = mousePos.Y - Position.Y;
            return Math.Sqrt(dx * dx + dy * dy) <= Thickness;
        }

        public void UpdateDimensions(double canvasWidth, double canvasHeight)
        {
            double d = Radius - Thickness / 2.0;
            C1 = new Point(Position.X + d * Math.Cos(Angle), Position.Y + d * Math.Sin(Angle));
            C2 = new Point(Position.X - d * Math.Cos(Angle), Position.Y - d * Math.Sin(Angle));
        }

        public bool TryIntersect(Point rayOrigin, double rayAngleRad, out double t, out double nx, out double ny)
        {
            t = -1; nx = 0; ny = 0;
            double dx = Math.Cos(rayAngleRad), dy = Math.Sin(rayAngleRad);

            bool HitCircle(Point C, out double tin, out double tout)
            {
                tin = tout = -1;
                double ox = rayOrigin.X - C.X, oy = rayOrigin.Y - C.Y;
                double b = 2 * (ox * dx + oy * dy);
                double c = ox * ox + oy * oy - Radius * Radius;
                double disc = b * b - 4 * c;
                if (disc < 0) return false;
                double sq = Math.Sqrt(disc);
                tin = (-b - sq) / 2.0; tout = (-b + sq) / 2.0;
                return true;
            }

            if (!HitCircle(C1, out double t1in, out double t1out)) return false;
            if (!HitCircle(C2, out double t2in, out double t2out)) return false;

            double t_in = Math.Max(t1in, t2in);
            double t_out = Math.Min(t1out, t2out);

            if (t_in <= t_out && t_out > 0.001)
            {
                double hitT = t_in > 0.001 ? t_in : t_out;
                t = hitT;
                Point hitP = new Point(rayOrigin.X + t * dx, rayOrigin.Y + t * dy);
                Point hitC = (hitT == t1in || hitT == t1out) ? C1 : C2;

                nx = (hitP.X - hitC.X) / Radius;
                ny = (hitP.Y - hitC.Y) / Radius;
                return true;
            }
            return false;
        }
    }
}