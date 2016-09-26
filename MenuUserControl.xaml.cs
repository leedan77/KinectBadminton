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

        private string identity = "student";

        private string motion = "lob";
        private string handedness = string.Empty;
        private string className = string.Empty;
        private string week = "week1";
        private string convertName = string.Empty;
        /// <summary> Delegate to use for placing a job with no arguments onto the Dispatcher </summary>
        private delegate void NoArgDelegate();

        /// <summary>
        /// Delegate to use for placing a job with a single string argument onto the Dispatcher
        /// </summary>
        /// <param name="arg">string argument</param>
        private delegate void OneArgDelegate(string arg);

        private delegate void TwoArgDelegate(string arg1, string arg2);

        private delegate void StringIntDelegate(string arg1, int arg2);

        private delegate void ThreeArgDelegate(string arg1, string arg2, int arg3);

        private delegate int MakeVideoDelegate(string arg1, string arg2);

        private delegate void RecordDelegate(KStudioClient arg2, KStudioEventStreamSelectorCollection arg3);

        private delegate void IntDelegate(int arg1);

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

        private KStudioClient client = null;
        
        private bool bodyConvertBad = false;
        private bool ColorConvertBad = false;
        private string xefFilePath = string.Empty;
        private string jointsJsonPath = string.Empty;
        private string frontDataPath = string.Empty;

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
            this.kinectBodyView = new KinectBodyView(this.kinectSensor, this.motion, MainWindow.MediaPlayerSize, this);
            //new add
            // create the Color visualizer
            this.kinectColorView = new KinectColorView(this.kinectSensor, MainWindow.MediaPlayerSize, this);
            this.DataContext = this;
            this.kinectColorbox.DataContext = this.kinectColorView;
            this.kinectBodybox.DataContext = this.kinectBodyView;
        }

        private void RefreshKinectSensor()
        {
            this.kinectSensor.Close();
            this.kinectSensor = null;
            this.kinectSensor = KinectSensor.GetDefault();
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.kinectSensor.Open();
            this.KinectStatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
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

            if (this.kinectStatusText == "Running")
            {
                this.client = KStudio.CreateClient();
                this.client.ConnectToService();
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
            //this.Owner.Show();
        }
        
        /// <summary>
        /// Handles the user clicking on the Record button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if(this.kinectStatusText == "Running")
            {
                LoadXefJointsPath();
                if (!string.IsNullOrEmpty(this.xefFilePath))
                {
                    this.isRecording = true;
                    this.RecordPlaybackStatusText = Properties.Resources.RecordingInProgressText;
                    this.UpdateState();

                    KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection();
                    //streamCollection.Add(KStudioEventStreamDataTypeIds.Ir);
                    streamCollection.Add(KStudioEventStreamDataTypeIds.Depth);
                    streamCollection.Add(KStudioEventStreamDataTypeIds.Body);
                    //streamCollection.Add(KStudioEventStreamDataTypeIds.BodyIndex);

                    //new add
                    streamCollection.Add(KStudioEventStreamDataTypeIds.UncompressedColor);
                    this.UpdateState();

                    RecordDelegate rd = new RecordDelegate(this.RecordClip);
                    rd.BeginInvoke(this.client, streamCollection, null, null);
                }
                NameUpdateState();
            }
            else
            {
                MessageBox.Show("請先連接Kinect才能進行錄製", "錯誤");
            }
        }
        
        private void RecordDone()
        {
            this.kinectBodyView.SaveJointData(this.jointsJsonPath);
        }
        private void RecordClip(KStudioClient client, KStudioEventStreamSelectorCollection streamCollection)
        {
            using (KStudioRecording recording = client.CreateRecording(this.xefFilePath, streamCollection))
            {
                //recording.StartTimed(this.duration);
                Console.WriteLine($"before start: {DateTime.Now}");
                recording.Start();
                Console.WriteLine($"after start: {DateTime.Now}");
                bool flag = false;
                while (recording.State == KStudioRecordingState.Recording && recordingStop == false)
                {
                    if (!flag)
                    {
                        Console.WriteLine($"Start recording: {DateTime.Now}");
                        flag = true;
                    }
                    Thread.Sleep(500);
                }
                recording.Stop();
            }

            //client.DisconnectFromService();
            //client.Dispose();
            this.recordingStop = false;
            this.isRecording = false;
            this.Dispatcher.BeginInvoke(new NoArgDelegate(RecordDone));
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
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
        /// Enables/Disables the record and playback buttons in the UI
        /// </summary>
        private void UpdateState()
        {
            Console.WriteLine($"update: {Thread.CurrentThread.ManagedThreadId}");
            if (this.isRecording)
            {
                this.studentRadio.IsEnabled = false;
                this.coachRadio.IsEnabled = false;
                this.lobRadio.IsEnabled = false;
                this.serveRadio.IsEnabled = false;
                this.smashRadio.IsEnabled = false;
                this.classList.IsEnabled = false;
                this.week_control.IsEnabled = false;
                this.lefthandedRadio.IsEnabled = false;
                this.righthandedRadio.IsEnabled = false;
                this.studentNameList.IsEnabled = false;
                this.ResetButton.IsEnabled = false;

                this.RecordButton.IsEnabled = false;
                this.RecordStopButton.IsEnabled = true;
                this.ConvertButton.IsEnabled = false;
            }
            else if (this.converting)
            {
                this.studentRadio.IsEnabled = false;
                this.coachRadio.IsEnabled = false;
                this.lobRadio.IsEnabled = false;
                this.serveRadio.IsEnabled = false;
                this.smashRadio.IsEnabled = false;
                this.classList.IsEnabled = false;
                this.week_control.IsEnabled = false;
                this.lefthandedRadio.IsEnabled = false;
                this.righthandedRadio.IsEnabled = false;
                this.studentNameList.IsEnabled = false;
                this.ResetButton.IsEnabled = false;


                this.RecordButton.IsEnabled = false;
                this.RecordStopButton.IsEnabled = false;
                this.ConvertButton.IsEnabled = false;
            }
            else
            {
                
                this.studentRadio.IsEnabled = true;
                this.coachRadio.IsEnabled = true;
                this.lobRadio.IsEnabled = true;
                this.serveRadio.IsEnabled = true;
                this.smashRadio.IsEnabled = true;
                this.classList.IsEnabled = true;
                this.week_control.IsEnabled = true;
                this.lefthandedRadio.IsEnabled = true;
                this.righthandedRadio.IsEnabled = true;
                this.studentNameList.IsEnabled = true;
                this.ResetButton.IsEnabled = true;

                if (this.PlayBackLabel.Visibility == Visibility.Visible)
                {
                    if (this.bodyConvertBad || this.ColorConvertBad)
                    {
                        MessageBox.Show("影片轉檔Lose frame嚴重，請重新轉檔。建議：將程式關閉重新開啟。", "轉檔失敗");
                        this.bodyConvertBad = false;
                        this.ColorConvertBad = false;
                        if (File.Exists($"{this.frontDataPath}\\color.avi"))
                        {
                            File.Delete($"{this.frontDataPath}\\color.avi");
                        }
                        if (File.Exists($"{this.frontDataPath}\\body.avi"))
                        {
                            File.Delete($"{this.frontDataPath}\\body.avi");
                        }
                    }
                    else
                    {
                        MessageBoxResult messageBoxResult1 = MessageBox.Show(
                            $".xef檔佔據大量儲存空間，是否刪除？",
                            "確認", MessageBoxButton.YesNo);
                        if (messageBoxResult1 == MessageBoxResult.Yes)
                        {
                            MessageBoxResult messageBoxResult2 = MessageBox.Show(
                            $"刪除後無法復原，包含重新製作彩色及骨架影片。",
                            "確認", MessageBoxButton.YesNo);
                            if (messageBoxResult2 == MessageBoxResult.Yes)
                            {
                                if (File.Exists(this.xefFilePath))
                                {
                                    File.Delete(this.xefFilePath);
                                    MessageBox.Show($"已刪除 {new FileInfo(this.xefFilePath).Name}");
                                    NameUpdateState();
                                }
                                else
                                    Console.WriteLine(this.xefFilePath);
                            }
                        }
                    }
                    
                }
                this.PlayBackLabel.Content = string.Empty;
                this.PlayBackLabel.Visibility = Visibility.Hidden;
                this.RecordPlaybackStatusText = string.Empty;
                this.RecordButton.IsEnabled = true;
                this.RecordStopButton.IsEnabled = false;
                this.ConvertButton.IsEnabled = true;

                // after converting resume the view
                this.kinectColorView = null;
                this.kinectBodyView = null;
                this.kinectColorView = new KinectColorView(this.kinectSensor, MainWindow.MediaPlayerSize, this);
                this.kinectBodyView = new KinectBodyView(this.kinectSensor, this.motion, MainWindow.MediaPlayerSize, this);
                this.kinectColorbox.DataContext = this.kinectColorView;
                this.kinectBodybox.DataContext = this.kinectBodyView;
            }
            //if(this.kinectStatusText != "Running")
            //{
            //    this.RecordButton.IsEnabled = false;
            //}
        }

        /// <summary>
        /// Launches the SaveFileDialog window to help user create a new recording file
        /// </summary>
        /// <returns>File path to use when recording a new event file</returns>
        private void LoadXefJointsPath()
        {
            string fileDir = string.Empty;
            string newName = string.Empty;
            if (identity == "coach")
            {
                newName = Microsoft.VisualBasic.Interaction.InputBox("請輸入選手名稱", "新增錄影", "", -1, -1);
                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("選手名稱不可為空白", "錯誤");
                }
                else
                {
                    string fileName = newName + "_" + DateTime.Now.ToString("HH-mm-ss(yyyy-MM-dd)");
                    //Console.WriteLine(folderName);
                    string cur = Environment.CurrentDirectory;
                    fileDir = $"{cur}\\..\\..\\..\\data\\coach\\{this.motion}\\xefjoints";
                }
            }
            else if(this.identity == "student")
            {
                
                if(classList.SelectedItem as string == "請選擇" || classList.SelectedItem as string == "新增班級")
                {
                    MessageBox.Show("請選擇班級名稱", "錯誤");
                }
                else
                {
                    newName = Microsoft.VisualBasic.Interaction.InputBox("請輸入學員名稱", "新增錄影", "", -1, -1);
                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        MessageBox.Show("學員名稱不可為空白", "錯誤");
                    }
                    else
                    {
                        string fileName = newName + "_" + DateTime.Now.ToString("HH-mm-ss(yyyy-MM-dd)");
                        //Console.WriteLine(folderName);
                        string cur = Environment.CurrentDirectory;
                        fileDir = $"{cur}\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}\\xefjoints";
                    }
                }
            }
            this.xefFilePath = $"{fileDir}\\{newName}.xef";
            this.jointsJsonPath = $"{fileDir}\\{newName}.json";
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            bool selectSuccess = false;
            if (this.kinectStatusText == "Running"/*"Kinect not available!"*/)
            {
                MessageBox.Show("請先移除Kinect裝置", "錯誤");
            }
            else
            {
                if (this.identity == "student")
                {
                    if (classList.SelectedItem as string == "請選擇" || classList.SelectedItem as string == "新增班級")
                    {
                        MessageBox.Show("請選擇班級名稱", "錯誤");
                    }
                    else if (String.IsNullOrEmpty(this.convertName))
                    {
                        MessageBox.Show("請選擇名稱", "錯誤");
                    }
                    else
                    {
                        selectSuccess = true;
                    }
                    string cur = Environment.CurrentDirectory;
                    this.frontDataPath = $"{cur}\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}\\{this.convertName}";
                }
                else if (this.identity == "coach")
                {
                    //filePath = this.OpenFileForConvert();
                    if (String.IsNullOrEmpty(this.convertName))
                    {
                        MessageBox.Show("請選擇名稱", "錯誤");
                    }
                    else
                    {
                        selectSuccess = true;
                    }
                    string cur = Environment.CurrentDirectory;
                    this.frontDataPath = $"{cur}\\..\\..\\..\\data\\coach\\{this.motion}\\{this.convertName}";
                }
                if (string.IsNullOrEmpty(this.handedness))
                {
                    MessageBox.Show("請選擇慣用手", "錯誤");
                }
                else
                {
                    if (selectSuccess)
                    {
                        if (File.Exists(this.xefFilePath))
                        {
                            ConvertBody();
                        }
                        else if (File.Exists(this.jointsJsonPath))
                        {
                            this.kinectBodyView.Judge(this.jointsJsonPath, this.handedness);
                        }
                    }
                }
            }
        }

        private void ConvertBody()
        {
            this.kinectBodybox.DataContext = null;
            this.kinectColorbox.DataContext = null;
            this.kinectBodyView = new KinectBodyView(this.kinectSensor, this.motion, MainWindow.MediaPlayerSize, this);

            this.converting = true;
            //this.kinectBodybox.DataContext = this.kinectBodyView;
            UpdateState();
            Console.WriteLine($"main: {Thread.CurrentThread.ManagedThreadId}");
            this.PlayBackLabel.Content = $"正在對{this.convertName.Split('_')[0]}進行評分，並製作彩色及骨架影片...";
            this.PlayBackLabel.Visibility = Visibility.Visible;
            NoArgDelegate bodyConvert = new NoArgDelegate(BodyConvertClip);
            bodyConvert.BeginInvoke(null, null);
        }
        
        private void ConvertColor()
        {
            this.kinectBodybox.DataContext = null;
            this.kinectColorbox.DataContext = null;
            //this.kinectColorView = new KinectColorView(this.kinectSensor, MainWindow.MediaPlayerSize, this);

            this.converting = true;
            //this.kinectColorbox.DataContext = this.kinectColorView;
            NoArgDelegate colorConvert = new NoArgDelegate(ColorConvertClip);
            colorConvert.BeginInvoke(null, null);
        }

        /// <summary>Plays back a .xef file to the Kinect sensor</summary>
        /// <param name="filePath">Full path to the .xef file that should be played back to the sensor</param
        /// <param name="personName">The name of tester in the video</param>
        public bool convertLock = false;
        private void BodyConvertClip()
        {
            Console.WriteLine(this.xefFilePath);
            int nowFrame = 0;
            int prevFrame = 0;
            int totalFrame = 0;
            using (KStudioClient client = KStudio.CreateClient())
            {

                client.ConnectToService();
                using (KStudioPlayback playback = client.CreatePlayback(this.xefFilePath))
                {
                    playback.LoopCount = 0;
                    playback.Start();
                    Console.WriteLine($"convert: {Thread.CurrentThread.ManagedThreadId}");
                    while (playback.State == KStudioPlaybackState.Playing)
                    {
                        this.kinectBodyView.converting = true;
                        nowFrame = (int)(playback.CurrentRelativeTime.TotalMilliseconds / 33.33);
                        if (nowFrame > prevFrame)
                        {
                            playback.Pause();
                            //while (this.convertLock)
                            //{

                            //}
                            Thread.Sleep(40);
                            playback.Resume();
                            Console.WriteLine(nowFrame);
                            totalFrame = nowFrame;
                        }
                        prevFrame = nowFrame;
                    }
                    playback.Dispose();
                }

                client.DisconnectFromService();
            }
            Thread.Sleep(40);
            this.Dispatcher.BeginInvoke(new IntDelegate(BodyConvertDone), totalFrame);
        }

        private void BodyConvertDone(int totalFrame)
        {

            int videoCount = makeVideo("body");
            double loseFrameRate = 1 - (double)videoCount/totalFrame;
            if (loseFrameRate > 0.045)
            {
                this.bodyConvertBad = true;
            }
            this.converting = false;
            this.kinectBodyView.converting = false;
            this.kinectBodyView.SaveJointData(this.jointsJsonPath);
            this.kinectBodyView.Judge(this.jointsJsonPath, this.handedness);
            this.kinectBodyView.Dispose();
            
            string colorAviPath = $"{Directory.GetParent(this.xefFilePath).FullName}\\color.avi";
            if (File.Exists(colorAviPath))
            {
                this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
            }
            else
            {
                this.Dispatcher.BeginInvoke(new NoArgDelegate(ConvertColor));
            }
            Console.WriteLine("here");
        }

        private void ColorConvertClip()
        {
            int totalFrame = 0;
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();
                int nowFrame = 0;
                int prevFrame = 0;
                // Create the playback object
                
                using (KStudioPlayback playback = client.CreatePlayback(this.xefFilePath))
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
                            //while (this.convertLock)
                            //{

                            //}
                            Thread.Sleep(100);
                            //Thread.Sleep(100);
                            playback.Resume();
                            Console.WriteLine(nowFrame);
                            totalFrame = prevFrame;
                        }
                        prevFrame = nowFrame;
                    }
                    playback.Dispose();
                }

                client.DisconnectFromService();
            }
            this.Dispatcher.BeginInvoke(new IntDelegate(ColorConvertDone), totalFrame);
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
        }

        private void ColorConvertDone(int totalFrame)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(this.xefFilePath);
            string fileDir = $"{Directory.GetParent(Directory.GetParent(this.xefFilePath).FullName).FullName}\\{fileName}";
            int videoCount = makeVideo("color");
            double loseFrameRate = 1 - (double)videoCount/totalFrame;
            Console.WriteLine(loseFrameRate);
            if (loseFrameRate > 0.045)
            {
                this.ColorConvertBad = true;
            }
            this.converting = false;
            this.kinectColorView.converting = false;
            this.kinectColorView.Dispose();
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

        private int makeVideo(string video_type)
        {
            List<Image<Bgr, byte>> Video = new List<Image<Bgr, byte>>();
            if (string.Compare(video_type, "body") == 0)
                Video = this.kinectBodyView.Video;

            else if (string.Compare(video_type, "color") == 0)
                Video = this.kinectColorView.Video;
            int videoCount = Video.Count;
            Console.WriteLine($"video count: {Video.Count}");
            Console.WriteLine($"{Video[0].Width} {Video[1].Height}");
            if (!Directory.Exists(this.frontDataPath))
                Directory.CreateDirectory(this.frontDataPath);

            using (VideoWriter vw = new VideoWriter($"{this.frontDataPath}\\{video_type}.avi", 30, Video[0].Width, Video[0].Height, true))
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
                this.identity = "coach";
            else
                this.identity = "student";
            NameUpdateState();
        }

        private void MotionRadio_Click(object sender, RoutedEventArgs e)
        {
            if (smashRadio.IsChecked == true)
                this.motion = "smash";
            else if (lobRadio.IsChecked == true)
                this.motion = "lob";
            else
                this.motion = "serve";
            MainWindow.actionTypeControl = this.motion;
            //Console.WriteLine(MainWindow.actionTypeControl);
            NameUpdateState();
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
            NameUpdateState();
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
            list.Add("請選擇");
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
                    string cur = Environment.CurrentDirectory;
                    string relatePath = $"\\..\\..\\..\\data\\student\\{newClassName}";
                    Directory.CreateDirectory(cur + relatePath);
                    ArrayList list = new ArrayList();
                    foreach (string s in comboBox.Items)
                    {
                        list.Add(s);
                    }
                    list.Add(newClassName);
                    comboBox.ItemsSource = list;
                    comboBox.SelectedIndex = comboBox.Items.Count - 1;
                    MainWindow.nowSelectedName = newClassName;
                }
            }
            else
                this.className = comboBox.SelectedItem as string;
            NameUpdateState();
            MainWindow.classNameControl = comboBox.SelectedItem as String;
        }

        private void studentNameList_Loaded(object sender, RoutedEventArgs e)
        {
            NameUpdateState();
        }

        private void studentNameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;

            this.convertName = comboBox.SelectedItem as string;

            string cur = Environment.CurrentDirectory;
            string parentDir = string.Empty;
            if (this.identity == "coach")
            {
                parentDir = $"{cur}\\..\\..\\..\\data\\coach\\{this.motion}";
            }
            else if(this.identity == "student")
            {
                parentDir = $"{cur}\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}";
            }
            this.xefFilePath = $"{parentDir}\\xefjoints\\{comboBox.SelectedItem.ToString()}.xef";
            this.jointsJsonPath = $"{parentDir}\\xefjoints\\{comboBox.SelectedItem.ToString()}.json";

            //Console.WriteLine(this.studentNameConvert);
        }

        private void NameUpdateState()
        {
            if (this.identity == "student")
            {
                this.classList.IsEnabled = true;
                this.week_control.IsEnabled = true;
            }
            else if (this.identity == "coach")
            {
                this.classList.IsEnabled = false;
                this.week_control.IsEnabled = false;
            }
            if (this.identity == "coach")
            {
                string cur = Environment.CurrentDirectory;
                string fileDir = $"{cur}\\..\\..\\..\\data\\coach\\{this.motion}\\xefjoints";
                //Directory.CreateDirectory(filePath);
                DirectoryInfo dirInfo = new DirectoryInfo(fileDir);
                ArrayList list = new ArrayList();
                foreach (FileInfo f in dirInfo.GetFiles())
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(f.FullName);
                    if (!list.Contains(name))
                        list.Add(name);
                }
                if (dirInfo.GetDirectories().Length == 0)
                    studentNameList.IsEnabled = false;
                else
                    studentNameList.IsEnabled = true;
                studentNameList.ItemsSource = list;
            }
            else if(this.identity == "student")
            {
                if (!(classList.SelectedItem as string == "請選擇") && !(classList.SelectedItem as string == "新增班級"))
                {
                    string cur = Environment.CurrentDirectory;
                    string fileDir = $"{cur}\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}\\xefjoints";
                    DirectoryInfo dirInfo = new DirectoryInfo(fileDir);
                    ArrayList list = new ArrayList();
                    foreach (FileInfo f in dirInfo.GetFiles())
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension(f.FullName);
                        
                        if (!list.Contains(name))
                            list.Add(name);
                    }
                    if (dirInfo.GetFiles().Length == 0)
                        studentNameList.IsEnabled = false;
                    else
                        studentNameList.IsEnabled = true;
                    studentNameList.ItemsSource = list;
                }
            }
            
        }    
    }
}
