using System.Windows;
using System.Windows.Media;

namespace UnfathomableMirrors.Models;

public class RayEmitter
{
    public int Id { get; set; }
    public Point Position { get; set; }
    public double Angle { get; private set; }
    public Point EndPoint { get; private set; }
    public Point NormalEndPoint { get; private set; }
    public Point ReflectionEndPoint { get; private set; }
    public bool IsHitting { get; private set; }
    public double IncidenceAngleDeg { get; private set; }

    public SolidColorBrush RayColor { get; private set; }

    private const double MaxRayLength = 2000;

    // Added color parameter to the constructor
    public RayEmitter(int id, double x, double y, SolidColorBrush color, double angleDegrees = 0)
    {
        Id = id;
        Position = new Point(x, y);
        RayColor = color;
        SetAngleDegrees(angleDegrees);
    }

    public void MoveTo(Point newPos)
    {
        Position = newPos;
    }

    public void AimAwayFrom(Point target)
    {
        double rawAngle = Math.Atan2(Position.Y - target.Y, Position.X - target.X);
        double snappedAngleDeg = Math.Round(rawAngle * 180.0 / Math.PI);
        Angle = snappedAngleDeg * Math.PI / 180.0;
    }

    public void SetAngleDegrees(double degrees)
    {
        Angle = degrees * Math.PI / 180.0;
    }

    public bool IsMouseOver(Point mousePos)
    {
        return Math.Abs(mousePos.X - Position.X) <= 15 && Math.Abs(mousePos.Y - Position.Y) <= 10;
    }

    public void UpdatePhysics(Mirror mirror)
    {
        EndPoint = new Point(Position.X + MaxRayLength * Math.Cos(Angle), Position.Y + MaxRayLength * Math.Sin(Angle));
        IsHitting = false;

        double dx = Position.X - mirror.Center.X;
        double dy = Position.Y - mirror.Center.Y;

        double b = 2 * (dx * Math.Cos(Angle) + dy * Math.Sin(Angle));
        double c = (dx * dx + dy * dy) - (mirror.Radius * mirror.Radius);
        double discriminant = (b * b) - (4 * c);

        if (discriminant < 0) return;

        double t1 = (-b + Math.Sqrt(discriminant)) / 2.0;
        double t2 = (-b - Math.Sqrt(discriminant)) / 2.0;
        double validT = -1;
        double minValidT = double.MaxValue;

        double[] possibleTs = { t1, t2 };
        for (int i = 0; i < 2; i++)
        {
            if (possibleTs[i] >= 0)
            {
                double hitX = Position.X + possibleTs[i] * Math.Cos(Angle);
                double hitY = Position.Y + possibleTs[i] * Math.Sin(Angle);
                double hitAngle = Math.Atan2(hitY - mirror.Center.Y, hitX - mirror.Center.X);

                if (mirror.IsAngleWithinBounds(hitAngle) && possibleTs[i] < minValidT)
                {
                    minValidT = possibleTs[i];
                    validT = possibleTs[i];
                }
            }
        }

        if (validT >= 0)
        {
            IsHitting = true;
            EndPoint = new Point(Position.X + validT * Math.Cos(Angle), Position.Y + validT * Math.Sin(Angle));
            NormalEndPoint = mirror.Center;

            double v1x = Position.X - EndPoint.X, v1y = Position.Y - EndPoint.Y;
            double v2x = mirror.Center.X - EndPoint.X, v2y = mirror.Center.Y - EndPoint.Y;
            double dotProduct = (v1x * v2x) + (v1y * v2y);
            double mag1 = Math.Sqrt(v1x * v1x + v1y * v1y);

            IncidenceAngleDeg = Math.Acos(dotProduct / (mag1 * mirror.Radius)) * 180.0 / Math.PI;

            double Ix = Math.Cos(Angle), Iy = Math.Sin(Angle);
            double Nx = v2x / mirror.Radius, Ny = v2y / mirror.Radius;
            double dotIN = (Ix * Nx) + (Iy * Ny);

            double Rx = Ix - 2 * dotIN * Nx;
            double Ry = Iy - 2 * dotIN * Ny;

            ReflectionEndPoint = new Point(EndPoint.X + MaxRayLength * Rx, EndPoint.Y + MaxRayLength * Ry);
        }
    }
}
