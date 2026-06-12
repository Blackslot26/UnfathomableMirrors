using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace UnfathomableMirrors.Models
{
    public struct RaySegment
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public bool IsHitting { get; set; }
        public Point NormalEnd { get; set; }
        public double IncidenceAngleDeg { get; set; }
        public string ActionType { get; set; }
    }

    public class RayEmitter
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public Point Position { get; set; }
        public double Angle { get; private set; }
        public SolidColorBrush RayColor { get; private set; }
        public double Wavelength { get; set; }
        public double DispersionModifier { get; private set; }
        public List<RaySegment> Segments { get; private set; } = new List<RaySegment>(25);

        private const double MaxRayLength = 2000;
        private const int MaxBounces = 25;

        public RayEmitter(int id, int groupId, double x, double y, SolidColorBrush color, double angleDegrees = 0, double wavelength = 550, double dispersion = 0)
        {
            Id = id; GroupId = groupId; Position = new Point(x, y); RayColor = color; SetAngleDegrees(angleDegrees); Wavelength = wavelength; DispersionModifier = dispersion;
        }

        public void MoveTo(Point newPos) => Position = newPos;
        public void AimAwayFrom(Point target) => Angle = Math.Round(Math.Atan2(Position.Y - target.Y, Position.X - target.X) * 180.0 / Math.PI) * Math.PI / 180.0;
        public void SetAngleDegrees(double degrees) => Angle = degrees * Math.PI / 180.0;
        public bool IsMouseOver(Point mousePos) => Math.Abs(mousePos.X - Position.X) <= 15 && Math.Abs(mousePos.Y - Position.Y) <= 10;

        public void UpdatePhysics(List<IOpticSurface> surfaces)
        {
            Segments.Clear();
            Point currentPos = Position;
            double currentAngle = Angle;

            for (int bounce = 0; bounce < MaxBounces; bounce++)
            {
                double minT = double.MaxValue, bestNx = 0, bestNy = 0;
                IOpticSurface hitSurface = null;
                double rayDx = Math.Cos(currentAngle);
                double rayDy = Math.Sin(currentAngle);

                foreach (var surface in surfaces)
                {
                    if (surface.TryIntersect(currentPos, rayDx, rayDy, out double t, out double nx, out double ny))
                    {
                        if (t < minT) { minT = t; bestNx = nx; bestNy = ny; hitSurface = surface; }
                    }
                }

                if (hitSurface != null)
                {
                    double dotIN = rayDx * bestNx + rayDy * bestNy;

                    bool isEntering = dotIN < 0;
                    double actualNx = isEntering ? bestNx : -bestNx;
                    double actualNy = isEntering ? bestNy : -bestNy;

                    double n1 = 1.0, n2 = 1.0;
                    if (hitSurface.IsRefractive)
                    {
                        n1 = isEntering ? 1.0 : (hitSurface.RefractiveIndex + DispersionModifier);
                        n2 = isEntering ? (hitSurface.RefractiveIndex + DispersionModifier) : 1.0;
                    }

                    double cosI = -(rayDx * actualNx + rayDy * actualNy);
                    double r = n1 / n2;
                    double sinT2 = r * r * (1.0 - cosI * cosI);
                    bool isTIR = hitSurface.IsRefractive && sinT2 > 1.0;

                    Point hitPoint = new Point(currentPos.X + minT * rayDx, currentPos.Y + minT * rayDy);
                    Point normalTarget = new Point(hitPoint.X + actualNx * 60, hitPoint.Y + actualNy * 60);
                    double incidence = Math.Acos(Math.Max(-1, Math.Min(1, cosI))) * 180.0 / Math.PI;

                    string action = !hitSurface.IsRefractive ? "Reflection" : (isTIR ? "TIR" : "Refraction");
                    Segments.Add(new RaySegment { Start = currentPos, End = hitPoint, IsHitting = true, NormalEnd = normalTarget, IncidenceAngleDeg = incidence, ActionType = action });

                    if (!hitSurface.IsRefractive || isTIR)
                    {
                        double Rx = rayDx + 2 * cosI * actualNx;
                        double Ry = rayDy + 2 * cosI * actualNy;
                        currentAngle = Math.Atan2(Ry, Rx);
                    }
                    else
                    {
                        double cosT = Math.Sqrt(1.0 - sinT2);
                        double refrX = r * rayDx + (r * cosI - cosT) * actualNx;
                        double refrY = r * rayDy + (r * cosI - cosT) * actualNy;
                        currentAngle = Math.Atan2(refrY, refrX);
                    }
                    currentPos = hitPoint;
                }
                else
                {
                    Segments.Add(new RaySegment { Start = currentPos, End = new Point(currentPos.X + MaxRayLength * rayDx, currentPos.Y + MaxRayLength * rayDy), IsHitting = false });
                    break;
                }
            }
        }
    }
}