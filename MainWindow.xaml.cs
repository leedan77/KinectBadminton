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
    using System.ComponentModel;
    using System.Threading;
    using System.Windows;
    using Microsoft.Win32;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Tools;


    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public sealed partial class MainWindow : Window //, INotifyPropertyChanged, IDisposable
    {   
        public MainWindow()
        {
            InitializeComponent();
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
            if (PlayButton.Content.ToString() == "Play")
            {

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

                MediaPlayer_left.Play();
                MediaPlayer_right.Play();

                PlayButton.Content = "Pause";
            }
            else
            {    
                MediaPlayer_left.Pause();
                MediaPlayer_right.Pause();

            }
        }

        private void MediaEnded(object sender, RoutedEventArgs e)
        {        
            PlayButton.Content = "Play";
        }
    }
}
