﻿using System.Windows;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using Microsoft.Kinect;
using Microsoft.Kinect.Tools;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public sealed partial class kinectRecord : Window, INotifyPropertyChanged, IDisposable
    {

        /// <summary> Indicates if a recording is currently in progress </summary>
        private bool isRecording = false;

        //new add
        /// <summary> Indicates if recording need to stop </summary>
        private bool recordingStop = false;

        private string lastFile = string.Empty;

        /// <summary> Number of playback iterations </summary>
        private uint loopCount = 0;

        /// <summary> Delegate to use for placing a job with no arguments onto the Dispatcher </summary>
        private delegate void NoArgDelegate();

        /// <summary>
        /// Delegate to use for placing a job with a single string argument onto the Dispatcher
        /// </summary>
        /// <param name="arg">string argument</param>
        private delegate void OneArgDelegate(string arg);

        /// <summary> Active Kinect sensor </summary>
        private KinectSensor kinectSensor = null;

        /// <summary> Current kinect sesnor status text to display </summary>
        private string kinectStatusText = string.Empty;

        /// <summary>
        /// Current record/playback status text to display
        /// </summary>
        private string recordPlayStatusText = string.Empty;

        /// left
        /// <summary>
        /// Infrared visualizer
        /// </summary>
        private KinectIRView kinectIRView = null;

        /// <summary>
        /// Depth visualizer
        /// </summary>
        private KinectDepthView kinectDepthView = null;

        /// <summary>
        /// BodyIndex visualizer
        /// </summary>
        private KinectBodyIndexView kinectBodyIndexView = null;

        /// <summary>
        /// Body visualizer
        /// </summary>
        private KinectBodyView kinectBodyView = null;

        //new add
        /// <summary>
        /// Color visualizer
        /// </summary>
        private KinectColorView kinectColorView = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public kinectRecord()
        {
            // initialize the components (controls) of the window
            this.InitializeComponent();

            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.KinectStatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // left
            // create the IR visualizer
            this.kinectIRView = new KinectIRView(this.kinectSensor);

            // create the Depth visualizer
            this.kinectDepthView = new KinectDepthView(this.kinectSensor);

            // create the BodyIndex visualizer
            this.kinectBodyIndexView = new KinectBodyIndexView(this.kinectSensor);

            // create the Body visualizer
            this.kinectBodyView = new KinectBodyView(this.kinectSensor);

            //new add
            // create the Color visualizer
            this.kinectColorView = new KinectColorView(this.kinectSensor);

            // set data context for display in UI
            //left
            this.DataContext = this;
            //this.kinectIRViewbox.DataContext = this.kinectIRView;
            //this.kinectDepthViewbox.DataContext = this.kinectDepthView;
            //this.kinectBodyIndexViewbox.DataContext = this.kinectBodyIndexView;
            this.kinectBodyViewbox.DataContext = this.kinectBodyView;
            //new add
            this.kinectColorViewbox.DataContext = this.kinectColorView;
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string KinectStatusText
        {
            get
            {
                return this.kinectStatusText;
            }

            set
            {
                if (this.kinectStatusText != value)
                {
                    this.kinectStatusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("KinectStatusText"));
                    }
                }
            }
        }


        /// <summary>
        /// Gets or sets the current status text to display for the record/playback features
        /// </summary>
        public string RecordPlaybackStatusText
        {
            get
            {
                return this.recordPlayStatusText;
            }

            set
            {
                if (this.recordPlayStatusText != value)
                {
                    this.recordPlayStatusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("RecordPlaybackStatusText"));
                    }
                }
            }
        }


        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            //left
            if (this.kinectIRView != null)
            {
                this.kinectIRView.Dispose();
                this.kinectIRView = null;
            }

            if (this.kinectDepthView != null)
            {
                this.kinectDepthView.Dispose();
                this.kinectDepthView = null;
            }

            if (this.kinectBodyIndexView != null)
            {
                this.kinectBodyIndexView.Dispose();
                this.kinectBodyIndexView = null;
            }

            if (this.kinectBodyView != null)
            {
                this.kinectBodyView.Dispose();
                this.kinectBodyView = null;
            }

            //new add
            if (this.kinectColorView != null)
            {
                this.kinectColorView.Dispose();
                this.kinectColorView = null;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void kinectRecord_Closing(object sender, CancelEventArgs e)
        {
            if (this.kinectIRView != null)
            {
                this.kinectIRView.Dispose();
                this.kinectIRView = null;
            }

            if (this.kinectDepthView != null)
            {
                this.kinectDepthView.Dispose();
                this.kinectDepthView = null;
            }

            if (this.kinectBodyIndexView != null)
            {
                this.kinectBodyIndexView.Dispose();
                this.kinectBodyIndexView = null;
            }

            if (this.kinectBodyView != null)
            {
                this.kinectBodyView.Dispose();
                this.kinectBodyView = null;
            }

            //new add
            if (this.kinectColorView != null)
            {
                this.kinectColorView.Dispose();
                this.kinectColorView = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
            this.Owner.Show();
            
           
        }

        /// <summary>
        /// Handles the event in which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the status text
            this.KinectStatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Handles the user clicking on the Record button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            string filePath = this.SaveRecordingAs();

            if (!string.IsNullOrEmpty(filePath))
            {
                this.lastFile = filePath;
                this.isRecording = true;
                this.RecordPlaybackStatusText = Properties.Resources.RecordingInProgressText;
                this.UpdateState();

                // Start running the recording asynchronously
                OneArgDelegate recording = new OneArgDelegate(this.RecordClip);
                recording.BeginInvoke(filePath, null, null);
            }
        }

        /// <summary>
        /// Handles the user clicking on the Record Stop button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void RecordStopButton_Click(object sender, RoutedEventArgs e)
        {
            this.recordingStop = true;
        }

        /// <summary>
        /// Records a new .xef file
        /// </summary>
        /// <param name="filePath">Full path to where the file should be saved to</param>
        private void RecordClip(string filePath)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                // Specify which streams should be recorded
                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection();
                streamCollection.Add(KStudioEventStreamDataTypeIds.Ir);
                streamCollection.Add(KStudioEventStreamDataTypeIds.Depth);
                streamCollection.Add(KStudioEventStreamDataTypeIds.Body);
                //streamCollection.Add(KStudioEventStreamDataTypeIds.BodyIndex);

                //new add
                streamCollection.Add(KStudioEventStreamDataTypeIds.UncompressedColor);

                // Create the recording object
                using (KStudioRecording recording = client.CreateRecording(filePath, streamCollection))
                {
                    //recording.StartTimed(this.duration);
                    recording.Start();
                    while (recording.State == KStudioRecordingState.Recording && recordingStop == false)
                    {
                        Thread.Sleep(500);
                    }
                    recording.Stop();
                }

                client.DisconnectFromService();
            }

            // Update UI after the background recording task has completed
            this.recordingStop = false;
            this.isRecording = false;
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
        }

        /// <summary>
        /// Enables/Disables the record and playback buttons in the UI
        /// </summary>
        private void UpdateState()
        {
            if (this.isRecording)
            {
                this.RecordButton.IsEnabled = false;
                this.RecordStopButton.IsEnabled = true;
            }
            else
            {
                this.RecordPlaybackStatusText = string.Empty;
                this.RecordButton.IsEnabled = true;
                this.RecordStopButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Launches the SaveFileDialog window to help user create a new recording file
        /// </summary>
        /// <returns>File path to use when recording a new event file</returns>
        private string SaveRecordingAs()
        {
            string fileName = string.Empty;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = "recordAndPlaybackBasics.xef";
            dlg.DefaultExt = Properties.Resources.XefExtension;
            dlg.AddExtension = true;
            dlg.Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter;
            dlg.CheckPathExists = true;
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                fileName = dlg.FileName;
            }

            return fileName;
        }
    }
}
