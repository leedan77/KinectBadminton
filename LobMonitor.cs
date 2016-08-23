using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    class LobMonitor
    {
        public struct JointCoord
        {
            public double x, y, z;
            public JointCoord(double cx, double cy, double cz)
            {
                x = cx;
                y = cy;
                z = cz;
            }
        }
        private List<Frames> FrameList;
        private List<int> result;

        public LobMonitor(List<Frames> frameList)
        {
            this.FrameList = frameList;
            this.result = new List<int>();
        }

        public void start()
        {
            int nowFrame = 0;
            nowFrame = CheckWristUp(nowFrame);
            nowFrame = CheckWristTurn(nowFrame);
        }

        public List<int> GetResult()
        {
            return this.result;
        }

        private int CheckWristUp(int nowFrame)
        {
            for(int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord thumbRight = new JointCoord(0, 0, 0);
                JointCoord handRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "ThumbRight")
                        thumbRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "HandRight")
                        handRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                double thumbHandYDiff = thumbRight.y - handRight.y;
                //Debug(i, thumbHandYDiff);
                if (thumbHandYDiff > 0.01)
                {
                    Console.WriteLine(i + " Wrist up");
                    this.result.Add(i);
                    return i;
                }
            }
            return this.FrameList.Count;
        }

        private int CheckWristTurn(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord thumbRight = new JointCoord(0, 0, 0);
                JointCoord handRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "ThumbRight")
                        thumbRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "HandRight")
                        handRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                double thumbHandXDiff = thumbRight.x - handRight.x;
                Debug(i, thumbHandXDiff);
            }
            return this.FrameList.Count;
        }

        private void Debug(int i, double value)
        {
            Console.WriteLine("Frame: "+ i + ", " + value);
        }
    }
}
