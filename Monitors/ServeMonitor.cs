﻿using Microsoft.Kinect;
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
    class ServeMonitor:Monitor
    {
        private double hipWidthWhenBalanceChange = 0;
        private String[] goals = { "重心腳在慣用腳" , "重心轉移至非慣用腳", "轉腰" , "手腕發力" , "肩膀轉向前" };

        public ServeMonitor(List<Frames> frameList, String handedness)
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
            nowFrame = CheckBalancePoint(nowFrame);
            //nowFrame = CheckBalancePoint(nowFrame, "left");
            nowFrame = CheckWaistTwist(nowFrame);
            nowFrame = CheckWristForward(nowFrame);
            nowFrame = CheckElbowEnded(nowFrame);
        }

        private int CheckBalancePoint(int nowFrame)
        {
            bool init = false;
            for(int i = nowFrame; i < FrameList.Count; i++)
            {
                Point3D hipRight = GetJoint(i, JointType.HipRight);
                Point3D kneeRight = GetJoint(i, JointType.KneeRight);
                Point3D ankleRight = GetJoint(i, JointType.AnkleRight);
                Point3D hipLeft = GetJoint(i, JointType.HipLeft);
                Point3D kneeLeft = GetJoint(i, JointType.KneeLeft);
                Point3D ankleLeft = GetJoint(i, JointType.AnkleLeft);

                Vector2 hipAngleRight = new Vector2(ankleRight, hipRight);
                Vector2 hipAngleLeft = new Vector2(ankleLeft, hipLeft);
                Vector2 horizentalLine = new Vector2(new Point3D(10, 0, 0), new Point3D(0, 0, 0));

                double hipAngleRightHorAngle = Math.Acos((hipAngleRight.x * horizentalLine.x + hipAngleRight.y + horizentalLine.y) / (hipAngleRight.d * horizentalLine.d)) * 180 / Math.PI;
                double hipAngleLeftHorAngle = Math.Acos((hipAngleLeft.x * horizentalLine.x + hipAngleLeft.y + horizentalLine.y) / (hipAngleLeft.d * horizentalLine.d)) * 180 / Math.PI;

                if (!init)
                {
                    if(this.handedness == "right")
                    {
                        if (Math.Abs(hipAngleRightHorAngle - 90) < Math.Abs(hipAngleLeftHorAngle - 90) - 5)
                        {
                            Record(i, "重心腳在慣用腳");
                            init = true;
                        }
                    }
                    else if(this.handedness == "left")
                    {
                        if (Math.Abs(hipAngleRightHorAngle - 90) > Math.Abs(hipAngleLeftHorAngle - 90) + 5)
                        {
                            Record(i, "重心腳在慣用腳");
                            init = true;
                        }
                    }
                }
                else
                {
                    if(this.handedness == "right")
                    {
                        if (Math.Abs(hipAngleRightHorAngle - 90) > Math.Abs(hipAngleLeftHorAngle - 90) + 5)
                        {
                            this.hipWidthWhenBalanceChange = hipRight.X - hipLeft.X;
                            return Record(i, "重心轉移至非慣用腳");
                        }
                    }
                    else if (this.handedness == "left")
                    {
                        if (Math.Abs(hipAngleRightHorAngle - 90) < Math.Abs(hipAngleLeftHorAngle - 90) - 5)
                        {
                            this.hipWidthWhenBalanceChange = hipRight.X - hipLeft.X;
                            return Record(i, "重心轉移至非慣用腳");
                        }
                    }
                }

            }
            return this.FrameList.Count;
        }

        private int CheckWaistTwist(int nowFrame)
        {
            for(int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D hipRight = GetJoint(i, JointType.HipRight);
                Point3D hipLeft = GetJoint(i, JointType.HipLeft);
                if (this.handedness == "right")
                {
                    if (hipRight.Z < hipLeft.Z)
                        return Record(i, "轉腰");
                }
                else if(this.handedness == "left")
                {
                    if (hipRight.Z > hipLeft.Z)
                        return Record(i, "轉腰");
                }
            }
            return this.FrameList.Count;
        }

        private int CheckWristForward(int nowFrame)
        {
            int nowResult = 0;
            int prevResult = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D handTipRight = GetJoint(i, JointType.HandTipRight);
                Point3D wristRight = GetJoint(i, JointType.WristRight);
                Point3D elbowRight = GetJoint(i, JointType.ElbowRight);
                Point3D handTipLeft = GetJoint(i, JointType.HandTipLeft);
                Point3D wristLeft = GetJoint(i, JointType.WristLeft);
                Point3D elbowLeft = GetJoint(i, JointType.ElbowLeft);
                Point3D spineBase = GetJoint(i, JointType.SpineBase);
                Point3D spineShoulder = GetJoint(i, JointType.SpineShoulder);

                if (this.handedness == "right")
                {
                    nowResult = CheckSide(elbowRight, wristRight, handTipRight);
                    double wristSpineAngle = CheckVec2Angle(spineShoulder, spineBase, wristRight);
                    if (nowResult * prevResult < 0 && (wristSpineAngle < 20 || handTipRight.X < spineBase.X))
                        return Record(i, "手腕發力");
                    prevResult = nowResult;
                }
                else if(this.handedness == "left")
                {
                    nowResult = CheckSide(elbowLeft, wristLeft, handTipLeft);
                    double wristSpineAngle = CheckVec2Angle(spineShoulder, spineBase, wristLeft);
                    if (nowResult * prevResult < 0 && (wristSpineAngle < 20 || handTipLeft.X > spineBase.X))
                        return Record(i, "手腕發力");
                    prevResult = nowResult;
                }
            }
            return this.FrameList.Count;

        }

        private int CheckElbowEnded(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D shoulderRight = GetJoint(i, JointType.ShoulderRight);
                Point3D elbowRight = GetJoint(i, JointType.ElbowRight);
                Point3D shoulderLeft = GetJoint(i, JointType.ShoulderLeft);
                Point3D elbowLeft = GetJoint(i, JointType.ElbowLeft);
                if (this.handedness == "right")
                {
                    if (elbowRight.Y > shoulderRight.Y)
                        return Record(i, "肩膀轉向前");
                }
                else if(this.handedness == "left")
                {
                    if (elbowLeft.Y > shoulderLeft.Y)
                        return Record(i, "肩膀轉向前");
                }
            }
            return this.FrameList.Count;
        }

        private double CheckVec2Angle(Point3D vertex, Point3D otherPoint1, Point3D otherPoint2)
        {
            Vector2 vector1 = new Vector2(otherPoint1, vertex);
            Vector2 vector2 = new Vector2(otherPoint2, vertex);
            double angle = Math.Acos((vector1.x * vector2.x + vector1.y * vector2.y) / (vector1.d * vector2.d)) * 180 / Math.PI;
            return angle;
        }
    }
}
