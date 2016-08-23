using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    /// <summary>
    /// ChooseCoachWindow.xaml 的互動邏輯
    /// </summary>
    public partial class ChooseCoachWindow : Window
    {
        DirectoryInfo dirInfo;
        FileInfo[] fileInfo;
        ArrayList list;
        string cur = Environment.CurrentDirectory;
        public string action_type;
        private string menuType;
        public string MenuType
        {
            get
            {
                return $"\\..\\..\\..\\data\\{menuType}\\{action_type}";
            }
            set
            {
                menuType = value;
                dirInfo = new DirectoryInfo(cur + this.MenuType);
                list = new ArrayList();
                DirectoryInfo[] subDir = dirInfo.GetDirectories();
                foreach (DirectoryInfo d in subDir)
                {
                    list.Add(d.Name);
                }
                CoachListBox.ItemsSource = list;
            }
        }

        private string dataType;
        public string DataType
        {
            get
            {
                return dataType;
            }
            set
            {
                dataType = value;
                //Console.WriteLine(MenuType);
                ////dirInfo = new DirectoryInfo(cur + coachDataPath + coachDataType);
                //dirInfo = new DirectoryInfo(cur + this.MenuType);
                //fileInfo = dirInfo.GetFiles("*.avi*");
                ////fileInfo = dirInfo.GetFiles("*.json*");
                //list = new ArrayList();
                //foreach (FileInfo f in fileInfo)
                //{
                //    list.Add(f.Name.Remove(f.Name.Length - ".json".Length));
                //}
                //CoachListBox.ItemsSource = list;
            }
        }

        public ChooseCoachWindow(string type, string action_type)
        {
            InitializeComponent();
            this.action_type = action_type;
            this.MenuType = type;         
           
        }

      

        private void CoachListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var parent = this.Owner as MainWindow;
            string selectedItem = CoachListBox.SelectedItem.ToString();
            if (this.menuType == "coach") 
                parent.CoachFileName = selectedItem;
            else if(this.menuType == "student")
                parent.StudentFileName = selectedItem;
            parent.LoadJudgement(selectedItem, action_type, menuType);
            this.Close();
        }

    }
}
