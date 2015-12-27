using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Research.Kinect.Nui;

namespace WpfApplication1
{
    public class VectorEventArgs: EventArgs
    {
        public Vector Position
        {
            get;
            set;
        }

        public VectorEventArgs(Vector V)
        {
            Position = V;
        }
    }
}
