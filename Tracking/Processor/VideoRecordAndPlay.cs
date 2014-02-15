using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [ViewTemplate("Video Record / Play", "VideoRecordAndPlay", "/FlockingDevice.Tracking;component/Resources/film2.png")]
    public class VideoRecordAndPlay : RgbProcessor
    {
        #region private fields

        private readonly Dictionary<VideoMetadata, VideoWriter> _recorders = new Dictionary<VideoMetadata, VideoWriter>();
        private Capture _player;

        private string _tempRecordingPath;

        private bool _isRecording;

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

            PlayCommand = new RelayCommand(() =>
            {

            });

            StopCommand = new RelayCommand(() =>
            {
                if (!_isRecording) return;

                _isRecording = false;

                var fileDialog = new SaveFileDialog { Filter = "Flocking Device Recording|.zip.fdr" };

                var result = fileDialog.ShowDialog(Application.Current.MainWindow);

                if (!result.Value) return;

                var task = Task.Factory.StartNew(() =>
                {
                    // stop recorders, otherwise the video data cannot be stored in zip archive
                    foreach (var recorder in _recorders.Values)
                        recorder.Dispose();

                    var metadata = new Metadata { Items = _recorders.Keys.ToList() };

                    var serializer = new XmlSerializer(typeof(Metadata));
                    using (var stream = new FileStream(GetTempFilePath(".metadata"), FileMode.Create))
                        serializer.Serialize(stream, metadata);

                    if (File.Exists(fileDialog.FileName))
                        File.Delete(fileDialog.FileName);

                    ZipFile.CreateFromDirectory(_tempRecordingPath, fileDialog.FileName, CompressionLevel.Optimal, false);

                    // cleanup resources
                    if (Directory.Exists(_tempRecordingPath))
                        Directory.Delete(_tempRecordingPath, true);
                });
                task.ContinueWith(t =>
                {
                    MessageBox.Show("Video recording saved.", "Recordings");
                });
            });

            RecordCommand = new RelayCommand(() =>
            {
                if (_isRecording) return;

                _tempRecordingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                if (!Directory.Exists(_tempRecordingPath))
                    Directory.CreateDirectory(_tempRecordingPath);

                _isRecording = true;
            });

            #endregion

            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case FilenamePropertyName:
                        UpdateRecorderAndPlayer();
                        break;
                    case ModePropertyName:
                        UpdateRecorderAndPlayer();
                        break;
                }
            };
            UpdateRecorderAndPlayer();
        }

        #endregion

        private void UpdateRecorderAndPlayer()
        {
            if (Equals(Mode, "Recorder"))
            {
                if (_player != null)
                    _player.Dispose();
            }
            else if (Equals(Mode, "Player"))
            {
                DisposeRecorders();

                _player = new Capture(Filename);

                new Thread(() =>
                {
                    long frameId = 0;

                    Start();

                    Image<Bgr, byte> image;
                    while ((image = _player.QueryFrame()) != null)
                    {
                        var imageCopy = image.Copy();
                        Publish(new DataContainer(++frameId, DateTime.Now)
                        {
                            new RgbImageData("depth", imageCopy.Convert<Rgb, byte>())
                        });
                        //image.Dispose();

                        Thread.Sleep(1000 / Fps);
                    }
                }).Start();
            }
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

                _recorders.Add(videoMetadata, new VideoWriter(
                    GetTempFilePath(videoMetadata.FileName),
                    Fps,
                    videoMetadata.Width,
                    videoMetadata.Height,
                    true)
                );
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

            if (_player != null)
                _player.Dispose();

            base.Stop();
        }

        private void DisposeRecorders()
        {
            foreach (var recorder in _recorders.Values)
                recorder.Dispose();
        }

        public string GetTempFilePath(string fileName, string extension = null)
        {
            fileName = string.Format("{0}{1}", fileName, extension ?? "");
            return Path.Combine(_tempRecordingPath, fileName);
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
