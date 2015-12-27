using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Research.Kinect.Nui;

namespace WpfApplication1
{
    interface INUIController
    {
        void Initialize(Runtime r);       
        void Destroy();
    }
}
