using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;


namespace HelperClasses
{
    public class NativeMethods
    {

        [DllImport("user32.dll")]

        public extern static IntPtr GetDesktopWindow();



        [System.Runtime.InteropServices.DllImport("user32.dll")]

        public static extern IntPtr GetWindowDC(IntPtr hwnd);



        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]

        public static extern IntPtr GetForegroundWindow();



        [System.Runtime.InteropServices.DllImport("gdi32.dll")]

        public static extern UInt64 BitBlt

             (IntPtr hDestDC,

             int x,

             int y,

             int nWidth,

             int nHeight,

             IntPtr hSrcDC,

             int xSrc,

             int ySrc,

             System.Int32 dwRop);


        public static void Screenshot(String filename)
        {
            Bitmap myImage = new Bitmap(Screen.PrimaryScreen.WorkingArea.Width,
                Screen.PrimaryScreen.WorkingArea.Height);

            Graphics gr1 = Graphics.FromImage(myImage);
            IntPtr dc1 = gr1.GetHdc();
            IntPtr dc2 = NativeMethods.GetWindowDC(NativeMethods.GetForegroundWindow());

            NativeMethods.BitBlt(dc1, Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, Screen.PrimaryScreen.Bounds.Width,
                Screen.PrimaryScreen.Bounds.Height, dc2, 0, 0, 13369376);
            gr1.ReleaseHdc(dc1);
            myImage.Save(filename, ImageFormat.Png); 
        }
    }

    
}
