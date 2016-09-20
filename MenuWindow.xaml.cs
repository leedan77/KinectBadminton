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
    public partial class MenuWindow : Window
    {
        DirectoryInfo dirInfo;
        ArrayList list;
        string cur = Environment.CurrentDirectory;

        private string actionType;
        public string ActionType
        {
            get
            {
                return actionType;
            }
            set
            {
                actionType = value;
                if (value == "lob")
                    this.actionTypeChinese = "挑球";
                else if (value == "serve")
                    this.actionTypeChinese = "發球";
                else if (value == "smash")
                    this.actionTypeChinese = "殺球";
            }
        }
        private string actionTypeChinese;
        public string experiment;
        public string week;

        private string menuType;
        private string selectedItem = string.Empty;

        public string MenuType
        {
            get
            {
                if (menuType == "student")
                {
                    return $"\\..\\..\\..\\data\\{menuType}\\{experiment}\\{week}\\{ActionType}";
                }
                else
                {
                    return $"\\..\\..\\..\\data\\{menuType}\\{ActionType}";
                }
                //return $"\\..\\..\\..\\data\\{menuType}\\{action_type}";
            }
            set
            {
                menuType = value;
                //Console.WriteLine(this.MenuType);
                if (!Directory.Exists(cur + this.MenuType))
                {
                    Directory.CreateDirectory(cur + this.MenuType);
                }
                dirInfo = new DirectoryInfo(cur + this.MenuType);
                list = new ArrayList();
                DirectoryInfo[] subDir = dirInfo.GetDirectories();
                foreach (DirectoryInfo d in subDir)
                {
                    list.Add(d.Name);
                }
                MenuListBox.ItemsSource = list;
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
            }
        }

        public MenuWindow(string type, string action_type, string experiment, string week)
        {
            InitializeComponent();
            this.ActionType = action_type;
            this.experiment = experiment;
            this.week = week;
            this.MenuType = type;
           
        }

        private void MenuListBox__SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //var parent = this.Owner as MainWindow;
            //string selectedItem = MenuListBox.SelectedItem.ToString();
            //if (this.menuType == "coach")
            //    parent.RightVideoChoosen(selectedItem);
            //else if (this.menuType == "student")
            //    parent.LeftVideoChoosen(selectedItem);
            //parent.LoadJudgement(selectedItem, ActionType, menuType, experiment, week);
            //this.Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MenuListBox.SelectedItem != null)
            {
                MessageBoxResult messageBoxResult = MessageBox.Show(
                    $"刪除後無法還原，包含 {MenuListBox.SelectedItem.ToString()} {this.actionTypeChinese} 的彩色及骨架影片", 
                    "Delete Confirmation", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    string path = cur + this.MenuType + $"\\{MenuListBox.SelectedItem.ToString()}";
                    Directory.Delete(path, true);
                    this.MenuType = this.menuType;
                }
            }
            else
                MessageBox.Show("請先選擇欲刪除的項目", "選單");
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if(MenuListBox.SelectedItem != null)
            {
                var parent = this.Owner as MainWindow;
                string selectedItem = MenuListBox.SelectedItem.ToString();
                if (this.menuType == "coach")
                    parent.RightVideoChoosen(selectedItem);
                else if (this.menuType == "student")
                    parent.LeftVideoChoosen(selectedItem);
                parent.LoadJudgement(selectedItem, ActionType, menuType, experiment, week);
                this.Close();
            }
            else
                MessageBox.Show("請先選擇欲播放的項目", "選單");
        }
    }
}
