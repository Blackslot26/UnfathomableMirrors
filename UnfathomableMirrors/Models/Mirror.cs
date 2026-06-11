using System.Windows;

namespace UnfathomableMirrors
{
    public class Mirror
    {
        public Point Center { get; private set; }
        public double Radius { get; private set; }
        public double Diameter { get; private set; }
        public double MaxAngle { get; private set; }

        public Mirror(double initialRadius)
        {
            SetRadius(initialRadius);
        }

        public void SetRadius(double newRadius)
        {
            // Forces the radius to always stay between 300 and 3000
            Radius = Math.Max(300, Math.Min(newRadius, 3000));
            Diameter = Radius * 2.0;
        }

        public void UpdateDimensions(double canvasWidth, double canvasHeight)
        {
            // Places the surface of the mirror exactly in the center of the window
            Center = new Point(canvasWidth / 2.0 - Radius, canvasHeight / 2.0);

            double cutDistanceY = canvasHeight / 4.0;
            double ratio = Math.Max(0, Math.Min(cutDistanceY / Radius, 1));
            MaxAngle = Math.Asin(ratio);
        }

        public bool IsAngleWithinBounds(double angleRad)
        {
            return angleRad >= -MaxAngle && angleRad <= MaxAngle;
        }

        public Point GetArcStartPoint()
        {
            return new Point(Center.X + Radius * Math.Cos(-MaxAngle), Center.Y + Radius * Math.Sin(-MaxAngle));
        }

        public Point GetArcEndPoint()
        {
            return new Point(Center.X + Radius * Math.Cos(MaxAngle), Center.Y + Radius * Math.Sin(MaxAngle));
        }
    }
}