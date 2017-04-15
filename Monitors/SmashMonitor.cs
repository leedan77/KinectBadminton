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
        private String[] goals = { "雙手手肘抬高", "雙手平衡", "側身", "手肘轉向前", "手腕發力", "肩膀向前收拍" };
        private double leftInitShoulderAngle = 0;
        private double rightInitShoulderAngle = 0;

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
            int shoulderAngleCount = 0;
            int shoulderAngleCountTo = 1;
            double leftShoulderAngleSum = 0;
            double rightShoulderAngleSum = 0;
            Console.WriteLine(nowFrame);
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D shoulderLeft = GetJoint(i, JointType.ShoulderLeft);
                Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
                //JointType[] joints = { JointType.ShoulderRight, JointType.ShoulderLeft };
                //Console.WriteLine(GetInferred(i, joints));
                if (shoulderAngleCount < shoulderAngleCountTo)
                {
                    leftShoulderAngleSum += GetAngle3D(shoulderLeft, shoulderRight, new Point3D(shoulderRight.X, shoulderRight.Y, shoulderRight.Z - 10));
                    rightShoulderAngleSum += GetAngle3D(shoulderRight, shoulderLeft, new Point3D(shoulderLeft.X, shoulderLeft.Y, shoulderLeft.Z - 10));
                    shoulderAngleCount++;
                }
            }
            rightInitShoulderAngle = rightShoulderAngleSum / shoulderAngleCountTo;
            leftInitShoulderAngle = leftShoulderAngleSum / shoulderAngleCountTo;
        }

        private int CheckElbowsUpBodySideArmBalance (int nowFrame)
        {
            bool elbowUp = false;
            bool bodySide = false;
            bool armBalance = false;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                int frame = 0;
                if (CheckElbowsUp(i) && !elbowUp)
                {
                    frame = Record(i, "雙手手肘抬高");
                    elbowUp = true;
                }
                if (CheckBodySide(i) && !bodySide)
                {
                    frame = Record(i, "側身");
                    bodySide = true;
                }
                if (CheckArmBalance(i) && !armBalance && elbowUp)
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

        private bool CheckElbowsUp(int i)
        {
            Point3D elbowRight = GetJoint(i, JointType.ElbowRight);
            Point3D elbowLeft = GetJoint(i, JointType.ElbowLeft);
            Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
            Point3D shoulderLeft = GetJoint(i, JointType.ShoulderLeft);
            Point3D spineShoulder = GetJoint(i, JointType.SpineShoulder);
            JointType[] joints = { JointType.ShoulderRight, JointType.ShoulderLeft, JointType.ElbowRight, JointType.ElbowLeft, JointType.SpineShoulder };
            double rightAngle = GetAngle2D(elbowRight, shoulderRight, spineShoulder);
            double leftAngle = GetAngle2D(elbowLeft, shoulderLeft, spineShoulder);

            double leftVertAngle = GetAngle2D(elbowLeft, shoulderLeft, new Point3D(shoulderLeft.X, shoulderLeft.Y - 10, shoulderLeft.Z));
            double rightVertAngle = GetAngle2D(elbowRight, shoulderRight, new Point3D(shoulderRight.X, shoulderRight.Y - 10, shoulderRight.Z));
            if (this.handedness == "right")
            {
                if (!GetInferred(i, joints) && (rightAngle > 145 || elbowRight.Y > shoulderRight.Y) && (leftAngle > 160 || elbowLeft.Y > shoulderRight.Y) && leftVertAngle > 60 && rightVertAngle > 45)
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (!GetInferred(i, joints) && (rightAngle > 160 || elbowRight.Y > shoulderRight.Y) && (leftAngle > 145 || elbowLeft.Y > shoulderRight.Y) && rightVertAngle > 60 && leftVertAngle > 45)
                {
                    return true;
                }
                return false;
            }
        }

        private bool CheckBodySide(int i)
        {
            Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
            Point3D shoulderLeft = GetJoint(i, JointType.ShoulderLeft);
            double rightAngle = GetAngle3D(shoulderRight, shoulderLeft, new Point3D(shoulderLeft.X, shoulderLeft.Y, shoulderLeft.Z - 10));
            double leftAngle = GetAngle3D(shoulderLeft, shoulderRight, new Point3D(shoulderRight.X , shoulderRight.Y, shoulderRight.Z - 10));
            JointType[] joints = { JointType.ShoulderRight, JointType.ShoulderLeft};

            Point3D footRight = GetJoint(i, JointType.FootRight);
            Point3D footLeft = GetJoint(i, JointType.FootLeft);

            if (this.handedness == "right")
            {
                if (!GetInferred(i, joints))
                {
                    Point3D handRight = GetJoint(i, JointType.HandRight);
                    Point3D kneeRight = GetJoint(i, JointType.KneeRight);
                    if (rightAngle - rightInitShoulderAngle > 55 && footRight.Z > footLeft.Z)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                if (!GetInferred(i, joints))
                {
                    if (leftAngle - leftInitShoulderAngle > 55 && footLeft.Z > footRight.Z)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private bool CheckArmBalance(int i)
        {
            Point3D handRight = GetJoint(i, JointType.HandRight);
            Point3D handLeft = GetJoint(i, JointType.HandLeft);
            Point3D elbowRight = GetJoint(i, JointType.ElbowRight);
            Point3D elbowLeft = GetJoint(i, JointType.ElbowLeft);
            Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
            Point3D shoulderLeft = GetJoint(i, JointType.ShoulderLeft);
            double rightAngle = GetAngle2D(handRight, elbowRight, new Point3D(elbowRight.X, elbowRight.Y + 10, elbowRight.Z));
            double leftAngle = GetAngle2D(handLeft, elbowLeft, new Point3D(elbowLeft.X, elbowLeft.Y + 10, elbowLeft.Z));

            double rightLeftShoulderAngle = GetAngle2D(shoulderLeft, shoulderRight, new Point3D(shoulderRight.X - 10, shoulderRight.Y, shoulderRight.Z));
            double leftRightShoulderAngle = GetAngle2D(shoulderRight, shoulderLeft, new Point3D(shoulderLeft.X + 10, shoulderLeft.Y, shoulderLeft.Z));
            if (this.handedness == "right")
            {
                if (rightAngle < 40 && handRight.Y > elbowRight.Y && leftAngle < 40 && handLeft.Y > elbowLeft.Y && CompareAfter(i) && rightLeftShoulderAngle < 5)
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (rightAngle < 40 && handRight.Y > elbowRight.Y && leftAngle < 40 && handLeft.Y > elbowLeft.Y && CompareAfter(i) && leftRightShoulderAngle < 5)
                {
                    return true;
                }
                return false;
            }
        }

        private bool CompareAfter(int frame)
        {
            JointType jointType;
            if (this.handedness == "right")
            {
                jointType = JointType.HandLeft;
            }
            else
            {
                jointType = JointType.HandRight;
            }
            if (frame < this.FrameList.Count - 6)
            {
                for (int i = frame + 1; i <= frame + 5; i++)
                {
                    if (GetJoint(frame, jointType).Y < GetJoint(i, jointType).Y)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private int CheckElbowForward(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D elbowRight = GetJoint(i, JointType.ElbowRight);
                Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
                Point3D elbowLeft = GetJoint(i, JointType.ElbowLeft);
                Point3D shoulderLeft = GetJoint(i, JointType.ShoulderLeft);
                Point3D spineShoulder = GetJoint(i, JointType.SpineShoulder);
                if (this.handedness == "right")
                {
                    if (elbowRight.Z < shoulderRight.Z)
                    {
                        return Record(i, "手肘轉向前");
                    }
                }
                else
                {
                    if (elbowLeft.Z < spineShoulder.Z)
                    {
                        return Record(i, "手肘轉向前");
                    }
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
                Point3D wristLeft = GetJoint(i, JointType.WristLeft);
                Point3D handTipLeft = GetJoint(i, JointType.HandTipLeft);
                Point3D handLeft = GetJoint(i, JointType.HandLeft);
                if (this.handedness == "right")
                {
                    JointType[] joints = { JointType.WristRight, JointType.HandTipRight, JointType.HandRight };
                    if (!GetInferred(i, joints))
                    {
                        if (wristRight.Y > handTipRight.Y && handRight.X < handLeft.X)
                        {
                            return Record(i, "手腕發力");
                        }
                    }
                }
                else
                {
                    JointType[] joints = { JointType.WristLeft, JointType.HandTipLeft, JointType.HandLeft };
                    if (!GetInferred(i, joints))
                    {
                        if (wristLeft.Y > handTipLeft.Y && handLeft.X < handRight.X)
                        {
                            return Record(i, "手腕發力");
                        }
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
                Point3D handLeft = GetJoint(i, JointType.HandLeft);
                Point3D spineBase = GetJoint(i, JointType.SpineBase);
                Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
                Point3D spineShoulder = GetJoint(i, JointType.SpineShoulder);
                Point3D elbowRight = GetJoint(i, JointType.ElbowRight);
                Point3D spineMid = GetJoint(i, JointType.SpineMid);
                Point3D footLeft = GetJoint(i, JointType.FootLeft);
                Point3D hipRight = GetJoint(i, JointType.HipRight);
                Point3D handTipRight = GetJoint(i, JointType.HandTipRight);
                Point3D shoulderLeft = GetJoint(i, JointType.ShoulderLeft);
                if (this.handedness == "right")
                {
                    JointType[] joints = { JointType.HandRight, JointType.ShoulderRight, JointType.SpineBase };
                    if (GetAngle2D(handRight, shoulderRight, spineBase) < 40)
                    {
                        return Record(i, "肩膀向前收拍");
                    }
                }
                else
                {
                    JointType[] joints = { JointType.HandLeft, JointType.ShoulderLeft, JointType.SpineBase };
                    if (GetAngle2D(handLeft, shoulderLeft, spineBase) < 40)
                    {
                        return Record(i, "肩膀向前收拍");
                    }
                }
            }
            return this.FrameList.Count;
        }
    }
}
