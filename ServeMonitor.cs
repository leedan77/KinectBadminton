using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    class ServeMonitor
    {
        public struct Vector
        {
            public double x, y, d;

            public Vector(double vx, double vy)
            {
                x = vx;
                y = vy;
                d = Math.Sqrt(Math.Pow(vx, 2) + Math.Pow(vy, 2));
            }
        }
        
        private List<Frames> FrameList;
        private double hipWidthWhenBalanceChange = 0;
        
        private double headNeckDiff = 0;
        private List<int> result;

        public ServeMonitor(List<Frames> frameList)
        {
            this.FrameList = frameList;
            this.result = new List<int>();
        }
        public void start()
        {
            GenerateCompareData();
            int nowFrame = 0;
            nowFrame = CheckBalancePoint(nowFrame, "right");
            nowFrame = CheckBalancePoint(nowFrame, "left");
            nowFrame = CheckWaistTwist(nowFrame);
            nowFrame = CheckWristForward(nowFrame);
        }
        public List<int> GetResult()
        {
            return this.result;
        }
        private void GenerateCompareData()
        {
            int headNeckCount = 0;
            foreach (Frames Frame in FrameList)
            {
                double headx = 0, heady = 0, headz = 0;
                double neckx = 0, necky = 0, neckz = 0;
                foreach (Joints joint in Frame.jointList)
                {
                    if (string.Compare(joint.jointType, "Head") == 0 && joint.x != 0)
                    {
                        headx = joint.x;
                        heady = joint.y;
                        headz = joint.z;
                        headNeckCount++;
                    }
                    else if (string.Compare(joint.jointType, "Neck") == 0 && joint.x != 0)
                    {
                        neckx = joint.x;
                        necky = joint.y;
                        neckz = joint.z;
                    }
                }
                if (headNeckCount <= 10)
                    headNeckDiff += Math.Sqrt(Math.Pow(headx - neckx, 2) + Math.Pow(heady - necky, 2) + Math.Pow(headz - neckz, 2));
            }
            headNeckDiff /= 10;
        }
        private int CheckBalancePoint(int nowFrame, String side)
        {
            for(int i = nowFrame; i < FrameList.Count; i++)
            {
                double hipRightx = 0, kneeRightx = 0, ankleRightx = 0;
                double hipRighty = 0, kneeRighty = 0, ankleRighty = 0;
                double hipLeftx = 0, kneeLeftx = 0, ankleLeftx = 0;
                double hipLefty = 0, kneeLefty = 0, ankleLefty = 0;
                foreach (Joints joint in FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "HipRight") == 0)
                    {
                        hipRightx = joint.x;
                        hipRighty = joint.y;
                    }
                    else if (string.Compare(joint.jointType, "KneeRight") == 0)
                    {
                        kneeRightx = joint.x;
                        kneeRighty = joint.y;
                    }
                    else if (string.Compare(joint.jointType, "AnkleRight") == 0)
                    {
                        ankleRightx = joint.x;
                        ankleRighty = joint.y;
                    }
                    else if (string.Compare(joint.jointType, "HipLeft") == 0)
                    {
                        hipLeftx = joint.x;
                        hipLefty = joint.y;
                    }
                    else if (string.Compare(joint.jointType, "KneeLeft") == 0)
                    {
                        kneeLeftx = joint.x;
                        kneeLefty = joint.y;
                    }
                    else if (string.Compare(joint.jointType, "AnkleLeft") == 0)
                    {
                        ankleLeftx = joint.x;
                        ankleLefty = joint.y;
                    }
                }

                Vector hipAngleRight = new Vector(ankleRightx - hipRightx, ankleRighty - hipRighty);
                Vector hipAngleLeft = new Vector(ankleLeftx - hipLeftx, ankleLefty - hipLefty);
                Vector horizentalLine = new Vector(10, 0);

                double hipAngleRightHorAngle = Math.Acos((hipAngleRight.x * horizentalLine.x + hipAngleRight.y + horizentalLine.y) / (hipAngleRight.d * horizentalLine.d)) * 180 / Math.PI;
                double hipAngleLeftHorAngle = Math.Acos((hipAngleLeft.x * horizentalLine.x + hipAngleLeft.y + horizentalLine.y) / (hipAngleLeft.d * horizentalLine.d)) * 180 / Math.PI;
                
                if(string.Compare(side, "right") == 0)
                {
                    if(Math.Abs(hipAngleRightHorAngle - 90) < Math.Abs(hipAngleLeftHorAngle - 90) - 5)
                    {
                        Console.WriteLine(i + " Balance right");
                        this.result.Add(i);
                        return i;
                    }
                }
                else if(string.Compare(side, "left") == 0)
                {
                    if (Math.Abs(hipAngleRightHorAngle - 90) > Math.Abs(hipAngleLeftHorAngle - 90) + 5)
                    {
                        Console.WriteLine(i + " Balance left");
                        this.hipWidthWhenBalanceChange = hipRightx - hipLeftx;
                        this.result.Add(i);
                        return i;
                    }
                }
            }
            return this.FrameList.Count;
        }

        private int CheckWaistTwist(int nowFrame)
        {
            double hipRightx = 0, hipLeftx = 0;
            double hipWidth = 0;
            for(int i = nowFrame; i < this.FrameList.Count; i++)
            {
                foreach (Joints joint in FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "HipRight") == 0)
                    {
                        hipRightx = joint.x;
                    }
                    else if(string.Compare(joint.jointType, "HipLeft") == 0)
                    {
                        hipLeftx = joint.x;
                    }
                }
                hipWidth = hipRightx - hipLeftx;
                if(hipWidth < this.hipWidthWhenBalanceChange / 3 * 2)
                {
                    Console.WriteLine(i + " Twist waist");
                    this.result.Add(i);
                    return i;
                }
            }
            return this.FrameList.Count;
        }
        private int CheckWristForward(int nowFrame)
        {
            double prevHandtipWristDiff = 0, nowHandtipWristDiff = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                double handTipRight = 0, wristRight = 0;
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "HandTipRight") == 0)
                        handTipRight = joint.y;
                    else if (string.Compare(joint.jointType, "WristRight") == 0)
                        wristRight = joint.y;
                }
                nowHandtipWristDiff = wristRight - handTipRight;
                Console.WriteLine("Frame " + i + ": " + nowHandtipWristDiff);
                if (prevHandtipWristDiff - nowHandtipWristDiff > this.headNeckDiff / 3 && prevHandtipWristDiff != 0)
                {
                    //Console.WriteLine(i + " Wrist forward");
                    //return i;
                }
                prevHandtipWristDiff = nowHandtipWristDiff;
            }
            return this.FrameList.Count;
        }
    }
}
