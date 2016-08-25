﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    class LobMonitor
    {
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
        private List<double> result;
        private double spineShoulderBaseDiff = 0;
        private double initAnkleRightZ = 0;
        private int videoCount = 0;

        public LobMonitor(List<Frames> frameList, int videoCount)
        {
            this.FrameList = frameList;
            this.result = new List<double>();
            this.videoCount = videoCount;
        }

        public void start()
        {
            GenerateCompareData();
            int nowFrame = 0;
            nowFrame = CheckWristUp(nowFrame);
            //nowFrame = CheckWristTurn(nowFrame);
            nowFrame = CheckStepForward(nowFrame);
            nowFrame = CheckFootGround(nowFrame);
            nowFrame = CheckWristForce(nowFrame);
        }

        public List<double> GetResult()
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
                Vector3 spineShoulderBaseVec = new Vector3(spineShoulder.x - spineBase.x, spineShoulder.y - spineBase.y, spineShoulder.z - spineBase.z);
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
                    return Record(i, "Wrist up");
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
                Debug(i, nowThumbHandXDiff);
                if(prevThumbHandXDiff < 0 && nowThumbHandXDiff > 0)
                    return Record(i, "Wrist turn");
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
                if (stepSpineRatio > 1.5)
                    return Record(i, "Step forward");
                //Debug(i, ankleRight.z);
            }
            return this.FrameList.Count;
        }

        private int CheckFootGround(int nowFrame)
        {
            int steadyCount = 0;
            double prevFootRightY = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord footRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "FootRight")
                        footRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                if (Math.Abs(footRight.y - prevFootRightY) < 0.005)
                    steadyCount++;
                else
                    steadyCount = 0;
                if (steadyCount >= 10)
                    return Record(i - 10, "Foot ground");
                prevFootRightY = footRight.y;
                //Debug(i, footRight.y);
            }
            return this.FrameList.Count;
        }

        private int CheckWristForce(int nowFrame)
        {
            int nowResult = 0;
            int prevResult = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                JointCoord elbowRight = new JointCoord(0, 0, 0);
                JointCoord wristRight = new JointCoord(0, 0, 0);
                JointCoord handTipRight = new JointCoord(0, 0, 0);
                foreach (Joints joint in this.FrameList[i].jointList)
                {
                    if (joint.jointType == "ElbowRight")
                        elbowRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "WristRight")
                        wristRight = new JointCoord(joint.x, joint.y, joint.z);
                    else if (joint.jointType == "HandTipRight")
                        handTipRight = new JointCoord(joint.x, joint.y, joint.z);
                }
                nowResult = CheckSide(wristRight, elbowRight, handTipRight);
                //Debug(i, nowResult);
                if (nowResult < 0 && prevResult > 0)
                    return Record(i, "Wrist force");
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
            this.result.Add((double)i / this.videoCount);
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