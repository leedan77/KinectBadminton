﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics.Monitors
{
    class Monitor
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

        public struct Vector2
        {
            public double x, y, d;

            public Vector2(Point3D point1, Point3D point2)
            {
                x = point1.X - point2.X;
                y = point1.Y - point2.Y;
                d = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
            }
        }

        public struct Vector3
        {
            public double x, y, z, d;

            public Vector3(Point3D point1, Point3D point2)
            {
                x = point1.X - point2.X;
                y = point1.Y - point2.Y;
                z = point1.Z - point2.Z;
                d = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
            }
        }


        public List<Frames> FrameList;
        public List<CriticalPoint> result;
        public int videoCount = 0;
        public String handedness;

        public virtual void Start()
        {

        }

        public virtual void GenerateCompareData(int nowFrame)
        {

        }

        public void initCriticalPoints(String[] cp)
        {
            for(int i = 0; i < cp.Length; i++)
            {
                this.result.Add(new CriticalPoint(cp[i], 100));
            }
        }

        public void Debug(int i, double value)
        {
            Console.WriteLine($"Frame: {i}, {value}");
        }

        public int Record(int frame, String criticalPoint)
        {
            Console.WriteLine(frame + " " + criticalPoint);
            for (int i = 0; i < this.result.Count; i++)
            {
                if (this.result[i].name == criticalPoint)
                    this.result[i] = new CriticalPoint(criticalPoint, (double)frame / videoCount);
            }
            //this.result.Add(new CriticalPoint(criticalPoint, (double)frame / videoCount));
            return frame;
        }

        public int CheckSide(Point3D lineCoord1, Point3D lineCoord2, Point3D checkPoint)
        {
            double a = (lineCoord2.Y - lineCoord1.Y) / (lineCoord2.X - lineCoord1.X);
            double b = lineCoord1.Y - a * lineCoord1.X;
            double result = checkPoint.Y - a * checkPoint.X - b;
            if ((a > 0 && result > 0 && checkPoint.X < lineCoord1.X) || (a > 0 && result < 0 && checkPoint.X > lineCoord1.X)
                || (a < 0 && result < 0 && checkPoint.Y < lineCoord1.Y) || (a < 0 && result > 0 && checkPoint.Y > lineCoord1.Y))
                return 1;
            return -1;
        }

        public List<CriticalPoint> GetResult()
        {
            this.result.Sort(delegate (CriticalPoint cp1, CriticalPoint cp2) { return cp1.portion.CompareTo(cp2.portion); });
            return this.result;
        }

        public int FromKeyExist()
        {
            for (int i = 0; i < this.FrameList.Count; i++)
            {
                if (this.FrameList[i].jointDict.Count != 0)
                {
                    return i;
                }
            }
            return this.FrameList.Count;
        }
    }
}
