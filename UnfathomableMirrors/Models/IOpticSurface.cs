using System.Windows;

namespace UnfathomableMirrors.Models
{
    public interface IOpticSurface
    {
        Point Position { get; set; }
        double Angle { get; set; }
        bool IsMouseOver(Point mousePos);
        void MoveTo(Point newPos);
        void AimAwayFrom(Point target);
        void UpdateDimensions(double canvasWidth, double canvasHeight);
        bool TryIntersect(Point rayOrigin, double rayAngleRad, out double t, out double nx, out double ny);
    }
}