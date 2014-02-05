using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [XmlType]
    [ViewTemplate("VideoRecordAndPlay")]
    public class VideoRecordAndPlay : RgbProcessor
    {
        #region private fields

        private Dictionary<string, VideoWriter> _recorders = new Dictionary<string, VideoWriter>();
        private Capture _player;

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

        #region Mode

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
                        Publish(new DataContainer(++frameId, DateTime.Now)
                        {
                            new RgbImageData("depth", image.Convert<Rgb, byte>())
                        });
                        image.Dispose();

                        Thread.Sleep(1000 / 1);
                    }
                }).Start();
            }
        }

        public override IData Process(IData data)
        {
            if (!string.IsNullOrWhiteSpace(ImageKey) && !Equals(ImageKey, data.Key))
                return base.Process(data);

            var imageData = data as RgbImageData;

            if (imageData != null)
                Record(imageData);

            return data;
        }

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            return image;
        }

        public void Record(RgbImageData imageData)
        {
            if (!_recorders.ContainsKey(imageData.Key))
            {
                var width = imageData.Image.Width;
                var height = imageData.Image.Height;
                const int fps = 30;
                var filename = string.Format("{0}_{1}_{2}x{3}_{4}.avi", Filename, imageData.Key, width, height, fps);
                _recorders.Add(imageData.Key, new VideoWriter(filename, fps, width, height, true));
            }

            var recorder = _recorders[imageData.Key];
                
            //_recorder = new VideoWriter(Filename, 10, 320, 240, true);
            if (recorder != null)
                recorder.WriteFrame(imageData.Image.Convert<Bgr, byte>());
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
    }
}
