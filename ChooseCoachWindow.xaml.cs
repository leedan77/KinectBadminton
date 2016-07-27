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
        const string coachDataPath = "\\..\\..\\..\\..\\coach_data";

        private string coachDataType;
        public string CoachDataType
        {
            get
            {
                return coachDataType;
            }
            set
            {
                coachDataType = value;
                dirInfo = new DirectoryInfo(cur + coachDataPath + coachDataType);
                fileInfo = dirInfo.GetFiles("*.avi*");
                list = new ArrayList();
                foreach (FileInfo f in fileInfo)
                {
                    list.Add(f.Name.Remove(f.Name.Length - 4));
                }
                CoachListBox.ItemsSource = list;
            }
        }

        public ChooseCoachWindow()
        {
            InitializeComponent();
            CoachDataType = "\\color";           
           
        }

      

        private void CoachListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var parent = this.Owner as MainWindow;
            parent.CoachFileName = fileInfo[CoachListBox.SelectedIndex].FullName;
            Console.WriteLine(fileInfo[CoachListBox.SelectedIndex].FullName);
            this.Close();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (colorRadio.IsChecked == true)
            {
                Console.WriteLine("color");
                CoachDataType = "\\color";
            }
            else
            {
                Console.WriteLine("body");
                CoachDataType = "\\body";
            }
        }
    }
}
