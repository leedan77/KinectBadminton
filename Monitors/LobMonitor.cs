using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics.Monitors
{
    class LobMonitor:Monitor
    {
        private double spineShoulderBaseDiff = 0;
        private double initAnkleRightZ = 0;
        private String[] goals = { "持拍立腕", "右腳跨步", "腳跟著地", "手腕發力" };

        public LobMonitor(List<Frames> frameList, bool handedness, int videoCount)
        {
            this.FrameList = frameList;
            this.result = new List<CriticalPoint>();
            this.videoCount = videoCount;
            this.handedness = handedness;
            initCriticalPoints(goals);
        }

        public override void Start()
        {
            int nowFrame = 0;
            nowFrame = FromKeyExist();
            GenerateCompareData(nowFrame);
            nowFrame = CheckWristUp(nowFrame);
            //nowFrame = CheckWristTurn(nowFrame);
            nowFrame = CheckStepForward(nowFrame);
            nowFrame = CheckFootGroundAndWristForce(nowFrame);
        }

        public override void GenerateCompareData(int nowFrame)
        {
            int spineShoulderBaseCount = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                Point3D spineBase = this.FrameList[i].jointDict[JointType.SpineBase];
                Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
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
            for(int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D thumbRight = this.FrameList[i].jointDict[JointType.ThumbRight];
                Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                double thumbHandYDiff = thumbRight.Y - handRight.Y;
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
            int recordFrame = 0;
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
                    recordFrame = Record(i - 5, "腳跟著地");
                    FootGrounded = true;
                }
                prevAnkleRight = ankleRight;

                nowResult = CheckSide(elbowRight, wristRight, handTipRight);
                if (nowResult < 0 && prevResult > 0 && !WristForced)
                {
                    recordFrame = Record(i, "手腕發力");
                    WristForced = true;
                }
                if(FootGrounded && WristForced)
                {
                    return recordFrame;
                }
                prevResult = nowResult;
            }
            return this.FrameList.Count;
        }
    }
}
