using System;
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
            Radius = newRadius;
            Diameter = newRadius * 2.0;
        }

        public void UpdateDimensions(double canvasWidth, double canvasHeight)
        {
            Center = new Point(canvasWidth * 0.75 - Radius, canvasHeight / 2.0);

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