using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    class LobMonitor
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
        public struct Vector3
        {
            public double x, y, z, d;

            public Vector3(Point3D point1, Point3D point2)
            {
                x = point1.X - point2.X;
                y = point1.Y - point2.Y;
                z = point1.Z - point2.Z;
                d = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
            }
        }
        private List<Frames> FrameList;
        private List<CriticalPoint> result;
        private double spineShoulderBaseDiff = 0;
        private double initAnkleRightZ = 0;
        private int videoCount = 0;

        public LobMonitor(List<Frames> frameList, int videoCount)
        {
            this.FrameList = frameList;
            this.result = new List<CriticalPoint>();
            this.videoCount = videoCount;
        }

        public void start()
        {
            GenerateCompareData();
            int nowFrame = 0;
            nowFrame = CheckWristUp(nowFrame);
            //nowFrame = CheckWristTurn(nowFrame);
            nowFrame = CheckStepForward(nowFrame);
            nowFrame = CheckFootGroundAndWristForce(nowFrame);
        }

        public List<CriticalPoint> GetResult()
        {
            return this.result;
        }


        private void GenerateCompareData()
        {
            int spineShoulderBaseCount = 0;
            foreach (Frames frame in this.FrameList)
            {
                Point3D spineShoulder = frame.jointDict[JointType.SpineShoulder];
                Point3D spineBase = frame.jointDict[JointType.SpineBase];
                Point3D ankleRight = frame.jointDict[JointType.AnkleRight];
                Console.WriteLine(spineShoulder.X);
                Vector3 spineShoulderBaseVec = new Vector3(spineShoulder, spineBase);
                if (spineShoulder.X != 0)
                {
                    spineShoulderBaseCount++;
                    this.spineShoulderBaseDiff += spineShoulderBaseVec.d;
                }
                if (ankleRight.Z != 0 && this.initAnkleRightZ == 0)
                    this.initAnkleRightZ = ankleRight.Z;
            }
            this.spineShoulderBaseDiff /= spineShoulderBaseCount;
        }

        private int CheckWristUp(int nowFrame)
        {
            Console.WriteLine("Checking wrist up");
            for(int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D thumbRight = this.FrameList[i].jointDict[JointType.ThumbRight];
                Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                double thumbHandYDiff = thumbRight.Y - handRight.Y;
                Debug(i, thumbHandYDiff);
                if (thumbHandYDiff > 0.01)
                    return Record(i, "持拍立腕");
            }
            return this.FrameList.Count;
        }

        private int CheckWristTurn(int nowFrame)
        {
            double nowThumbHandXDiff = 0;
            double prevThumbHandXDiff = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D handTipRight = this.FrameList[i].jointDict[JointType.HandTipRight];
                Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                nowThumbHandXDiff = handTipRight.X - handRight.X;
                if(prevThumbHandXDiff < 0 && nowThumbHandXDiff > 0)
                    return Record(i, "手腕轉動");
                prevThumbHandXDiff = nowThumbHandXDiff;
            }
            return this.FrameList.Count;
        }

        private int CheckStepForward(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                Point3D spineBase = this.FrameList[i].jointDict[JointType.SpineBase];
                double stepSpineRatio = (this.initAnkleRightZ - ankleRight.Z) / this.spineShoulderBaseDiff;
                if (stepSpineRatio > 1)
                    return Record(i, "右腳跨步");
            }
            return this.FrameList.Count;
        }

        private int CheckFootGroundAndWristForce(int nowFrame)
        {
            int steadyCount = 0;
            int errorFrame = 0;
            Point3D prevAnkleRight = new Point3D(0, 0, 0);

            int nowResult = 0;
            int prevResult = 0;

            bool FootGrounded = false;
            bool WristForced = false;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D footRight = this.FrameList[i].jointDict[JointType.FootRight];
                Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                Point3D wristRight = this.FrameList[i].jointDict[JointType.WristRight];
                Point3D handTipRight = this.FrameList[i].jointDict[JointType.HandTipRight];
                Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                Vector3 ankleMovement = new Vector3(ankleRight, prevAnkleRight);
                steadyCount++;
                if (Math.Abs(ankleMovement.d) > 0.01)
                {
                    if(i < errorFrame + 5)
                        steadyCount = i - errorFrame;
                    errorFrame = i;
                }
                if (steadyCount >= 5 && !FootGrounded)
                {
                    Record(i - 5, "腳跟著地");
                    FootGrounded = true;
                }
                prevAnkleRight = ankleRight;

                nowResult = CheckSide(wristRight, elbowRight, handRight);
                if (nowResult < 0 && prevResult > 0 && !WristForced)
                {
                    Record(i, "手腕發力");
                    WristForced = true;
                }
                Debug(i, nowResult);
                prevResult = nowResult;
            }
            return this.FrameList.Count;
        }

        private void Debug(int i, double value)
        {
            Console.WriteLine("Frame: "+ i + ", " + value);
        }

        private int Record(int i, String criticalPoint)
        {
            Console.WriteLine(i + " " + criticalPoint);
            this.result.Add(new CriticalPoint(criticalPoint, (double)i / this.videoCount));
            return i;
        }

        private int CheckSide(Point3D lineCoord1, Point3D lineCoord2, Point3D checkPoint)
        {
            double a = (lineCoord2.Y - lineCoord1.Y) / (lineCoord2.X - lineCoord1.X);
            double b = lineCoord1.Y - a * lineCoord1.X;
            double result = checkPoint.Y - a * checkPoint.X - b;
            Console.WriteLine(a);
            Console.WriteLine(result);
            if (result * a < 0)
                return -1;
            return 1;
        }
    }
}
