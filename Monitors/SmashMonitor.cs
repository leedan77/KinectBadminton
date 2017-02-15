using Microsoft.Kinect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics.Monitors
{
    class SmashMonitor:Monitor
    {
        private double hipMaxDiff = 0;
        private double initRightShoulderElbowDiff = 0;
        private double headNeckDiff = 0;
        private double elbowSpineMaxDiff = 0;
        private String[] goals = { "雙手手肘抬高", "雙手平衡", "側身", "手肘轉向前", "手腕發力", "肩膀向前收拍" };

        public SmashMonitor(List<Frames> frameList, String handedness)
        {
            this.FrameList = frameList;
            this.result = new List<CriticalPoint>();
            this.handedness = handedness;
            initCriticalPoints(goals);
        }

        public override void Start()
        {
            int nowFrame = 0;
            nowFrame = FromKeyExist();
            GenerateCompareData(nowFrame);
            nowFrame = CheckElbowsUpBodySideArmBalance(nowFrame);
            nowFrame = CheckElbowForward(nowFrame);
            //nowFrame = CheckWristForward(nowFrame);
            //nowFrame = CheckElbowEnded(nowFrame);
        }

        public override void GenerateCompareData(int nowFrame)
        {
            int headNeckCount = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D hipRight = this.FrameList[i].jointDict[JointType.HipRight];
                Point3D hipLeft = this.FrameList[i].jointDict[JointType.HipLeft];
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                Point3D shoulderRight = this.FrameList[i].jointDict[JointType.ShoulderRight];
                Point3D head = this.FrameList[i].jointDict[JointType.Head];
                Point3D neck = this.FrameList[i].jointDict[JointType.Neck];
                Point3D spineMid = this.FrameList[i].jointDict[JointType.SpineMid];
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

        private int CheckElbowsUpBodySideArmBalance (int nowFrame)
        {
            bool elbowUp = false;
            bool bodySide = false;
            bool armBalance = false;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D elbowRight = GetJoint(i, JointType.ElbowRight);
                Point3D elbowLeft = GetJoint(i, JointType.ElbowLeft);
                Point3D spineShoulder = GetJoint(i, JointType.SpineShoulder);
                Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
                Point3D shoulderLeft = GetJoint(i, JointType.ShoulderLeft);
                Point3D hipRight = GetJoint(i, JointType.HipRight);
                Point3D hipLeft = GetJoint(i, JointType.HipLeft);
                Point3D handRight = GetJoint(i, JointType.HandRight);
                Point3D handLeft = GetJoint(i, JointType.HandLeft);

                int frame = 0;
                if (CheckElbowsUp(elbowRight, elbowLeft, shoulderRight, shoulderLeft, spineShoulder) && !elbowUp)
                {
                    frame = Record(i, "雙手手肘抬高");
                    elbowUp = true;
                }
                if (CheckBodySide(shoulderRight, shoulderLeft) && !bodySide)
                {
                    frame = Record(i, "側身");
                    bodySide = true;
                }
                if (CheckArmBalance(handRight, handLeft, elbowRight, elbowLeft, i) && !armBalance && elbowUp)
                {
                    frame = Record(i, "雙手平衡");
                    armBalance = true;
                }
                if (elbowUp && bodySide && armBalance)
                {
                    return frame;
                }
            }
            return this.FrameList.Count;
        } 

        private bool CheckElbowsUp(Point3D elbowRight, Point3D elbowLeft, Point3D shoulderRight, Point3D shoulderLeft, Point3D spineShoulder)
        {
            double rightAngle = GetAngle2D(elbowRight, shoulderRight, spineShoulder);
            double leftAngle = GetAngle2D(elbowLeft, shoulderLeft, spineShoulder);
            
            if (rightAngle > 160 && leftAngle > 145)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckBodySide(Point3D shoulderRight, Point3D shoulderLeft)
        {
            double rightAngle = GetAngle3D(shoulderRight, shoulderLeft, new Point3D(shoulderLeft.X + 10, shoulderLeft.Y, shoulderLeft.Z));
            if (rightAngle > 25 && shoulderRight.Z > shoulderLeft.Z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckArmBalance(Point3D handRight, Point3D handLeft, Point3D elbowRight, Point3D elbowLeft, int frame = 0)
        {
            double rightAngle = GetAngle2D(handRight, elbowRight, new Point3D(elbowRight.X, elbowRight.Y + 10, elbowRight.Z));
            double leftAngle = GetAngle2D(handLeft, elbowLeft, new Point3D(elbowLeft.X, elbowLeft.Y + 10, elbowLeft.Z));
            if (rightAngle < 40 && handRight.Y > elbowRight.Y && leftAngle < 40 && handLeft.Y > elbowLeft.Y)
            {
                return true;
            }
            return false;
        }

        private int CheckElbowForward(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D elbowRight = GetJoint(i, JointType.ElbowRight);
                Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
                Debug(i, elbowRight.Z - shoulderRight.Z);
                if (elbowRight.Z < shoulderRight.Z)
                {
                    return Record(i, "手肘轉向前");
                }
            }
            return this.FrameList.Count;
        }

        //private int CheckWristForward(int nowFrame)
        //{
        //    double prevHandtipWristDiff = 0, nowHandtipWristDiff = 0;
        //    for (int i = nowFrame; i < this.FrameList.Count; i++)
        //    {
        //        Point3D handTipRight = this.FrameList[i].jointDict[JointType.HandTipRight];
        //        Point3D wristRight = this.FrameList[i].jointDict[JointType.WristRight];
        //        nowHandtipWristDiff = handTipRight.Y - wristRight.Y;
        //        if (prevHandtipWristDiff - nowHandtipWristDiff > this.headNeckDiff / 3 && prevHandtipWristDiff != 0)
        //        {
        //            return Record(i, "手腕發力");
        //        }
        //        prevHandtipWristDiff = nowHandtipWristDiff;
        //    }
        //    return this.FrameList.Count;
        //}
        //private int CheckElbowEnded(int nowFrame)
        //{
        //    for (int i = nowFrame; i < this.FrameList.Count; i++)
        //    {
        //        Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
        //        Point3D spineMid = this.FrameList[i].jointDict[JointType.SpineMid];
        //        double elbowSpineDiff = Math.Abs(elbowRight.X - spineMid.X);
        //        if (elbowSpineDiff < elbowSpineMaxDiff / 6)
        //        {
        //            return Record(i, "收拍");
        //        }

        //    }
        //    return this.FrameList.Count;
        //}
    }
}
