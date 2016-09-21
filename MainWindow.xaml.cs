﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <description>
// Demonstrates usage of the Kinect Tooling APIs, including basic record/playback functionality
// </description>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    using System;
    using System.Windows;
    using System.Windows.Threading;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    using System.Diagnostics;
    using System.IO;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Threading;
    using System.Text;
    using System.Collections;
    using System.Windows.Input;/// <summary>
                               /// Interaction logic for the MainWindow
                               /// </summary>
    public sealed partial class MainWindow : Window //, INotifyPropertyChanged, IDisposable
    {
        public struct PersonalRecord
        {
            public String name;
            public int[] performance;
            public PersonalRecord(String n, int[] p)
            {
                name = n;
                performance = p;
            }
        }

        private bool leftPlaying = false;
        private bool leftPausing = false;

        private bool rightPlaying = false;
        private bool rightPausing = false;

        DispatcherTimer _timer = new DispatcherTimer();

        private string cur = Environment.CurrentDirectory;
        private string dataBasePath = $"\\..\\..\\..\\data";

        private string student_color_or_body;
        private string coach_color_or_body;
        public string action_type;

        private string week = string.Empty;

        private string className = string.Empty;

        //for week in main to change same as record 
        public static string weekFromControl;
        public static string classNameControl;

        public int coachVideoCount = 0;
        public int studentVideoCount = 0;

        private List<Monitors.Monitor.CriticalPoint> studentJudgement;
        private List<Monitors.Monitor.CriticalPoint> coachJudgement;
        private String[] smashGoals = {"側身", "手肘抬高", "手肘轉向前", "手腕發力", "收拍"};
        private String[] serveGoals = { "重心腳在左腳", "重心腳移到右腳", "轉腰", "手腕發力", "肩膀向前" };
        private String[] lobGoals = { "持拍立腕", "右腳跨步", "腳跟著地", "手腕發力" };

        private double rightVideoDuration = 0;
        private double leftVideoDuration = 0;
        
        public static string nowSelectedName = string.Empty;
        private string prevSeletedName = string.Empty;

        public struct Goals
        {
            public String[] smashGoals;
            public String[] serveGoals;
            public String[] lobGoals;
            public Goals(String[] smash, String[] serve, String[] lob)
            {
                smashGoals = smash;
                serveGoals = serve;
                lobGoals = lob;
            }
        }
        private Goals goals;

        private String studentFileName;
        private String StudentFileName
        {
            get
            {
                return this.studentFileName;
            }
            set
            {
                this.studentFileName = value;
                string path = cur + dataBasePath + $"\\student\\{this.className}\\{week}\\{action_type}\\{StudentFileName}\\{student_color_or_body}.avi";
                MediaPlayer_left.Source = new Uri(path);
                MediaPlayer_left.Play();
                MediaPlayer_left.Pause();
            }
        }

        private String coachFileName;
        private String CoachFileName
        {
            get
            {
                return this.coachFileName;
            }
            set
            {
                this.coachFileName = value;
                // play right media
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                MediaPlayer_right.Source = new Uri(path);
                MediaPlayer_right.Play();
                MediaPlayer_right.Pause();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += new EventHandler(ticktock);
            _timer.Start();

            this.action_type = "lob";
            this.student_color_or_body = "color";
            this.coach_color_or_body = "color";
            this.goals = new Goals(this.smashGoals, this.serveGoals, this.lobGoals);

            MediaPlayer_left.ScrubbingEnabled = true;
            MediaPlayer_right.ScrubbingEnabled = true;
        }

        void ticktock(object sender, EventArgs e)
        {
            if (!LeftTimelineSlider.IsMouseCaptureWithin)
            {
                LeftTimelineSlider.Value = MediaPlayer_left.Position.TotalMilliseconds;
                LeftMediaLabel.Text = String.Format("{0:ss}:{0:fff}", MediaPlayer_left.Position);
            }
            if (!RightTimelineSlider.IsMouseCaptureWithin)
            {
                RightTimelineSlider.Value = MediaPlayer_right.Position.TotalMilliseconds;
                RightMediaLabel.Text = String.Format("{0:ss}:{0:fff}", MediaPlayer_right.Position);
            }
        }

        private void MediaLeftOpened(object sender, RoutedEventArgs e)
        {
            LeftTimelineSlider.Minimum = 0;
            LeftTimelineSlider.Maximum = MediaPlayer_left.NaturalDuration.TimeSpan.TotalMilliseconds;
            this.leftVideoDuration = MediaPlayer_left.NaturalDuration.TimeSpan.TotalMilliseconds;
            PlayPauseLeftButton.IsEnabled = true;
        }

        private void MediaRightOpened(object sender, RoutedEventArgs e)
        {
            RightTimelineSlider.Minimum = 0;
            RightTimelineSlider.Maximum = MediaPlayer_right.NaturalDuration.TimeSpan.TotalMilliseconds;
            this.rightVideoDuration = MediaPlayer_right.NaturalDuration.TimeSpan.TotalMilliseconds;
            PlayPauseRightButton.IsEnabled = true;
        }
        
        private void MediaRightEnded(object sender, RoutedEventArgs e)
        {
            MediaPlayer_right.Stop();
            MediaPlayer_right.Position = new TimeSpan(0, 0, 0, 0, 0);
            this.rightPlaying = false;
            this.rightPausing = true;
            MediaRightControlUpdateState();
        }

        private void MediaLeftEnded(object sender, RoutedEventArgs e)
        {
            MediaPlayer_left.Stop();
            MediaPlayer_left.Position = new TimeSpan(0, 0, 0, 0, 0);
            this.leftPlaying = false;
            this.leftPausing = true;
            MediaLeftControlUpdateState();
        }

        private void MediaPlayer_right_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //MenuWindow ccw = new MenuWindow("coach", action_type);
            //experiment and week is useless here
            MenuWindow ccw = new MenuWindow("coach", this.action_type, this.className, this.week);
            ccw.Owner = this;
            ccw.ShowDialog();
        }

        private void MediaPlayer_left_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //MenuWindow ccw = new MenuWindow("student", action_type);
            MenuWindow ccw = new MenuWindow("student", action_type, this.className, week);
            ccw.Owner = this;
            ccw.ShowDialog();
        }

        private void student_Button_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_left.Pause();
            double positionInMillisecond = this.leftVideoDuration * this.studentJudgement[(int)(sender as Button).Tag - 1].portion;
            MediaPlayer_left.Position = new TimeSpan(0, 0, 0, 0, (int)positionInMillisecond);
        }

        private void coach_Button_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_right.Pause();
            Console.WriteLine(this.coachJudgement.Count);
            Console.WriteLine(this.coachJudgement[(int)(sender as Button).Tag - 1].portion);
            double positionInMillisecond = this.rightVideoDuration * this.coachJudgement[(int)(sender as Button).Tag - 1].portion;
            MediaPlayer_right.Position = new TimeSpan(0, 0, 0, 0, (int)positionInMillisecond);
        }

        public void RightVideoChoosen(String selectedItem)
        {
            this.CoachFileName = selectedItem;
        }

        public void LeftVideoChoosen(String selectedItem)
        {
            this.StudentFileName = selectedItem;
        }
        public void LoadJudgement(string name, string action_type, string person_type, string className, string week, bool output_txt)
        {
            //String judgementDir = "../../../data/" + person_type + "/" + action_type + "/" + name + "/judgement.json";
            String judgementDir = null;
            if (person_type == "student")
            {
                judgementDir = "../../../data/" + person_type + "/" + className + "/" + week + "/" + action_type + "/" + name + "/judgement.json";
            }
            //person_type == coach
            else
            {
                judgementDir = "../../../data/" + person_type + "/" + action_type + "/" + name + "/judgement.json";
            }
            String rawJsonData = File.ReadAllText(judgementDir);
            if(person_type == "coach")
                this.coachJudgement = JsonConvert.DeserializeObject<List<Monitors.Monitor.CriticalPoint>>(rawJsonData);
            else if (person_type == "student")
                this.studentJudgement = JsonConvert.DeserializeObject<List<Monitors.Monitor.CriticalPoint>>(rawJsonData);


            if (person_type == "coach")
            {
                textBlock2.Text = name;
                
                for (int i = 0; i < this.coachJudgement.Count; ++i)
                {
                    Grid grid = new Grid();
                    Grid.SetRow(grid, i);
                    Grid.SetColumn(grid, 1);
                    Button button = new Button()
                    {
                        Content = string.Format(this.coachJudgement[i].name),
                        Tag = i + 1,
                        BorderThickness = new Thickness(0, 0, 0, 0),
                        FontSize = 16,
                        FontWeight = FontWeights.Heavy,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#001C70"),
                        Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#E5DDB8"),
                    };
                    button.Click += new RoutedEventHandler(coach_Button_Click);

                    grid.Children.Add(button);
                    coachGrid.Children.Add(grid);
                   
                }
            }
            else if(person_type == "student")
            {
                textBlock1.Text = name;
                int[] correct = new int[this.studentJudgement.Count];
                for (int i = 0; i < this.studentJudgement.Count; i++)
                {
                    Image image = new Image();
                    if (this.studentJudgement[i].portion <= 1)
                    {
                        image.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
                        correct[i] = 1;
                    }
                    else
                    {
                        image.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
                        correct[i] = 0;
                    }
                    image.Height = 20;
                    image.Width = 20;
                    image.HorizontalAlignment = HorizontalAlignment.Center;
                    Grid.SetRow(image, i);
                    Grid.SetColumn(image, 0);
                    stuGrid.Children.Add(image);
                }
                for (int i = 0; i < this.studentJudgement.Count; ++i)
                {
                    Grid grid = new Grid();
                    Grid.SetRow(grid, i);
                    Grid.SetColumn(grid, 1);
                    Button button = new Button()
                    {
                        Content = string.Format(this.studentJudgement[i].name),
                        Tag = i + 1,
                        BorderThickness = new Thickness(0, 0, 0, 0),
                        FontSize = 16,
                        FontWeight = FontWeights.Heavy,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#001C70"),
                        Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#E5DDB8"),
                    };
                    button.Click += new RoutedEventHandler(student_Button_Click);

                    grid.Children.Add(button);
                    stuGrid.Children.Add(grid);
                }
                if (output_txt)
                {
                    ReadRecordJson(className, week, action_type, name, correct);
                    //Output_TXT(experiment, week, action_type, class_name, name, correct);
                }
            }
        }
    
        private void PlayPauseRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.rightPlaying)
            {
                this.rightPlaying = true;
                this.rightPausing = false;
                MediaPlayer_right.Play();
            }
            else
            {
                this.rightPlaying = false;
                this.rightPausing = true;
                MediaPlayer_right.Pause();
            }
            MediaRightControlUpdateState();
        }

        private void StopRightButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_right.Stop();
            MediaPlayer_right.Position = new TimeSpan(0, 0, 0, 0, 0);
            this.rightPlaying = false;
            this.rightPausing = true;
            MediaRightControlUpdateState();
        }

        private void PlayPauseLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.leftPlaying)
            {
                this.leftPlaying = true;
                this.leftPausing = false;
                MediaPlayer_left.Play();
            }
            else
            {
                this.leftPlaying = false;
                this.leftPausing = true;
                MediaPlayer_left.Pause();
            }
            MediaLeftControlUpdateState();
        }

        private void StopLeftButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_left.Stop();
            MediaPlayer_left.Position = new TimeSpan(0, 0, 0, 0, 0);
            this.leftPlaying = false;
            this.leftPausing = true;
            MediaLeftControlUpdateState();
        }

        private void RightTimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RightTimelineSlider.IsMouseCaptureWithin)
            {
                MediaPlayer_right.Pause();
                int SliderValue = (int)RightTimelineSlider.Value;
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                MediaPlayer_right.Position = ts;
            }
        }

        private void LeftTimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LeftTimelineSlider.IsMouseCaptureWithin)
            {
                MediaPlayer_left.Pause();
                int SliderValue = (int)LeftTimelineSlider.Value;
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                MediaPlayer_left.Position = ts;
            }
        }

        private void RightSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer_right.SpeedRatio = (double)RightSpeedSlider.Value;
        }

        private void RightSpeedSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer_left.SpeedRatio = (double)LeftSpeedSlider.Value;
        }

        private void MenuLeftButton_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //MenuWindow ccw = new MenuWindow("student", action_type);
            MenuWindow ccw = new MenuWindow("student", action_type, this.className, week);
            ccw.Owner = this;
            ccw.ShowDialog();
        }

        private void MenuRightButton_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //MenuWindow ccw = new MenuWindow("coach", action_type);
            //experiment and week is useless here
            MenuWindow ccw = new MenuWindow("coach", action_type, this.className, week);
            ccw.Owner = this;
            ccw.ShowDialog();
        }

        private void ToggleButtonLeft_Checked(object sender, RoutedEventArgs e)
        {
            if (leftColorRadio.IsChecked == true)
            {
                student_color_or_body = "color";
                string path = cur + dataBasePath + $"\\student\\{this.className}\\{week}\\{action_type}\\{StudentFileName}\\{student_color_or_body}.avi";
                //Console.WriteLine(path);
                if (File.Exists(path))
                {
                    resetUri(MediaPlayer_left, path);
                }
            }
            else if (leftBodyRadio.IsChecked == true)
            {
                student_color_or_body = "body";
                string path = cur + dataBasePath + $"\\student\\{this.className}\\{week}\\{action_type}\\{StudentFileName}\\{student_color_or_body}.avi";
                //Console.WriteLine(path);
                if (File.Exists(path))
                {
                    resetUri(MediaPlayer_left, path);
                }
            }
        }

        private void ToggleButtonRight_Checked(object sender, RoutedEventArgs e)
        {
            if (rightBodyRadio.IsChecked == true)
            {
                coach_color_or_body = "body";
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                resetUri(MediaPlayer_right, path);
            }
            else if (rightColorRadio.IsChecked == true)
            {
                coach_color_or_body = "color";
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                resetUri(MediaPlayer_right, path);
            }
        }

        private void ActionSwitch_Click(object sender, RoutedEventArgs e)
        {
            clearUserBtns();
            clearCoachBtns();
            if (lobRadio.IsChecked == true)
            {
                action_type = "lob";
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                if(File.Exists(path))
                {
                    resetUri(MediaPlayer_right, path);
                    //experiment and week is useless here
                    LoadJudgement(CoachFileName, action_type, "coach", this.className, week, false);
                    //LoadJudgement(CoachFileName, action_type, "coach");
                    releaseMediaElement(MediaPlayer_left);
                } 
                else
                {
                    releaseMediaElement(MediaPlayer_left);
                    releaseMediaElement(MediaPlayer_right);
                }
            }
            else if (serveRadio.IsChecked == true)
            {
                action_type = "serve";
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                if (File.Exists(path))
                {
                    resetUri(MediaPlayer_right, path);
                    LoadJudgement(CoachFileName, action_type, "coach", this.className, week, false);
                    releaseMediaElement(MediaPlayer_left);
                }
                else
                {
                    releaseMediaElement(MediaPlayer_left);
                    releaseMediaElement(MediaPlayer_right);
                }
            }
            else if (smashRadio.IsChecked == true)
            {
                action_type = "smash";
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                if (File.Exists(path))
                {
                    resetUri(MediaPlayer_right, path);
                    LoadJudgement(CoachFileName, action_type, "coach", this.className, week, false);
                    releaseMediaElement(MediaPlayer_left);
                }
                else
                {
                    releaseMediaElement(MediaPlayer_left);
                    releaseMediaElement(MediaPlayer_right);
                }
            }
        }

        private void resetUri(MediaElement me, string path)
        {
            me.Source = new Uri(path);
            me.Stop();
        }

        private void releaseMediaElement(MediaElement me)
        {
            me.Close();
            me.Source = null; 
        }
        
        private void clearCoachBtns()
        {
            coachGrid.Children.Clear();
        }

        private void clearUserBtns()
        {
            stuGrid.Children.Clear();
        }

        private void MediaRightControlUpdateState()
        {
            if (this.rightPlaying)
            {
                PlayPauseRightButton.Source = new BitmapImage(new Uri(@"Images\pause-circle.png", UriKind.Relative));
            }
            else if (this.rightPausing)
            {
                PlayPauseRightButton.Source = new BitmapImage(new Uri(@"Images\play-circle.png", UriKind.Relative));
            }
        }
        
        private void MediaLeftControlUpdateState()
        {
            if (this.leftPlaying)
            {
                PlayPauseLeftButton.Source = new BitmapImage(new Uri(@"Images\pause-circle.png", UriKind.Relative));
            }
            else if (this.leftPausing)
            {
                PlayPauseLeftButton.Source = new BitmapImage(new Uri(@"Images\play-circle.png", UriKind.Relative));
            }
        }

        

        private void ComboBox_Loaded_Main(object sender, RoutedEventArgs e)
        {
            // ... A List.
            List<string> data = new List<string>();
            data.Add("第一週");
            data.Add("第二週");
            data.Add("第三週");
            data.Add("第四週");
            data.Add("第五週");
            data.Add("第六週");
            data.Add("第七週");
            data.Add("第八週");
            data.Add("第九週");
            data.Add("第十週");
            data.Add("other");

            // ... Get the ComboBox reference.
            var comboBox = sender as ComboBox;

            // ... Assign the ItemsSource to the List.
            comboBox.ItemsSource = data;

            // ... Make the first item selected.
            comboBox.SelectedIndex = 0;
        }

        private void ComboBox_SelectionChanged_Main(object sender, SelectionChangedEventArgs e)
        {
            // ... Get the ComboBox.
            var comboBox = sender as ComboBox;

            // ... Set SelectedItem as Window Title.
            week = comboBox.SelectedItem as string;
            //Console.WriteLine(week);
        }
        private void ReadRecordJson(string className, string week, string action_type, String name, int[] performance)
        {
            String cur = Environment.CurrentDirectory;
            String directory = $"\\..\\..\\..\\data\\{className}\\{week}\\{action_type}\\";
            Directory.CreateDirectory(cur + directory);
            String filePath = $"{cur}\\..\\..\\..\\data\\{className}\\{week}\\{action_type}\\class_record.json";
            List<PersonalRecord> recordList = new List<PersonalRecord>();
            string encodeName = Encoding.GetEncoding(950).GetString(Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(950), Encoding.Unicode.GetBytes(name)));
            PersonalRecord pr = new PersonalRecord(encodeName, performance);
            

            if (File.Exists(filePath))
            {
                String rawJsonData = File.ReadAllText(filePath);
                recordList = JsonConvert.DeserializeObject<List<PersonalRecord>>(rawJsonData);
            }
            bool nameExist = false;
            for (int i = 0; i < recordList.Count; i++)
            {
                if(recordList[i].name == pr.name)
                {
                    nameExist = true;
                    MessageBoxResult messageBoxResult = MessageBox.Show(
                    $"{recordList[i].name} 的資料已經存在，要複寫嗎？",
                    "Overwrite Confirmation", MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        recordList[i] = new PersonalRecord(pr.name, pr.performance);
                    }
                    break;
                }
            }
            if(!nameExist)
                recordList.Add(pr);
            String recordResult = JsonConvert.SerializeObject(recordList);
            File.WriteAllText(filePath, recordResult);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tabItem = ((sender as TabControl).SelectedItem as TabItem).Header as string;
            if (!Main.IsSelected)
            {
                if (!(string.Compare(week, weekFromControl) == 0))
                {
                    week = weekFromControl;
                    //Console.WriteLine(week);
                    // ... Get the ComboBox.
                    var comboBox = this.week_main;
                    comboBox.SelectedItem = week;
                    // ... Set SelectedItem as Window Title.
                    //Console.WriteLine("combobox     "+comboBox.SelectedItem as string);
                }
            }
            else
            {
                ClassUpdateState(-1);
                if(nowSelectedName != prevSeletedName)
                {
                    classList.SelectedIndex = classList.Items.IndexOf(nowSelectedName);
                    prevSeletedName = nowSelectedName;
                }
            }
                //if (!(string.Compare(this.className, classNameControl) == 0) && classNameControl != "---" && classNameControl != "新增班級")
                //{
                //    this.className = classNameControl;
                //    // ... Get the ComboBox.
                //    //要先update;
                //    var comboBox = this.classList;
                //    comboBox.SelectedItem = this.className;
                //}
                       
        }

        private void Output_txt_Click(object sender, RoutedEventArgs e)
        {
            String cur = Environment.CurrentDirectory;
            String jsonFilePath = $"{cur}\\..\\..\\..\\data\\{this.className}\\{this.week}\\{this.action_type}\\class_record.json";
            List<PersonalRecord> recordList = new List<PersonalRecord>();


            if (File.Exists(jsonFilePath))
            {
                String rawJsonData = File.ReadAllText(jsonFilePath);
                recordList = JsonConvert.DeserializeObject<List<PersonalRecord>>(rawJsonData);
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string filename = DateTime.Now.ToString("yyyy-MM-dd");
            string filePath = $"{desktopPath}\\{this.className}_{filename}.txt";
            FileStream fileStream = new FileStream(filePath, FileMode.Create);
            fileStream.Close();
            if (action_type == "smash")
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
                {
                    file.WriteLine(" 姓名     側身   手肘抬高   手肘轉向前   手腕發力   收拍     ");
                }
            }
            else if (action_type == "lob")
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
                {
                    file.WriteLine(" 姓名     持拍立腕   右腳跨步   腳跟著地   手腕發力   ");
                }
            }
            else
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
                {
                    file.WriteLine(" 姓名     重心腳在左腳   重心腳移到右腳     轉腰        手腕發力       肩膀向前      ");
                }
            }

            foreach (PersonalRecord pr in recordList)
            {
                string output_judge = null;

                if (action_type == "smash")
                {
                    output_judge = pr.name + "      ";
                }
                else if (action_type == "lob")
                {
                    output_judge = pr.name + "    ";
                }
                else
                {
                    output_judge = pr.name + "          ";
                }
                for (int i = 0; i < pr.performance.Length; i++)
                {
                    if (action_type == "smash")
                    {
                        output_judge = output_judge + pr.performance[i] + "         ";
                    }
                    else if (action_type == "lob")
                    {
                        output_judge = output_judge + pr.performance[i] + "          ";
                    }
                    //action_type == serve
                    else
                    {
                        output_judge = output_judge + pr.performance[i] + "              ";
                    }
                }
                //output_judge = output_judge + DateTime.Now.ToString("HH:mm:ss(yyyy/MM/dd)");
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, true))
                {
                    file.WriteLine(output_judge);
                }
            }

            MessageBox.Show($"已將 {this.className} {this.week} {this.action_type} 的結果輸出至桌面", "輸出完成");
        }

        private void ClassLoaded(object sender, RoutedEventArgs e)
        {
            ClassUpdateState(0);
        }

        private void ClassSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            var comboBox = sender as ComboBox;
            this.className = comboBox.SelectedItem as string;
        }

        public void ClassUpdateState(int selectecClass)
        {
            string cur = Environment.CurrentDirectory;
            string relatePath = $"\\..\\..\\..\\data\\student";
            DirectoryInfo dirInfo = new DirectoryInfo(cur + relatePath);
            ArrayList list = new ArrayList();
            foreach (DirectoryInfo d in dirInfo.GetDirectories())
            {
                list.Add(d.Name);
            }
            classList.ItemsSource = list;
            if(selectecClass != -1)
                classList.SelectedIndex = selectecClass;
        }
    }
}