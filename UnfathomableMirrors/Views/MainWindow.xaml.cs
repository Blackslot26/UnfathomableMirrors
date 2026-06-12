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
    public class SurfaceDto { public string Type { get; set; } public double X { get; set; } public double Y { get; set; } public double Angle { get; set; } public double Radius { get; set; } public double Length { get; set; } public double Thickness { get; set; } public double RefractiveIndex { get; set; } }
    public class EmitterDto { public int Id { get; set; } public int GroupId { get; set; } public double X { get; set; } public double Y { get; set; } public double AngleDegrees { get; set; } public double Wavelength { get; set; } public double DispersionModifier { get; set; } public string ColorHex { get; set; } }
    public class SceneDto { public List<SurfaceDto> Surfaces { get; set; } = new List<SurfaceDto>(); public List<EmitterDto> Emitters { get; set; } = new List<EmitterDto>(); }

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
        private bool isDarkTheme = false;

        private readonly LinearGradientBrush blockBrush = new LinearGradientBrush(Color.FromArgb(90, 0, 150, 255), Color.FromArgb(20, 0, 80, 150), 45.0);
        private readonly LinearGradientBrush lensBrush = new LinearGradientBrush(Color.FromArgb(120, 0, 200, 255), Color.FromArgb(30, 0, 120, 180), 45.0);
        private readonly SolidColorBrush tooltipBgBrush = new SolidColorBrush(Color.FromArgb(220, 30, 30, 35));
        private readonly SolidColorBrush handleBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));

        private readonly SolidColorBrush[] rayColors = {
            Brushes.DeepSkyBlue, Brushes.LimeGreen, Brushes.Orange, Brushes.MediumOrchid,
            Brushes.Cyan, Brushes.Magenta, Brushes.HotPink, Brushes.SpringGreen, Brushes.Gold
        };

        private List<Line> linePool = new List<Line>();
        private int lineIndex = 0;
        private List<Polyline> polylinePool = new List<Polyline>();
        private int polylineIndex = 0;
        private List<TextBlock> textPool = new List<TextBlock>();
        private int textIndex = 0;
        private List<Path> pathPool = new List<Path>();
        private int pathIndex = 0;
        private List<Polygon> polygonPool = new List<Polygon>();
        private int polygonIndex = 0;
        private List<Ellipse> ellipsePool = new List<Ellipse>();
        private int ellipseIndex = 0;
        private List<Grid> nozzlePool = new List<Grid>();
        private int nozzleIndex = 0;
        private List<Grid> emitterUiPool = new List<Grid>();
        private int emitterUiIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
            ApplyTheme();
            surfaces.Add(new BiconvexLens(250, 100, 1.5) { Position = new Point(700, 300) });
            emitters.Add(new RayEmitter(rayCounter++, groupCounter++, 200, 300, WavelengthToBrush(550), 0, 550, 0));
            Loaded += (s, e) =>
            {
                if (ModeLabel != null) ModeLabel.Text = "Modo: MOVER (Tecla 'A')";
                UpdateAndDraw();
            };
            SizeChanged += (s, e) => UpdateAndDraw();
        }

        private T GetPooledElement<T>(List<T> pool, ref int index) where T : UIElement, new()
        {
            if (index >= pool.Count)
            {
                var element = new T();
                pool.Add(element);
                SimCanvas.Children.Add(element);
            }
            var item = pool[index++];
            item.Visibility = Visibility.Visible;
            return item;
        }

        private Grid GetNozzle()
        {
            if (nozzleIndex >= nozzlePool.Count)
            {
                Grid nozzleUI = new Grid { Width = 14, Height = 10 };
                nozzleUI.Children.Add(new Rectangle { Fill = Brushes.DarkSlateGray, Stroke = Brushes.Black, StrokeThickness = 1, RadiusX = 1, RadiusY = 1 });
                nozzlePool.Add(nozzleUI);
                SimCanvas.Children.Add(nozzleUI);
            }
            var item = nozzlePool[nozzleIndex++];
            item.Visibility = Visibility.Visible;
            return item;
        }

        private Grid GetEmitterUI()
        {
            if (emitterUiIndex >= emitterUiPool.Count)
            {
                Grid emitterUI = new Grid { Width = 30, Height = 20 };
                emitterUI.Children.Add(new Rectangle { RadiusX = 3, RadiusY = 3 });
                emitterUI.Children.Add(new TextBlock { Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.Bold });
                emitterUiPool.Add(emitterUI);
                SimCanvas.Children.Add(emitterUI);
            }
            var item = emitterUiPool[emitterUiIndex++];
            item.Visibility = Visibility.Visible;
            return item;
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            ApplyTheme();
            UpdateAndDraw();
        }

        private void ApplyTheme()
        {
            if (isDarkTheme)
            {
                this.Resources["WindowBg"] = new SolidColorBrush(Color.FromRgb(25, 25, 30));
                this.Resources["PanelBg"] = new SolidColorBrush(Color.FromRgb(35, 35, 42));
                this.Resources["TextFg"] = Brushes.WhiteSmoke;
                this.Resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(60, 60, 70));
                this.Resources["CanvasBg"] = new SolidColorBrush(Color.FromRgb(15, 15, 18));
                this.Resources["HeaderBg"] = new SolidColorBrush(Color.FromRgb(45, 45, 52));
            }
            else
            {
                this.Resources["WindowBg"] = new SolidColorBrush(Color.FromRgb(240, 242, 245));
                this.Resources["PanelBg"] = Brushes.White;
                this.Resources["TextFg"] = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                this.Resources["BorderColor"] = new SolidColorBrush(Color.FromRgb(209, 213, 219));
                this.Resources["CanvasBg"] = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                this.Resources["HeaderBg"] = new SolidColorBrush(Color.FromRgb(229, 231, 235));
            }

            double cmPx = 96.0 / 2.54;
            DrawingBrush gridBrush = new DrawingBrush
            {
                Viewport = new Rect(0, 0, cmPx, cmPx),
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile
            };
            GeometryDrawing drawing = new GeometryDrawing
            {
                Pen = new Pen(new SolidColorBrush(isDarkTheme ? Color.FromArgb(20, 255, 255, 255) : Color.FromArgb(15, 0, 0, 0)), 1),
                Geometry = new GeometryGroup { Children = new GeometryCollection { new LineGeometry(new Point(0, 0), new Point(cmPx, 0)), new LineGeometry(new Point(0, 0), new Point(0, cmPx)) } }
            };
            gridBrush.Drawing = drawing;
            if (SimCanvas != null) SimCanvas.Background = gridBrush;
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

            lineIndex = 0; polylineIndex = 0; textIndex = 0; pathIndex = 0; polygonIndex = 0; ellipseIndex = 0; nozzleIndex = 0; emitterUiIndex = 0;
            foreach (UIElement child in SimCanvas.Children) child.Visibility = Visibility.Hidden;

            foreach (var surface in surfaces)
            {
                surface.UpdateDimensions(SimCanvas.ActualWidth, SimCanvas.ActualHeight);
                DrawSurface(surface);
            }

            bool showGuides = ShowGuidesCheck?.IsChecked ?? false;
            var dataExport = new List<object>();

            bool tieneReflexion = false;
            bool tieneRefraccion = false;
            bool tieneRIT = false;

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
                        string tipoEspanol = "Desconocido";
                        if (segment.ActionType == "Reflection") { tipoEspanol = "Reflexión"; tieneReflexion = true; }
                        else if (segment.ActionType == "Refraction") { tipoEspanol = "Refracción"; tieneRefraccion = true; }
                        else if (segment.ActionType == "TIR") { tipoEspanol = "R.I.T."; tieneRIT = true; }

                        dataExport.Add(new { Grupo = ray.GroupId, Espectro = (int)ray.Wavelength + " nm", Impacto = hitOrder++, Fenómeno = tipoEspanol, Ángulo = Math.Round(segment.IncidenceAngleDeg, 1) + "°" });

                        if (showGuides)
                        {
                            Line guide = GetPooledElement(linePool, ref lineIndex);
                            guide.X1 = segment.End.X; guide.Y1 = segment.End.Y; guide.X2 = segment.NormalEnd.X; guide.Y2 = segment.NormalEnd.Y;
                            guide.Stroke = Brushes.DimGray; guide.StrokeThickness = 1; guide.StrokeDashArray = new DoubleCollection { 2, 4 };
                        }

                        TextBlock angleText = GetPooledElement(textPool, ref textIndex);
                        angleText.Text = $"{Math.Round(segment.IncidenceAngleDeg, 1)}°";
                        angleText.Foreground = Brushes.White; angleText.Background = tooltipBgBrush; angleText.Padding = new Thickness(3); angleText.FontSize = 11; angleText.FontWeight = FontWeights.Bold;
                        Canvas.SetLeft(angleText, segment.End.X + 10); Canvas.SetTop(angleText, segment.End.Y - 20); Canvas.SetZIndex(angleText, 100);
                    }
                }

                Polyline glow = GetPooledElement(polylinePool, ref polylineIndex);
                glow.Points = pathPoints; glow.Stroke = ray.RayColor; glow.StrokeThickness = 6; glow.Opacity = 0.2;

                Polyline core = GetPooledElement(polylinePool, ref polylineIndex);
                core.Points = pathPoints; core.Stroke = ray.RayColor; core.StrokeThickness = 2; core.Opacity = 1.0;

                Grid nozzleUI = GetNozzle();
                nozzleUI.RenderTransformOrigin = new Point(0.5, 0.5); nozzleUI.RenderTransform = new RotateTransform(ray.Angle * 180.0 / Math.PI);
                Canvas.SetLeft(nozzleUI, ray.Position.X - 7); Canvas.SetTop(nozzleUI, ray.Position.Y - 5); Canvas.SetZIndex(nozzleUI, 99);

                var groupRays = emitters.FindAll(r => r.GroupId == ray.GroupId);
                if (groupRays.Count > 1 && ray.Id == groupRays[0].Id && ray.DispersionModifier == 0.0)
                {
                    bool isLaserGroup = false;
                    foreach (var gr in groupRays) { if (gr.Position != ray.Position) { isLaserGroup = true; break; } }
                    if (isLaserGroup)
                    {
                        Line housingBack = GetPooledElement(linePool, ref lineIndex);
                        housingBack.X1 = groupRays[0].Position.X; housingBack.Y1 = groupRays[0].Position.Y; housingBack.X2 = groupRays[groupRays.Count - 1].Position.X; housingBack.Y2 = groupRays[groupRays.Count - 1].Position.Y;
                        housingBack.Stroke = Brushes.DimGray; housingBack.StrokeThickness = 12; housingBack.StrokeStartLineCap = PenLineCap.Round; housingBack.StrokeEndLineCap = PenLineCap.Round; housingBack.StrokeDashArray = null;
                        Canvas.SetZIndex(housingBack, 98);
                    }
                }

                if (ray.Id == groupRays[0].Id)
                {
                    Grid emitterUI = GetEmitterUI();
                    emitterUI.RenderTransformOrigin = new Point(0.5, 0.5); emitterUI.RenderTransform = new RotateTransform(ray.Angle * 180.0 / Math.PI);
                    ((Rectangle)emitterUI.Children[0]).Fill = isMovingMode ? Brushes.DodgerBlue : Brushes.Crimson;
                    ((TextBlock)emitterUI.Children[1]).Text = ray.GroupId.ToString();
                    Canvas.SetLeft(emitterUI, ray.Position.X - 15); Canvas.SetTop(emitterUI, ray.Position.Y - 10); Canvas.SetZIndex(emitterUI, 101);
                }
            }

            PhysicsDataTable.ItemsSource = dataExport;
            UpdateFormulasPanel(tieneReflexion, tieneRefraccion, tieneRIT);

            if (measureStart != null)
            {
                Point p1 = measureStart.Value;
                Point p2 = (Mouse.LeftButton == MouseButtonState.Pressed && isMeasuring) ? mousePos : (measureEnd ?? mousePos);

                Line mainRuler = GetPooledElement(linePool, ref lineIndex);
                mainRuler.X1 = p1.X; mainRuler.Y1 = p1.Y; mainRuler.X2 = p2.X; mainRuler.Y2 = p2.Y;
                mainRuler.Stroke = Brushes.DeepSkyBlue; mainRuler.StrokeThickness = 2; mainRuler.StrokeDashArray = null;

                double dx = p2.X - p1.X;
                double dy = p2.Y - p1.Y;
                double distPx = Math.Sqrt(dx * dx + dy * dy);
                double distCm = distPx * (2.54 / 96.0);

                if (MeasureLabel != null)
                    MeasureLabel.Text = $"Regla: {Math.Round(distCm, 1)} cm";

                TextBlock floatingTooltip = GetPooledElement(textPool, ref textIndex);
                floatingTooltip.Text = $"{Math.Round(distCm, 1)} cm"; floatingTooltip.Foreground = Brushes.White; floatingTooltip.Background = Brushes.DeepSkyBlue; floatingTooltip.Padding = new Thickness(4, 2, 4, 2); floatingTooltip.FontSize = 11; floatingTooltip.FontWeight = FontWeights.Bold;
                Canvas.SetLeft(floatingTooltip, p2.X + 12); Canvas.SetTop(floatingTooltip, p2.Y - 22); Canvas.SetZIndex(floatingTooltip, 120);

                if (distPx > 5)
                {
                    double ux = dx / distPx; double uy = dy / distPx;
                    double nx = -uy; double ny = ux;
                    double pxPerCm = 96.0 / 2.54;

                    for (double cm = 0; cm <= distCm; cm += 0.5)
                    {
                        double offsetPx = cm * pxPerCm;
                        if (offsetPx > distPx) break;

                        double tx = p1.X + ux * offsetPx; double ty = p1.Y + uy * offsetPx;
                        bool esEnteroCm = Math.Abs(cm - Math.Round(cm)) < 0.01;
                        double tickLen = esEnteroCm ? 12 : 6;

                        Line tick = GetPooledElement(linePool, ref lineIndex);
                        tick.X1 = tx; tick.Y1 = ty; tick.X2 = tx + nx * tickLen; tick.Y2 = ty + ny * tickLen; tick.Stroke = Brushes.DeepSkyBlue; tick.StrokeThickness = esEnteroCm ? 1.5 : 1; tick.StrokeDashArray = null;

                        if (esEnteroCm && cm > 0 && offsetPx < distPx - 15)
                        {
                            TextBlock tickText = GetPooledElement(textPool, ref textIndex);
                            tickText.Text = $"{Math.Round(cm)} cm"; tickText.Foreground = Brushes.DeepSkyBlue; tickText.Background = Brushes.Transparent; tickText.Padding = new Thickness(0); tickText.FontSize = 9; tickText.FontWeight = FontWeights.Bold;
                            Canvas.SetLeft(tickText, tx + nx * 16 - 8); Canvas.SetTop(tickText, ty + ny * 16 - 5); Canvas.SetZIndex(tickText, 100);
                        }
                    }
                }
            }

            if ((activeEmitter != null || activeSurface != null) && !isMovingMode && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point startPoint = activeEmitter != null ? activeEmitter.Position : activeSurface.Position;
                Line aimLine = GetPooledElement(linePool, ref lineIndex);
                aimLine.X1 = startPoint.X; aimLine.Y1 = startPoint.Y; aimLine.X2 = mousePos.X; aimLine.Y2 = mousePos.Y;
                aimLine.Stroke = Brushes.LightGray; aimLine.StrokeThickness = 1; aimLine.StrokeDashArray = new DoubleCollection { 3, 3 };
            }
        }

        private void UpdateFormulasPanel(bool reflek, bool refrak, bool rit)
        {
            if (FormulasTextBlock == null) return;
            string txt = "SISTEMA EN EQUILIBRIO\n\n";
            if (reflek) txt += "• Ley de Reflexión:\n  θi = θr\n  El ángulo incidente equivale al reflejado.\n\n";
            if (refrak) txt += "• Ley de Snell (Refracción):\n  n1 · sin(θi) = n2 · sin(θt)\n  Desviación por cambio de medio.\n\n";
            if (rit) txt += "• Reflexión Interna Total (R.I.T):\n  θi ≥ θc = arcsin(n2 / n1)\n  Luz confinada en medio denso.\n\n";
            if (!reflek && !refrak && !rit) txt += "Dispare haces luminosos hacia los componentes para desplegar ecuaciones analíticas aplicadas.";

            if (FormulasTextBlock.Text != txt) FormulasTextBlock.Text = txt;
        }

        private void DrawSurface(IOpticSurface surface)
        {
            Brush strokeBrush = isDarkTheme ? Brushes.LightGray : Brushes.DarkBlue;
            Brush mirrorLineBrush = isDarkTheme ? Brushes.WhiteSmoke : Brushes.Black;

            if (surface is BiconvexLens lens)
            {
                Path path = GetPooledElement(pathPool, ref pathIndex);
                path.Data = new CombinedGeometry(GeometryCombineMode.Intersect, new EllipseGeometry(lens.C1, lens.Radius, lens.Radius), new EllipseGeometry(lens.C2, lens.Radius, lens.Radius));
                path.Fill = lensBrush; path.Stroke = strokeBrush; path.StrokeThickness = 1;
            }
            else if (surface is RefractionBlock block)
            {
                Polygon rect = GetPooledElement(polygonPool, ref polygonIndex);
                rect.Points.Clear(); foreach (var p in block.Corners) rect.Points.Add(p);
                rect.Fill = blockBrush; rect.Stroke = strokeBrush; rect.StrokeThickness = 1;
            }
            else if (surface is CurvedMirror curved)
            {
                Path path = GetPooledElement(pathPool, ref pathIndex);
                PathGeometry pg = new PathGeometry(); PathFigure pf = new PathFigure();
                double expectedAngle = curved.Angle + Math.PI;
                pf.StartPoint = new Point(curved.Center.X + curved.Radius * Math.Cos(expectedAngle - curved.MaxAngle), curved.Center.Y + curved.Radius * Math.Sin(expectedAngle - curved.MaxAngle));
                pf.Segments.Add(new ArcSegment { Point = new Point(curved.Center.X + curved.Radius * Math.Cos(expectedAngle + curved.MaxAngle), curved.Center.Y + curved.Radius * Math.Sin(expectedAngle + curved.MaxAngle)), Size = new Size(curved.Radius, curved.Radius), SweepDirection = SweepDirection.Clockwise });
                pg.Figures.Add(pf); path.Data = pg; path.Stroke = mirrorLineBrush; path.StrokeThickness = 4; path.Fill = null;
            }
            else if (surface is StraightMirror straight)
            {
                Line line = GetPooledElement(linePool, ref lineIndex);
                line.X1 = straight.StartPoint.X; line.Y1 = straight.StartPoint.Y; line.X2 = straight.EndPoint.X; line.Y2 = straight.EndPoint.Y;
                line.Stroke = mirrorLineBrush; line.StrokeThickness = 5; line.StrokeDashArray = null;
            }

            Ellipse dragHandle = GetPooledElement(ellipsePool, ref ellipseIndex);
            dragHandle.Width = 10; dragHandle.Height = 10; dragHandle.Fill = handleBrush; dragHandle.Cursor = Cursors.SizeAll;
            Canvas.SetLeft(dragHandle, surface.Position.X - 5); Canvas.SetTop(dragHandle, surface.Position.Y - 5); Canvas.SetZIndex(dragHandle, 10);
        }

        private void AddSurface_Click(object sender, RoutedEventArgs e)
        {
            if (SurfaceSelector == null) return;
            double radius = double.TryParse(RadiusInput.Text, out double r) ? r : 600;
            double length = double.TryParse(LengthInput.Text, out double l) ? l : 240;
            double thickness = double.TryParse(ThicknessInput.Text, out double th) ? th : 100;
            double index = double.TryParse(IndexInput.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double n) ? n : 1.5;

            string sel = ((ComboBoxItem)SurfaceSelector.SelectedItem).Content.ToString();
            if (sel == "Espejo Curvo") surfaces.Add(new CurvedMirror(radius, length) { Position = new Point(400, 250) });
            else if (sel == "Espejo Recto") surfaces.Add(new StraightMirror(length) { Position = new Point(400, 250) });
            else if (sel == "Bloque de Refracción") surfaces.Add(new RefractionBlock(length, thickness, index) { Position = new Point(400, 250) });
            else if (sel == "Lente Biconvexa") surfaces.Add(new BiconvexLens(radius, thickness, index) { Position = new Point(400, 250) });
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
            MeasureLabel.Text = isMeasuring ? "Regla: Clic y Arrastre" : "";
            UpdateAndDraw();
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point currentClick = e.GetPosition(SimCanvas);
            if (isMeasuring)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    measureStart = currentClick;
                    measureEnd = null;
                    UpdateAndDraw();
                }
                return;
            }
            mousePos = currentClick;
            measureStart = null;
            measureEnd = null;

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
                    bool isLamp = false;

                    if (groupRays.Count > 1)
                    {
                        bool samePosition = true;
                        for (int i = 1; i < groupRays.Count; i++)
                        {
                            if (groupRays[i].Position != groupRays[0].Position) { samePosition = false; break; }
                        }
                        if (samePosition)
                        {
                            for (int i = 1; i < groupRays.Count; i++)
                            {
                                if (Math.Abs(groupRays[i].Angle - groupRays[0].Angle) > 0.001) { isLamp = true; break; }
                            }
                        }
                    }

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
            if (isMeasuring && measureStart != null && e.ChangedButton == MouseButton.Left)
            {
                measureEnd = e.GetPosition(SimCanvas);
                double dx = measureEnd.Value.X - measureStart.Value.X;
                double dy = measureEnd.Value.Y - measureStart.Value.Y;
                double distancePx = Math.Sqrt(dx * dx + dy * dy);
                double distanceCm = distancePx * (2.54 / 96.0);
                MeasureLabel.Text = $"Dist: {Math.Round(distanceCm, 1)} cm";
                isMeasuring = false;
                UpdateAndDraw();
                return;
            }
            activeEmitter = null; activeSurface = null; UpdateAndDraw();
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        private void ShowGuides_Click(object sender, RoutedEventArgs e) => UpdateAndDraw();
        private void WavelengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { if (WavelengthLabel != null) WavelengthLabel.Text = $"{(int)WavelengthSlider.Value} nm"; }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                isMovingMode = !isMovingMode;
                if (ModeLabel != null)
                {
                    ModeLabel.Text = isMovingMode ? "Modo: MOVER (Tecla 'A')" : "Modo: APUNTAR (Tecla 'A')";
                    ModeLabel.Foreground = isMovingMode ? Brushes.DodgerBlue : Brushes.Crimson;
                }
                UpdateAndDraw();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "JSON Files (*.json)|*.json", FileName = "escena.json" };
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
                catch (Exception ex) { MessageBox.Show("Error al cargar: " + ex.Message); }
            }
        }
    }
}