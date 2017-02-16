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
            nowFrame = CheckWristForward(nowFrame);
            nowFrame = CheckElbowEnded(nowFrame);
        }

        public override void GenerateCompareData(int nowFrame)
        {
            
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
                if (elbowRight.Z < shoulderRight.Z)
                {
                    return Record(i, "手肘轉向前");
                }
            }
            return this.FrameList.Count;
        }

        private int CheckWristForward(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D wristRight = GetJoint(i, JointType.WristRight);
                Point3D handTipRight = GetJoint(i, JointType.HandTipRight);
                Point3D handRight = GetJoint(i, JointType.HandRight);
                Point3D handLeft = GetJoint(i, JointType.HandLeft);
                JointType[] joints = { JointType.WristRight, JointType.HandTipRight, JointType.HandRight };
                if (!GetInferred(i, joints))
                {
                    if (wristRight.Y > handTipRight.Y && handRight.X < handLeft.X)   
                    {
                        return Record(i, "手腕發力");
                    }
                }
            }
            return this.FrameList.Count;
        }

        private int CheckElbowEnded(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D handRight = GetJoint(i, JointType.HandRight);
                Point3D spineBase = GetJoint(i, JointType.SpineBase);
                JointType[] joints = { JointType.HandRight, JointType.SpineBase };
                if (handRight.Y < spineBase.Y && !GetInferred(i, joints))
                {
                    return Record(i, "肩膀向前收拍");
                }
            }
            return this.FrameList.Count;
        }

        //private int CheckElbowEnded(int nowFrame)
        //{
        //    for (int i = nowFrame; i < this.FrameList.Count; i++)
        //    {
        //        Point3D elbo wRight = this.FrameList[i].jointDict[JointType.ElbowRight];
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
