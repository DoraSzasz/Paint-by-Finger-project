using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WpfApplication1;
using Microsoft.Research.Kinect.Nui;
using nui = Microsoft.Research.Kinect.Nui;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using HelperClasses;

namespace WpfApplication1.Skeleton
{
    public class SkeletonController : INUIController
    {

        Runtime Runtime;
        bool Paused;

        SkeletonData lastSkeleton;

        Queue<SkeletonData> lastSkeletons;
        Queue<SkeletonGesture> lastGestures;
        SkeletonGesture lastDetectedGesture = SkeletonGesture.None;
        int gesturesQueueLength = 32;

        public event EventHandler<DistanceEventArgs> DistanceEvent;
        public event EventHandler<VectorEventArgs> WristDetected;
        public event EventHandler<SkeletonGestureEventArgs> GestureDetected;
        public event EventHandler<SkeletonGestureEventArgs> SkeletonDetected;

        public bool IsPaused
        {
            get
            {
                return Paused;
            }
            set
            {
                Paused = value;
            }
        }

        public bool SaveFrames
        {
            get;
            set;
        }

        public void Initialize(Runtime r)
        {
            this.Runtime = r;
            Runtime.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(Runtime_SkeletonFrameReady);
            lastSkeletons = new Queue<SkeletonData>(128);
            lastGestures = new Queue<SkeletonGesture>(gesturesQueueLength);
        }

        void Runtime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (IsPaused)
            {
                return;
            }

            if (e.SkeletonFrame.Skeletons.Length > 0)
            {
                // find an active skeleton
                lastSkeleton = e.SkeletonFrame.Skeletons.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
                if (lastSkeleton == null)
                {
                    return;
                }
                lastSkeletons.Enqueue(lastSkeleton);

                if (DistanceEvent != null)
                {
                    if (lastSkeleton.Joints[JointID.Head].TrackingState != JointTrackingState.Tracked)
                    {
                        DistanceEvent(this, new DistanceEventArgs(lastSkeleton.JointsDistance(JointID.Head, JointID.WristRight)));
                    }
                }
                if (SkeletonDetected != null)
                {
                    this.SkeletonDetected(this, new SkeletonGestureEventArgs(SkeletonGesture.None) { Skeleton = lastSkeleton });
                }
                // some debug code
                if (WristDetected != null)
                {
                    float posX, posY;
                    Runtime.SkeletonEngine.SkeletonToDepthImage(
                        lastSkeleton.Joints[JointID.WristRight].Position, out posX, out posY);

                    posX = Math.Max(0, Math.Min(posX * 320, 320));  //convert to 320, 240 space
                    posY = Math.Max(0, Math.Min(posY * 240, 240));  //convert to 320, 240 space

                    WristDetected(this, new VectorEventArgs(new nui.Vector() { X = posX, Y = posY }));
                }

                // search for gestures and launch the event
                if (GestureDetected != null)
                {
                    SkeletonGesture currentGesture = Detect(lastSkeleton);
                    lastGestures.Enqueue(currentGesture);
                    if (lastGestures.Count > gesturesQueueLength)
                    {
                        lastGestures.Dequeue();
                        SkeletonGesture detectedGesture =
                            (from g in lastGestures
                             group g by g into p
                             select new { Gesture = p, GestureCount = p.Count() }).OrderByDescending(g => g.GestureCount).First().Gesture.First();
                        if (detectedGesture != lastDetectedGesture)
                        {
                            GestureDetected(this, new SkeletonGestureEventArgs(detectedGesture));
                        }
                        lastGestures.Clear();
                        lastDetectedGesture = detectedGesture;
                    }
                    
                }
            }
        }

        public void Pause()
        {
            Paused = true;
        }

        public void Destroy()
        {
            Runtime = null;
        }

        public void SaveFrame()
        {
            int fileID = 0;
            string fileName;
            do
            {
                fileName = "SkelFrame" + fileID + ".raw";
                fileID++;
            }
            while (File.Exists(fileName));

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                BinaryFormatter fmt = new BinaryFormatter();
                fmt.Serialize(fs, new SkeletonDataPOCO(lastSkeleton));
                fs.Flush();
                fs.Close();
            }
        }

        public void SaveModel()
        {
            int fileID = 0;
            string fileName;
            do
            {
                fileName = "SkelModel" + fileID + ".raw";
                fileID++;
            }
            while (File.Exists(fileName));

            throw new NotImplementedException("Implement IT!!!");
        }

        #region skeleton gesture detection

        /// <summary>
        /// Detects a gesture from the SkeletonData structure
        /// </summary>
        /// <param name="skel">NUI skeleton data</param>
        /// <returns></returns>
        private SkeletonGesture Detect(SkeletonData skel)
        {
            if (DetectAngleGrab(skel))
            {
                return SkeletonGesture.AnkleGrab;
            }
            if (DetectJesus(skel))
            {
                return SkeletonGesture.Jesus;
            }
            if (DetectHeadScratch(skel))
            {
                return SkeletonGesture.HeadScratch;
            }
            return SkeletonGesture.None;
        }

        /// <summary>
        /// Detects angle grab gesture
        /// </summary>
        /// <param name="skel"></param>
        /// <returns></returns>
        private bool DetectAngleGrab(SkeletonData skel)
        {
            double threshold = 0.7;
            // check if the joints are tracked and if the distance is under the threshold
            // right wrist&ankle
            if (((skel.Joints[JointID.WristRight].TrackingState == JointTrackingState.Tracked) &&
                (skel.Joints[JointID.AnkleRight].TrackingState == JointTrackingState.Tracked))
                && (skel.JointsDistance(JointID.WristRight, JointID.AnkleRight) < threshold))
            {
                return true;
            }
            // left wrist and ankle
            if (((skel.Joints[JointID.WristLeft].TrackingState == JointTrackingState.Tracked) &&
                (skel.Joints[JointID.AnkleLeft].TrackingState == JointTrackingState.Tracked))
                && (skel.JointsDistance(JointID.WristLeft, JointID.AnkleLeft) < threshold))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detects "Jesus" gestures
        /// </summary>
        /// <param name="skel"></param>
        /// <returns></returns>
        private bool DetectJesus(SkeletonData skel)
        {
            double thresh = 1.2;

            if (((skel.Joints[JointID.WristRight].TrackingState == JointTrackingState.Tracked) &&
                (skel.Joints[JointID.WristLeft].TrackingState == JointTrackingState.Tracked)) &&
                (skel.JointsDistance(JointID.WristLeft, JointID.WristRight) > thresh))
            {
                // prevent false detection when the arms are not both in the air
                if (Math.Abs(skel.Joints[JointID.WristLeft].Position.Z - skel.Joints[JointID.WristRight].Position.Z) < 0.3)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Detects head scratch gesture
        /// </summary>
        /// <param name="skel"></param>
        /// <returns></returns>
        private bool DetectHeadScratch(SkeletonData skel)
        {
            double thresh = 0.25;
            if (((skel.Joints[JointID.WristRight].TrackingState == JointTrackingState.Tracked) &&
                (skel.Joints[JointID.WristLeft].TrackingState == JointTrackingState.Tracked)) &&
                (skel.Joints[JointID.Head].TrackingState == JointTrackingState.Tracked) &&
                (
                    (skel.JointsDistance(JointID.Head, JointID.WristRight) < thresh) ||
                    (skel.JointsDistance(JointID.Head, JointID.WristLeft) < thresh)
                    ))
            {
                return true;
            }
            return false;
        }

        #endregion
    }







    public class DistanceEventArgs : EventArgs
    {
        public double Distance
        {
            get;
            set;
        }

        public DistanceEventArgs(Double d)
        {
            Distance = d;
        }


    }

    public class SkeletonGestureEventArgs : EventArgs
    {
        public SkeletonGesture Gesture
        {
            get;
            set;
        }

        public SkeletonData Skeleton
        {
            get;
            set;
        }

        public SkeletonGestureEventArgs(SkeletonGesture g)
        {
            Gesture = g;
        }
    }

}
