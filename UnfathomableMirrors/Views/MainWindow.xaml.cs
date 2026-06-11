using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UnfathomableMirrors.Models;

namespace UnfathomableMirrors.Views
{
    public partial class MainWindow : Window
    {
        private List<RayEmitter> emitters = new List<RayEmitter>();
        private List<IOpticSurface> surfaces = new List<IOpticSurface>();
        private RayEmitter activeEmitter = null;
        private IOpticSurface activeSurface = null;
        private bool isMovingMode = true;
        private Point mousePos;
        private int rayCounter = 1;
        private int colorIndex = 0;
        private SolidColorBrush[] rayColors = { Brushes.Blue, Brushes.Green, Brushes.DarkOrange, Brushes.Purple, Brushes.Teal, Brushes.Magenta, Brushes.DeepPink, Brushes.DarkCyan, Brushes.Indigo };

        public MainWindow()
        {
            InitializeComponent();
            surfaces.Add(new CurvedMirror(600) { Position = new Point(700, 300) });
            emitters.Add(new RayEmitter(rayCounter++, 200, 300, GetNextColor(), angleDegrees: 0));
            Loaded += (s, e) => UpdateAndDraw();
            SizeChanged += (s, e) => UpdateAndDraw();
        }

        private SolidColorBrush GetNextColor() => rayColors[colorIndex++ % rayColors.Length];

        private void UpdateAndDraw()
        {
            if (SimCanvas == null) return;
            SimCanvas.Children.Clear();

            foreach (var surface in surfaces)
            {
                surface.UpdateDimensions(SimCanvas.ActualWidth, SimCanvas.ActualHeight);
                DrawSurface(surface);
            }

            bool showGuides = ShowGuidesCheck?.IsChecked ?? false;

            foreach (var ray in emitters)
            {
                ray.UpdatePhysics(surfaces);

                foreach (var segment in ray.Segments)
                {
                    SimCanvas.Children.Add(new Line { X1 = segment.Start.X, Y1 = segment.Start.Y, X2 = segment.End.X, Y2 = segment.End.Y, Stroke = ray.RayColor, StrokeThickness = 2 });

                    if (segment.IsHitting)
                    {
                        if (showGuides)
                        {
                            SimCanvas.Children.Add(new Line { X1 = segment.End.X, Y1 = segment.End.Y, X2 = segment.NormalEnd.X, Y2 = segment.NormalEnd.Y, Stroke = Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 2, 4 } });
                        }

                        TextBlock angleText = new TextBlock { Text = $"{Math.Round(segment.IncidenceAngleDeg, 1)}°", Foreground = ray.RayColor, FontSize = 11, FontWeight = FontWeights.SemiBold };
                        Canvas.SetLeft(angleText, segment.End.X + 10);
                        Canvas.SetTop(angleText, segment.End.Y - 20);
                        SimCanvas.Children.Add(angleText);
                    }
                }

                Grid emitterUI = new Grid { Width = 30, Height = 20, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new RotateTransform(ray.Angle * 180.0 / Math.PI) };
                emitterUI.Children.Add(new Rectangle { Fill = isMovingMode ? Brushes.DodgerBlue : Brushes.Crimson, RadiusX = 3, RadiusY = 3 });
                emitterUI.Children.Add(new TextBlock { Text = ray.Id.ToString(), Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold });
                Canvas.SetLeft(emitterUI, ray.Position.X - 15);
                Canvas.SetTop(emitterUI, ray.Position.Y - 10);
                SimCanvas.Children.Add(emitterUI);
            }

            if ((activeEmitter != null || activeSurface != null) && !isMovingMode && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point startPoint = activeEmitter != null ? activeEmitter.Position : activeSurface.Position;
                SimCanvas.Children.Add(new Line { X1 = startPoint.X, Y1 = startPoint.Y, X2 = mousePos.X, Y2 = mousePos.Y, Stroke = Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 } });
            }
        }

        private void DrawSurface(IOpticSurface surface)
        {
            if (surface is CurvedMirror curved)
            {
                Path geometryPath = new Path { Stroke = Brushes.Black, StrokeThickness = 4 };
                PathGeometry pathGeometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();

                double expectedAngle = curved.Angle + Math.PI;
                pathFigure.StartPoint = new Point(curved.Center.X + curved.Radius * Math.Cos(expectedAngle - curved.MaxAngle), curved.Center.Y + curved.Radius * Math.Sin(expectedAngle - curved.MaxAngle));
                pathFigure.Segments.Add(new ArcSegment { Point = new Point(curved.Center.X + curved.Radius * Math.Cos(expectedAngle + curved.MaxAngle), curved.Center.Y + curved.Radius * Math.Sin(expectedAngle + curved.MaxAngle)), Size = new Size(curved.Radius, curved.Radius), SweepDirection = SweepDirection.Clockwise, IsLargeArc = false });

                pathGeometry.Figures.Add(pathFigure);
                geometryPath.Data = pathGeometry;
                SimCanvas.Children.Add(geometryPath);
            }
            else if (surface is PlaneMirror plane)
            {
                SimCanvas.Children.Add(new Line { X1 = plane.StartPoint.X, Y1 = plane.StartPoint.Y, X2 = plane.EndPoint.X, Y2 = plane.EndPoint.Y, Stroke = Brushes.Black, StrokeThickness = 5 });
            }

            Ellipse dragHandle = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Black, Cursor = Cursors.SizeAll };
            Canvas.SetLeft(dragHandle, surface.Position.X - 5);
            Canvas.SetTop(dragHandle, surface.Position.Y - 5);
            SimCanvas.Children.Add(dragHandle);
        }

        private void AddSurface_Click(object sender, RoutedEventArgs e)
        {
            if (SurfaceSelector == null) return;
            double radius = double.TryParse(RadiusInput.Text, out double r) ? r : 600;
            if (((ComboBoxItem)SurfaceSelector.SelectedItem).Content.ToString() == "Curved Mirror") surfaces.Add(new CurvedMirror(radius) { Position = new Point(400, 250) });
            else surfaces.Add(new PlaneMirror() { Position = new Point(400, 250) });
            UpdateAndDraw();
        }

        private void AddRay_Click(object sender, RoutedEventArgs e) { emitters.Add(new RayEmitter(rayCounter++, 100, 100, GetNextColor())); UpdateAndDraw(); }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mousePos = e.GetPosition(SimCanvas);
            if (e.ChangedButton == MouseButton.Right) { emitters.RemoveAll(r => r.IsMouseOver(mousePos)); surfaces.RemoveAll(s => s.IsMouseOver(mousePos)); UpdateAndDraw(); return; }
            activeEmitter = emitters.Find(r => r.IsMouseOver(mousePos));
            if (activeEmitter == null) activeSurface = surfaces.Find(s => s.IsMouseOver(mousePos));
            if ((activeEmitter != null || activeSurface != null) && !isMovingMode) { activeEmitter?.AimAwayFrom(mousePos); activeSurface?.AimAwayFrom(mousePos); UpdateAndDraw(); }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            mousePos = e.GetPosition(SimCanvas);
            if (activeEmitter != null) { if (isMovingMode) activeEmitter.MoveTo(mousePos); else activeEmitter.AimAwayFrom(mousePos); }
            else if (activeSurface != null) { if (isMovingMode) activeSurface.MoveTo(mousePos); else activeSurface.AimAwayFrom(mousePos); }
            UpdateAndDraw();
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e) { activeEmitter = null; activeSurface = null; UpdateAndDraw(); }
        private void RadiusInput_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        private void ShowGuides_Click(object sender, RoutedEventArgs e) => UpdateAndDraw();
        private void Window_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.A) { isMovingMode = !isMovingMode; ModeLabel.Text = isMovingMode ? "Mode: MOVING (Press 'A') | Right-Click to Delete" : "Mode: AIMING (Press 'A') | Right-Click to Delete"; UpdateAndDraw(); } }
    }
}