using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace UnfathomableMirrors.Models
{
    public class RaySegment
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public bool IsHitting { get; set; }
        public Point NormalEnd { get; set; }
        public double IncidenceAngleDeg { get; set; }
    }

    public class RayEmitter
    {
        public int Id { get; set; }
        public Point Position { get; set; }
        public double Angle { get; private set; }
        public SolidColorBrush RayColor { get; private set; }
        public List<RaySegment> Segments { get; private set; } = new List<RaySegment>();

        private const double MaxRayLength = 2000;
        private const int MaxBounces = 15;

        public RayEmitter(int id, double x, double y, SolidColorBrush color, double angleDegrees = 0)
        {
            Id = id; Position = new Point(x, y); RayColor = color; SetAngleDegrees(angleDegrees);
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
            IOpticSurface lastHitSurface = null;

            for (int bounce = 0; bounce < MaxBounces; bounce++)
            {
                double minT = double.MaxValue, bestNx = 0, bestNy = 0;
                IOpticSurface hitSurface = null;

                foreach (var surface in surfaces)
                {
                    if (surface == lastHitSurface) continue;
                    if (surface.TryIntersect(currentPos, currentAngle, out double t, out double nx, out double ny))
                    {
                        if (t < minT) { minT = t; bestNx = nx; bestNy = ny; hitSurface = surface; }
                    }
                }

                if (hitSurface != null)
                {
                    double rayDx = Math.Cos(currentAngle), rayDy = Math.Sin(currentAngle);

                    // Invierte la normal si apunta en la misma dirección que el rayo
                    if (rayDx * bestNx + rayDy * bestNy > 0) { bestNx = -bestNx; bestNy = -bestNy; }

                    Point hitPoint = new Point(currentPos.X + minT * rayDx, currentPos.Y + minT * rayDy);
                    Point normalTarget = new Point(hitPoint.X + bestNx * 60, hitPoint.Y + bestNy * 60);

                    double dotProduct = -(rayDx * bestNx + rayDy * bestNy);
                    double incidence = Math.Acos(Math.Max(-1, Math.Min(1, dotProduct))) * 180.0 / Math.PI;

                    Segments.Add(new RaySegment { Start = currentPos, End = hitPoint, IsHitting = true, NormalEnd = normalTarget, IncidenceAngleDeg = incidence });

                    double dotIN = rayDx * bestNx + rayDy * bestNy;
                    double Rx = rayDx - 2 * dotIN * bestNx, Ry = rayDy - 2 * dotIN * bestNy;

                    currentPos = hitPoint;
                    currentAngle = Math.Atan2(Ry, Rx);
                    lastHitSurface = hitSurface;
                }
                else
                {
                    Segments.Add(new RaySegment { Start = currentPos, End = new Point(currentPos.X + MaxRayLength * Math.Cos(currentAngle), currentPos.Y + MaxRayLength * Math.Sin(currentAngle)), IsHitting = false });
                    break;
                }
            }
        }
    }
}