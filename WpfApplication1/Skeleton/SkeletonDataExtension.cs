using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Research.Kinect.Nui;

namespace WpfApplication1.Skeleton
{
    /// <summary>
    /// SkeletonData and Vector Static class extensions for computing the distance between two vectors and between two skeletons
    /// </summary>
    public static class SkeletonDataExtension
    {
        public static double AverageDistance(this SkeletonData s1, SkeletonData s2)
        {
            if (s1.Joints.Count != s2.Joints.Count)
            {
                throw new ArgumentException("Both skeleton data must have the same number of joints");
            }
                        
            double sumDistances = 0;
            foreach (Joint j1 in s1.Joints)
            {
                Joint j2 = s2.Joints[j1.ID];
                if ((j1.TrackingState == JointTrackingState.NotTracked) ||
                    (j2.TrackingState == JointTrackingState.NotTracked))
                {
                    return Double.MaxValue;
                }

                sumDistances += j1.Position.Minus(s1.Position).DistanceTo(j2.Position.Minus(s2.Position));
            }
            
            return sumDistances / s1.Joints.Count;
        }

        public static double AverageDistance(this SkeletonData s1, SkeletonData s2, IEnumerable<JointID> JointIDs)
        {
            double sumDistances = 0.0;
            
            foreach (JointID jid in JointIDs)
            {
                Joint j1 = s1.Joints[jid];
                Joint j2 = s2.Joints[jid];
                sumDistances += j1.Position.Minus(s1.Position).DistanceTo(j2.Position.Minus(s2.Position));
            }
            return sumDistances / JointIDs.Count();
        }

        public static double JointsDistance(this SkeletonData s1, JointID jid1, JointID jid2)
        {
            Vector v1 = s1.Joints[jid1].Position;
            Vector v2 = s1.Joints[jid2].Position;
            return v1.DistanceTo(v2);
        }

        static public double DistanceTo(this Vector v1, Vector v2)
        {
            return Math.Sqrt(Math.Pow((v1.X - v2.X), 2) +
                Math.Pow((v1.Y - v2.Y), 2) +
                Math.Pow((v1.Z - v2.Z), 2));
        }

        static public Vector Minus(this Vector v1, Vector v2)
        {
            return new Vector() { X = v1.X - v2.X, Y = v1.Y - v2.Y, Z = v1.Z - v2.Z, W = v1.W - v2.W };
        }
    }
}
