using System.Windows;

namespace UnfathomableMirrors.Models
{
    public interface IOpticSurface
    {
        Point Position { get; set; }
        double Angle { get; set; }
        bool IsRefractive { get; }
        double RefractiveIndex { get; }
        bool IsMouseOver(Point mousePos);
        void MoveTo(Point newPos);
        void AimAwayFrom(Point target);
        void UpdateDimensions(double canvasWidth, double canvasHeight);
        bool TryIntersect(Point rayOrigin, double rayDx, double rayDy, out double t, out double nx, out double ny);
    }
}