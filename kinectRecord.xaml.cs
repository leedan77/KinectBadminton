using System.Windows;
using System;
using System.ComponentModel;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public sealed partial class kinectRecord : Window, INotifyPropertyChanged, IDisposable
    {
        private double curX;

        /// <summary> Indicates if a recording is currently in progress </summary>
        private bool isRecording = false;

        //new add
        /// <summary> Indicates if recording need to stop </summary>
        private bool recordingStop = false;

        private bool converting = false;

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

        /// <summary>
        /// Body visualizer
        /// </summary>
        private KinectBodyView kinectBodyView = null;

        //new add
        /// <summary>
        /// Color visualizer
        /// </summary>
        private KinectColorView kinectColorView = null;

        private string ConvertFilePath;

        private string ConvertFileName;

        private string type;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public kinectRecord(string type)
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

            // create the Body visualizer

            //new add
            // create the Color visualizer

            // set data context for display in UI
            //left
            this.DataContext = this;
            //this.kinectIRViewbox.DataContext = this.kinectIRView;
            //this.kinectDepthViewbox.DataContext = this.kinectDepthView;
            //this.kinectBodyIndexViewbox.DataContext = this.kinectBodyIndexView;

            this.type = type;
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
                streamCollection.Add(KStudioEventStreamDataTypeIds.CompressedColor);

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
                this.BodyConvertButton.IsEnabled = false;
                this.ColorConvertButton.IsEnabled = false;
            }
            else if(this.converting)
            {
                this.RecordButton.IsEnabled = false;
                this.RecordStopButton.IsEnabled = false;
                this.BodyConvertButton.IsEnabled = false;
                this.ColorConvertButton.IsEnabled = false;
            }
            else
            {
                this.RecordPlaybackStatusText = string.Empty;
                this.RecordButton.IsEnabled = true;
                this.RecordStopButton.IsEnabled = false;
                this.BodyConvertButton.IsEnabled = true;
                this.ColorConvertButton.IsEnabled = true;
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

        private void BodyConvertButton_Click(object sender, RoutedEventArgs e)
        {
            this.kinectBodyView = new KinectBodyView(this.kinectSensor, this.type);
            string filePath = this.OpenFileForConvert();
            ConvertFilePath = filePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                this.lastFile = filePath;
                this.converting = true;
                
                this.kinectBodyView.converting = true;
                this.kinectViewbox.DataContext = this.kinectBodyView;
                OneArgDelegate bodyConvert = new OneArgDelegate(this.BodyConvertClip);
                bodyConvert.BeginInvoke(filePath, null, null);
            }
        }

        private void ColorConvertButton_Click(object sender, RoutedEventArgs e)
        {
            this.kinectColorView = new KinectColorView(this.kinectSensor);
            string filePath = this.OpenFileForConvert();
            ConvertFilePath = filePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                this.lastFile = filePath;
                this.converting = true;
                
                this.kinectColorView.converting = true;
                this.kinectViewbox.DataContext = this.kinectColorView;
                OneArgDelegate colorConvert = new OneArgDelegate(this.ColorConvertClip);
                colorConvert.BeginInvoke(filePath, null, null);
            }
        }

        /// <summary>
        /// Plays back a .xef file to the Kinect sensor
        /// </summary>
        /// <param name="filePath">Full path to the .xef file that should be played back to the sensor</param>
        private void BodyConvertClip(string filePath)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                // Create the playback object
                using (KStudioPlayback playback = client.CreatePlayback(filePath))
                {
                    playback.LoopCount = this.loopCount;
                    playback.Start();

                    while (playback.State == KStudioPlaybackState.Playing)
                    {

                    }
                    playback.Dispose();
                }

                client.DisconnectFromService();
            }


            string filename = Path.GetFileName(filePath);
            filename = filename.Remove(filename.IndexOf(".xef"), 4);

            // Update the UI after the convert playback task has completed
            makeVideo("body", filename);
            Console.WriteLine(this.kinectBodyView.Video.Count);
            this.converting = false;
            this.kinectBodyView.converting = false;
            this.kinectBodyView.SaveData(filename);
            this.kinectBodyView.Dispose();
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
        }

        private void ColorConvertClip(string filePath)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                // Create the playback object
                using (KStudioPlayback playback = client.CreatePlayback(filePath))
                {
                    playback.LoopCount = this.loopCount;
                    playback.Start();

                    while (playback.State == KStudioPlaybackState.Playing)
                    {

                    }
                    playback.Dispose();
                }

                client.DisconnectFromService();
            }

            string filename = Path.GetFileName(filePath);
            filename = filename.Remove(filename.IndexOf(".xef"), 4);

            // Update the UI after the convert playback task has completed
            makeVideo("color", filename);
            Console.WriteLine(this.kinectColorView.Video.Count);
            this.converting = false;
            this.kinectColorView.converting = false;
            this.kinectColorView.Dispose();
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
        }
        

        /// <summary>
        /// Launches the OpenFileDialog window to help user find/select an event file for playback
        /// </summary>
        /// <returns>Path to the event file selected by the user</returns>
        private string OpenFileForConvert()
        {
            string fileName = string.Empty;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = this.lastFile;
            dlg.DefaultExt = Properties.Resources.XefExtension; // Default file extension
            dlg.Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter; // Filter files by extension 
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                this.ConvertFileName = dlg.SafeFileName;
                fileName = dlg.FileName;
            }

            return fileName;
        }

        private void makeVideo(string type, string filename)
        {
            if (string.Compare(type, "body") == 0)
            {
                List<Image<Bgr, byte>> BodyVideo = this.kinectBodyView.Video;
                Console.WriteLine(BodyVideo.Count);
                Console.WriteLine(this.type);
                string path = @"..\..\..\data\coach\" + this.type + @"\body\";
                Directory.CreateDirectory(path);
                
                using (VideoWriter vw = new VideoWriter(path + filename + @".avi", 30, BodyVideo[0].Width, BodyVideo[0].Height, true))
                {
                    for (int i = 0; i < BodyVideo.Count; i++)
                    {
                        vw.WriteFrame(BodyVideo[i]);
                    }
                }
                this.kinectBodyView.Video.Clear();
                BodyVideo.Clear();
            }
            else if (string.Compare(type, "color") == 0)
            {
                List<Image<Bgr, byte>> ColorVideo = this.kinectColorView.Video;
                Console.WriteLine(ColorVideo.Count);
                string path = @"..\..\..\data\coach\" + this.type + @"\color\";
                Directory.CreateDirectory(path);
                using (VideoWriter vw = new VideoWriter(path + filename + @".avi", 30, ColorVideo[0].Width, ColorVideo[0].Height, true))
                {
                    for (int i = 0; i < ColorVideo.Count; i++)
                    {
                        vw.WriteFrame(ColorVideo[i]);
                    }
                }
                this.kinectColorView.Video.Clear();
                ColorVideo.Clear();
            }
        }

        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!converting)
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    Point pt = e.GetPosition(this);
                    double delta = curX - pt.X;
                    this.kinectBodyView.angle += delta / 3 * Math.PI / 180;
                    curX = pt.X;
                }
                else
                {
                    curX = e.GetPosition(this).X;
                }
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            this.kinectBodyView.angle = 0;
        }
    }
}
