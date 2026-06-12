using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using UnfathomableMirrors.Models;

namespace UnfathomableMirrors.Views
{
    public class SurfaceDto
    {
        public string Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }
        public double Radius { get; set; }
        public double Length { get; set; }
        public double Thickness { get; set; }
        public double RefractiveIndex { get; set; }
    }

    public class EmitterDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double AngleDegrees { get; set; }
        public double Wavelength { get; set; }
        public double DispersionModifier { get; set; }
        public string ColorHex { get; set; }
    }

    public class SceneDto
    {
        public List<SurfaceDto> Surfaces { get; set; } = new List<SurfaceDto>();
        public List<EmitterDto> Emitters { get; set; } = new List<EmitterDto>();
    }

    public partial class MainWindow : Window
    {
        private List<RayEmitter> emitters = new List<RayEmitter>();
        private List<IOpticSurface> surfaces = new List<IOpticSurface>();
        private RayEmitter activeEmitter = null;
        private IOpticSurface activeSurface = null;
        private bool isMovingMode = true;
        private Point mousePos;
        private int rayCounter = 1;
        private int groupCounter = 1;
        private int colorIndex = 0;
        private bool isMeasuring = false;
        private Point? measureStart = null;
        private Point? measureEnd = null;
        private SolidColorBrush[] rayColors = { Brushes.Blue, Brushes.Green, Brushes.DarkOrange, Brushes.Purple, Brushes.Teal, Brushes.Magenta, Brushes.DeepPink, Brushes.DarkCyan, Brushes.Indigo };

        public MainWindow()
        {
            InitializeComponent();
            surfaces.Add(new BiconvexLens(250, 100, 1.5) { Position = new Point(700, 300) });
            emitters.Add(new RayEmitter(rayCounter++, groupCounter++, 200, 300, WavelengthToBrush(550), 0, 550, 0));
            Loaded += (s, e) => UpdateAndDraw();
            SizeChanged += (s, e) => UpdateAndDraw();
        }

        private SolidColorBrush GetNextColor() => rayColors[colorIndex++ % rayColors.Length];

        private SolidColorBrush WavelengthToBrush(double wavelength)
        {
            double r = 0, g = 0, b = 0;
            if (wavelength >= 380 && wavelength < 440) { r = -(wavelength - 440) / (440 - 380); b = 1; }
            else if (wavelength >= 440 && wavelength < 490) { g = (wavelength - 440) / (490 - 440); b = 1; }
            else if (wavelength >= 490 && wavelength < 510) { g = 1; b = -(wavelength - 510) / (510 - 490); }
            else if (wavelength >= 510 && wavelength < 580) { r = (wavelength - 510) / (580 - 510); g = 1; }
            else if (wavelength >= 580 && wavelength < 645) { r = 1; g = -(wavelength - 645) / (645 - 580); }
            else if (wavelength >= 645 && wavelength <= 750) { r = 1; }

            double factor = 1.0;
            if (wavelength >= 380 && wavelength < 420) factor = 0.3 + 0.7 * (wavelength - 380) / (420 - 380);
            else if (wavelength >= 700 && wavelength <= 750) factor = 0.3 + 0.7 * (750 - wavelength) / (750 - 700);

            return new SolidColorBrush(Color.FromRgb((byte)(r * factor * 255), (byte)(g * factor * 255), (byte)(b * factor * 255)));
        }

        private double WavelengthToDispersion(double wavelength) => (550.0 - wavelength) * 0.0002;

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
            var dataExport = new List<object>();

            foreach (var ray in emitters)
            {
                ray.UpdatePhysics(surfaces);
                int hitOrder = 1;
                bool isPrimaryRay = ray.DispersionModifier == 0.0 || ray.Id == emitters.Find(r => r.GroupId == ray.GroupId).Id;

                PointCollection pathPoints = new PointCollection();
                if (ray.Segments.Count > 0) pathPoints.Add(ray.Segments[0].Start);

                foreach (var segment in ray.Segments)
                {
                    pathPoints.Add(segment.End);
                    if (segment.IsHitting && isPrimaryRay)
                    {
                        dataExport.Add(new { RayGrp = ray.GroupId, Wave = (int)ray.Wavelength + "nm", Bounce = hitOrder++, Action = segment.ActionType, Angle = Math.Round(segment.IncidenceAngleDeg, 1) + "°" });
                        if (showGuides) SimCanvas.Children.Add(new Line { X1 = segment.End.X, Y1 = segment.End.Y, X2 = segment.NormalEnd.X, Y2 = segment.NormalEnd.Y, Stroke = Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 2, 4 } });
                        TextBlock angleText = new TextBlock { Text = $"{Math.Round(segment.IncidenceAngleDeg, 1)}°", Foreground = Brushes.Black, Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), Padding = new Thickness(2), FontSize = 11, FontWeight = FontWeights.Bold };
                        Canvas.SetLeft(angleText, segment.End.X + 10); Canvas.SetTop(angleText, segment.End.Y - 20); Canvas.SetZIndex(angleText, 100);
                        SimCanvas.Children.Add(angleText);
                    }
                }

                SimCanvas.Children.Add(new Polyline { Points = pathPoints, Stroke = ray.RayColor, StrokeThickness = 2 });

                Grid nozzleUI = new Grid { Width = 14, Height = 10, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new RotateTransform(ray.Angle * 180.0 / Math.PI) };
                nozzleUI.Children.Add(new Rectangle { Fill = Brushes.DarkSlateGray, Stroke = Brushes.Black, StrokeThickness = 1, RadiusX = 1, RadiusY = 1 });
                Canvas.SetLeft(nozzleUI, ray.Position.X - 7); Canvas.SetTop(nozzleUI, ray.Position.Y - 5); Canvas.SetZIndex(nozzleUI, 99);
                SimCanvas.Children.Add(nozzleUI);

                var groupRays = emitters.FindAll(r => r.GroupId == ray.GroupId);
                if (groupRays.Count > 1 && ray.Id == groupRays[0].Id && ray.DispersionModifier == 0.0)
                {
                    bool isLaserGroup = false;
                    foreach (var gr in groupRays) { if (gr.Position != ray.Position) { isLaserGroup = true; break; } }
                    if (isLaserGroup)
                    {
                        Line housingBack = new Line { X1 = groupRays[0].Position.X, Y1 = groupRays[0].Position.Y, X2 = groupRays[groupRays.Count - 1].Position.X, Y2 = groupRays[groupRays.Count - 1].Position.Y, Stroke = Brushes.DimGray, StrokeThickness = 12, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
                        Canvas.SetZIndex(housingBack, 98);
                        SimCanvas.Children.Add(housingBack);
                    }
                }

                if (ray.Id == groupRays[0].Id)
                {
                    Grid emitterUI = new Grid { Width = 30, Height = 20, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new RotateTransform(ray.Angle * 180.0 / Math.PI) };
                    emitterUI.Children.Add(new Rectangle { Fill = isMovingMode ? Brushes.DodgerBlue : Brushes.Crimson, RadiusX = 3, RadiusY = 3 });
                    emitterUI.Children.Add(new TextBlock { Text = ray.GroupId.ToString(), Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold });
                    Canvas.SetLeft(emitterUI, ray.Position.X - 15); Canvas.SetTop(emitterUI, ray.Position.Y - 10); Canvas.SetZIndex(emitterUI, 101);
                    SimCanvas.Children.Add(emitterUI);
                }
            }

            PhysicsDataTable.ItemsSource = dataExport;

            if (measureStart != null)
            {
                Point p1 = measureStart.Value;
                Point p2 = (Mouse.LeftButton == MouseButtonState.Pressed && isMeasuring) ? mousePos : (measureEnd ?? mousePos);

                SimCanvas.Children.Add(new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = Brushes.Purple, StrokeThickness = 2 });

                double dx = p2.X - p1.X; double dy = p2.Y - p1.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist > 5)
                {
                    double ux = dx / dist; double uy = dy / dist;
                    double nx = -uy; double ny = ux;

                    for (double offset = 0; offset <= dist; offset += 20)
                    {
                        double tx = p1.X + ux * offset; double ty = p1.Y + uy * offset;
                        double tickLen = ((int)Math.Round(offset) % 100 == 0) ? 10 : 5;
                        SimCanvas.Children.Add(new Line { X1 = tx, Y1 = ty, X2 = tx + nx * tickLen, Y2 = ty + ny * tickLen, Stroke = Brushes.Purple, StrokeThickness = 1 });

                        if ((int)Math.Round(offset) % 100 == 0 && offset > 0 && offset < dist - 10)
                        {
                            TextBlock tickText = new TextBlock { Text = $"{(int)offset}", Foreground = Brushes.Purple, FontSize = 9, FontWeight = FontWeights.Bold };
                            Canvas.SetLeft(tickText, tx + nx * 12 - 5); Canvas.SetTop(tickText, ty + ny * 12 - 5);
                            SimCanvas.Children.Add(tickText);
                        }
                    }
                }
            }

            if ((activeEmitter != null || activeSurface != null) && !isMovingMode && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point startPoint = activeEmitter != null ? activeEmitter.Position : activeSurface.Position;
                SimCanvas.Children.Add(new Line { X1 = startPoint.X, Y1 = startPoint.Y, X2 = mousePos.X, Y2 = mousePos.Y, Stroke = Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 } });
            }
        }

        private void DrawSurface(IOpticSurface surface)
        {
            if (surface is BiconvexLens lens)
            {
                EllipseGeometry e1 = new EllipseGeometry(lens.C1, lens.Radius, lens.Radius);
                EllipseGeometry e2 = new EllipseGeometry(lens.C2, lens.Radius, lens.Radius);
                SimCanvas.Children.Add(new Path { Data = new CombinedGeometry(GeometryCombineMode.Intersect, e1, e2), Fill = new SolidColorBrush(Color.FromArgb(80, 0, 200, 255)), Stroke = Brushes.DarkBlue, StrokeThickness = 2 });
            }
            else if (surface is RefractionBlock block)
            {
                Polygon rect = new Polygon { Fill = new SolidColorBrush(Color.FromArgb(50, 0, 150, 255)), Stroke = Brushes.DarkBlue, StrokeThickness = 2 };
                foreach (var p in block.Corners) rect.Points.Add(p);
                SimCanvas.Children.Add(rect);
            }
            else if (surface is CurvedMirror curved)
            {
                PathGeometry pathGeometry = new PathGeometry(); PathFigure pathFigure = new PathFigure();
                double expectedAngle = curved.Angle + Math.PI;
                pathFigure.StartPoint = new Point(curved.Center.X + curved.Radius * Math.Cos(expectedAngle - curved.MaxAngle), curved.Center.Y + curved.Radius * Math.Sin(expectedAngle - curved.MaxAngle));
                pathFigure.Segments.Add(new ArcSegment { Point = new Point(curved.Center.X + curved.Radius * Math.Cos(expectedAngle + curved.MaxAngle), curved.Center.Y + curved.Radius * Math.Sin(expectedAngle + curved.MaxAngle)), Size = new Size(curved.Radius, curved.Radius), SweepDirection = SweepDirection.Clockwise });
                pathGeometry.Figures.Add(pathFigure);
                SimCanvas.Children.Add(new Path { Data = pathGeometry, Stroke = Brushes.Black, StrokeThickness = 4 });
            }
            else if (surface is StraightMirror straight)
            {
                SimCanvas.Children.Add(new Line { X1 = straight.StartPoint.X, Y1 = straight.StartPoint.Y, X2 = straight.EndPoint.X, Y2 = straight.EndPoint.Y, Stroke = Brushes.Black, StrokeThickness = 5 });
            }

            Ellipse dragHandle = new Ellipse { Width = 10, Height = 10, Fill = Brushes.Black, Cursor = Cursors.SizeAll };
            Canvas.SetLeft(dragHandle, surface.Position.X - 5); Canvas.SetTop(dragHandle, surface.Position.Y - 5); Canvas.SetZIndex(dragHandle, 10);
            SimCanvas.Children.Add(dragHandle);
        }

        private void AddSurface_Click(object sender, RoutedEventArgs e)
        {
            if (SurfaceSelector == null) return;
            double radius = double.TryParse(RadiusInput.Text, out double r) ? r : 600;
            double length = double.TryParse(LengthInput.Text, out double l) ? l : 240;
            double thickness = double.TryParse(ThicknessInput.Text, out double th) ? th : 100;
            double index = double.TryParse(IndexInput.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double n) ? n : 1.5;

            string sel = ((ComboBoxItem)SurfaceSelector.SelectedItem).Content.ToString();
            if (sel == "Curved Mirror") surfaces.Add(new CurvedMirror(radius, length) { Position = new Point(400, 250) });
            else if (sel == "Straight Mirror") surfaces.Add(new StraightMirror(length) { Position = new Point(400, 250) });
            else if (sel == "Refraction Block") surfaces.Add(new RefractionBlock(length, thickness, index) { Position = new Point(400, 250) });
            else if (sel == "Biconvex Lens") surfaces.Add(new BiconvexLens(radius, thickness, index) { Position = new Point(400, 250) });
            UpdateAndDraw();
        }

        private void AddRay_Click(object sender, RoutedEventArgs e)
        {
            double w = WavelengthSlider.Value;
            emitters.Add(new RayEmitter(rayCounter++, groupCounter++, 100, 100, WavelengthToBrush(w), 0, w, WavelengthToDispersion(w)));
            UpdateAndDraw();
        }

        private void AddWhiteLight_Click(object sender, RoutedEventArgs e)
        {
            int count = int.TryParse(RayDensityInput.Text, out int num) ? num : 5;
            count = Math.Max(2, Math.Min(count, 35));
            int grp = groupCounter++;
            double startW = 400; double endW = 700;
            double stepW = (endW - startW) / Math.Max(1, count - 1);

            for (int i = 0; i < count; i++)
            {
                double w = startW + i * stepW;
                emitters.Add(new RayEmitter(rayCounter++, grp, 100, 150, WavelengthToBrush(w), 0, w, WavelengthToDispersion(w)));
            }
            UpdateAndDraw();
        }

        private void AddLamp_Click(object sender, RoutedEventArgs e)
        {
            double w = WavelengthSlider.Value;
            int grp = groupCounter++;
            int count = int.TryParse(RayDensityInput.Text, out int num) ? num : 24;
            count = Math.Max(4, Math.Min(count, 120));
            double step = 360.0 / count;
            for (int i = 0; i < count; i++) emitters.Add(new RayEmitter(rayCounter++, grp, 200, 300, WavelengthToBrush(w), i * step, w, WavelengthToDispersion(w)));
            UpdateAndDraw();
        }

        private void AddLaserBeam_Click(object sender, RoutedEventArgs e)
        {
            double w = WavelengthSlider.Value;
            int grp = groupCounter++;
            int count = int.TryParse(RayDensityInput.Text, out int num) ? num : 6;
            count = Math.Max(1, Math.Min(count, 60));
            double spacing = 12;
            double startY = 300 - ((count - 1) * spacing / 2.0);
            for (int i = 0; i < count; i++) emitters.Add(new RayEmitter(rayCounter++, grp, 200, startY + i * spacing, WavelengthToBrush(w), 0, w, WavelengthToDispersion(w)));
            UpdateAndDraw();
        }

        private void ToggleRuler_Click(object sender, RoutedEventArgs e)
        {
            isMeasuring = !isMeasuring; measureStart = null; measureEnd = null;
            MeasureLabel.Text = isMeasuring ? "Ruler: Click & Drag" : "";
            UpdateAndDraw();
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point currentClick = e.GetPosition(SimCanvas);
            if (isMeasuring) { measureStart = currentClick; measureEnd = null; UpdateAndDraw(); return; }
            mousePos = currentClick;

            if (e.ChangedButton == MouseButton.Right)
            {
                var toDelete = emitters.Find(r => r.IsMouseOver(mousePos));
                if (toDelete != null) emitters.RemoveAll(r => r.GroupId == toDelete.GroupId);
                surfaces.RemoveAll(s => s.IsMouseOver(mousePos));
                UpdateAndDraw(); return;
            }
            activeEmitter = emitters.Find(r => r.IsMouseOver(mousePos));
            if (activeEmitter == null) activeSurface = surfaces.Find(s => s.IsMouseOver(mousePos));
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point newMousePos = e.GetPosition(SimCanvas);
            double deltaX = newMousePos.X - mousePos.X;
            double deltaY = newMousePos.Y - mousePos.Y;
            mousePos = newMousePos;

            if (isMeasuring && measureStart != null && e.LeftButton == MouseButtonState.Pressed) { UpdateAndDraw(); return; }
            if (e.LeftButton != MouseButtonState.Pressed) return;

            if (activeEmitter != null)
            {
                var groupRays = emitters.FindAll(r => r.GroupId == activeEmitter.GroupId);
                if (isMovingMode)
                {
                    foreach (var em in groupRays) em.MoveTo(new Point(em.Position.X + deltaX, em.Position.Y + deltaY));
                }
                else
                {
                    double rawAngle = Math.Atan2(activeEmitter.Position.Y - newMousePos.Y, activeEmitter.Position.X - newMousePos.X);
                    double snappedAngleDeg = Math.Round(rawAngle * 180.0 / Math.PI);
                    double newAngleRad = snappedAngleDeg * Math.PI / 180.0;
                    bool isLamp = groupRays.Count > 10;
                    if (isLamp)
                    {
                        double step = 360.0 / groupRays.Count; int lampIndex = 0;
                        foreach (var em in groupRays) { em.SetAngleDegrees((newAngleRad * 180.0 / Math.PI) + (lampIndex * step)); lampIndex++; }
                    }
                    else { foreach (var em in groupRays) em.SetAngleDegrees(newAngleRad * 180.0 / Math.PI); }
                }
            }
            else if (activeSurface != null)
            {
                if (isMovingMode) activeSurface.MoveTo(new Point(activeSurface.Position.X + deltaX, activeSurface.Position.Y + deltaY));
                else activeSurface.AimAwayFrom(newMousePos);
            }
            UpdateAndDraw();
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isMeasuring && measureStart != null)
            {
                measureEnd = e.GetPosition(SimCanvas);
                double distance = Math.Round(Math.Sqrt(Math.Pow(measureEnd.Value.X - measureStart.Value.X, 2) + Math.Pow(measureEnd.Value.Y - measureStart.Value.Y, 2)), 1);
                MeasureLabel.Text = $"Dist: {distance} px";
            }
            activeEmitter = null; activeSurface = null; UpdateAndDraw();
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        private void ShowGuides_Click(object sender, RoutedEventArgs e) => UpdateAndDraw();
        private void WavelengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (WavelengthLabel != null) WavelengthLabel.Text = $"{(int)WavelengthSlider.Value} nm"; }
        private void Window_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.A) { isMovingMode = !isMovingMode; ModeLabel.Text = isMovingMode ? "Mode: MOVING" : "Mode: AIMING"; ModeLabel.Foreground = isMovingMode ? Brushes.DodgerBlue : Brushes.Crimson; UpdateAndDraw(); } }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "JSON Files (*.json)|*.json", FileName = "scene.json" };
            if (sfd.ShowDialog() == true)
            {
                var dto = new SceneDto();
                foreach (var s in surfaces)
                {
                    var sd = new SurfaceDto { X = s.Position.X, Y = s.Position.Y, Angle = s.Angle, RefractiveIndex = s.RefractiveIndex };
                    if (s is CurvedMirror cm) { sd.Type = "CurvedMirror"; sd.Radius = cm.Radius; sd.Length = cm.Length; }
                    else if (s is StraightMirror sm) { sd.Type = "StraightMirror"; sd.Length = sm.Length; }
                    else if (s is RefractionBlock rb) { sd.Type = "RefractionBlock"; sd.Length = rb.Length; sd.Thickness = rb.Thickness; }
                    else if (s is BiconvexLens bl) { sd.Type = "BiconvexLens"; sd.Radius = bl.Radius; sd.Thickness = bl.Thickness; }
                    dto.Surfaces.Add(sd);
                }
                foreach (var em in emitters) dto.Emitters.Add(new EmitterDto { Id = em.Id, GroupId = em.GroupId, X = em.Position.X, Y = em.Position.Y, AngleDegrees = em.Angle * 180.0 / Math.PI, Wavelength = em.Wavelength, DispersionModifier = em.DispersionModifier, ColorHex = em.RayColor.Color.ToString() });
                System.IO.File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(dto));
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "JSON Files (*.json)|*.json" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<SceneDto>(System.IO.File.ReadAllText(ofd.FileName));
                    if (dto == null) return;
                    surfaces.Clear(); emitters.Clear();
                    foreach (var s in dto.Surfaces)
                    {
                        IOpticSurface surf = null;
                        if (s.Type == "CurvedMirror") surf = new CurvedMirror(s.Radius, s.Length);
                        else if (s.Type == "StraightMirror") surf = new StraightMirror(s.Length);
                        else if (s.Type == "RefractionBlock") surf = new RefractionBlock(s.Length, s.Thickness, s.RefractiveIndex);
                        else if (s.Type == "BiconvexLens") surf = new BiconvexLens(s.Radius, s.Thickness, s.RefractiveIndex);
                        if (surf != null) { surf.Position = new Point(s.X, s.Y); surf.Angle = s.Angle; surfaces.Add(surf); }
                    }
                    foreach (var em in dto.Emitters) emitters.Add(new RayEmitter(em.Id, em.GroupId, em.X, em.Y, new SolidColorBrush((Color)ColorConverter.ConvertFromString(em.ColorHex)), em.AngleDegrees, em.Wavelength, em.DispersionModifier));
                    rayCounter = emitters.Count > 0 ? emitters[emitters.Count - 1].Id + 1 : 1;
                    groupCounter = emitters.Count > 0 ? emitters[emitters.Count - 1].GroupId + 1 : 1;
                    UpdateAndDraw();
                }
                catch (Exception ex) { MessageBox.Show("Error loading: " + ex.Message); }
            }
        }
    }
}