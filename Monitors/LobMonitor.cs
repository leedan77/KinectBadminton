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
        private double initAnkleLeftZ = 0;
        private double initHandRightSpineMidXDiff = 0;
        private double initHandLeftSpineMidXDiff = 0;
        private String[] goals = { "持拍立腕", "慣用腳跨步", "手腕轉動", "腳跟著地", "手腕發力" };

        public LobMonitor(List<Frames> frameList, String handedness, int videoCount)
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
            nowFrame = CheckWristTurnAndStepForward(nowFrame);
            //nowFrame = CheckWristTurn(nowFrame);
            //nowFrame = CheckStepForward(nowFrame);
            nowFrame = CheckFootGroundAndWristForce(nowFrame);
        }

        public override void GenerateCompareData(int nowFrame)
        {
            int spineShoulderBaseCount = 0;
            int handSpineMidCount = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                Point3D spineBase = this.FrameList[i].jointDict[JointType.SpineBase];
                Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                Point3D ankleLeft = this.FrameList[i].jointDict[JointType.AnkleLeft];
                Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                Point3D handLeft = this.FrameList[i].jointDict[JointType.HandLeft];
                Point3D spineMid = this.FrameList[i].jointDict[JointType.SpineMid];
                Vector3 spineShoulderBaseVec = new Vector3(spineShoulder, spineBase);
                if (spineShoulder.X != 0)
                {
                    spineShoulderBaseCount++;
                    this.spineShoulderBaseDiff += spineShoulderBaseVec.d;
                }
                if (ankleRight.Z != 0 && this.initAnkleRightZ == 0)
                    this.initAnkleRightZ = ankleRight.Z;
                if (ankleLeft.Z != 0 && this.initAnkleLeftZ == 0)
                    this.initAnkleLeftZ = ankleLeft.Z;
                if(handSpineMidCount < 3)
                {
                    this.initHandRightSpineMidXDiff += Math.Abs(handRight.X - spineMid.X);
                    this.initHandLeftSpineMidXDiff += Math.Abs(handLeft.X - spineMid.X);
                    handSpineMidCount++;
                }
            }
            this.spineShoulderBaseDiff /= spineShoulderBaseCount;
            this.initHandRightSpineMidXDiff /= 3;
            this.initHandLeftSpineMidXDiff /= 3;
        }

        private int CheckWristUp(int nowFrame)
        {
            int steadyCount = 0;
            int errorFrame = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                if(this.handedness == "right")
                {
                    Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                    Point3D handTipRight = this.FrameList[i].jointDict[JointType.HandTipRight];
                    double handTipHandYDiff = handTipRight.Y - handRight.Y;
                    steadyCount++;
                    if (handTipHandYDiff <= 0.01)
                    {
                        if (i < errorFrame + 5)
                            steadyCount = i - errorFrame;
                        errorFrame = i;
                    }
                    Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                    double stepSpineRatio = (this.initAnkleRightZ - ankleRight.Z) / this.spineShoulderBaseDiff;
                    if (steadyCount > 2 && stepSpineRatio <= 0.75)
                        return Record(i, "持拍立腕");
                }
                else
                {
                    Point3D handLeft = this.FrameList[i].jointDict[JointType.HandLeft];
                    Point3D handTipLeft = this.FrameList[i].jointDict[JointType.HandTipLeft];
                    double handTipHandYDiff = handTipLeft.Y - handLeft.Y;
                    steadyCount++;
                    if (handTipHandYDiff <= 0.01)
                    {
                        if (i < errorFrame + 5)
                            steadyCount = i - errorFrame;
                        errorFrame = i;
                    }
                    Point3D ankleLeft = this.FrameList[i].jointDict[JointType.AnkleLeft];
                    double stepSpineRatio = (this.initAnkleRightZ - ankleLeft.Z) / this.spineShoulderBaseDiff;
                    if (steadyCount > 2 && stepSpineRatio <= 0.75)
                        return Record(i, "持拍立腕");
                }
            }
            return this.FrameList.Count;
        }

        private int CheckWristTurnAndStepForward(int nowFrame)
        {
            bool wristTurned = false;
            bool steppedForward = false;
            int cp = 0;

            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D spineMid = this.FrameList[i].jointDict[JointType.SpineMid];
                if (this.handedness == "right" && !wristTurned)
                {
                    Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                    if (handRight.X - spineMid.X < this.initHandRightSpineMidXDiff / 6)
                    {
                        cp = i;
                        wristTurned = true;
                        Record(i, "手腕轉動");
                    }
                }
                else if (this.handedness == "left" && !wristTurned)
                {
                    Point3D handLeft = this.FrameList[i].jointDict[JointType.HandLeft];
                    if (spineMid.X - handLeft.X < this.initHandLeftSpineMidXDiff / 6)
                    {
                        cp = i;
                        wristTurned = true;
                        Record(i, "手腕轉動");
                    }
                }

                if (this.handedness == "right" && !steppedForward)
                {
                    Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                    double stepSpineRatio = (this.initAnkleRightZ - ankleRight.Z) / this.spineShoulderBaseDiff;
                    if (stepSpineRatio > 0.75)
                    {
                        cp = i;
                        steppedForward = true;
                        Record(i, "慣用腳跨步");
                    }
                }
                else if(this.handedness == "left" && !steppedForward)
                {
                    Point3D ankleLeft = this.FrameList[i].jointDict[JointType.AnkleLeft];
                    double stepSpineRatio = (this.initAnkleRightZ - ankleLeft.Z) / this.spineShoulderBaseDiff;
                    if (stepSpineRatio > 0.75)
                    {
                        cp = i;
                        steppedForward = true;
                        Record(i, "慣用腳跨步");
                    }
                }
                if(wristTurned && steppedForward)
                {
                    return cp;
                }
            }
            return this.FrameList.Count;
        }

        private int CheckWristTurn(int nowFrame)
        {
            
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D spineMid = this.FrameList[i].jointDict[JointType.SpineMid];
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                Point3D spineBase = this.FrameList[i].jointDict[JointType.SpineBase];
                if (this.handedness == "right")
                {
                    Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                    if (handRight.X - spineMid.X< this.initHandRightSpineMidXDiff / 5)
                    {
                        return Record(i, "手腕轉動");
                    }
                }
                else if(this.handedness == "left")
                {
                    Point3D handLeft = this.FrameList[i].jointDict[JointType.HandRight];
                    if (spineMid.X - handLeft.X > this.initHandLeftSpineMidXDiff / 5)
                    {
                        return Record(i, "手腕轉動");
                    }
                }
            }
            return this.FrameList.Count;
        }

        private int CheckStepForward(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                Point3D spineBase = this.FrameList[i].jointDict[JointType.SpineBase];
                if (this.handedness == "right")
                {
                    Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                    double stepSpineRatio = (this.initAnkleRightZ - ankleRight.Z) / this.spineShoulderBaseDiff;
                    if (stepSpineRatio > 0.75)
                        return Record(i, "慣用腳跨步");
                }
                else
                {
                    Point3D ankleLeft = this.FrameList[i].jointDict[JointType.AnkleLeft];
                    double stepSpineRatio = (this.initAnkleRightZ - ankleLeft.Z) / this.spineShoulderBaseDiff;
                    if (stepSpineRatio > 0.75)
                        return Record(i, "慣用腳跨步");
                }
            }
            return this.FrameList.Count;
        }

        private int CheckFootGroundAndWristForce(int nowFrame)
        {
            int steadyCount = 0;
            int errorFrame = 0;
            Point3D prevAnkleRight = new Point3D(0, 0, 0);
            Point3D prevAnkleLeft = new Point3D(0, 0, 0);

            int nowResult = 0;
            int prevResult = 0;

            bool FootGrounded = false;
            bool WristForced = false;
            int recordFrame = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                if (this.handedness == "right")
                {
                    Point3D footRight = this.FrameList[i].jointDict[JointType.FootRight];
                    Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                    Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                    Point3D wristRight = this.FrameList[i].jointDict[JointType.WristRight];
                    Point3D handTipRight = this.FrameList[i].jointDict[JointType.HandTipRight];
                    Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                    Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                    Vector3 ankleMovement = new Vector3(ankleRight, prevAnkleRight);
                    steadyCount++;
                    if (Math.Abs(ankleMovement.d) > 0.01)
                    {
                        if (i < errorFrame + 5)
                            steadyCount = i - errorFrame;
                        errorFrame = i;
                    }
                    if (steadyCount >= 5 && !FootGrounded)
                    {
                        recordFrame = Record(i - 7, "腳跟著地");
                        FootGrounded = true;
                    }
                    prevAnkleRight = ankleRight;

                    nowResult = CheckSide(elbowRight, wristRight, handTipRight);
                    Debug(i, nowResult);
                    if (nowResult < 0 && prevResult > 0 && handTipRight.X > spineShoulder.X && !WristForced)
                    {
                        recordFrame = Record(i, "手腕發力");
                        WristForced = true;
                    }
                    if (FootGrounded && WristForced)
                    {
                        return recordFrame;
                    }
                    prevResult = nowResult;
                }
                else
                {
                    Point3D footLeft = this.FrameList[i].jointDict[JointType.FootLeft];
                    Point3D ankleLeft = this.FrameList[i].jointDict[JointType.AnkleLeft];
                    Point3D elbowLeft = this.FrameList[i].jointDict[JointType.ElbowLeft];
                    Point3D wristLeft = this.FrameList[i].jointDict[JointType.WristLeft];
                    Point3D handTipLeft = this.FrameList[i].jointDict[JointType.HandTipLeft];
                    Point3D handLeft = this.FrameList[i].jointDict[JointType.HandLeft];
                    Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                    Vector3 ankleMovement = new Vector3(ankleLeft, prevAnkleLeft);
                    steadyCount++;
                    if (Math.Abs(ankleMovement.d) > 0.01)
                    {
                        if (i < errorFrame + 5)
                            steadyCount = i - errorFrame;
                        errorFrame = i;
                    }
                    if (steadyCount >= 5 && !FootGrounded)
                    {
                        recordFrame = Record(i - 7, "腳跟著地");
                        FootGrounded = true;
                    }
                    prevAnkleLeft = ankleLeft;

                    nowResult = CheckSide(elbowLeft, wristLeft, handTipLeft);
                    if (nowResult > 0 && prevResult < 0 && handTipLeft.X < spineShoulder.X && !WristForced)
                    {
                        recordFrame = Record(i, "手腕發力");
                        WristForced = true;
                    }
                    if (FootGrounded && WristForced)
                    {
                        return recordFrame;
                    }
                    prevResult = nowResult;
                }
            }
            return this.FrameList.Count;
        }
    }
}
