using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Research.Kinect.Nui;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace HelperClasses
{
    [Serializable]
    public class SkeletonDataPOCO
    {
        public VectorPOCO Position
        {
            get;
            set;
        }

        public Dictionary<JointID, VectorPOCO> Joints;

        public SkeletonDataPOCO()
        {
        }

        public SkeletonDataPOCO(SkeletonData skel)
        {
            this.Position = new VectorPOCO(skel.Position);
            Joints = new Dictionary<JointID, VectorPOCO>();
            foreach (Joint j in skel.Joints)
            {
                Joints[j.ID] = new VectorPOCO(j.Position);
            }
        }

        public double AverageDistance(SkeletonDataPOCO s2, IEnumerable<JointID> JointIDs)
        {
            double sumDistances = 0.0;

            foreach (JointID jid in JointIDs)
            {
                VectorPOCO j1 = this.Joints[jid] - Position;
                VectorPOCO j2 = s2.Joints[jid] - s2.Position;
                sumDistances += j1.DistanceTo(j2);
            }
            return sumDistances / JointIDs.Count();
        }

        public static SkeletonDataPOCO Load(String filename)
        {
            SkeletonDataPOCO skel;
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                BinaryFormatter bf = new BinaryFormatter();
                skel = (SkeletonDataPOCO)bf.Deserialize(fs);
                fs.Close();
            }
            return skel;
        }

    }
}
