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
        public struct Vector2
        {
            public double x, y, d;

            public Vector2(double vx, double vy)
            {
                x = vx;
                y = vy;
                d = Math.Sqrt(Math.Pow(vx, 2) + Math.Pow(vy, 2));
            }
        }

        public struct Vector3
        {
            public double x, y, z, d;

            public Vector3(double vx, double vy, double vz)
            {
                x = vx;
                y = vy;
                z = vz;
                d = Math.Sqrt(Math.Pow(vx, 2) + Math.Pow(vy, 2) + Math.Pow(vz, 2));
            }
        }

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
            nowFrame = CheckElbowEnded(nowFrame);
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
                JointCoord head = new JointCoord(0, 0, 0);
                JointCoord neck = new JointCoord(0, 0, 0);
                foreach (Joints joint in Frame.jointList)
                {
                    if (string.Compare(joint.jointType, "Head") == 0 && joint.x != 0)
                    {
                        head.x = joint.x;
                        head.y = joint.y;
                        head.z = joint.z;
                        headNeckCount++;
                    }
                    else if (string.Compare(joint.jointType, "Neck") == 0 && joint.x != 0)
                    {
                        neck.x = joint.x;
                        neck.y = joint.y;
                        neck.z = joint.z;
                    }
                }
                if (headNeckCount <= 10)
                    headNeckDiff += Math.Sqrt(Math.Pow(head.x - neck.x, 2) + Math.Pow(head.y - neck.y, 2) + Math.Pow(head.z - neck.z, 2));
            }
            headNeckDiff /= 10;
        }
        private int CheckBalancePoint(int nowFrame, String side)
        {
            for(int i = nowFrame; i < FrameList.Count; i++)
            {
                JointCoord hipRight = new JointCoord(0, 0, 0);
                JointCoord kneeRight = new JointCoord(0, 0, 0);
                JointCoord ankleRight = new JointCoord(0, 0, 0);
                JointCoord hipLeft = new JointCoord(0, 0, 0);
                JointCoord kneeLeft = new JointCoord(0, 0, 0);
                JointCoord ankleLeft = new JointCoord(0, 0, 0);
                foreach (Joints joint in FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "HipRight") == 0)
                        hipRight = new JointCoord(joint.x, joint.y, 0);
                    else if (string.Compare(joint.jointType, "KneeRight") == 0)
                        kneeRight = new JointCoord(joint.x, joint.y, 0);
                    else if (string.Compare(joint.jointType, "AnkleRight") == 0)
                        ankleRight = new JointCoord(joint.x, joint.y, 0);
                    else if (string.Compare(joint.jointType, "HipLeft") == 0)
                        hipLeft = new JointCoord(joint.x, joint.y, 0);
                    else if (string.Compare(joint.jointType, "KneeLeft") == 0)
                        kneeLeft = new JointCoord(joint.x, joint.y, 0);
                    else if (string.Compare(joint.jointType, "AnkleLeft") == 0)
                        ankleLeft = new JointCoord(joint.x, joint.y, 0);
                }

                Vector2 hipAngleRight = new Vector2(ankleRight.x - hipRight.x, ankleRight.y - hipRight.y);
                Vector2 hipAngleLeft = new Vector2(ankleLeft.x - hipLeft.x, ankleLeft.y - hipLeft.y);
                Vector2 horizentalLine = new Vector2(10, 0);

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
                        this.hipWidthWhenBalanceChange = hipRight.x - hipLeft.x;
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
            double nowResult = 0;
            double prevResult = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord handTipRight = new JointCoord(0, 0, 0);
                JointCoord wristRight = new JointCoord(0, 0, 0);
                JointCoord elbowRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "HandTipRight")
                        handTipRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "WristRight")
                        wristRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "ElbowRight")
                        elbowRight = new JointCoord(joint.x, joint.y, joint.z);
                }

                nowResult = CheckSide(wristRight, elbowRight, handTipRight);
                //Console.WriteLine(nowResult);
                //Console.WriteLine("Frame: " + i + ", " + (handTipRight.y - wristRight.y));
                //Console.WriteLine("Frame: " + i + ", " + nowResult);
                if (nowResult * prevResult < 0)
                {
                    Console.WriteLine(i + " Wrist forward");
                    this.result.Add(i);
                    return i;
                }
                prevResult = nowResult;
            }
            return this.FrameList.Count;

        }

        private int CheckElbowEnded(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord shoulderRight = new JointCoord(0, 0, 0);
                JointCoord spineShoulder = new JointCoord(0, 0, 0);
                JointCoord handRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "ShoulderRight")
                        shoulderRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "SpineShoulder")
                        spineShoulder = new JointCoord(joint.x, joint.y, joint.z);
                    else if(joint.jointType == "HandRight")
                        handRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                double handSpineZDiff = handRight.z - spineShoulder.z;
                if(handSpineZDiff < 0)
                {
                    Console.WriteLine(i + " Elbow ended");
                    this.result.Add(i);
                    return i;
                }
            }
            return this.FrameList.Count;
        }

        private double CheckSide(JointCoord lineCoord1, JointCoord lineCoord2, JointCoord checkPoint)
        {
            double a = (lineCoord2.y - lineCoord1.y) / (lineCoord2.x - lineCoord1.x);
            double b = lineCoord1.y - a * lineCoord1.x;
            double result = checkPoint.y - a * checkPoint.x - b;
            if ((result < 0 && a > 0) || (result > 0 && a < 0))
                return -1;
            return 1;
        }
    }
}
