using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            foreach (Frames Frame in FrameList)
            {
                double hipRight = 0, hipLeft = 0;
                double elbowRighty = 0, shoulderRight = 0;
                double headx = 0, heady = 0, headz = 0;
                double neckx = 0, necky = 0, neckz = 0;
                double elbowRightx = 0, spineMid = 0;
                foreach (Joints joint in Frame.jointList)
                {
                    if (string.Compare(joint.jointType, "HipRight") == 0)
                        hipRight = joint.x;
                    else if (string.Compare(joint.jointType, "HipLeft") == 0)
                        hipLeft = joint.x;
                    else if (string.Compare(joint.jointType, "ShoulderRight") == 0)
                        shoulderRight = joint.y;
                    else if (string.Compare(joint.jointType, "ElbowRight") == 0)
                    {
                        elbowRighty = joint.y;
                        elbowRightx = joint.x;
                    }
                    else if (string.Compare(joint.jointType, "Head") == 0 && joint.x != 0)
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
                    else if (string.Compare(joint.jointType, "SpineMid") == 0)
                        spineMid = joint.x;
                }
                if (Math.Abs(hipRight - hipLeft) > hipMaxDiff)
                    hipMaxDiff = Math.Abs(hipRight - hipLeft);
                if (Math.Abs(elbowRighty - shoulderRight) != 0 && initRightShoulderElbowDiff == 0)
                    initRightShoulderElbowDiff = Math.Abs(elbowRighty - shoulderRight);
                if (headNeckCount <= 10)
                    headNeckDiff += Math.Sqrt(Math.Pow(headx - neckx, 2) + Math.Pow(heady - necky, 2) + Math.Pow(headz - neckz, 2));
                if (Math.Abs(elbowRightx - spineMid) > elbowSpineMaxDiff)
                    elbowSpineMaxDiff = Math.Abs(elbowRightx - spineMid);
            }
            headNeckDiff /= 10;
        }

        private int CheckSide(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                double hipRight = 0;
                double hipLeft = 0;
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "HipRight") == 0)
                        hipRight = joint.x;
                    else if (string.Compare(joint.jointType, "HipLeft") == 0)
                        hipLeft = joint.x;
                }
                //Console.WriteLine(Frame.num + ": " + Math.Abs(hipRight - hipLeft));
                if (Math.Abs(hipRight - hipLeft) < hipMaxDiff * 0.7 && Math.Abs(hipRight - hipLeft) != 0)
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
                double shoulderRight = 0, elbowRight = 0;
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "ShoulderRight") == 0)
                        shoulderRight = joint.y;
                    else if (string.Compare(joint.jointType, "ElbowRight") == 0)
                        elbowRight = joint.y;
                }
                if (Math.Abs(elbowRight - shoulderRight) < initRightShoulderElbowDiff / 6)
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
                double elbowRight = 0, shoulderRight = 0;
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "ElbowRight") == 0)
                        elbowRight = joint.z;
                    else if (string.Compare(joint.jointType, "ShoulderRight") == 0)
                        shoulderRight = joint.z;
                }
                nowRightElbowShoulderDiff = elbowRight - shoulderRight;
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
                double handTipRight = 0, wristRight = 0;
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "HandTipRight") == 0)
                        handTipRight = joint.y;
                    else if (string.Compare(joint.jointType, "WristRight") == 0)
                        wristRight = joint.y;
                }
                nowHandtipWristDiff = handTipRight - wristRight;
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
            double elbowRight = 0, spineMid = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "ElbowRight") == 0)
                    {
                        elbowRight = joint.x;
                    }
                    else if (string.Compare(joint.jointType, "SpineMid") == 0)
                        spineMid = joint.x;
                }
                double elbowSpineDiff = Math.Abs(elbowRight - spineMid);
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
