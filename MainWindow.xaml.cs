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

    using System.Diagnostics;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public sealed partial class MainWindow : Window //, INotifyPropertyChanged, IDisposable
    {
        /// <summary> Indicates if a playback is currently in progress </summary>
        private bool isPlaying = false;

        private bool pausing = false;

        DispatcherTimer _timer = new DispatcherTimer();

        private SmashMonitor smashMonitor;
        private ServeMonitor serveMonitor;

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
            smashMonitor = new SmashMonitor();
            serveMonitor = new ServeMonitor();
            //smashMonitor.start();
            serveMonitor.start();
        }

        void ticktock(object sender, EventArgs e)
        {
            if (!TimelineSlider.IsMouseCaptureWithin)
            {
                TimelineSlider.Value = MediaPlayer_left.Position.TotalSeconds;
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
            TimelineSlider.Minimum = 0;
            TimelineSlider.Maximum = MediaPlayer_left.NaturalDuration.TimeSpan.TotalSeconds;
        }

        private void MediaRightOpened(object sender, RoutedEventArgs e)
        {
            TimelineSlider.Minimum = 0;
            TimelineSlider.Maximum = MediaPlayer_right.NaturalDuration.TimeSpan.TotalSeconds;
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

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TimelineSlider.IsMouseCaptureWithin)
            {
                MediaPlayer_left.Pause();
                MediaPlayer_right.Pause();
                int SliderValue = (int)TimelineSlider.Value;

                // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
                // Create a TimeSpan with miliseconds equal to the slider value.
                TimeSpan ts = new TimeSpan(0, 0, 0, SliderValue, 0);
                MediaPlayer_left.Position = ts;
                MediaPlayer_right.Position = ts;

            }
            else
            {
                MediaPlayer_left.Play();
                MediaPlayer_right.Play();
            }

        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer_left.SpeedRatio = (double)SpeedSlider.Value;
            MediaPlayer_right.SpeedRatio = (double)SpeedSlider.Value;
        }

        private void MediaPlayer_right_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChooseCoachWindow ccw = new ChooseCoachWindow();
            ccw.Owner = this;
            ccw.ShowDialog();
           
        }

       
    }
}
