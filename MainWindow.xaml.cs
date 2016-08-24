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
    using System.Collections.Generic;/// <summary>
                                     /// Interaction logic for the MainWindow
                                     /// </summary>
    public sealed partial class MainWindow : Window //, INotifyPropertyChanged, IDisposable
    {
        /// <summary> Indicates if a playback is currently in progress </summary>
        private bool isPlaying = false;
        private bool pausing = false;

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
        
        private string studentFileName;

        public int coachVideoCount = 0;
        public int studentVideoCount = 0;

        private List<double> studentJudgement;
        private List<double> coachJudgement;

        private double rightVideoDuration = 0;
        private double leftVideoDuration = 0;

        private string StudentFileName
        {
            get
            {
                return this.studentFileName;
            }
            set
            {
                this.studentFileName = value;
                // play right media
                string path = cur + dataBasePath + $"\\student\\{action_type}\\{StudentFileName}\\{student_color_or_body}.avi";
                MediaPlayer_left.Source = new Uri(path);
                MediaPlayer_left.Stop();
            }
        }

        private string coachFileName;
        private string CoachFileName
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
                MediaPlayer_right.Stop();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            Console.WriteLine("Debug mode");
            this.DebugPanel.Visibility = Visibility.Visible;
#else
            Console.WriteLine("Release mode");
            this.DebugPanel.Visibility = Visibility.Collapsed;
#endif
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += new EventHandler(ticktock);
            _timer.Start();

            this.action_type = "lob";
            this.student_color_or_body = "color";
            this.coach_color_or_body = "color";

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



        /// <summary>
        /// Enables/Disables the record and playback buttons in the UI
        /// </summary>
        private void UpdateState()
        {
            if (this.isPlaying && !this.pausing)
            {
                this.RecordButton.IsEnabled = false;
                this.PlayButton.IsEnabled = false;
                this.PauseButton.IsEnabled = true;
            }
            else if (this.pausing)
            {
                this.RecordButton.IsEnabled = false;
                this.PlayButton.IsEnabled = true;
                this.PauseButton.IsEnabled = false;
            }
            else
            {
                this.RecordButton.IsEnabled = true;
                this.PlayButton.IsEnabled = true;
                this.PauseButton.IsEnabled = false;
            }
        }



        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            kinectRecord w = new kinectRecord();
            w.Owner = this;
            w.Show();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            this.isPlaying = true;
            if (!this.pausing)
            {
                this.pausing = false;
                this.UpdateState();
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.FileName = "Videos"; // Default file name
                dialog.DefaultExt = ".WMV"; // Default file extension
                dialog.Filter = "AVI文件|*.avi|所有文件|*.*"; // Filter files by extension 

                // Show open file dialog box
                Nullable<bool> result = dialog.ShowDialog();

                // Process open file dialog box results 
                if (result == true)
                {
                    MediaPlayer_left.Source = new Uri(dialog.FileName);
                }

            }
            else
            {
                this.pausing = false;
                this.UpdateState();
            }
            MediaPlayer_left.Play();
            
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            this.pausing = true;
            this.UpdateState();
            MediaPlayer_left.Pause();
            MediaPlayer_right.Pause();
        }

        private void MediaLeftOpened(object sender, RoutedEventArgs e)
        {
            LeftTimelineSlider.Minimum = 0;
            LeftTimelineSlider.Maximum = MediaPlayer_left.NaturalDuration.TimeSpan.TotalMilliseconds;
            this.leftVideoDuration = MediaPlayer_left.NaturalDuration.TimeSpan.TotalMilliseconds;
            Console.WriteLine(leftVideoDuration);
            //LeftTimelineSlider.Ticks = 
        }

        private void MediaRightOpened(object sender, RoutedEventArgs e)
        {
            RightTimelineSlider.Minimum = 0;
            RightTimelineSlider.Maximum = MediaPlayer_right.NaturalDuration.TimeSpan.TotalMilliseconds;
            this.rightVideoDuration = MediaPlayer_right.NaturalDuration.TimeSpan.TotalMilliseconds;
        }
        
        private void MediaEnded(object sender, RoutedEventArgs e)
        {
            // MediaPlayer_left.Close();
            // MediaPlayer_right.Close();
            this.isPlaying = false;
            this.UpdateState();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_left.Stop();
            MediaPlayer_left.Close();
            MediaPlayer_right.Stop();
            MediaPlayer_right.Close();
            this.isPlaying = false;
            this.pausing = false;
            this.UpdateState();
        }

        private void MediaPlayer_right_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MenuWindow ccw = new MenuWindow("coach", action_type);
            ccw.Owner = this;
            ccw.ShowDialog();
           
        }

        private void student_Button_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_left.Pause();
            double positionInMillisecond = this.leftVideoDuration * this.studentJudgement[(int)(sender as Button).Tag - 1];
            MediaPlayer_left.Position = new TimeSpan(0, 0, 0, 0, (int)positionInMillisecond);
        }

        private void teacher_Button_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_right.Pause();
            double positionInMillisecond = this.rightVideoDuration * this.coachJudgement[(int)(sender as Button).Tag - 1];
            MediaPlayer_right.Position = new TimeSpan(0, 0, 0, 0, (int)positionInMillisecond);
        }

        private void student_Click(object sender, RoutedEventArgs e)
        {
            if(grid1.Children.Count != 0)
            {
                grid1.Children.Remove((Button)grid1.Children[0]);
                grid2.Children.Remove((Button)grid2.Children[0]);
                grid3.Children.Remove((Button)grid3.Children[0]);
                grid4.Children.Remove((Button)grid4.Children[0]);
                grid5.Children.Remove((Button)grid5.Children[0]);
            }
            textBlock1.Text = "學員";
            image1.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
            image2.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
            image3.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
            image4.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
            image5.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
            for (int i = 0; i < 5; ++i)
            {
                string[] content = { "手肘抬高", "側身", "手肘轉向前", "手腕發力", "收拍" };
                Button button = new Button()
                {
                    Content = string.Format(content[i]),
                    Tag = i+1,
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    FontSize = 16,
                    FontWeight = FontWeights.Heavy,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#001C70"),
                    Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#E5DDB8"),
                };
                button.Click += new RoutedEventHandler(student_Button_Click);

                if (i == 0)
                {
                    this.grid1.Children.Add(button);
                }
                else if (i == 1)
                {
                    this.grid2.Children.Add(button);
                }
                else if (i == 2)
                {
                    this.grid3.Children.Add(button);
                }
                else if (i == 3)
                {
                    this.grid4.Children.Add(button);
                }
                else if (i == 4)
                {
                    this.grid5.Children.Add(button);
                }

                
            }
        }

        public void RightVideoChoosen(String selectedItem)
        {
            this.CoachFileName = selectedItem;
        }

        public void LeftVideoChoosen(String selectedItem)
        {
            this.StudentFileName = selectedItem;
        }

        public void LoadJudgement(String name, String action_type, String person_type)
        {
            String judgementDir = "../../../data/" + person_type + "/" + action_type + "/" + name + "/judgement.json";
            String rawJsonData = File.ReadAllText(judgementDir);
            List<String> goals = new List<String>();
            if(person_type == "coach")
                this.coachJudgement = JsonConvert.DeserializeObject<List<double>>(rawJsonData);
            else if (person_type == "student")
                this.studentJudgement = JsonConvert.DeserializeObject<List<double>>(rawJsonData);
            

            if(action_type == "smash")
            {
                goals.Add("側身");
                goals.Add("手肘抬高");
                goals.Add("手肘轉向前");
                goals.Add("手腕發力");
                goals.Add("收拍");
            }
            else if(action_type == "serve")
            {
                goals.Add("重心腳在右腳");
                goals.Add("重心轉移到左腳");
                //goals.Add("左手放球");
                goals.Add("轉腰");
                goals.Add("手腕發力");
                goals.Add("肩膀向前");
            }
            else if(action_type == "lob")
            {
                goals.Add("持拍立腕");
                //goals.Add("手腕轉動");
                goals.Add("右腳跨步");
                goals.Add("腳跟著地");
                goals.Add("拇指發力上勾");
            }

            if(person_type == "coach")
            {
                grid11.Children.Clear();
                grid12.Children.Clear();
                grid13.Children.Clear();
                grid14.Children.Clear();
                grid15.Children.Clear();
                textBlock2.Text = name;
                for (int i = 0; i < goals.Count; ++i)
                {
                    Button button = new Button()
                    {
                        Content = string.Format(goals[i]),
                        Tag = i + 1,
                        BorderThickness = new Thickness(0, 0, 0, 0),
                        FontSize = 16,
                        FontWeight = FontWeights.Heavy,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#001C70"),
                        Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#E5DDB8"),
                    };
                    button.Click += new RoutedEventHandler(teacher_Button_Click);

                    if (i == 0)
                        this.grid11.Children.Add(button);
                    else if (i == 1)
                        this.grid12.Children.Add(button);
                    else if (i == 2)
                        this.grid13.Children.Add(button);
                    else if (i == 3)
                        this.grid14.Children.Add(button);
                    else if (i == 4)
                        this.grid15.Children.Add(button);
                }
            }
            else if(person_type == "student")
            {
                if (grid1.Children.Count != 0)
                {
                    grid1.Children.Remove((Button)grid1.Children[0]);
                    grid2.Children.Remove((Button)grid2.Children[0]);
                    grid3.Children.Remove((Button)grid3.Children[0]);
                    grid4.Children.Remove((Button)grid4.Children[0]);
                    grid5.Children.Remove((Button)grid5.Children[0]);
                }
                textBlock1.Text = "學員";
                if(studentJudgement.Count > 0)
                    image1.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
                else
                    image1.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
                if (studentJudgement.Count > 1)
                    image2.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
                else
                    image2.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
                if (studentJudgement.Count > 2)
                    image3.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
                else
                    image3.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
                if (studentJudgement.Count > 3)
                    image4.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
                else
                    image4.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
                if (studentJudgement.Count > 4)
                    image5.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
                else
                    image5.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
                for (int i = 0; i < goals.Count; ++i)
                {
                    Button button = new Button()
                    {
                        Content = string.Format(goals[i]),
                        Tag = i + 1,
                        BorderThickness = new Thickness(0, 0, 0, 0),
                        FontSize = 16,
                        FontWeight = FontWeights.Heavy,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#001C70"),
                        Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#E5DDB8"),
                    };
                    button.Click += new RoutedEventHandler(student_Button_Click);

                    if (i == 0)
                    {
                        this.grid1.Children.Add(button);
                    }
                    else if (i == 1)
                    {
                        this.grid2.Children.Add(button);
                    }
                    else if (i == 2)
                    {
                        this.grid3.Children.Add(button);
                    }
                    else if (i == 3)
                    {
                        this.grid4.Children.Add(button);
                    }
                    else if (i == 4)
                    {
                        this.grid5.Children.Add(button);
                    }
                }
            }
        }
    
        private void leftUpdateState()
        {
            if (this.leftPlaying && !this.leftPausing)
            {
                this.RecordButton.IsEnabled = false;
                this.PlayLeftButton.IsEnabled = false;
                this.PauseLeftButton.IsEnabled = true;
            }
            else if (this.leftPausing)
            {
                this.RecordButton.IsEnabled = false;
                this.PlayLeftButton.IsEnabled = true;
                this.PauseLeftButton.IsEnabled = false;
            }
            else
            {
                this.RecordButton.IsEnabled = true;
                this.PlayLeftButton.IsEnabled = true;
                this.PauseLeftButton.IsEnabled = false;
            }
        }

        private void rightUpdateState()
        {
            if (this.rightPlaying && !this.rightPausing)
            {
                this.RecordButton.IsEnabled = false;
                this.PlayRightButton.IsEnabled = false;
                this.PauseRightButton.IsEnabled = true;
            }
            else if (this.rightPausing)
            {
                this.RecordButton.IsEnabled = false;
                this.PlayRightButton.IsEnabled = true;
                this.PauseRightButton.IsEnabled = false;
            }
            else
            {
                this.RecordButton.IsEnabled = true;
                this.PlayRightButton.IsEnabled = true;
                this.PauseRightButton.IsEnabled = false;
            }
        }

        private void PlayRightButton_Click(object sender, RoutedEventArgs e)
        {
            this.rightPlaying = true;
            this.rightPausing = false;
            this.rightUpdateState();
            MediaPlayer_right.Play();
        }

        private void PlayLeftButton_Click(object sender, RoutedEventArgs e)
        {
            this.leftPlaying = true;
            this.leftPausing = false;
            this.leftUpdateState();
            MediaPlayer_left.Play();
        }

        private void PauseRightButton_Click(object sender, RoutedEventArgs e)
        {
            this.rightPlaying = false;
            this.rightPausing = true;
            this.rightUpdateState();
            MediaPlayer_right.Pause();
        }

        private void PauseLeftButton_Click(object sender, RoutedEventArgs e)
        {
            this.leftPlaying = false;
            this.leftPausing = true;
            this.leftUpdateState();
            MediaPlayer_left.Pause();
        }

        private void StopRightButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_right.Stop();
            MediaPlayer_right.Position = new TimeSpan(0, 0, 0, 0, 0);
            this.rightPlaying = false;
            this.rightPausing = false;
            this.rightUpdateState();
        }

        private void StopLeftButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_left.Stop();
            MediaPlayer_left.Position = new TimeSpan(0, 0, 0, 0, 0);
            this.leftPlaying = false;
            this.leftPausing = false;
            this.leftUpdateState();
        }

        private void RightTimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RightTimelineSlider.IsMouseCaptureWithin)
            {
                MediaPlayer_right.Pause();
                int SliderValue = (int)RightTimelineSlider.Value;
                // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
                // Create a TimeSpan with miliseconds equal to the slider value.
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                MediaPlayer_right.Position = ts;

            }
            else if(this.rightPlaying && !this.rightPausing)
            {
                MediaPlayer_right.Play();
            }
        }

        private void LeftTimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LeftTimelineSlider.IsMouseCaptureWithin)
            {
                MediaPlayer_left.Pause();
                int SliderValue = (int)LeftTimelineSlider.Value;
                // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
                // Create a TimeSpan with miliseconds equal to the slider value.
                TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
                MediaPlayer_left.Position = ts;
            }
            else if(this.leftPlaying && !this.leftPausing)
            {
                MediaPlayer_left.Play();
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

        private void MediaPlay_left_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MenuWindow ccw = new MenuWindow("student", action_type);
            ccw.Owner = this;
            ccw.ShowDialog();
        }

        private void MenuLeftButton_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MenuWindow ccw = new MenuWindow("student", action_type);
            ccw.Owner = this;
            ccw.ShowDialog();
        }

        private void MenuRightButton_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MenuWindow ccw = new MenuWindow("coach", action_type);
            ccw.Owner = this;
            ccw.ShowDialog();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (leftColorRadio.IsChecked == true)
            {
                student_color_or_body = "color";
                string path = cur + dataBasePath + $"\\student\\{action_type}\\{StudentFileName}\\{student_color_or_body}.avi";
                resetUri(MediaPlayer_left, path);
            }
            else if (leftBodyRadio.IsChecked == true)
            {
                student_color_or_body = "body";
                string path = cur + dataBasePath + $"\\student\\{action_type}\\{StudentFileName}\\{student_color_or_body}.avi";
                resetUri(MediaPlayer_left, path);
            }
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
            if (lobRadio.IsChecked == true)
            {
                action_type = "lob";
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                if(File.Exists(path))
                {
                    Console.WriteLine("file exist");
                    Console.WriteLine(path);
                    resetUri(MediaPlayer_right, path);
                    LoadJudgement(CoachFileName, action_type, "coach");
                    releaseMediaElement(MediaPlayer_left);
                } 
                else
                {
                    Console.WriteLine("file not exist");
                    releaseMediaElement(MediaPlayer_left);
                    releaseMediaElement(MediaPlayer_right);
                    clearCoachBtns();
                    
                }
            }
            else if (serveRadio.IsChecked == true)
            {
                action_type = "serve";
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                if (File.Exists(path))
                {
                    Console.WriteLine("file exist");
                    resetUri(MediaPlayer_right, path);
                    LoadJudgement(CoachFileName, action_type, "coach");
                    releaseMediaElement(MediaPlayer_left);
                }
                else
                {
                    Console.WriteLine("file not exist");
                    releaseMediaElement(MediaPlayer_left);
                    releaseMediaElement(MediaPlayer_right);
                    clearCoachBtns();
                   
                }
            }
            else if (smashRadio.IsChecked == true)
            {
                action_type = "smash";
                string path = cur + dataBasePath + $"\\coach\\{action_type}\\{CoachFileName}\\{coach_color_or_body}.avi";
                if (File.Exists(path))
                {
                    Console.WriteLine("file exist");
                    resetUri(MediaPlayer_right, path);
                    LoadJudgement(CoachFileName, action_type, "coach");
                    releaseMediaElement(MediaPlayer_left);
                }
                else
                {
                    Console.WriteLine("file not exist");
                    releaseMediaElement(MediaPlayer_left);
                    releaseMediaElement(MediaPlayer_right);
                    clearCoachBtns();
                    
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
            grid11.Children.Clear();
            grid12.Children.Clear();
            grid13.Children.Clear();
            grid14.Children.Clear();
            grid15.Children.Clear();
        }

        private void clearUserBtns()
        {
            image1.Source = null;
            image2.Source = null;
            image3.Source = null;
            image4.Source = null;
            image5.Source = null;
            grid1.Children.Clear();
            grid2.Children.Clear();
            grid3.Children.Clear();
            grid4.Children.Clear();
            grid5.Children.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
