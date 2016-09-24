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
        private string studentNameConvert = string.Empty;
        private string coachNameConvert = string.Empty;
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

        private string justConvertFilePath = string.Empty;
        private bool bodyConvertBad = false;
        private bool ColorConvertBad = false;

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
            if(this.kinectStatusText == "Running")
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
                NameUpdateState();
            }
            else
            {
                MessageBox.Show("請先連接Kinect才能進行錄製", "錯誤");
            }
        }

        private void RecordDone(string fileDir)
        {
            this.kinectBodyView.SaveJointData(fileDir);
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
            this.Dispatcher.BeginInvoke(new OneArgDelegate(RecordDone), filePath);
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
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
                if (this.bodyConvertBad || this.ColorConvertBad)
                {
                    MessageBox.Show("影片轉檔Lose frame嚴重。建議：先刪除檔案並將程式關閉重啟。", "建議");
                    this.bodyConvertBad = false;
                    this.ColorConvertBad = false;
                }
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
                    MessageBoxResult messageBoxResult1 = MessageBox.Show(
                    $".xef檔佔據大量儲存空間，是否刪除？",
                    "確認", MessageBoxButton.YesNo);
                    if (messageBoxResult1 == MessageBoxResult.Yes)
                    {
                        MessageBoxResult messageBoxResult2 = MessageBox.Show(
                        $"刪除後無法復原，包含重新評分與製作彩色及骨架影片。",
                        "確認", MessageBoxButton.YesNo);
                        if (messageBoxResult2 == MessageBoxResult.Yes)
                        {
                            if (File.Exists(this.justConvertFilePath))
                            {
                                File.Delete(this.justConvertFilePath);
                                MessageBox.Show($"已刪除 {new FileInfo(this.justConvertFilePath).Name}");
                                NameUpdateState();
                            }
                            else
                                Console.WriteLine(this.justConvertFilePath);
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
                
                if(classList.SelectedItem as string == "請選擇" || classList.SelectedItem as string == "新增班級")
                {
                    MessageBox.Show("請選擇班級名稱", "錯誤");
                }
                else
                {
                    string newStudentName = Microsoft.VisualBasic.Interaction.InputBox("請輸入學員名稱", "新增錄影", "", -1, -1);
                    if (string.IsNullOrWhiteSpace(newStudentName))
                    {
                        MessageBox.Show("學員名稱不可為空白", "錯誤");
                    }
                    else
                    {
                        string folderName = newStudentName + "_" + DateTime.Now.ToString("HH-mm-ss(yyyy-MM-dd)");
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
            }
            return fileName;
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.kinectStatusText == "Running"/*"Kinect not available!"*/)
            {
                MessageBox.Show("請先移除Kinect裝置", "錯誤");
            }
            else
            {
                string filePath = string.Empty;
                string xefPath = string.Empty;
                string jointJsonPath = string.Empty;
                if (this.idenity == "student")
                {
                    if (classList.SelectedItem as string == "請選擇" || classList.SelectedItem as string == "新增班級")
                    {
                        MessageBox.Show("請選擇班級名稱", "錯誤");
                    }
                    else if (this.studentNameConvert == "請選擇" || String.IsNullOrEmpty(this.studentNameConvert))
                    {
                        MessageBox.Show("請選擇名稱", "錯誤");
                    }
                    else
                    {
                        string cur = Environment.CurrentDirectory;
                        filePath = $"{cur}\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}\\{this.studentNameConvert}";
                        xefPath = $"{filePath}\\{this.studentNameConvert}.xef";
                        jointJsonPath = $"{filePath}\\joints.json";
                    }
                }
                //idenity == coach
                else
                {
                    //filePath = this.OpenFileForConvert();
                    if (this.coachNameConvert == "請選擇" || String.IsNullOrEmpty(this.coachNameConvert))
                    {
                        MessageBox.Show("請選擇名稱", "錯誤");
                    }
                    else
                    {
                        string cur = Environment.CurrentDirectory;
                        filePath = $"{cur}\\..\\..\\..\\data\\coach\\{this.motion}\\{this.coachNameConvert}";
                        xefPath = $"{filePath}\\{this.coachNameConvert}.xef";
                        jointJsonPath = $"{filePath}\\joints.json";
                    }

                }
                if (File.Exists(xefPath))
                {
                    this.justConvertFilePath = xefPath;
                    ConvertBody(xefPath);
                }
                else if (File.Exists(jointJsonPath))
                {
                    Console.WriteLine("joint json exist");
                    this.kinectBodyView.Judge(jointJsonPath, this.handedness);
                }
                else
                {
                    Console.WriteLine(xefPath);
                    Console.WriteLine(jointJsonPath);
                }
            }
        }

        private void ConvertBody(String filePath)
        {
            this.kinectBodybox.DataContext = null;
            this.kinectColorbox.DataContext = null;
            this.kinectBodyView = new KinectBodyView(this.kinectSensor, this.motion, MainWindow.MediaPlayerSize, this);

            this.converting = true;
            //this.kinectBodybox.DataContext = this.kinectBodyView;
            UpdateState();
            Console.WriteLine($"main: {Thread.CurrentThread.ManagedThreadId}");
            //string name = new DirectoryInfo(System.IO.Path.GetDirectoryName(filePath)).Name;
            string name;
            if (this.idenity == "student")
            {
                name = this.studentNameConvert;
            }
            else
            {
                name = new DirectoryInfo(System.IO.Path.GetDirectoryName(filePath)).Name;
            }
            this.PlayBackLabel.Content = $"正在對{name.Split('_')[0]}進行評分，並製作彩色及骨架影片...";
            this.PlayBackLabel.Visibility = Visibility.Visible;
            TwoArgDelegate bodyConvert = new TwoArgDelegate(this.BodyConvertClip);
            AsyncCallback callback = new AsyncCallback(myCallbackMethod);
            IAsyncResult result = bodyConvert.BeginInvoke(filePath, name, callback, null);
        }

        private void myCallbackMethod(IAsyncResult result)
        {
            Console.WriteLine($"callback: {Thread.CurrentThread.ManagedThreadId}");
        }
        
        private void ConvertColor(String filePath)
        {
            Thread.Sleep(100);
            this.kinectBodybox.DataContext = null;
            this.kinectColorbox.DataContext = null;
            //Console.WriteLine($"menu: {MainWindow.MediaPlayerSize.Width}");
            //this.kinectColorView = new KinectColorView(this.kinectSensor, MainWindow.MediaPlayerSize, this);

            this.converting = true;
            //this.kinectColorbox.DataContext = this.kinectColorView;
            string name;
            if (this.idenity == "student")
            {
                name = this.studentNameConvert;
            }
            else
            {
                name = new DirectoryInfo(System.IO.Path.GetDirectoryName(filePath)).Name;
            }
            TwoArgDelegate colorConvert = new TwoArgDelegate(this.ColorConvertClip);
            colorConvert.BeginInvoke(filePath, name, null, null);
        }

        /// <summary>Plays back a .xef file to the Kinect sensor</summary>
        /// <param name="filePath">Full path to the .xef file that should be played back to the sensor</param
        /// <param name="personName">The name of tester in the video</param>
        public bool convertLock = false;
        private void BodyConvertClip(string filePath, string personName)
        {
            
            int nowFrame = 0;
            int prevFrame = 0;
            int totalFrame = 0;
            using (KStudioClient client = KStudio.CreateClient())
            {

                client.ConnectToService();
                using (KStudioPlayback playback = client.CreatePlayback(filePath))
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
                            while (this.convertLock)
                            {

                            }
                            Thread.Sleep(5);
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
            this.Dispatcher.BeginInvoke(new ThreeArgDelegate(BodyConvertDone), personName, filePath, totalFrame);
        }

        private void BodyConvertDone(string personName, string filePath, int totalFrame)
        {
            int videoCount = makeVideo("body", personName);
            double loseFrameRate = 1 - (double)videoCount/totalFrame;
            if (loseFrameRate > 0.06)
            {
                this.bodyConvertBad = true;
                Console.WriteLine("body bad");
            }
            this.converting = false;
            this.kinectBodyView.converting = false;
            this.kinectBodyView.SaveJointData(filePath);
            this.kinectBodyView.Judge(filePath, this.handedness);
            this.kinectBodyView.Dispose();
            
            string colorAviPath = $"{Directory.GetParent(filePath).FullName}\\color.avi";
            if (File.Exists(colorAviPath))
            {
                this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
            }
            else
                this.Dispatcher.BeginInvoke(new OneArgDelegate(ConvertColor), filePath);
        }

        private void ColorConvertClip(string filePath, string personName)
        {
            int totalFrame = 0;
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
                        //Console.WriteLine()
                        if(nowFrame > prevFrame)
                        {
                            playback.Pause();
                            while (this.convertLock)
                            {

                            }
                            Thread.Sleep(50);
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
            this.Dispatcher.BeginInvoke(new StringIntDelegate(ColorConvertDone), personName, totalFrame);
            this.Dispatcher.BeginInvoke(new NoArgDelegate(UpdateState));
        }

        private void ColorConvertDone(string personName, int totalFrame)
        {
            int videoCount = makeVideo("color", personName);
            double loseFrameRate = 1 - (double)videoCount/totalFrame;
            Console.WriteLine(loseFrameRate);
            if (loseFrameRate > 0.06)
            {
                this.ColorConvertBad = true;
                Console.WriteLine("color bad");
                //MessageBox.Show("彩色影片Lose frame嚴重。建議改善方式：將程式關閉並重新開啟", "建議");
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

        private int makeVideo(string video_type, string personName)
        {
            List<Image<Bgr, byte>> Video = new List<Image<Bgr, byte>>();
            if (string.Compare(video_type, "body") == 0)
                Video = this.kinectBodyView.Video;

            else if (string.Compare(video_type, "color") == 0)
                Video = this.kinectColorView.Video;
            int videoCount = Video.Count;
            Console.WriteLine($"video count: {Video.Count}");
            string path = null;
            if (this.idenity == "student")
            {
                path = $"{Environment.CurrentDirectory}\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}\\{personName}";
            }
            //idenity == coach
            else if (this.idenity == "coach")
            {
                path = $"{Environment.CurrentDirectory}\\..\\..\\..\\data\\coach\\{this.motion}\\{personName}";
            }
            Directory.CreateDirectory(path);
            Console.WriteLine($"{Video[0].Width} {Video[1].Height}");
            using (VideoWriter vw = new VideoWriter($"{path}\\{video_type}.avi", 30, Video[0].Width, Video[0].Height, true))
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
            string filepath = string.Empty;
            if (this.idenity == "coach")
            {
                string cur = Environment.CurrentDirectory;
                string relatePath = $"\\..\\..\\..\\data\\coach\\{this.motion}";
                filepath = cur + relatePath;
                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }
                DirectoryInfo dirInfo = new DirectoryInfo(filepath);
                ArrayList list = new ArrayList();
                list.Add("請選擇");
                foreach (DirectoryInfo d in dirInfo.GetDirectories())
                {
                    list.Add(d.Name);
                }
                var comboBox = sender as ComboBox;
                comboBox.ItemsSource = list;
                comboBox.SelectedIndex = 0;
            }
            //idenity == student
            else if (!(classList.SelectedItem as string == "請選擇") && !(classList.SelectedItem as string == "新增班級"))
            {
                string cur = Environment.CurrentDirectory;
                string relatePath = $"\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}";
                filepath = cur + relatePath;
                if (!Directory.Exists(filepath))
                {
                    Directory.CreateDirectory(filepath);
                }
                DirectoryInfo dirInfo = new DirectoryInfo(filepath);
                ArrayList list = new ArrayList();
                list.Add("請選擇");
                foreach (DirectoryInfo d in dirInfo.GetDirectories())
                {
                    list.Add(d.Name);
                }
                var comboBox = sender as ComboBox;
                comboBox.ItemsSource = list;
                comboBox.SelectedIndex = 0;
            }
        }

        private void studentNameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (this.idenity == "student")
            {
                this.studentNameConvert = comboBox.SelectedItem as string;
            }
            //idenity == coach
            else
            {
                this.coachNameConvert = comboBox.SelectedItem as string;
            }
            //Console.WriteLine(this.studentNameConvert);
        }

        private void NameUpdateState()
        {
            if (this.idenity == "student")
            {
                this.classList.IsEnabled = true;
                this.week_control.IsEnabled = true;
            }
            else if (this.idenity == "coach")
            {
                this.classList.IsEnabled = false;
                this.week_control.IsEnabled = false;
            }
            string filepath = string.Empty;
            if (this.idenity == "coach")
            {
                string cur = Environment.CurrentDirectory;
                string relatePath = $"\\..\\..\\..\\data\\coach\\{this.motion}";
                filepath = cur + relatePath;
                Console.WriteLine(filepath);
                Directory.CreateDirectory(filepath);
                DirectoryInfo dirInfo = new DirectoryInfo(filepath);
                ArrayList list = new ArrayList();
                foreach (DirectoryInfo d in dirInfo.GetDirectories())
                {
                    string xefPath = $"{d.FullName}\\{d.Name}.xef";
                    string jointJsonPath = $"{d.FullName}\\joints.json";
                    //Console.WriteLine(jointJsonPath);
                    if (File.Exists(xefPath) || File.Exists(jointJsonPath))
                        list.Add(d.Name);
                }
                if (dirInfo.GetDirectories().Length == 0)
                    studentNameList.IsEnabled = false;
                else
                    studentNameList.IsEnabled = true;
                studentNameList.ItemsSource = list;
            }
            else
            {
                if (!(classList.SelectedItem as string == "請選擇") && !(classList.SelectedItem as string == "新增班級"))
                {
                    string cur = Environment.CurrentDirectory;
                    string relatePath = $"\\..\\..\\..\\data\\student\\{this.className}\\{this.week}\\{this.motion}";
                    filepath = cur + relatePath;
                    Directory.CreateDirectory(filepath);
                    DirectoryInfo dirInfo = new DirectoryInfo(filepath);
                    ArrayList list = new ArrayList();
                    foreach (DirectoryInfo d in dirInfo.GetDirectories())
                    {
                        string xefPath = $"{d.FullName}\\{d.Name}.xef";
                        string jointJsonPath = $"{d.FullName}\\joints.json";
                        //Console.WriteLine(jointJsonPath);
                        if (File.Exists(xefPath) || File.Exists(jointJsonPath))
                            list.Add(d.Name);
                    }
                    if (dirInfo.GetDirectories().Length == 0)
                        studentNameList.IsEnabled = false;
                    else
                        studentNameList.IsEnabled = true;
                    studentNameList.ItemsSource = list;
                }
            }
            
        }    
    }
}
