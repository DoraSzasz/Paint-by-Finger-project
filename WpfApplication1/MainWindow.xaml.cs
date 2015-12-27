using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Speech;
using Microsoft.Research.Kinect.Nui;
using nui = Microsoft.Research.Kinect.Nui;

using WpfApplication1.Skeleton;
using WpfApplication1.VolumetricPad;
using WpfApplication1.Speech;
using io = System.IO;
using System.Threading;
using System.Windows.Threading;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected Skeleton.SkeletonController skelController;
        protected VolumetricPadController volumeController;
        protected SpeechController speechController;


        protected GUIState State = GUIState.Canvas;

        protected Brush CurrentBrush = Brushes.Blue;
        protected float BrushThickness = 1;

        public bool IsDrawing
        {
            get;
            set;
        }


        private bool _Drawing = false; // used for device simulation

        private BrushNamePair[] _colorBrushes;
        public BrushNamePair[] colorBrushes
        {
            get
            {
                if (_colorBrushes == null)
                {
                    ResourceDictionary rdBrushes = (Application.Current.Resources["ColorBrushes"] as ResourceDictionary);
                    List<BrushNamePair> result = new List<BrushNamePair>();
                    foreach (string key in rdBrushes.Keys)
                    {
                        Brush b = rdBrushes[key] as Brush;
                        if (b != null)
                        {
                            result.Add(new BrushNamePair(key, b));
                        }
                    }
                    _colorBrushes = result.ToArray();
                }
                return _colorBrushes;
            }
        }

        private BrushNamePair[] _textureBrushes;
        public BrushNamePair[] textureBrushes
        {
            get
            {
                if (_textureBrushes == null)
                {
                    ResourceDictionary rdBrushes = (Application.Current.Resources["TexturedBrushes"] as ResourceDictionary);
                    List<BrushNamePair> result = new List<BrushNamePair>();
                    foreach (string key in rdBrushes.Keys)
                    {
                        Brush b = rdBrushes[key] as Brush;
                        if (b != null)
                        {
                            result.Add(new BrushNamePair(key, b));
                        }
                    }
                    _textureBrushes = result.ToArray();
                }
                return _textureBrushes;
            }
        }

        Point lastDrawingPoint;
        List<List<Path>> drawnPaths = new List<List<Path>>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowStyle = System.Windows.WindowStyle.None;
            WindowState = WindowState.Maximized;
            ccChooser.DataContext = colorBrushes;
            ccTexChooser.DataContext = textureBrushes;
            NewFigure();


            SpeechAction saMenu = new SpeechAction("menu");
            saMenu.Triggered += new EventHandler(saMenu_Triggered);
            SpeechAction saPalette = new SpeechAction("color");
            saPalette.Triggered += new EventHandler(saPalette_Triggered);
            saPalette.Children = new List<SpeechAction>();
            foreach (BrushNamePair bnp in colorBrushes)
            {
                SpeechAction saColor = new SpeechAction(bnp.Name);
                saColor.Triggered += new EventHandler(saColor_Triggered);
                saPalette.Children.Add(saColor);
            }

            SpeechAction saTextures = new SpeechAction("texture");
            saTextures.Triggered += new EventHandler(saTextures_Triggered);
            saTextures.Children = new List<SpeechAction>();
            foreach (BrushNamePair bnp in textureBrushes)
            {
                SpeechAction saTexture = new SpeechAction(bnp.Name);
                saTexture.Triggered += new EventHandler(saTexture_Triggered);
                saTextures.Children.Add(saTexture);
            }

            SpeechAction saUndo = new SpeechAction("undo");
            saUndo.Triggered += new EventHandler(saUndo_Triggered);

            SpeechAction saDebug = new SpeechAction("debug");
            saDebug.Triggered += new EventHandler(saDebug_Triggered);

            SpeechAction saCanvas = new SpeechAction("canvas");
            saCanvas.Triggered += new EventHandler(saCanvas_Triggered);

            SpeechAction saClear = new SpeechAction("clear");
            saClear.Triggered += new EventHandler(saClear_Triggered);

            SpeechAction saCalibrate = new SpeechAction("calibrate");
            saCalibrate.Triggered += new EventHandler(saCalibrate_Triggered);

            SpeechAction saDone = new SpeechAction("done");
            saDone.Triggered += new EventHandler(saDone_Triggered);

            SpeechAction saSave = new SpeechAction("save");
            saSave.Triggered += new EventHandler(saSave_Triggered);

            SpeechAction saHelp = new SpeechAction("help");
            saHelp.Triggered += new EventHandler(saHelp_Triggered);


            IList<SpeechAction> speechActions = new SpeechAction[] {
                saMenu, saPalette, saTextures, saUndo, saDebug, saCanvas, saClear, saCalibrate, saDone, saSave, saHelp
            };

            speechController = new SpeechController();
            speechController.Initialize(NUIManager.Instance.Runtime);
            System.Threading.Thread.Sleep(1500);
            speechController.LoadSpeechActions(speechActions);

            try
            {
                NUIManager.Instance.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\nReason: " + ex.InnerException.Message);
                //Environment.Exit(1);
                return;
            }

            NUIManager.Instance.Runtime.DepthFrameReady += new EventHandler<Microsoft.Research.Kinect.Nui.ImageFrameReadyEventArgs>(Runtime_DepthFrameReady);

            skelController = new SkeletonController();
            skelController.Initialize(NUIManager.Instance.Runtime);

            skelController.DistanceEvent += new EventHandler<DistanceEventArgs>(skelController_DistanceEvent);
            skelController.WristDetected += new EventHandler<VectorEventArgs>(skelController_WristDetected);
            skelController.GestureDetected += new EventHandler<SkeletonGestureEventArgs>(skelController_GestureDetected);
            skelController.SkeletonDetected += new EventHandler<SkeletonGestureEventArgs>(skelController_SkeletonDetected);

            volumeController = new VolumetricPadController();
            volumeController.Initialize(NUIManager.Instance.Runtime);
            volumeController.FingerDetected += new EventHandler<VectorEventArgs>(volumeController_FingerDetected);
            volumeController.Click += new EventHandler<VectorEventArgs>(volumeController_Click);

        }

        void saHelp_Triggered(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(
                    delegate
                    {
                        ToggleHelp();
                    }));
        }

        #region Speech Actions
        void saSave_Triggered(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(
                    delegate
                    {
                        Save();
                    }));
        }

        void saDone_Triggered(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(
                    delegate
                    {
                        if (volumeController.IsCalibrating)
                        {
                            StopCalibration();
                        }
                    }));
        }

        void saCalibrate_Triggered(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(
                    delegate
                    {
                        Calibrate();
                    }));
        }

        void saClear_Triggered(object sender, EventArgs e)
        {
            if (!IsDrawing)
                Dispatcher.Invoke(new Action(
                        delegate
                        {
                            gdCanvas.Children.Clear();
                            IsDrawing = false;
                        }));
        }

        void saCanvas_Triggered(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(
                    delegate
                    {
                        SetState(GUIState.Canvas);
                    }));
        }

        void saDebug_Triggered(object sender, EventArgs e)
        {
            ToggleDebug();
        }

        void saUndo_Triggered(object sender, EventArgs e)
        {
            if (!IsDrawing)
                Dispatcher.Invoke(new Action(
                        delegate
                        {
                            Undo();
                        }));
        }

        void saTexture_Triggered(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(
                    delegate
                    {
                        SpeechAction sa = sender as SpeechAction;
                        SetBrush(textureBrushes.First(b => b.Name == sa.Name).Brush);
                        SetState(GUIState.Canvas);
                        ShowMessage(sa.Name, true);
                    }));
        }

        void saTextures_Triggered(object sender, EventArgs e)
        {
            ToggleTexturePalette();
        }

        void saColor_Triggered(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(
                    delegate
                    {
                        SpeechAction sa = sender as SpeechAction;
                        SetBrush(colorBrushes.First(b => b.Name == sa.Name).Brush);
                        SetState(GUIState.Canvas);
                        ShowMessage(sa.Name, true);
                    }));
        }

        #endregion

        #region NUI Controllers events

        void saPalette_Triggered(object sender, EventArgs e)
        {
            TogglePalette();
        }

        void saMenu_Triggered(object sender, EventArgs e)
        {
            ToggleMenu();
        }

        void skelController_SkeletonDetected(object sender, SkeletonGestureEventArgs e)
        {
            Point p;

            p = ConvertJointToPoint(e.Skeleton.Joints[JointID.Head]);
            eHead.Margin = new Thickness() { Left = p.X - 5, Top = p.Y - 5 };

            p = ConvertJointToPoint(e.Skeleton.Joints[JointID.WristRight]);
            eWristRight.Margin = new Thickness() { Left = p.X - 5, Top = p.Y - 5 };

            p = ConvertJointToPoint(e.Skeleton.Joints[JointID.WristLeft]);
            eWristLeft.Margin = new Thickness() { Left = p.X - 5, Top = p.Y - 5 };

            p = ConvertJointToPoint(e.Skeleton.Joints[JointID.AnkleRight]);
            eAnkleRight.Margin = new Thickness() { Left = p.X - 5, Top = p.Y - 5 };

            p = ConvertJointToPoint(e.Skeleton.Joints[JointID.AnkleLeft]);
            eAnkleLeft.Margin = new Thickness() { Left = p.X - 5, Top = p.Y - 5 };
        }

        void skelController_GestureDetected(object sender, SkeletonGestureEventArgs e)
        {
            lbDebug.Content = e.Gesture.ToString();

            if (e.Gesture == SkeletonGesture.HeadScratch)
            {
                ToggleHelp();
            }

            if (e.Gesture == SkeletonGesture.Jesus)
            {
                Save();
            }
            if (e.Gesture == SkeletonGesture.AnkleGrab)
            {
                Undo();
            }
        }

        void skelController_WristDetected(object sender, VectorEventArgs e)
        {
            volumeController.SetWristPosition(e.Position);
            //nui.Vector pos = volumeController.ScaleCoordinates(e.Position, new Size(this.Width, this.Height));
            //eFinger.Margin = new Thickness((int)pos.X, (int)pos.Y, 0, 0);
        }

        void volumeController_Click(object sender, VectorEventArgs e)
        {
            if (!volumeController.IsCalibrated)
            {
                return;
            }
            nui.Vector pos = volumeController.ScaleCoordinates(e.Position, new Size(this.Width, this.Height));
            if (!IsDrawing)
            {
                NewFigure();
                StartDrawing(new Point(pos.X, pos.Y));
            }
            else
            {
                AddPoint(new Point(pos.X, pos.Y), pos.Z / 5);
            }
        }

        void volumeController_FingerDetected(object sender, VectorEventArgs e)
        {
            nui.Vector pos = volumeController.ScaleCoordinates(e.Position, new Size(this.Width, this.Height));

            double size = eFinger.Width = eFinger.Height = (int)Math.Max(pos.Z / 5, 10);
            eFinger.Margin = new Thickness(pos.X - size / 2, pos.Y - size / 2, 0, 0);
            if (pos.Z < 10)
                eFinger.Opacity = 0.5;
            else
                eFinger.Opacity = 0.25;
            if ((pos.Z < 0) && IsDrawing)
            {
                StopDrawing();
                NewFigure();
            }
        }

        void skelController_DistanceEvent(object sender, DistanceEventArgs e)
        {
            //lbTest.Content = e.Distance.ToString("F4");
        }

        void Runtime_DepthFrameReady(object sender, Microsoft.Research.Kinect.Nui.ImageFrameReadyEventArgs e)
        {
            PlanarImage Image = e.ImageFrame.Image;
            byte[] depth = new byte[Image.Width * Image.Height];
            for (int idx = 0; idx < Image.Width * Image.Height; idx++)
            {
                //depth[idx] = Image.Bits[2 * idx + 1];// (Image.Bits[2*idx + 1] << 5) | (Image.Bits[2*idx] >> 3); //8192
                depth[idx] = (byte)((Image.Bits[2 * idx + 1] << 5 | Image.Bits[2 * idx] >> 3) / (5192 / 255.0));

            }

            video.Source = BitmapSource.Create(
               Image.Width, Image.Height, 96, 96, PixelFormats.Gray8, null, depth, Image.Width);

        }

        #endregion

        protected void Calibrate()
        {
            volumeController.Calibrate();
            ShowMessage("Calibrating...", false);
        }

        protected void StopCalibration()
        {
            volumeController.StopCalibration();
            ShowMessage("Done!", true);
        }




        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            NUIManager.Instance.Uninitialize();
            if (volumeController != null)
            {
                volumeController.Destroy();
            }
            if (skelController != null)
            {
                skelController.Destroy();
            }
            if (speechController != null)
            {
                speechController.Destroy();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            if (e.Key == Key.A)
            {
                ToggleMenu();
            }
            if (e.Key == Key.Enter)
            {
                if (!_Drawing)
                {
                    StartDrawing(Mouse.GetPosition(gdCanvas));
                    _Drawing = true;
                }
                else
                {
                    StopDrawing();
                    _Drawing = false;
                }
            }

            if (e.Key == Key.C)
            {
                if (!volumeController.IsCalibrating)
                {
                    Calibrate();
                }
                else
                {
                    StopCalibration();
                }
            }

            if (e.Key == Key.Space)
            {
                TogglePalette();
            }

            if (e.Key == Key.Q)
            {
                volumeController.SaveFrame();
            }

            if (e.Key == Key.S)
            {
                skelController.SaveFrame();
            }

            if (e.Key == Key.T)
            {
                ToggleTexturePalette();
            }
            if (e.Key == Key.D)
            {
                ToggleDebug();
            }
            if (e.Key == Key.U)
            {
                Undo();
            }
            if (e.Key == Key.V)
            {
                Save();
            }
            if (e.Key == Key.H)
            {
                ToggleHelp();
            }
        }

        private void gdCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

            Random r = new Random(DateTime.Now.Millisecond);
            AddPoint(e.MouseDevice.GetPosition(gdCanvas), (int)(r.NextDouble() * 80));
        }

        #region GUI State Methods
        protected void ToggleMenu()
        {
            if (State == GUIState.Canvas)
            {
                Dispatcher.Invoke(new Action(
                    delegate
                    {
                        SetState(GUIState.Menu);
                    }));
                return;
            }
            if (State == GUIState.Menu)
            {
                Dispatcher.Invoke(new Action(
                    delegate
                    {
                        SetState(GUIState.Canvas);
                    }));
                return;
            }
        }

        protected void TogglePalette()
        {
            if (State == GUIState.ColorPalette)
            {
                Dispatcher.Invoke(new Action(
                    delegate
                    {
                        SetBrush(ccChooser.SelectedBrush);
                        SetState(GUIState.Canvas);
                    }));
            }
            else
            {
                Dispatcher.Invoke(new Action(
                    delegate
                    {
                        SetState(GUIState.ColorPalette);
                    }));
            }
        }

        protected void ToggleTexturePalette()
        {
            if (State == GUIState.TexturePalette)
            {
                Dispatcher.Invoke(new Action(
                   delegate
                   {
                       SetBrush(ccTexChooser.SelectedBrush);
                       SetState(GUIState.Canvas);
                   }));
            }
            else
            {
                Dispatcher.Invoke(new Action(
                   delegate
                   {
                       SetState(GUIState.TexturePalette);
                   }));

            }
        }

        protected void ToggleDebug()
        {
            if (State == GUIState.Debug)
            {
                Dispatcher.Invoke(new Action(
                  delegate
                  {
                      SetState(GUIState.Canvas);
                  }));

            }
            else
            {
                Dispatcher.Invoke(new Action(
                  delegate
                  {
                      SetState(GUIState.Canvas);
                  }));
                SetState(GUIState.Debug);
            }
        }

        protected void ToggleHelp()
        {
            if (State != GUIState.Help)
            {
                SetState(GUIState.Help);
            }
            else
            {
                SetState(GUIState.Canvas);
            }
        }

        protected void SetState(GUIState state)
        {
            gdCanvas.Visibility = Visibility.Collapsed;
            gdMenu.Visibility = Visibility.Collapsed;
            gdColorPalette.Visibility = Visibility.Collapsed;
            gdTexturePalette.Visibility = Visibility.Collapsed;
            gdDebug.Visibility = Visibility.Collapsed;
            gdHelp.Visibility = Visibility.Collapsed;

            switch (state)
            {
                case GUIState.Menu:
                    gdMenu.Visibility = Visibility.Visible;
                    break;
                case GUIState.Canvas:
                    gdCanvas.Visibility = Visibility.Visible;
                    break;
                case GUIState.ColorPalette:
                    gdColorPalette.Visibility = Visibility.Visible;
                    break;
                case GUIState.TexturePalette:
                    gdTexturePalette.Visibility = Visibility.Visible;
                    break;
                case GUIState.Debug:
                    gdDebug.Visibility = Visibility.Visible;
                    break;
                case GUIState.Help:
                    gdHelp.Visibility = Visibility.Visible;
                    break;
            }
            State = state;
        }

        #endregion

        #region Drawing Methods

        Shape CurrentShape = null;

        public void StartDrawing(Point point)
        {
            IsDrawing = true;
            Path path = new Path();
            path.Stroke = CurrentBrush;
            PathGeometry pg = new PathGeometry();
            pg.Figures = new PathFigureCollection();
            pg.Figures.Add(
                new PathFigure(point, new PathSegment[] {
                    new LineSegment(point, true) { IsSmoothJoin = true }
                    },
                true)
            );
            path.Data = pg;
            CurrentShape = path;

            Grid newGrid = gdCanvas.Children[gdCanvas.Children.Count - 1] as Grid;
            newGrid.Children.Add(path);
        }

        public void NewFigure()
        {
            Grid newGrid = new Grid();
            Path p = new Path();
            newGrid.Children.Add(p);
            gdCanvas.Children.Add(newGrid);
        }

        public void Clear()
        {
            gdCanvas.Children.Clear();
            IsDrawing = false;
            NewFigure();
        }

        public void StopDrawing()
        {
            IsDrawing = false;
            CurrentShape = null;
        }

        public void AddPoint(Point point)
        {
            if (CurrentShape == null)
            {
                return;
            }
            Grid lastGrid = gdCanvas.Children[gdCanvas.Children.Count - 1] as Grid;
            Path p = lastGrid.Children[lastGrid.Children.Count - 1] as Path;
            PathGeometry pg = p.Data as PathGeometry;
            PathFigure pf = pg.Figures[0] as PathFigure;
            pf.Segments.Add(new LineSegment(point, true) { IsSmoothJoin = true });

            StopDrawing();
            StartDrawing(point);
            lastDrawingPoint = point;
        }

        public void AddPoint(Point point, float thickness)
        {
            SetThickness(thickness);
            AddPoint(point);
        }

        public void SetThickness(float thick)
        {
            if (CurrentShape == null)
            {
                return;
            }
            Path p = CurrentShape as Path;
            p.StrokeThickness = thick;
            BrushThickness = thick;
        }

        public void SetBrush(Brush brush)
        {
            CurrentBrush = brush;
            if (CurrentShape == null)
            {
                return;
            }
            Path p = CurrentShape as Path;
            p.Stroke = brush;
        }

        public void Undo()
        {
            if (gdCanvas.Children.Count > 1)
            {
                
                gdCanvas.Children.RemoveAt(gdCanvas.Children.Count - 1);
                gdCanvas.Children.RemoveAt(gdCanvas.Children.Count - 1);
                
                NewFigure();
            }
            ShowMessage("Undo", true);
        }

        public void Save()
        {
            int fileID = 0;
            string fileName;
            do
            {
                fileName = "drawing" + fileID + ".png";
                fileID++;
            }
            while (io.File.Exists(fileName));
            HelperClasses.NativeMethods.Screenshot(fileName);
            ShowMessage("Image saved (" + fileName + ")", true);
        }

        #endregion

        public Point ConvertJointToPoint(Joint j)
        {
            if (j.TrackingState != JointTrackingState.Tracked)
            {
                return new Point(0, 0);
            }
            float outX, outY;
            NUIManager.Instance.Runtime.SkeletonEngine.SkeletonToDepthImage(j.Position, out outX, out outY);
            outX = Math.Max(0, Math.Min(outX * 320, 320));  //convert to 320, 240 space
            outY = Math.Max(0, Math.Min(outY * 240, 240));  //convert to 320, 240 space
            return new Point(outX, outY);
        }

        protected void ShowMessage(String Message, bool Wait)
        {
            lbMessage.Content = Message;

            if (Wait)
            {
                Thread th = new Thread((ThreadStart)
                    delegate
                    {
                        Thread.Sleep(2000);
                        Dispatcher.Invoke(new Action(
                            delegate
                            {
                                lbMessage.Content = "";
                            }));
                    });
                th.Start();
            }
        }
    }

    public enum GUIState
    {
        Canvas, Menu, ColorPalette, TexturePalette, Debug, Help
    }
}
