using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    class ServeMonitor
    {
        private List<Frames> FrameList;
        public ServeMonitor()
        {
            String jsonString = File.ReadAllText("../../../data/data.json");
            FrameList = JsonConvert.DeserializeObject<List<Frames>>(jsonString);
        }
        public void start()
        {
            GenerateCompareData();
            int nowFrame = 0;
            nowFrame = CheckBalancePoint(nowFrame);
        }
        private void GenerateCompareData()
        {
            
        }
        private int CheckBalancePoint(int nowFrame)
        {
            for(int i = nowFrame; i < FrameList.Count; i++)
            {
                Console.WriteLine("Frame: " + i);
                double hipRight = 0, kneeRight = 0, ankleRight = 0;
                foreach (Joints joint in FrameList[i].jointList)
                {
                    if (string.Compare(joint.jointType, "HipRight") == 0)
                    {
                        hipRight = joint.x;
                        Console.WriteLine(hipRight);
                    }
                    else if (string.Compare(joint.jointType, "KneeRight") == 0)
                    {
                        kneeRight = joint.x;
                        Console.WriteLine(kneeRight);
                    }
                    else if (string.Compare(joint.jointType, "AnkleRight") == 0)
                    {
                        ankleRight = joint.x;
                        Console.WriteLine(ankleRight);
                    }
                }
            }
            return FrameList.Count;
        }
    }
}
