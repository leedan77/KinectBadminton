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
    class ServeMonitor:Monitor
    {
        private double hipWidthWhenBalanceChange = 0;
        private String[] goals = { "重心腳在右腳" , "重心轉移到左腳", "轉腰" , "手腕發力" , "手肘向前" };

        public ServeMonitor(List<Frames> frameList, bool handedness, int videoCount)
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
            nowFrame = CheckBalancePoint(nowFrame, "right");
            nowFrame = CheckBalancePoint(nowFrame, "left");
            nowFrame = CheckWaistTwist(nowFrame);
            nowFrame = CheckWristForward(nowFrame);
            nowFrame = CheckElbowEnded(nowFrame);
        }

        private int CheckBalancePoint(int nowFrame, String side)
        {
            for(int i = nowFrame; i < FrameList.Count; i++)
            {
                Point3D hipRight = this.FrameList[i].jointDict[JointType.HipRight];
                Point3D kneeRight = this.FrameList[i].jointDict[JointType.KneeRight];
                Point3D ankleRight = this.FrameList[i].jointDict[JointType.AnkleRight];
                Point3D hipLeft = this.FrameList[i].jointDict[JointType.HipLeft];
                Point3D kneeLeft = this.FrameList[i].jointDict[JointType.KneeLeft];
                Point3D ankleLeft = this.FrameList[i].jointDict[JointType.AnkleLeft];

                Vector2 hipAngleRight = new Vector2(ankleRight, hipRight);
                Vector2 hipAngleLeft = new Vector2(ankleLeft, hipLeft);
                Vector2 horizentalLine = new Vector2(new Point3D(10, 0, 0), new Point3D(0, 0, 0));

                double hipAngleRightHorAngle = Math.Acos((hipAngleRight.x * horizentalLine.x + hipAngleRight.y + horizentalLine.y) / (hipAngleRight.d * horizentalLine.d)) * 180 / Math.PI;
                double hipAngleLeftHorAngle = Math.Acos((hipAngleLeft.x * horizentalLine.x + hipAngleLeft.y + horizentalLine.y) / (hipAngleLeft.d * horizentalLine.d)) * 180 / Math.PI;
                
                if(string.Compare(side, "right") == 0)
                {
                    if(Math.Abs(hipAngleRightHorAngle - 90) < Math.Abs(hipAngleLeftHorAngle - 90) - 5)
                        return Record(i, "重心腳在右腳");
                }
                else if(string.Compare(side, "left") == 0)
                {
                    if (Math.Abs(hipAngleRightHorAngle - 90) > Math.Abs(hipAngleLeftHorAngle - 90) + 5)
                    {
                        this.hipWidthWhenBalanceChange = hipRight.X - hipLeft.X;
                        return Record(i, "重心轉移到左腳");
                    }
                }
            }
            return this.FrameList.Count;
        }

        private int CheckWaistTwist(int nowFrame)
        {
            double hipWidth = 0;
            for(int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D hipRight = this.FrameList[i].jointDict[JointType.HipRight];
                Point3D hipLeft = this.FrameList[i].jointDict[JointType.HipLeft];
                hipWidth = hipRight.X - hipLeft.X;
                if (hipRight.Z < hipLeft.Z)
                    return Record(i, "轉腰");
            }
            return this.FrameList.Count;
        }

        private int CheckWristForward(int nowFrame)
        {
            int nowResult = 0;
            int prevResult = 0;
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D handTipRight = this.FrameList[i].jointDict[JointType.HandTipRight];
                Point3D wristRight = this.FrameList[i].jointDict[JointType.WristRight];
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                Point3D spineBase = this.FrameList[i].jointDict[JointType.SpineBase];
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];

                nowResult = CheckSide(elbowRight, wristRight, handTipRight);
                double wristSpineAngle = CheckVec2Angle(spineShoulder, spineBase, wristRight);
                if (nowResult * prevResult < 0 && (wristSpineAngle < 20 || handTipRight.X < spineBase.X))
                    return Record(i, "手腕發力");
                prevResult = nowResult;
            }
            return this.FrameList.Count;

        }

        private int CheckElbowEnded(int nowFrame)
        {
            for (int i = nowFrame; i < this.FrameList.Count; i++)
            {
                Point3D shoulderRight = this.FrameList[i].jointDict[JointType.ShoulderRight];
                Point3D spineShoulder = this.FrameList[i].jointDict[JointType.SpineShoulder];
                Point3D handRight = this.FrameList[i].jointDict[JointType.HandRight];
                Point3D elbowRight = this.FrameList[i].jointDict[JointType.ElbowRight];
                if (elbowRight.Y > shoulderRight.Y)
                    return Record(i, "手肘向前");
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
