using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Kinect.Nui;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using HelperClasses;
using WpfApplication1;
using win = System.Windows;


namespace WpfApplication1.VolumetricPad
{
    public class VolumetricPadController : INUIController
    {

        public event EventHandler<VectorEventArgs> FingerDetected;
        public event EventHandler<VectorEventArgs> Click;

        public static int DepthQueue = 15; //15
        public static int PositionQueue = 15; //30

        public Vector WristPosition;
        protected bool _IsWristPositionAvailable;


        Runtime Runtime;
        PlanarImage lastFrame;

        public bool IsPaused
        {
            get;
            set;
        }

        protected bool _IsCalibrating;
        public bool IsCalibrating
        {
            get { return _IsCalibrating; }
        }

        List<Vector> _CalibrationData;
        win.Rect CanvasRect;
        double CanvasDepth;

        Queue<float> _LastDepths;
        Queue<Vector> _LastFingerPositions;

        protected bool _IsCalibrated;
        public bool IsCalibrated
        {
            get { return _IsCalibrated; }
        }


        #region INUIController

        public void Initialize(Runtime r)
        {
            Runtime = r;
            Runtime.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(Runtime_DepthFrameReady);
            CanvasRect = new win.Rect(new win.Size(320, 240));
            CanvasDepth = 1000;
            _LastDepths = new Queue<float>(DepthQueue);
            _LastFingerPositions = new Queue<Vector>(PositionQueue);
            _IsCalibrated = false;
        }

        void Runtime_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            if (IsPaused)
                return;
            lastFrame = e.ImageFrame.Image;

            Vector pos = GetFingerPosition(e.ImageFrame.Image, (int)Math.Max(CanvasDepth - 500, 300), (int)CanvasDepth + 500);

            _LastFingerPositions.Enqueue(pos);
            if (_LastFingerPositions.Count > PositionQueue)
            {
                _LastFingerPositions.Dequeue();
            }
            pos.X = _LastFingerPositions.Average(v => v.X);
            pos.Y = _LastFingerPositions.Average(v => v.Y);


            _LastDepths.Enqueue(pos.Z);
            if (_LastDepths.Count > DepthQueue)
            {
                _LastDepths.Dequeue();
            }
            pos.Z = _LastDepths.Average();

            // TODO test if it is in the drawing canvas 
            if (CanvasDepth - pos.Z > 0)
            {
                if (Click != null)
                {
                    Click(this, new VectorEventArgs(pos));
                }
            }

            // launch event
            if (FingerDetected != null)
            {
                FingerDetected(this, new VectorEventArgs(pos));
            }
            // see if we need to use the data for calibration
            if (IsCalibrating)
            {
                _CalibrationData.Add(pos);
            }
        }

        public void Destroy()
        {
            Runtime = null;
        }

        #endregion

        public void SaveFrame()
        {
            SaveFrame(lastFrame);
        }

        private void SaveFrame(PlanarImage planarImage)
        {
            int fileID = 0;
            string fileName;
            do
            {
                fileName = "DepthFrame" + fileID + ".raw";
                fileID++;
            }
            while (File.Exists(fileName));

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                BinaryFormatter fmt = new BinaryFormatter();
                PlanarImagePOCO pco = new PlanarImagePOCO()
                {
                    Width = planarImage.Width,
                    Height = planarImage.Height,
                    Bits = planarImage.Bits
                };
                fmt.Serialize(fs, pco);
                fs.Flush();
                fs.Close();
            }

        }

        private Vector GetFingerPosition(PlanarImage pco, int thresholdMin, int thresholdMax)
        {
            int[] distance = new int[pco.Height * pco.Width];
            int min = thresholdMax + 1;
            int minX = pco.Width;
            int minY = pco.Height;
            int maxX = 0;
            int maxY = 0;
            int maxDepth = 8192;

            win.Rect ROI;
            if (_IsWristPositionAvailable)
            {
                ROI = new win.Rect()
                {
                    X = Math.Max(WristPosition.X - 25, 0),
                    Y = Math.Max(WristPosition.Y - 25, 0),
                    Width = 50,
                    Height = 50
                };
            }
            //else
            //{
            ROI = new win.Rect()
            {
                X = 0,
                Y = 0,
                Width = pco.Width,
                Height = pco.Height
            };
            //}

            //for (int i = (int)(ROI.X + ROI.Y * pco.Width); i < Math.Min((int)((ROI.X + ROI.Width - 1) + (ROI.Y + ROI.Height - 1) * pco.Width - 1), pco.Width * pco.Height); i++)
            for (int i = 0; i < pco.Height * pco.Width; i++)
            {
                distance[i] = (int)(pco.Bits[i * 2] >> 3 | pco.Bits[i * 2 + 1] << 5);
                if (distance[i] == 0 || distance[i] < thresholdMin || distance[i] > thresholdMax) distance[i] = maxDepth;
                if (min > distance[i]) min = distance[i];
            }

            // TODO Exceptionehere!!!!!
            for (int y = (int)ROI.Y; y < ROI.Y + ROI.Height; y++)
            {
                for (int x = (int)ROI.X; x < ROI.X + ROI.Width; x++)
                {
                    if (min == distance[y * pco.Width + x])
                    {
                        if (minX > x) minX = x;
                        if (minY > y) minY = y;
                        if (maxX < x) maxX = x;
                        if (maxY < y) maxY = y;
                    }
                    //else distance[y * pco.Width + x] = maxDepth; 
                }
            }

            //saveImage(distance, "depth.bmp", pco.Width, pco.Height);
            return new Vector()
                {
                    X = (maxX + minX) / 2,
                    Y = (maxY + minY) / 2,
                    Z = min
                };
        }

        public void SetWristPosition(Vector v)
        {
            WristPosition = v;
            _IsWristPositionAvailable = true;
        }

        public void Calibrate()
        {
            _CalibrationData = new List<Vector>();
            _IsCalibrating = true;
        }

        public void StopCalibration()
        {
            _IsCalibrating = false;
            ComputeVolumeBoundaries();
        }

        private void ComputeVolumeBoundaries()
        {
            double minX = _CalibrationData.Min(v => v.X);
            double minY = _CalibrationData.Min(v => v.Y);
            win.Rect r = new win.Rect()
            {
                X = minX,
                Y = minY,
                Height = _CalibrationData.Max(v => v.Y) - minY,
                Width = _CalibrationData.Max(v => v.X) - minX
            };
            CanvasRect = r;
            CanvasDepth = _CalibrationData.Average(v => v.Z);
            _CalibrationData.Clear();
            _IsCalibrated = true;
        }

        public Vector ScaleCoordinates(Vector pos, win.Size size)
        {
            Vector V = new Vector();

            V.X = (float)((pos.X - CanvasRect.X) / CanvasRect.Width * size.Width);
            V.Y = (float)((pos.Y - CanvasRect.Y) / CanvasRect.Height * size.Height);
            V.Z = (float)CanvasDepth - pos.Z;
            return V;
        }
    }
}
