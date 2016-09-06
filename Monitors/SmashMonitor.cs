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
    class SmashMonitor
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
        private List<Frames> FrameList;
        private double hipMaxDiff = 0;
        private double initRightShoulderElbowDiff = 0;
        private double headNeckDiff = 0;
        private double elbowSpineMaxDiff = 0;
        private List<CriticalPoint> result;
        private int videoCount = 0;

        public SmashMonitor(List<Frames> frameList, int videoCount)
        {
            this.FrameList = frameList;
            this.result = new List<CriticalPoint>();
            this.videoCount = videoCount;
        }

        public void start()
        {
            GenerateCompareData();
            int nowFrame = 0;
            nowFrame = CheckSide(nowFrame);
            nowFrame = CheckElbowUp(nowFrame);
            nowFrame = CheckElbowForward(nowFrame);
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
            foreach (Frames frame in FrameList)
            {
                Point3D hipRight = frame.jointDict[JointType.HipRight];
                Point3D hipLeft = frame.jointDict[JointType.HipLeft];
                Point3D elbowRight = frame.jointDict[JointType.ElbowRight];
                Point3D shoulderRight = frame.jointDict[JointType.ShoulderRight];
                Point3D head = frame.jointDict[JointType.Head];
                Point3D neck = frame.jointDict[JointType.Neck];
                Point3D spineMid = frame.jointDict[JointType.SpineMid];
                if (Math.Abs(hipRight.X - hipLeft.X) > hipMaxDiff)
                    hipMaxDiff = Math.Abs(hipRight.X - hipLeft.X);
                if (Math.Abs(elbowRight.Y - shoulderRight.Y) != 0 && initRightShoulderElbowDiff == 0)
                    initRightShoulderElbowDiff = Math.Abs(elbowRight.Y - shoulderRight.Y);
                if (headNeckCount <= 10)
                    headNeckDiff += Math.Sqrt(Math.Pow(head.X - neck.X, 2) + Math.Pow(head.Y - neck.Y, 2) + Math.Pow(head.Z- neck.Z, 2));
                if (Math.Abs(elbowRight.X - spineMid.X) > elbowSpineMaxDiff)
                    elbowSpineMaxDiff = Math.Abs(elbowRight.X - spineMid.X);
            }
            headNeckDiff /= 10;
        }

        private int CheckSide(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D hipRight = this.FrameList[i].jointDict[JointType.HipRight];
                Point3D hipLeft = this.FrameList[i].jointDict[JointType.HipLeft];
                if (Math.Abs(hipRight.X - hipLeft.X) < hipMaxDiff * 0.7 && Math.Abs(hipRight.X - hipLeft.X) != 0)
                {
                    return Record(i, "側身");
                }
            }
            return this.FrameList.Count;
        }

        private int CheckElbowUp(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D shoulderRight = this.FrameList[i].jointDict[JointType.ShoulderRight];
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                if (Math.Abs(elbowRight.Y - shoulderRight.Y) < initRightShoulderElbowDiff / 6)
                {
                    return Record(i, "手肘抬高");
                }
            }
            return this.FrameList.Count;
        }

        private int CheckElbowForward(int nowFrame)
        {
            double prevRightElbowShoulderDiff = 0, nowRightElbowShoulderDiff = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                Point3D shoulderRight = this.FrameList[i].jointDict[JointType.ShoulderRight];
                nowRightElbowShoulderDiff = elbowRight.Z - shoulderRight.Z;
                if (nowRightElbowShoulderDiff * prevRightElbowShoulderDiff < 0)
                {
                    return Record(i, "手肘轉向前");
                }
                prevRightElbowShoulderDiff = nowRightElbowShoulderDiff;
            }
            return this.FrameList.Count;
        }

        private int CheckWristForward(int nowFrame)
        {
            double prevHandtipWristDiff = 0, nowHandtipWristDiff = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D handTipRight = this.FrameList[i].jointDict[JointType.HandTipRight];
                Point3D wristRight = this.FrameList[i].jointDict[JointType.WristRight];
                nowHandtipWristDiff = handTipRight.Y - wristRight.Y;
                if (prevHandtipWristDiff - nowHandtipWristDiff > this.headNeckDiff / 3 && prevHandtipWristDiff != 0)
                {
                    return Record(i, "手腕發力");
                }
                prevHandtipWristDiff = nowHandtipWristDiff;
            }
            return this.FrameList.Count;
        }
        private int CheckElbowEnded(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                Point3D spineMid = this.FrameList[i].jointDict[JointType.SpineMid];
                double elbowSpineDiff = Math.Abs(elbowRight.X - spineMid.X);
                if (elbowSpineDiff < elbowSpineMaxDiff / 6)
                {
                    return Record(i, "收拍");
                }

            }
            return this.FrameList.Count;
        }
        
        private int Record(int i, String criticalPoint)
        {
            Console.WriteLine(i + " " + criticalPoint);
            this.result.Add(new CriticalPoint(criticalPoint, (double)i / this.videoCount));
            return i;
        }
    }
}
