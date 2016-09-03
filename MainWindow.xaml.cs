//------------------------------------------------------------------------------
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
        

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            kinectRecord w = new kinectRecord();
            w.Owner = this;
            w.Show();
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
        
        private void MediaEnded(object sender, RoutedEventArgs e)
        {
            // MediaPlayer_left.Close();
            // MediaPlayer_right.Close();
            this.isPlaying = false;
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
                textBlock2.Text = name;
                
                for (int i = 0; i < goals.Count; ++i)
                {
                    Console.WriteLine(goals[i]);
                    Grid grid = new Grid();
                    Grid.SetRow(grid, i);
                    Grid.SetColumn(grid, 1);
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

                    grid.Children.Add(button);
                    coachGrid.Children.Add(grid);
                   
                }
            }
            else if(person_type == "student")
            {
                textBlock1.Text = name;

                for (int i = 0; i < goals.Count; i++)
                {
                    Image image = new Image();
                    if(i < studentJudgement.Count)
                        image.Source = new BitmapImage(new Uri(@"Images\tick.png", UriKind.Relative));
                    else
                        image.Source = new BitmapImage(new Uri(@"Images\cross.png", UriKind.Relative));
                    image.Height = 20;
                    image.Width = 20;
                    image.HorizontalAlignment = HorizontalAlignment.Center;
                    Grid.SetRow(image, i);
                    Grid.SetColumn(image, 0);
                    stuGrid.Children.Add(image);
                }
                
                for (int i = 0; i < goals.Count; ++i)
                {
                    Grid grid = new Grid();
                    Grid.SetRow(grid, i);
                    Grid.SetColumn(grid, 1);
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

                    grid.Children.Add(button);
                    stuGrid.Children.Add(grid);
                }
            }
        }
    
        private void leftUpdateState()
        {
            //if (this.leftPlaying && !this.leftPausing)
            //{
            //    this.RecordButton.IsEnabled = false;
            //}
            //else if (this.leftPausing)
            //{
            //    this.RecordButton.IsEnabled = false;
            //}
            //else
            //{
            //    this.RecordButton.IsEnabled = true;
            //}
        }

        private void rightUpdateState()
        {
            //if (this.rightPlaying && !this.rightPausing)
            //{
            //    this.RecordButton.IsEnabled = false;
            //}
            //else if (this.rightPausing)
            //{
            //    this.RecordButton.IsEnabled = false;
            //}
            //else
            //{
            //    this.RecordButton.IsEnabled = true;
            //}
        }
        
        private void PlayPauseRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.rightPlaying)
            {
                this.rightPlaying = true;
                this.rightPausing = false;
                MediaPlayer_right.Play();
                PlayPauseRightButton.Source = new BitmapImage(new Uri(@"Images\pause-circle.png", UriKind.Relative));
            }
            else
            {
                this.rightPlaying = false;
                this.rightPausing = true;
                MediaPlayer_right.Pause();
                PlayPauseRightButton.Source = new BitmapImage(new Uri(@"Images\play-circle.png", UriKind.Relative));
            }
            //this.rightUpdateState();
        }

        private void StopRightButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_right.Stop();
            MediaPlayer_right.Position = new TimeSpan(0, 0, 0, 0, 0);
            this.rightPlaying = false;
            this.rightPausing = false;
            //this.rightUpdateState();
        }


        private void PlayPauseLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.leftPlaying)
            {
                this.leftPlaying = true;
                this.leftPausing = false;
                MediaPlayer_left.Play();
                PlayPauseLeftButton.Source = new BitmapImage(new Uri(@"Images\pause-circle.png", UriKind.Relative));
            }
            else
            {
                this.leftPlaying = false;
                this.leftPausing = true;
                MediaPlayer_left.Pause();
                PlayPauseLeftButton.Source = new BitmapImage(new Uri(@"Images\play-circle.png", UriKind.Relative));
            }
            //this.leftUpdateState();
        }

        private void StopLeftButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer_left.Stop();
            MediaPlayer_left.Position = new TimeSpan(0, 0, 0, 0, 0);
            this.leftPlaying = false;
            this.leftPausing = false;
            //this.leftUpdateState();
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
            coachGrid.Children.Clear();
        }

        private void clearUserBtns()
        {
            stuGrid.Children.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        
    }
}
