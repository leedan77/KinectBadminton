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
        public struct CriticalPoint
        {
            public String name;
            public double portion;
            public CriticalPoint(String n, double p)
            {
                name = n;
                portion = p;
            }
        }
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
        private List<CriticalPoint> result;
        private int videoCount = 0;

        public ServeMonitor(List<Frames> frameList, int videoCount)
        {
            this.FrameList = frameList;
            this.result = new List<CriticalPoint>();
            this.videoCount = videoCount;
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
        public List<CriticalPoint> GetResult()
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
                        return Record(i, "重心腳在右腳");
                }
                else if(string.Compare(side, "left") == 0)
                {
                    if (Math.Abs(hipAngleRightHorAngle - 90) > Math.Abs(hipAngleLeftHorAngle - 90) + 5)
                    {
                        this.hipWidthWhenBalanceChange = hipRight.x - hipLeft.x;
                        return Record(i, "重心轉移到左腳");
                    }
                }
            }
            return this.FrameList.Count;
        }

        private int CheckWaistTwist(int nowFrame)
        {
            double hipWidth = 0;
            for(int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord hipRight = new JointCoord(0, 0, 0);
                JointCoord hipLeft = new JointCoord(0, 0, 0);
                foreach (Joints joint in FrameList[i].jointList)
                {
                    if (joint.jointType == "HipRight")
                        hipRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (string.Compare(joint.jointType, "HipLeft") == 0)
                        hipLeft = new JointCoord(joint.x, joint.y, joint.z);
                }
                hipWidth = hipRight.x - hipLeft.x;
                if (hipRight.z < hipLeft.z)
                    return Record(i, "轉腰");
            }
            return this.FrameList.Count;
        }
        private int CheckWristForward(int nowFrame)
        {
            int nowResult = 0;
            int prevResult = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord handTipRight = new JointCoord(0, 0, 0);
                JointCoord wristRight = new JointCoord(0, 0, 0);
                JointCoord elbowRight = new JointCoord(0, 0, 0);
                JointCoord spineBase = new JointCoord(0, 0, 0);
                JointCoord spineShoulder = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "HandTipRight")
                        handTipRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "WristRight")
                        wristRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "ElbowRight")
                        elbowRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "SpineBase")
                        spineBase = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "SpineShoulder")
                        spineShoulder = new JointCoord(joint.x, joint.y, joint.z);
                }

                nowResult = CheckSide(wristRight, elbowRight, handTipRight);
                double wristSpineAngle = CheckVec2Angle(spineShoulder, spineBase, wristRight);
                if (nowResult * prevResult < 0 && (wristSpineAngle < 20 || handTipRight.x < spineBase.x))
                    return Record(i, "手腕發力");
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
                JointCoord elbowRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "ShoulderRight")
                        shoulderRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "SpineShoulder")
                        spineShoulder = new JointCoord(joint.x, joint.y, joint.z);
                    else if(joint.jointType == "HandRight")
                        handRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "ElbowRight")
                        elbowRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                if (elbowRight.y > shoulderRight.y)
                    return Record(i, "手肘向前");
            }
            return this.FrameList.Count;
        }

        private int CheckSide(JointCoord lineCoord1, JointCoord lineCoord2, JointCoord checkPoint)
        {
            double a = (lineCoord2.y - lineCoord1.y) / (lineCoord2.x - lineCoord1.x);
            double b = lineCoord1.y - a * lineCoord1.x;
            double result = checkPoint.y - a * checkPoint.x - b;
            if ((result < 0 && a > 0) || (result > 0 && a < 0))
                return -1;
            return 1;
        }

        private double CheckVec2Angle(JointCoord vertex, JointCoord otherPoint1, JointCoord otherPoint2)
        {
            Vector2 vector1 = new Vector2(otherPoint1.x - vertex.x, otherPoint1.y - vertex.y);
            Vector2 vector2 = new Vector2(otherPoint2.x - vertex.x, otherPoint2.y - vertex.y);
            double angle = Math.Acos((vector1.x * vector2.x + vector1.y * vector2.y) / (vector1.d * vector2.d)) * 180 / Math.PI;
            return angle;
        }

        private int Record(int i, String criticalPoint)
        {
            Console.WriteLine(i + " " + criticalPoint);
            this.result.Add(new CriticalPoint(criticalPoint, (double)i / this.videoCount));
            return i;
        }
        
        private void Debug(int i, double value)
        {
            Console.WriteLine("Frame: " + i + ", " + value);
        }
    }
}
