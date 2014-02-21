using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.External.Extensions;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Util;
using Microsoft.Win32;

namespace Huddle.Engine.Processor
{
    [ViewTemplate("Video Record / Play", "VideoRecordAndPlay", "/Huddle.Engine;component/Resources/film2.png")]
    public class VideoRecordAndPlay : RgbProcessor
    {
        #region private fields

        private readonly Dictionary<VideoMetadata, VideoWriter> _recorders = new Dictionary<VideoMetadata, VideoWriter>();
        private readonly Dictionary<VideoMetadata, Capture> _players = new Dictionary<VideoMetadata, Capture>();

        private string _tmpRecordPath;
        private string _tmpPlayPath;

        private bool _isRecording;
        private bool _isPlaying;

        #endregion

        #region commands

        public RelayCommand PlayCommand { get; private set; }

        public RelayCommand StopCommand { get; private set; }

        public RelayCommand RecordCommand { get; private set; }

        #endregion

        #region properties

        #region Filename

        /// <summary>
        /// The <see cref="Filename" /> property's name.
        /// </summary>
        public const string FilenamePropertyName = "Filename";

        private string _filename = "MyRecording";

        /// <summary>
        /// Sets and gets the Filename property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Filename
        {
            get
            {
                return _filename;
            }

            set
            {
                if (_filename == value)
                {
                    return;
                }

                RaisePropertyChanging(FilenamePropertyName);
                _filename = value;
                RaisePropertyChanged(FilenamePropertyName);
            }
        }

        #endregion

        #region Fps

        /// <summary>
        /// The <see cref="Fps" /> property's name.
        /// </summary>
        public const string FpsPropertyName = "Fps";

        private int _fps = 30;

        /// <summary>
        /// Sets and gets the Fps property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int Fps
        {
            get
            {
                return _fps;
            }

            set
            {
                if (_fps == value)
                {
                    return;
                }

                RaisePropertyChanging(FpsPropertyName);
                _fps = value;
                RaisePropertyChanged(FpsPropertyName);
            }
        }

        #endregion

        #region Mode

        [IgnoreDataMember]
        public static string[] Modes { get { return new[] { "Recorder", "Player" }; } }

        /// <summary>
        /// The <see cref="Mode" /> property's name.
        /// </summary>
        public const string ModePropertyName = "Mode";

        private string _mode = Modes[0];

        /// <summary>
        /// Sets and gets the Mode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                if (_mode == value)
                {
                    return;
                }

                RaisePropertyChanging(ModePropertyName);
                _mode = value;
                RaisePropertyChanged(ModePropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public VideoRecordAndPlay()
        {
            #region commands

            RecordCommand = new RelayCommand(OnRecord);
            PlayCommand = new RelayCommand(OnPlay);
            StopCommand = new RelayCommand(OnStop);

            #endregion

            //PropertyChanged += (s, e) =>
            //{
            //    switch (e.PropertyName)
            //    {
            //        case FilenamePropertyName:
            //            UpdateRecorderAndPlayer();
            //            break;
            //        case ModePropertyName:
            //            UpdateRecorderAndPlayer();
            //            break;
            //    }
            //};
            //UpdateRecorderAndPlayer();

            var fileInfo = new FileInfo(@"C:\Users\raedle\Downloads\test23\color.avi");
            if (!fileInfo.Exists)
                return;

            var fileInfoCopy = fileInfo.CopyTo(Path.Combine(fileInfo.DirectoryName, "test3.avi"), true);

            _capture = new Capture(fileInfoCopy.FullName);
        }

        #endregion

        private void UpdateRecorderAndPlayer()
        {
            //if (Equals(Mode, "Recorder"))
            //{
            //    if (_player != null)
            //        _player.Dispose();
            //}
            //else if (Equals(Mode, "Player"))
            //{
            //    DisposeRecorders();

            //    _player = new Capture(Filename);

            //    new Thread(() =>
            //    {
            //        long frameId = 0;

            //        Start();

            //        Image<Bgr, byte> image;
            //        while ((image = _player.QueryFrame()) != null)
            //        {
            //            var imageCopy = image.Copy();
            //            Publish(new DataContainer(++frameId, DateTime.Now)
            //            {
            //                new RgbImageData("depth", imageCopy.Convert<Rgb, byte>())
            //            });
            //            //image.Dispose();

            //            Thread.Sleep(1000 / Fps);
            //        }
            //    }).Start();
            //}
        }

        public override IData Process(IData data)
        {
            var imageData = data as RgbImageData;

            if (_isRecording && imageData != null)
                Record(imageData);

            return data;
        }

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            return image;
        }

        public void Record(RgbImageData imageData)
        {
            if (!_recorders.Any(kvp => Equals(kvp.Key.Key, imageData.Key)))
            {
                //var width = imageData.Image.Width;
                //var height = imageData.Image.Height;
                //var filename = string.Format("{0}_{1}_{2}x{3}_{4}.avi", Filename, imageData.Key, width, height, Fps);

                var videoMetadata = new VideoMetadata
                {
                    Key = imageData.Key,
                    FileName = string.Format("{0}{1}", imageData.Key, ".avi"),
                    Width = imageData.Image.Width,
                    Height = imageData.Image.Height,
                    Fps = Fps
                };

                //VideoWriter captureOutput = new VideoWriter(@"test.avi", -1, 1, width, height, true);
                //var captureOutput = new VideoWriter(@"test.avi", CvInvoke.CV_FOURCC('W', 'M', 'V', '3'), 1, width, height, true);
                var videoWriter = new VideoWriter(
                    GetTempFilePath(_tmpRecordPath, videoMetadata.FileName),
                    -1,
                    Fps,
                    videoMetadata.Width,
                    videoMetadata.Height,
                    true);

                _recorders.Add(videoMetadata, videoWriter);
            }

            // TODO: The _recorders.Single may raise an exception if sequence is empty or multiple items in sequence match
            var recorder = _recorders.Single(kvp => Equals(kvp.Key.Key, imageData.Key)).Value;

            //_recorder = new VideoWriter(Filename, 10, 320, 240, true);
            if (recorder != null)
            {
                Image<Rgb, byte> imageCopy = null;
                try
                {
                    imageCopy = imageData.Image;
                    recorder.WriteFrame(imageCopy.Convert<Bgr, byte>());
                }
                finally
                {
                    if (imageCopy != null)
                        imageCopy.Dispose();
                }
            }
        }

        public override void Stop()
        {
            DisposeRecorders();
            DisposePlayers();

            //if (_player != null)
            //    _player.Dispose();

            base.Stop();
        }

        #region Record and Play Actions

        private void OnRecord()
        {
            if (_isRecording) return;

            _tmpRecordPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            if (!Directory.Exists(_tmpRecordPath))
                Directory.CreateDirectory(_tmpRecordPath);

            _isRecording = true;
        }

        private Capture _capture;
        private Image<Bgr, byte> _frame; 

        private void OnPlay()
        {
            if (_isPlaying) return;

            _isPlaying = true;

            new Thread(() =>
            {
                while (_isPlaying)
                {
                    try
                    {
                        _frame = _capture.QueryFrame();
                        Console.WriteLine("Queried");
                    }
                    catch (AccessViolationException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Thread.Sleep(5000);
                }
            }).Start();

            //var fileDialog = new OpenFileDialog { Filter = "Huddle Engine Recording|*.rec.huddle" };

            //var result = fileDialog.ShowDialog(Application.Current.MainWindow);

            //if (!result.Value) return;

            //var task = Task.Factory.StartNew(() =>
            //{
            //    _tmpPlayPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            //    ZipFile.ExtractToDirectory(fileDialog.FileName, _tmpPlayPath);


            //    //Start();

            ////    while (_isPlaying)
            ////    {
            ////        foreach (var kvp in _players)
            ////        {
            ////            var videoMetadata = kvp.Key;
            ////            var player = kvp.Value;

            ////            try
            ////            {
            ////                Image<Bgr, byte> image;
            ////                if ((image = player.QueryFrame()) == null) continue;
            ////                var imageCopy = image.Clone();
            ////                image.Dispose();

            ////                Console.WriteLine("run");

            ////                var imageCopy2 = imageCopy.Copy();
            ////                DispatcherHelper.RunAsync(() =>
            ////                {
            ////                    PostProcessImage = imageCopy2.ToBitmapSource();
            ////                    imageCopy2.Dispose();
            ////                });

            ////                //Stage(new RgbImageData(videoMetadata.Key, imageCopy.Convert<Rgb, byte>()));
            ////            }
            ////            catch (Exception)
            ////            {
            ////                // ignore
            ////            }
            ////        }
            ////        Push();

            ////        //Thread.Sleep(1000 / Fps);
            ////    }
            //});

            //task.ContinueWith(t =>
            //{
            //    Metadata metadata;

            //    var serializer = new XmlSerializer(typeof(Metadata));
            //    using (var stream = new FileStream(GetTempFilePath(_tmpPlayPath, ".metadata"), FileMode.Open))
            //        metadata = serializer.Deserialize(stream) as Metadata;

            //    if (metadata == null)
            //        throw new Exception("Could not load recording metadata");

            //    foreach (var item in metadata.Items)
            //    {
            //        var player = new Capture(GetTempFilePath(_tmpPlayPath, item.FileName));
            //        _players.Add(item, player);

            //        player.ImageGrabbed += player_ImageGrabbed;
            //        player.Start();
            //    }
            //    //DisposePlayers();
            //    //StopPlaying();
            //}, TaskScheduler.Current);
        }

        private int i = 0;

        void player_ImageGrabbed(object sender, EventArgs e)
        {
            Console.WriteLine("echo {0}", i++);

            var frame = _capture.RetrieveBgrFrame();

            //Image<Gray, Byte> grayFrame = image.Convert<Gray, Byte>();
            //Image<Gray, Byte> smallGrayFrame = grayFrame.PyrDown();
            //Image<Gray, Byte> smoothedGrayFrame = smallGrayFrame.PyrUp();
            //Image<Gray, Byte> cannyFrame = smoothedGrayFrame.Canny(100, 60);

            //Image<Bgr, byte> image;
            //if ((image = player.RetrieveBgrFrame()) == null) return;
            //var imageCopy = image.Clone();
            //image.Dispose();

            Console.WriteLine("run");

            //var imageCopy2 = image.Copy();
            //DispatcherHelper.RunAsync(() =>
            //{
            //    PostProcessImage = imageCopy2.ToBitmapSource();
            //    imageCopy2.Dispose();
            //});

            //Stage(new RgbImageData("mykey", image.Convert<Rgb, byte>()));
            //Push();
        }

        private void OnStop()
        {
            //Delete _tmpPlayPath if isPlaying

            if (_isRecording)
            {
                SaveRecording();
            }
            else if (_isPlaying)
            {
                StopPlaying();
            }
        }

        private void SaveRecording()
        {
            var fileDialog = new SaveFileDialog { Filter = "Huddle Engine Recording|*.rec.huddle" };

            var result = fileDialog.ShowDialog(Application.Current.MainWindow);

            if (!result.Value) return;

            var task = Task.Factory.StartNew(() =>
            {
                // stop recorders, otherwise the video data cannot be stored in zip archive
                foreach (var recorder in _recorders.Values)
                    recorder.Dispose();

                var metadata = new Metadata { Items = _recorders.Keys.ToList() };

                var serializer = new XmlSerializer(typeof(Metadata));
                using (var stream = new FileStream(GetTempFilePath(_tmpRecordPath, ".metadata"), FileMode.Create))
                    serializer.Serialize(stream, metadata);

                if (File.Exists(fileDialog.FileName))
                    File.Delete(fileDialog.FileName);

                ZipFile.CreateFromDirectory(_tmpRecordPath, fileDialog.FileName, CompressionLevel.Optimal, false);

                // cleanup resources
                if (Directory.Exists(_tmpRecordPath))
                    Directory.Delete(_tmpRecordPath, true);
            });
            task.ContinueWith(t =>
            {
                _isRecording = false;
                MessageBox.Show("Video recording saved.", "Recordings");
            });
        }

        private void StopPlaying()
        {
            // cleanup resources
            if (Directory.Exists(_tmpPlayPath))
                Directory.Delete(_tmpPlayPath, true);

            _isPlaying = false;
        }

        #endregion

        private void DisposeRecorders()
        {
            foreach (var recorder in _recorders.Values)
                recorder.Dispose();

            _recorders.Clear();
        }

        private void DisposePlayers()
        {
            foreach (var player in _players.Values)
                player.Dispose();

            _players.Clear();
        }

        public string GetTempFilePath(string tmpPath, string fileName, string extension = null)
        {
            fileName = string.Format("{0}{1}", fileName, extension ?? "");
            return Path.Combine(tmpPath, fileName);
        }
    }

    [XmlRoot]
    public class Metadata
    {
        #region properties

        #region Items

        [XmlArray]
        [XmlArrayItem]
        public List<VideoMetadata> Items { get; set; }

        #endregion

        #endregion
    }

    [XmlType]
    public class VideoMetadata
    {
        #region properties

        #region Key

        [XmlAttribute]
        public string Key { get; set; }

        #endregion

        #region FileName

        [XmlAttribute]
        public string FileName { get; set; }

        #endregion

        #region Width

        [XmlElement]
        public int Width { get; set; }

        #endregion

        #region Height

        [XmlElement]
        public int Height { get; set; }

        #endregion

        #region Fps

        [XmlElement]
        public int Fps { get; set; }

        #endregion

        #endregion

        #region Override Equals and HashCode

        public override bool Equals(object obj)
        {
            var other = obj as VideoMetadata;
            return other != null && Equals(Key, other.Key);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        #endregion
    }
}
