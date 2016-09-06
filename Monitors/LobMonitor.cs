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

            public Vector3(JointCoord point1, JointCoord point2)
            {
                x = point1.x - point2.x;
                y = point1.y - point2.y;
                z = point1.z - point2.z;
                d = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
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
            //nowFrame = CheckFootGround(nowFrame);
            //nowFrame = CheckWristForce(nowFrame);
            nowFrame = CheckFootGroundAndWristForce(nowFrame);
        }

        public List<CriticalPoint> GetResult()
        {
            return this.result;
        }


        private void GenerateCompareData()
        {
            int spineShoulderBaseCount = 0;
            foreach (Frames Frame in FrameList)
            {
                JointCoord spineShoulder = new JointCoord(0, 0, 0);
                JointCoord spineBase = new JointCoord(0, 0, 0);
                JointCoord ankleRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in Frame.jointList)
                {
                    if (joint.jointType == "SpineShoulder")
                        spineShoulder = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "SpineBase")
                        spineBase = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "AnkleRight")
                        ankleRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                Vector3 spineShoulderBaseVec = new Vector3(spineShoulder, spineBase);
                if (spineShoulder.x != 0)
                {
                    spineShoulderBaseCount++;
                    this.spineShoulderBaseDiff += spineShoulderBaseVec.d;
                }
                if (ankleRight.z != 0 && this.initAnkleRightZ == 0)
                    this.initAnkleRightZ = ankleRight.z;
            }
            this.spineShoulderBaseDiff /= spineShoulderBaseCount;
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
                JointCoord handTipRight = new JointCoord(0, 0, 0);
                JointCoord handRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "HandTipRight")
                        handTipRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "HandRight")
                        handRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                nowThumbHandXDiff = handTipRight.x - handRight.x;
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
                JointCoord ankleRight = new JointCoord(0, 0, 0);
                JointCoord spineShoulder = new JointCoord(0, 0, 0);
                JointCoord spineBase = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "AnkleRight")
                        ankleRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "SpineShoulder")
                        spineShoulder = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "SpineBase")
                        spineBase = new JointCoord(joint.x, joint.y, joint.z);
                }
                double stepSpineRatio = (this.initAnkleRightZ - ankleRight.z) / this.spineShoulderBaseDiff;
                if (stepSpineRatio > 1.2)
                    return Record(i, "右腳跨步");
            }
            return this.FrameList.Count;
        }

        private int CheckFootGroundAndWristForce(int nowFrame)
        {
            int steadyCount = 0;
            JointCoord prevAnkleRight = new JointCoord(0, 0, 0);

            int nowResult = 0;
            int prevResult = 0;

            bool FootGrounded = false;
            bool WristForced = false;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord footRight = new JointCoord(0, 0, 0);
                JointCoord ankleRight = new JointCoord(0, 0, 0);
                JointCoord elbowRight = new JointCoord(0, 0, 0);
                JointCoord wristRight = new JointCoord(0, 0, 0);
                JointCoord handTipRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "FootRight")
                        footRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "AnkleRight")
                        ankleRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "ElbowRight")
                        elbowRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "WristRight")
                        wristRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "HandTipRight")
                        handTipRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                Vector3 ankleMovement = new Vector3(ankleRight, prevAnkleRight);
                if (Math.Abs(ankleMovement.d) < 0.01)
                    steadyCount++;
                else
                    steadyCount = 0;
                if (steadyCount >= 5 && !FootGrounded)
                {
                    Record(i - 5, "腳跟著地");
                    FootGrounded = true;
                }
                prevAnkleRight = ankleRight;

                nowResult = CheckSide(wristRight, elbowRight, handTipRight);
                if (nowResult < 0 && prevResult > 0 && !WristForced)
                {
                    Record(i, "手腕發力");
                    WristForced = true;
                }
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

        private int CheckSide(JointCoord lineCoord1, JointCoord lineCoord2, JointCoord checkPoint)
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
