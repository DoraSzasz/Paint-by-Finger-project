using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Research.Kinect.Nui;

namespace HelperClasses
{
    [Serializable]
    public class VectorPOCO
    {
        public double X
        {
            get;
            set;
        }

        public double Y
        {
            get;
            set;
        }

        public double Z
        {
            get;
            set;
        }

        public double W
        {
            get;
            set;
        }

        public VectorPOCO()
        {
        }

        public VectorPOCO(Vector v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
            W = v.W;
        }


        public override String ToString()
        {
            return String.Format("({0}, \t{1}, \t{2}, \t{3})", X.ToString("F4"), Y.ToString("F4"), Z.ToString("F4"), W.ToString("F4"));
        }

        public double DistanceTo(VectorPOCO v)
        {
            return Math.Sqrt(Math.Pow((this.X - v.X), 2) +
                Math.Pow((this.Y - v.Y), 2) +
                Math.Pow((this.Z - v.Z), 2));
        }

        public static VectorPOCO operator -(VectorPOCO v1, VectorPOCO v2)
        {
            return new VectorPOCO() { X = v1.X - v2.X, Y = v1.Y - v2.Y, Z = v1.Z - v2.Z, W = v1.W - v2.W };

        }


    }
}
