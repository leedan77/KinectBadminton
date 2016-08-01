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

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public sealed partial class MainWindow : Window //, INotifyPropertyChanged, IDisposable
    {
        /// <summary> Indicates if a playback is currently in progress </summary>
        private bool isPlaying = false;

        private bool pausing = false;

        //for whether to delete grid button
        private bool teacherFirstTime = true;
        private bool studentFirstTime = true;

        DispatcherTimer _timer = new DispatcherTimer();

        private SmashMonitor smashMonitor;
        private ServeMonitor serveMonitor;
        
        private String type;
        private string coachFileName;
        public string CoachFileName
        {
            get
            {
                return this.coachFileName;
            }
            set
            {
                this.coachFileName = value;
                // play right media
                MediaPlayer_right.Source = new Uri(this.coachFileName);
                MediaPlayer_right.Play();
            }
        }

        public MainWindow()
        {
            InitializeComponent();        
            _timer.Interval = TimeSpan.FromMilliseconds(1000);
            _timer.Tick += new EventHandler(ticktock);
            _timer.Start();

            this.type = "smash";

            if(string.Compare(this.type, "smash") == 0)
            {
                smashMonitor = new SmashMonitor();
                smashMonitor.start();
            }
            else if(string.Compare(this.type, "serve") == 0)
            {
                serveMonitor = new ServeMonitor();
                serveMonitor.start();
            }
        }

        void ticktock(object sender, EventArgs e)
        {
            if (!LeftTimelineSlider.IsMouseCaptureWithin)
            {
                LeftTimelineSlider.Value = MediaPlayer_left.Position.TotalSeconds;
            }
            if (!RightTimelineSlider.IsMouseCaptureWithin)
            {
                RightTimelineSlider.Value = MediaPlayer_right.Position.TotalSeconds;
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
            kinectRecord w = new kinectRecord(this.type);
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
                /*
                Microsoft.Win32.OpenFileDialog dialog2 = new Microsoft.Win32.OpenFileDialog();
                dialog.FileName = "Videos"; // Default file name
                dialog.DefaultExt = ".WMV"; // Default file extension
                dialog.Filter = "AVI文件|*.avi|所有文件|*.*"; // Filter files by extension 

                // Show open file dialog box
                Nullable<bool> result2 = dialog.ShowDialog();

                // Process open file dialog box results 
                if (result2 == true)
                {
                    // Open document                    
                    MediaPlayer_right.Source = new Uri(dialog.FileName);
                }
                */
                // MediaPlayer_right.Source = new Uri(this.CoachFileName);

                //OneArgDelegate playback = new OneArgDelegate(this.PlaybackClip);
                //playback.BeginInvoke(filePath, null, null);
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
            LeftTimelineSlider.Maximum = MediaPlayer_left.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void MediaRightOpened(object sender, RoutedEventArgs e)
        {
            RightTimelineSlider.Minimum = 0;
            RightTimelineSlider.Maximum = MediaPlayer_right.NaturalDuration.TimeSpan.TotalSeconds;
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
            ChooseCoachWindow ccw = new ChooseCoachWindow();
            ccw.Owner = this;
            ccw.ShowDialog();
           
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(string.Format("You clicked on the {0}. button.", (sender as Button).Tag));
        }

        private void student_Click(object sender, RoutedEventArgs e)
        {
            if (studentFirstTime == true)
            {
                studentFirstTime = false;
            }
            else
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
                button.Click += new RoutedEventHandler(button_Click);

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

        public void CoachChoosen(String name)
        {
            if (teacherFirstTime == true)
            {
                teacherFirstTime = false;
            }
            else
            {
                grid1.Children.Remove((Button)grid11.Children[0]);
                grid2.Children.Remove((Button)grid12.Children[0]);
                grid3.Children.Remove((Button)grid13.Children[0]);
                grid4.Children.Remove((Button)grid14.Children[0]);
                grid5.Children.Remove((Button)grid15.Children[0]);
            }
            textBlock2.Text = name;
            for (int i = 0; i < 5; ++i)
            {

                string[] content = { "手肘抬高", "側身", "手肘轉向前", "手腕發力", "收拍" };
                Button button = new Button()
                {
                    Content = string.Format(content[i]),
                    Tag = i + 1,
                    BorderThickness = new Thickness(0, 0, 0, 0),
                    FontSize = 16,
                    FontWeight = FontWeights.Heavy,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#001C70"),
                    Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#E5DDB8"),
                };
                button.Click += new RoutedEventHandler(button_Click);

                if (i == 0)
                {
                    this.grid11.Children.Add(button);
                }
                else if (i == 1)
                {
                    this.grid12.Children.Add(button);
                }
                else if (i == 2)
                {
                    this.grid13.Children.Add(button);
                }
                else if (i == 3)
                {
                    this.grid14.Children.Add(button);
                }
                else if (i == 4)
                {
                    this.grid15.Children.Add(button);
                }
            }
        }

        private void StopLeftButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlayLeftButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PauseLeftButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StopRightButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlayRightButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PauseRightButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RightTimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RightTimelineSlider.IsMouseCaptureWithin)
            {
                MediaPlayer_right.Pause();
                int SliderValue = (int)RightTimelineSlider.Value;

                // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
                // Create a TimeSpan with miliseconds equal to the slider value.
                TimeSpan ts = new TimeSpan(0, 0, 0, SliderValue, 0);
                MediaPlayer_right.Position = ts;

            }
            else
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
                TimeSpan ts = new TimeSpan(0, 0, 0, SliderValue, 0);
                MediaPlayer_left.Position = ts;

            }
            else
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
    }
}
