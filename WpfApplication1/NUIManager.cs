using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Research.Kinect.Nui;

namespace WpfApplication1
{
    public class NUIManager
    {
        #region Singleton
        private static NUIManager _Instance;

        public static NUIManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new NUIManager();
                }
                return _Instance;
            }
        }

        private NUIManager() { }
        #endregion

        private DateTime lastDepthTick;
        private int depthFrameTicks;

        public Double FPS;


        public Runtime Runtime;

        public void Initialize()
        {
            Runtime = new Runtime();

            try
            {
                Runtime.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception("Runtime initialization failed. Please make sure Kinect device is plugged in.", ex);
            }


            try
            {
                Runtime.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception("Failed to open stream. Please make sure to specify a supported image type and resolution.", ex);
            }

            lastDepthTick = DateTime.Now;

            Runtime.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(Runtime_DepthFrameReady);
        }

        protected void Runtime_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            depthFrameTicks++;
            if ((DateTime.Now - lastDepthTick) > TimeSpan.FromSeconds(1))
            {
                FPS = depthFrameTicks;
                lastDepthTick = DateTime.Now;
                depthFrameTicks = 0;
            }
        }

        public void Uninitialize()
        {
            if (Runtime == null)
                return;
            Runtime.Uninitialize();
            FPS = 0;
            depthFrameTicks = 0;
        }
    }
}
