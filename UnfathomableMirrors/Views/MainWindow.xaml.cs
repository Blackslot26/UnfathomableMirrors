using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UnfathomableMirrors.Models; // Links the Engine to the UI

namespace UnfathomableMirrors.Views
{
    public partial class MainWindow : Window
    {
        private List<RayEmitter> emitters = new List<RayEmitter>();
        private Mirror mirror = new Mirror(900);
        private RayEmitter activeEmitter = null;
        private bool isMovingMode = true;
        private Point mousePos;
        private int rayCounter = 1;

        private SolidColorBrush[] rayColors = new SolidColorBrush[]
        {
            Brushes.Blue, Brushes.Green, Brushes.DarkOrange, Brushes.Purple,
            Brushes.Teal, Brushes.Magenta, Brushes.DeepPink, Brushes.DarkCyan, Brushes.Indigo
        };
        private int colorIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
            emitters.Add(new RayEmitter(rayCounter++, 300, 300, GetNextColor()));

            Loaded += (s, e) => { UpdateAndDraw(); };
            SizeChanged += (s, e) => { UpdateAndDraw(); };
        }

        private SolidColorBrush GetNextColor()
        {
            var color = rayColors[colorIndex % rayColors.Length];
            colorIndex++;
            return color;
        }

        private void AddRay_Click(object sender, RoutedEventArgs e)
        {
            emitters.Add(new RayEmitter(rayCounter++, 300, 300, GetNextColor()));
            UpdateAndDraw();
        }

        private void RadiusInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void RadiusInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mirror != null && this.IsLoaded && double.TryParse(RadiusInput.Text, out double newRadius))
            {
                mirror.SetRadius(newRadius);
                UpdateAndDraw();
            }
        }

        private void ShowGuides_Click(object sender, RoutedEventArgs e)
        {
            UpdateAndDraw();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                isMovingMode = !isMovingMode;
                ModeLabel.Text = isMovingMode ? "Mode: MOVING (Press 'A')  |  Right-Click Box to Delete"
                                              : "Mode: AIMING (Press 'A')  |  Right-Click Box to Delete";
                ModeLabel.Foreground = isMovingMode ? Brushes.DodgerBlue : Brushes.Crimson;
                UpdateAndDraw();
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mousePos = e.GetPosition(SimCanvas);
            foreach (var emitter in emitters)
            {
                if (emitter.IsMouseOver(mousePos))
                {
                    activeEmitter = emitter;
                    break;
                }
            }
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPos = e.GetPosition(SimCanvas);
            for (int i = emitters.Count - 1; i >= 0; i--)
            {
                if (emitters[i].IsMouseOver(clickPos))
                {
                    emitters.RemoveAt(i);
                    UpdateAndDraw();
                    break;
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            mousePos = e.GetPosition(SimCanvas);

            if (activeEmitter != null && e.LeftButton == MouseButtonState.Pressed)
            {
                if (isMovingMode)
                    activeEmitter.MoveTo(mousePos);
                else
                    activeEmitter.AimAwayFrom(mousePos);

                UpdateAndDraw();
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            activeEmitter = null;
            UpdateAndDraw();
        }

        private void UpdateAndDraw()
        {
            SimCanvas.Children.Clear();
            if (SimCanvas.ActualWidth == 0 || SimCanvas.ActualHeight == 0) return;

            mirror.UpdateDimensions(SimCanvas.ActualWidth, SimCanvas.ActualHeight);
            foreach (var ray in emitters)
            {
                ray.UpdatePhysics(mirror);
            }

            PathGeometry arcGeo = new PathGeometry();
            PathFigure arcFig = new PathFigure { StartPoint = mirror.GetArcStartPoint(), IsClosed = false };
            arcFig.Segments.Add(new ArcSegment(mirror.GetArcEndPoint(), new Size(mirror.Radius, mirror.Radius), 0, false, SweepDirection.Clockwise, true));
            arcGeo.Figures.Add(arcFig);
            SimCanvas.Children.Add(new Path { Data = arcGeo, Stroke = Brushes.Black, StrokeThickness = 3 });

            if (ShowGuidesCheck.IsChecked == true)
            {
                Ellipse centerDot = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Black };
                Canvas.SetLeft(centerDot, mirror.Center.X - 5);
                Canvas.SetTop(centerDot, mirror.Center.Y - 5);
                SimCanvas.Children.Add(centerDot);
            }

            foreach (var ray in emitters)
            {
                SimCanvas.Children.Add(new Line { X1 = ray.Position.X, Y1 = ray.Position.Y, X2 = ray.EndPoint.X, Y2 = ray.EndPoint.Y, Stroke = ray.RayColor, StrokeThickness = 2 });

                if (ray.IsHitting)
                {
                    if (ShowGuidesCheck.IsChecked == true)
                    {
                        Line normalLine = new Line
                        {
                            X1 = ray.EndPoint.X,
                            Y1 = ray.EndPoint.Y,
                            X2 = ray.NormalEndPoint.X,
                            Y2 = ray.NormalEndPoint.Y,
                            Stroke = Brushes.Red,
                            StrokeThickness = 1.5,
                            StrokeDashArray = new DoubleCollection { 4, 4 }
                        };
                        SimCanvas.Children.Add(normalLine);
                    }

                    SimCanvas.Children.Add(new Line { X1 = ray.EndPoint.X, Y1 = ray.EndPoint.Y, X2 = ray.ReflectionEndPoint.X, Y2 = ray.ReflectionEndPoint.Y, Stroke = ray.RayColor, StrokeThickness = 2 });

                    TextBlock angleText = new TextBlock { Text = $"Ray {ray.Id}\nIncidence: {ray.IncidenceAngleDeg:F1}°", Foreground = ray.RayColor, FontWeight = FontWeights.SemiBold };
                    Canvas.SetLeft(angleText, ray.EndPoint.X + 10);
                    Canvas.SetTop(angleText, ray.EndPoint.Y - 20);
                    SimCanvas.Children.Add(angleText);
                }

                Brush boxColor = isMovingMode ? Brushes.DodgerBlue : Brushes.Crimson;
                Grid emitterUI = new Grid { Width = 30, Height = 20 };
                emitterUI.Children.Add(new Rectangle { Fill = boxColor, RadiusX = 3, RadiusY = 3 });
                emitterUI.Children.Add(new TextBlock { Text = ray.Id.ToString(), Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold });

                emitterUI.RenderTransformOrigin = new Point(0.5, 0.5);
                emitterUI.RenderTransform = new RotateTransform(ray.Angle * 180.0 / Math.PI);

                Canvas.SetLeft(emitterUI, ray.Position.X - 15);
                Canvas.SetTop(emitterUI, ray.Position.Y - 10);
                SimCanvas.Children.Add(emitterUI);
            }

            if (activeEmitter != null && !isMovingMode && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Line dottedLine = new Line { X1 = activeEmitter.Position.X, Y1 = activeEmitter.Position.Y, X2 = mousePos.X, Y2 = mousePos.Y, Stroke = Brushes.Gray, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 2, 2 } };
                SimCanvas.Children.Add(dottedLine);
            }
        }
    }
}