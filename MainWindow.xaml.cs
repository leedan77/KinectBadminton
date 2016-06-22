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


    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public sealed partial class MainWindow : Window //, INotifyPropertyChanged, IDisposable
    {
        /// <summary> Indicates if a playback is currently in progress </summary>
        private bool isPlaying = false;

        private bool pausing = false;

        public MainWindow()
        {
            InitializeComponent();
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

                //OneArgDelegate playback = new OneArgDelegate(this.PlaybackClip);
                //playback.BeginInvoke(filePath, null, null);
            }
            else
            {
                this.pausing = false;
                this.UpdateState();
            }
            MediaPlayer_left.Play();
            MediaPlayer_right.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            this.pausing = true;
            this.UpdateState();
            MediaPlayer_left.Pause();
            MediaPlayer_right.Pause();
        }

        private void MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaPlayer_left.Close();
            MediaPlayer_right.Close();
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
    }
}
