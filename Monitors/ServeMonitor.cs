using Microsoft.Kinect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

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

        }
        private int CheckBalancePoint(int nowFrame, String side)
        {
            for(int i = nowFrame; i < FrameList.Count; i++)
            {
                Point3D hipRight = this.FrameList[i].jointDict[JointType.HipRight];
                Point3D kneeRight = this.FrameList[i].jointDict[JointType.KneeRight];
                Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                Point3D hipLeft = this.FrameList[i].jointDict[JointType.HipLeft];
                Point3D kneeLeft = this.FrameList[i].jointDict[JointType.KneeLeft];
                Point3D ankleLeft = this.FrameList[i].jointDict[JointType.AnkleLeft];

                Vector2 hipAngleRight = new Vector2(ankleRight.X - hipRight.X, ankleRight.Y - hipRight.Y);
                Vector2 hipAngleLeft = new Vector2(ankleLeft.X - hipLeft.X, ankleLeft.Y - hipLeft.Y);
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
                        this.hipWidthWhenBalanceChange = hipRight.X - hipLeft.X;
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
                Point3D hipRight = this.FrameList[i].jointDict[JointType.HipRight];
                Point3D hipLeft = this.FrameList[i].jointDict[JointType.HipLeft];
                hipWidth = hipRight.X - hipLeft.X;
                if (hipRight.Z < hipLeft.Z)
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
                Point3D handTipRight = this.FrameList[i].jointDict[JointType.HandTipRight];
                Point3D wristRight = this.FrameList[i].jointDict[JointType.WristRight];
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                Point3D spineBase = this.FrameList[i].jointDict[JointType.SpineBase];
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];

                nowResult = CheckSide(wristRight, elbowRight, handTipRight);
                double wristSpineAngle = CheckVec2Angle(spineShoulder, spineBase, wristRight);
                if (nowResult * prevResult < 0 && (wristSpineAngle < 20 || handTipRight.X < spineBase.X))
                    return Record(i, "手腕發力");
                prevResult = nowResult;
            }
            return this.FrameList.Count;

        }

        private int CheckElbowEnded(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D shoulderRight = this.FrameList[i].jointDict[JointType.ShoulderRight];
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                if (elbowRight.Y > shoulderRight.Y)
                    return Record(i, "手肘向前");
            }
            return this.FrameList.Count;
        }

        private int CheckSide(Point3D lineCoord1, Point3D lineCoord2, Point3D checkPoint)
        {
            double a = (lineCoord2.Y - lineCoord1.Y) / (lineCoord2.X - lineCoord1.X);
            double b = lineCoord1.Y - a * lineCoord1.X;
            double result = checkPoint.Y - a * checkPoint.X - b;
            if ((result < 0 && a > 0) || (result > 0 && a < 0))
                return -1;
            return 1;
        }

        private double CheckVec2Angle(Point3D vertex, Point3D otherPoint1, Point3D otherPoint2)
        {
            Vector2 vector1 = new Vector2(otherPoint1.X - vertex.X, otherPoint1.Y - vertex.Y);
            Vector2 vector2 = new Vector2(otherPoint2.X - vertex.X, otherPoint2.Y - vertex.Y);
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
