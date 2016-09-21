using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.ComponentModel;
using System.Threading;
using Microsoft.Win32;
using System.Collections;

namespace Microsoft.Samples.Kinect.RecordAndPlaybackBasics
{
    /// <summary>
    /// testUC.xaml 的互動邏輯
    /// </summary>
    public partial class MenuUserControl : UserControl
    {
        private double curX;

        /// <summary> Indicates if a recording is currently in progress </summary>
        private bool isRecording = false;

        //new add
        /// <summary> Indicates if recording need to stop </summary>
        private bool recordingStop = false;

        private bool converting = false;

        private string idenity = "student";

        private string motion = "lob";
        private string handedness = "right";
        private string className = string.Empty;
        private string week = "week1";

        private string auto_convert = null;

        /// <summary> Delegate to use for placing a job with no arguments onto the Dispatcher </summary>
        private delegate void NoArgDelegate();

        /// <summary>
        /// Delegate to use for placing a job with a single string argument onto the Dispatcher
        /// </summary>
        /// <param name="arg">string argument</param>
        private delegate void OneArgDelegate(string arg);

        private delegate void TwoArgDelegate(string arg1, string arg2);

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
        
        /// <summary>
        /// Color visualizer
        /// </summary>
        private KinectColorView kinectColorView = null;
        
        public MenuUserControl()
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
            this.kinectBodyView = new KinectBodyView(this.kinectSensor, this.motion);
            //new add
            // create the Color visualizer
            this.kinectColorView = new KinectColorView(this.kinectSensor);
            this.DataContext = this;
            this.kinectColorbox.DataContext = this.kinectColorView;
            this.kinectBodybox.DataContext = this.kinectBodyView;
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

            if (this.kinectColorView != null)
            {
                this.kinectColorView.Dispose();
                this.kinectColorView = null;
            }
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the status text
            this.KinectStatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
            //Console.WriteLine(this.kinectStatusText);
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
            //this.Owner.Show();
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
            /*Console.WriteLine(this.auto_convert);
            if (!string.IsNullOrEmpty(this.auto_convert))
            {
                ConvertBody(this.auto_convert);
            }
            this.auto_convert = null;*/
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
                this.ConvertButton.IsEnabled = false;
            }
            else if (this.converting)
            {
                this.RecordButton.IsEnabled = false;
                this.RecordStopButton.IsEnabled = false;
                this.ConvertButton.IsEnabled = false;
            }
            else
            {
                this.RecordPlaybackStatusText = string.Empty;
                this.RecordButton.IsEnabled = true;
                this.RecordStopButton.IsEnabled = false;
                this.ConvertButton.IsEnabled = true;

                // after converting resume the view
                this.kinectColorbox.DataContext = this.kinectColorView;
                this.kinectBodybox.DataContext = this.kinectBodyView;
            }
        }

        /// <summary>
        /// Launches the SaveFileDialog window to help user create a new recording file
        /// </summary>
        /// <returns>File path to use when recording a new event file</returns>
        private string SaveRecordingAs()
        {
            string fileName = string.Empty;

            if (idenity == "coach")
            {
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
            }
            //idenity == student
            else
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || nameBox.Text == "please enter your neme here !")
                {
                    MessageBox.Show("please enter your name", "Error");
                }
                else if(classList.SelectedItem as string == "---" || classList.SelectedItem as string == "新增班級")
                {
                    MessageBox.Show("請選擇班級名稱", "錯誤");
                }
                else
                {
                    string folderName = nameBox.Text + "_" + DateTime.Now.ToString("HH-mm-ss(yyyy-MM-dd)");
                    //Console.WriteLine(folderName);
                    string cur = Environment.CurrentDirectory;
                    string relativePath = $"\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}\\";
                    string filePath = cur + relativePath + folderName;
                    if (Directory.Exists(filePath))
                    {
                        MessageBox.Show("The folder has existed", "Error");
                    }
                    else
                    {
                        //Directory.CreateDirectory(filePath);
                        SaveFileDialog dlg = new SaveFileDialog();
                        dlg.FileName = filePath + "\\" + folderName + ".xef";
                        fileName = dlg.FileName;
                    }
                }
            }
            this.auto_convert = fileName;
            return fileName;
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.kinectStatusText == "Running"/*"Kinect not available!"*/)
            {
                MessageBox.Show("請先移除Kinect裝置", "錯誤");
            }
            else if (classList.SelectedItem as string == "---" || classList.SelectedItem as string == "新增班級")
            {
                MessageBox.Show("請選擇班級名稱", "錯誤");
            }
            else
            {
                string filePath = this.OpenFileForConvert();
                if (!string.IsNullOrEmpty(filePath))
                {
                    ConvertBody(filePath);
                }
            }
        }

        private void ConvertBody(String filePath)
        {
            this.kinectBodybox.DataContext = null;
            this.kinectColorbox.DataContext = null;
            this.kinectBodyView = new KinectBodyView(this.kinectSensor, this.motion);

            this.converting = true;
            this.kinectBodybox.DataContext = this.kinectBodyView;
            string name = new DirectoryInfo(System.IO.Path.GetDirectoryName(filePath)).Name;
            TwoArgDelegate bodyConvert = new TwoArgDelegate(this.BodyConvertClip);
            bodyConvert.BeginInvoke(filePath, name, null, null);
            //bodyConvert.BeginInvoke(filePath, nameBox.Text, null, null);
        }

        private void ConvertColor(String filePath)
        {
            this.kinectBodybox.DataContext = null;
            this.kinectColorbox.DataContext = null;
            this.kinectColorView = new KinectColorView(this.kinectSensor);

            this.converting = true;
            this.kinectColorbox.DataContext = this.kinectColorView;
            string name = new DirectoryInfo(System.IO.Path.GetDirectoryName(filePath)).Name;
            TwoArgDelegate colorConvert = new TwoArgDelegate(this.ColorConvertClip);
            colorConvert.BeginInvoke(filePath, name, null, null);
            //colorConvert.BeginInvoke(filePath, nameBox.Text, null, null);
        }

        /// <summary>Plays back a .xef file to the Kinect sensor</summary>
        /// <param name="filePath">Full path to the .xef file that should be played back to the sensor</param
        /// <param name="personName">The name of tester in the video</param>
        private void BodyConvertClip(string filePath, string personName)
        {
            
            using (KStudioClient client = KStudio.CreateClient())
            {

                int nowFrame = 0;
                int prevFrame = 0;
                client.ConnectToService();
                using (KStudioPlayback playback = client.CreatePlayback(filePath))
                {
                    playback.LoopCount = 0;
                    playback.Start();
                    while (playback.State == KStudioPlaybackState.Playing)
                    {
                        this.kinectBodyView.converting = true;
                        nowFrame = (int)(playback.CurrentRelativeTime.TotalMilliseconds / 33.33);
                        if (nowFrame > prevFrame)
                        {
                            playback.Pause();
                            Thread.Sleep(40);
                            playback.Resume();
                            Console.WriteLine(nowFrame);
                        }
                        prevFrame = nowFrame;
                    }
                    playback.Dispose();
                }

                client.DisconnectFromService();
            }
            Thread.Sleep(40);
            int videoCount = makeVideo("body", personName);
            this.converting = false;
            this.kinectBodyView.converting = false;
            this.kinectBodyView.Judge(personName, this.idenity, this.handedness, this.className,  this.week, videoCount);
            this.kinectBodyView.Dispose();
            string path = null;
            if (this.idenity == "student")
            {
                path = @"..\..\..\data\" + this.idenity + @"\" + this.className + @"\" + this.week + @"\" + this.motion + @"\" + personName + @"\color.avi";
            }
            else if(this.idenity == "coach")
            {
                path = @"..\..\..\data\" + this.idenity + @"\" + this.motion + @"\" + personName + @"\color.avi";
            }
            if (File.Exists(path))
            {
                Thread.Sleep(1000);
                this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
            }
            else
                this.Dispatcher.BeginInvoke(new OneArgDelegate(ConvertColor), filePath);
        }
        private void ColorConvertClip(string filePath, string personName)
        {
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();
                int nowFrame = 0;
                int prevFrame = 0;
                // Create the playback object
                using (KStudioPlayback playback = client.CreatePlayback(filePath))
                {                   
                    playback.LoopCount = 0;
                    playback.Start();
                    this.kinectColorView.converting = true;
                    while (playback.State == KStudioPlaybackState.Playing)
                    {
                        nowFrame = (int)(playback.CurrentRelativeTime.TotalMilliseconds / 33.33);
                        if(nowFrame > prevFrame)
                        {
                            playback.Pause();
                            Thread.Sleep(100);
                            playback.Resume();
                            Console.WriteLine(nowFrame); 
                        }
                        prevFrame = nowFrame;
                    }
                    playback.Dispose();
                }

                client.DisconnectFromService();
            }

            // Update the UI after the convert playback task has completed
            makeVideo("color", personName);
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
            dlg.Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter; // Filter files by extension 
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                fileName = dlg.FileName;
            }

            return fileName;
        }

        private int makeVideo(string video_type, string personName)
        {
            List<Image<Bgr, byte>> Video = new List<Image<Bgr, byte>>();
            if (string.Compare(video_type, "body") == 0)
                Video = this.kinectBodyView.Video;
                
            else if (string.Compare(video_type, "color") == 0)
                Video = this.kinectColorView.Video;
            int videoCount = Video.Count;
            Console.WriteLine($"video count: {Video.Count}");
            //string path = @"..\..\..\data\" + this.idenity + @"\"+  this.experiment + @"\" + this.week + @"\" + this.motion + @"\" + personName + @"\";
            string path = null;
            if (this.idenity == "student")
            {
                //path = $"\\..\\..\\..\\data\\{this.idenity}\\{this.experiment}\\{this.week}\\{this.motion}\\{personName}";
                path = @"..\..\..\data\" + this.idenity + @"\" + this.className + @"\" + this.week + @"\" + this.motion + @"\" + personName + @"\";
            }
            //idenity == coach
            else
            {
                //path = $"\\..\\..\\..\\data\\{this.idenity}\\{this.motion}\\{personName}";
                path = @"..\..\..\data\" + this.idenity + @"\" + this.motion + @"\" + personName + @"\";
            }
            Directory.CreateDirectory(path);

            using (VideoWriter vw = new VideoWriter(path + video_type + @".avi", 30, Video[0].Width, Video[0].Height, true))
            {
                for (int i = 0; i < Video.Count; i++)
                {
                    vw.WriteFrame(Video[i]);
                }
            }
            if (string.Compare(video_type, "body") == 0)
                this.kinectBodyView.Video.Clear();
            else if (string.Compare(video_type, "color") == 0)
                this.kinectColorView.Video.Clear();
            Video.Clear();
            return videoCount;
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

        private void IdentiyButton_Checked(object sender, RoutedEventArgs e)
        {
            if (coachRadio.IsChecked == true)
                this.idenity = "coach";
            else
                this.idenity = "student";
        }

        private void MotionRadio_Click(object sender, RoutedEventArgs e)
        {
            if (smashRadio.IsChecked == true)
                this.motion = "smash";
            else if (lobRadio.IsChecked == true)
                this.motion = "lob";
            else
                this.motion = "serve";
        }

        private void HandednessRadio_Click(object sender, RoutedEventArgs e)
        {
            if (lefthandedRadio.IsChecked == true)
                this.handedness = "left";
            else
                this.handedness = "right";
        }

        //private void ExperimentButton_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (experimentalRadio.IsChecked == true)
        //    {
        //        experiment = "experimental";
        //        //Console.WriteLine(experiment);
        //    }
        //    else
        //    {
        //        experiment = "control";
        //        //Console.WriteLine(experiment);
        //    }
        //}

        private void ComboBox_Loaded_Control(object sender, RoutedEventArgs e)
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

        private void ComboBox_SelectionChanged_Control(object sender, SelectionChangedEventArgs e)
        {
            // ... Get the ComboBox.
            var comboBox = sender as ComboBox;

            // ... Set SelectedItem as Window Title.
            week = comboBox.SelectedItem as string;
            //Console.WriteLine(week);
            MainWindow.weekFromControl = week;
        }

        private void ClassLoaded(object sender, RoutedEventArgs e)
        {
            string cur = Environment.CurrentDirectory;
            string relatePath = $"\\..\\..\\..\\data\\student";
            if (!Directory.Exists(cur + relatePath))
            {
                Directory.CreateDirectory(cur + relatePath);
            }
            DirectoryInfo dirInfo = new DirectoryInfo(cur + relatePath);
            ArrayList list = new ArrayList();
            list.Add("---");
            foreach (DirectoryInfo d in dirInfo.GetDirectories())
            {
                list.Add(d.Name);
            }
            list.Add("新增班級");
            
            var comboBox = sender as ComboBox;
            comboBox.ItemsSource = list;
            comboBox.SelectedIndex = 0;
        }

        private void ClassSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if(comboBox.SelectedItem as string == "新增班級")
            {
                string newClassName = Microsoft.VisualBasic.Interaction.InputBox("請輸入班級名稱", "新增班級", "", -1, -1);
                bool classNameExist = false;
                if (newClassName == "")
                {
                    MessageBox.Show("班級名稱不可為空白", "錯誤");
                    classNameExist = true;
                }
                foreach (string s in comboBox.Items)
                {
                    if(s == newClassName)
                    {
                        classNameExist = true;
                        MessageBox.Show($"班級名稱 {newClassName} 已存在", "錯誤");
                        break;
                    }
                }
                if (!classNameExist)
                {
                    ArrayList list = new ArrayList();
                    foreach (string s in comboBox.Items)
                    {
                        list.Add(s);
                    }
                    list.Add(newClassName);
                    comboBox.ItemsSource = list;
                    comboBox.SelectedIndex = comboBox.Items.Count - 1;
                    MainWindow.nowSelectedName = newClassName;
                    //MainWindow.nowSelectedClass = comboBox.Items.Count - 1;
                    //MainWindow.ClassUpdateState(comboBox.Items.Count - 1);
                }
            }
            else
                this.className = comboBox.SelectedItem as string;
            MainWindow.classNameControl = comboBox.SelectedItem as String;
        }


    }


}
