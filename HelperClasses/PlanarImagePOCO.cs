using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HelperClasses
{
    [Serializable]
    public class PlanarImagePOCO
    {
        public int Height
        {
            get;
            set;
        }

        public int Width
        {
            get;
            set;
        }

        public byte[] Bits
        {
            get;
            set;
        }
    }
}
