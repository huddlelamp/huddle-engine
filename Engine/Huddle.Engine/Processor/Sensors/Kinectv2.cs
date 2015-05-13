using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Command;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.Sensors.Utils;
using Huddle.Engine.Util;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;
using Microsoft.Kinect;

namespace Huddle.Engine.Processor.Sensors
{
    [ViewTemplate("Kinectv2", "Kinectv2", "/Huddle.Engine;component/Resources/kinect.png")]
    public class Kinectv2 : BaseProcessor
    {
        #region private fields

        private KinectSensor _sensor;

        private MultiSourceFrameReader _reader;

        private bool _isRunning;

        private long _frameId = -1;

        private Rectangle _rgbInDepthROI = Rectangle.Empty;

        #endregion

        #region properties

        #region ColorImageProfile

        [IgnoreDataMember]
        public static string[] ColorImageProfiles
        {
            get
            {
                return new[]
                {
                    "1280 x 720",
                    "640 x 480"
                };
            }
        }

        /// <summary>
        /// The <see cref="ColorImageProfile" /> property's name.
        /// </summary>
        public const string ColorImageProfilePropertyName = "ColorImageProfile";

        private string _colorImageProfile = ColorImageProfiles.First();

        /// <summary>
        /// Sets and gets the ColorImageProfile property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string ColorImageProfile
        {
            get
            {
                return _colorImageProfile;
            }

            set
            {
                if (_colorImageProfile == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorImageProfilePropertyName);
                _colorImageProfile = value;
                RaisePropertyChanged(ColorImageProfilePropertyName);
            }
        }

        #endregion

        #region DepthConfidenceThreshold

        /// <summary>
        /// The <see cref="DepthConfidenceThreshold" /> property's name.
        /// </summary>
        public const string DepthConfidenceThresholdPropertyName = "DepthConfidenceThreshold";

        private float _depthConfidenceThreshold = 0;

        /// <summary>
        /// Sets and gets the DepthConfidenceThreshold property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public float DepthConfidenceThreshold
        {
            get
            {
                return _depthConfidenceThreshold;
            }

            set
            {
                if (_depthConfidenceThreshold == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthConfidenceThresholdPropertyName);
                _depthConfidenceThreshold = value;
                RaisePropertyChanged(DepthConfidenceThresholdPropertyName);
            }
        }

        #endregion

        #region DepthConfidenceThresholdHigh

        /// <summary>
        /// The <see cref="DepthConfidenceThresholdHigh" /> property's name.
        /// </summary>
        public const string DepthConfidenceThresholdHighPropertyName = "DepthConfidenceThresholdHigh";

        private float _depthConfidenceThreholdHigh = 300.0f;

        /// <summary>
        /// Sets and gets the DepthConfidenceThresholdHigh property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float DepthConfidenceThresholdHigh
        {
            get
            {
                return _depthConfidenceThreholdHigh;
            }

            set
            {
                if (_depthConfidenceThreholdHigh == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthConfidenceThresholdHighPropertyName);
                _depthConfidenceThreholdHigh = value;
                RaisePropertyChanged(DepthConfidenceThresholdHighPropertyName);
            }
        }

        #endregion

        #region DepthSmoothing

        /// <summary>
        /// The <see cref="DepthSmoothing" /> property's name.
        /// </summary>
        public const string DepthSmoothingPropertyName = "DepthSmoothing";

        private bool _depthSmoothing = false;

        /// <summary>
        /// Sets and gets the DepthSmoothing property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool DepthSmoothing
        {
            get
            {
                return _depthSmoothing;
            }

            set
            {
                if (_depthSmoothing == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthSmoothingPropertyName);
                _depthSmoothing = value;
                RaisePropertyChanged(DepthSmoothingPropertyName);
            }
        }

        #endregion

        #region ColorImageFrameTime
        /// <summary>
        /// The <see cref="ColorImageFrameTime" /> property's name.
        /// </summary>
        public const string ColorImageFrameTimePropertyName = "ColorImageFrameTime";

        private long _ColorImageFrameTime = 0;

        /// <summary>
        /// Sets and gets the ColorImageFrameTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long ColorImageFrameTime
        {
            get
            {
                return _ColorImageFrameTime;
            }

            set
            {
                if (_ColorImageFrameTime == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorImageFrameTimePropertyName);
                _ColorImageFrameTime = value;
                RaisePropertyChanged(ColorImageFrameTimePropertyName);
            }
        }
        #endregion

        #region DepthImageFrameTime
        /// <summary>
        /// The <see cref="DepthImageFrameTime" /> property's name.
        /// </summary>
        public const string DepthImageFrameTimePropertyName = "DepthImageFrameTime";

        private long _DepthImageFrameTime = 0;

        /// <summary>
        /// Sets and gets the DepthImageFrameTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long DepthImageFrameTime
        {
            get
            {
                return _DepthImageFrameTime;
            }

            set
            {
                if (_DepthImageFrameTime == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthImageFrameTimePropertyName);
                _DepthImageFrameTime = value;
                RaisePropertyChanged(DepthImageFrameTimePropertyName);
            }
        }
        #endregion

        #region ConfidenceMapImageFrameTime
        /// <summary>
        /// The <see cref="ConfidenceMapImageFrameTime" /> property's name.
        /// </summary>
        public const string ConfidenceMapImageFrameTimePropertyName = "ConfidenceMapImageFrameTime";

        private long _ConfidenceMapImageFrameTime = 0;

        /// <summary>
        /// Sets and gets the ConfidenceMapImageFrameTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long ConfidenceMapImageFrameTime
        {
            get
            {
                return _ConfidenceMapImageFrameTime;
            }

            set
            {
                if (_ConfidenceMapImageFrameTime == value)
                {
                    return;
                }

                RaisePropertyChanging(ConfidenceMapImageFrameTimePropertyName);
                _ConfidenceMapImageFrameTime = value;
                RaisePropertyChanged(ConfidenceMapImageFrameTimePropertyName);
            }
        }
        #endregion

        #region MaxDepthValue

        /// <summary>
        /// The <see cref="MaxDepthValue" /> property's name.
        /// </summary>
        public const string MaxDepthValuePropertyName = "MaxDepthValue";

        private float _maxDepthValue = 5000.0f;

        /// <summary>
        /// Sets and gets the MaxDepthValue property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float MaxDepthValue
        {
            get
            {
                return _maxDepthValue;
            }

            set
            {
                if (_maxDepthValue == value)
                {
                    return;
                }

                RaisePropertyChanging(MaxDepthValuePropertyName);
                _maxDepthValue = value;
                RaisePropertyChanged(MaxDepthValuePropertyName);
            }
        }

        #endregion

        #region Image Sources

        #region ColorImageSource

        /// <summary>
        /// The <see cref="ColorImageSource" /> property's name.
        /// </summary>
        public const string ColorImageSourcePropertyName = "ColorImageSource";

        private BitmapSource _colorImageSource = null;

        /// <summary>
        /// Sets and gets the ColorImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource ColorImageSource
        {
            get
            {
                return _colorImageSource;
            }

            set
            {
                if (_colorImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorImageSourcePropertyName);
                _colorImageSource = value;
                RaisePropertyChanged(ColorImageSourcePropertyName);
            }
        }

        #endregion

        #region DepthImageSource

        /// <summary>
        /// The <see cref="DepthImageSource" /> property's name.
        /// </summary>
        public const string DepthImageSourcePropertyName = "DepthImageSource";

        private BitmapSource _depthImageSource = null;

        /// <summary>
        /// Sets and gets the DepthImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource DepthImageSource
        {
            get
            {
                return _depthImageSource;
            }

            set
            {
                if (_depthImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthImageSourcePropertyName);
                _depthImageSource = value;
                RaisePropertyChanged(DepthImageSourcePropertyName);
            }
        }

        #endregion

        #region ConfidenceMapImageSource

        /// <summary>
        /// The <see cref="ConfidenceMapImageSource" /> property's name.
        /// </summary>
        public const string ConfidenceMapImageSourcePropertyName = "ConfidenceMapImageSource";

        private BitmapSource _confidenceMapImageSource = null;

        /// <summary>
        /// Sets and gets the ConfidenceMapImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource ConfidenceMapImageSource
        {
            get
            {
                return _confidenceMapImageSource;
            }

            set
            {
                if (_confidenceMapImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(ConfidenceMapImageSourcePropertyName);
                _confidenceMapImageSource = value;
                RaisePropertyChanged(ConfidenceMapImageSourcePropertyName);
            }
        }

        #endregion

        #region AdaptiveSensingMaskImageSource

        /// <summary>
        /// The <see cref="AdaptiveSensingMaskImageSource" /> property's name.
        /// </summary>
        public const string AdaptiveSensingMaskImageSourcePropertyName = "AdaptiveSensingMaskImageSource";

        private BitmapSource _adaptiveSensingMaskImageSource = null;

        /// <summary>
        /// Sets and gets the AdaptiveSensingMaskImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource AdaptiveSensingMaskImageSource
        {
            get
            {
                return _adaptiveSensingMaskImageSource;
            }

            set
            {
                if (_adaptiveSensingMaskImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(AdaptiveSensingMaskImageSourcePropertyName);
                _adaptiveSensingMaskImageSource = value;
                RaisePropertyChanged(AdaptiveSensingMaskImageSourcePropertyName);
            }
        }

        #endregion

        #endregion

        #region IRThreshold

        /// <summary>
        /// The <see cref="IRThreshold" /> property's name.
        /// </summary>
        public const string IRThresholdPropertyName = "IRThreshold";

        private byte _irThreshold = 50;

        /// <summary>
        /// Sets and gets the IRThreshold property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public byte IRThreshold
        {
            get
            {
                return _irThreshold;
            }

            set
            {
                if (_irThreshold == value)
                {
                    return;
                }

                RaisePropertyChanging(IRThresholdPropertyName);
                _irThreshold = value;
                RaisePropertyChanged(IRThresholdPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public Kinectv2()
        {
        }

        #endregion

        public override IData Process(IData data)
        {
            return null;
        }

        #region override methods

        /// <summary>
        /// Start the Senz3D node and initializes a Senz3D camera using the
        /// Intel Perceptual SDK driver.
        /// </summary>
        public override void Start()
        {
            if (InitializeCamera())
            {
            }
        }

        public override void Stop()
        {
            if (_reader != null)
                _reader.Dispose();

            if (_sensor != null)
                _sensor.Close();

            _rgbInDepthROI = Rectangle.Empty;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Initializes the camera and returns true on success, false otherwise.
        /// </summary>
        /// <returns>Initialization successful (true) or failed (false)</returns>
        private bool InitializeCamera()
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }

            return true;
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            Image<Rgb, byte> colorImage = null;
            Image<Rgb, byte> infraredImage = null;
            Image<Gray, float> depthImage = null;

            var reference = e.FrameReference.AcquireFrame();

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    colorImage = frame.ToImage();
                }
            }

            // Depth
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    depthImage = frame.ToImage();
                }
            }

            // Infrared
            using (var frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    infraredImage = frame.ToImage(IRThreshold);
                }
            }


            if (IsRenderContent)
                RenderImages(colorImage, depthImage, infraredImage);

            // publish images finally
            PublishImages(colorImage, depthImage, infraredImage);
        }

        /// <summary>
        /// Publishes images to the pipelines.
        /// </summary>
        /// <param name="colorImage"></param>
        /// <param name="depthImage"></param>
        /// <param name="confidenceMapImage"></param>
        private void PublishImages(
            Image<Rgb, byte> colorImage,
            Image<Gray, float> depthImage,
            Image<Rgb, byte> confidenceMapImage)
        {
            var dc = new DataContainer(++_frameId, DateTime.Now)
                    {
                        new RgbImageData(this, "color", colorImage),
                        new GrayFloatImage(this, "depth", depthImage),
                        new RgbImageData(this, "confidence", confidenceMapImage),
                    };

            Publish(dc);
        }

        private void RenderImages(
            Image<Rgb, byte> colorImage,
            Image<Gray, float> depthImage,
            Image<Rgb, byte> confindenceMapImage)
        {
            //Render color image.
            var colorImageCopy = colorImage.Copy();
            Task.Factory.StartNew(() =>
            {
                var bitmap = colorImageCopy.ToBitmapSource(true);
                colorImageCopy.Dispose();
                return bitmap;
            }).ContinueWith(s => ColorImageSource = s.Result);

            // Render depth image.
            var depthImageCopy = depthImage.Copy();
            Task.Factory.StartNew(() =>
            {
                var bitmap = depthImageCopy.ToGradientBitmapSource(true, EmguExtensions.LowConfidence, EmguExtensions.Saturation);
                depthImageCopy.Dispose();
                return bitmap;
            }).ContinueWith(s => DepthImageSource = s.Result);

            // Render confidence map image.
            var confidenceMapImageCopy = confindenceMapImage.Copy();
            Task.Factory.StartNew(() =>
            {
                var bitmap = confidenceMapImageCopy.ToBitmapSource(true);
                confidenceMapImageCopy.Dispose();
                return bitmap;
            }).ContinueWith(s => ConfidenceMapImageSource = s.Result);


            //if (adaptiveSensingMaskImage != null)
            //{
            //    var adaptiveSensingMaskImageCopy = adaptiveSensingMaskImage.Copy();
            //    Task.Factory.StartNew(() =>
            //    {
            //        var bitmap = adaptiveSensingMaskImageCopy.ToBitmapSource(true);
            //        confidenceMapImageCopy.Dispose();
            //        return bitmap;
            //    }).ContinueWith(s => AdaptiveSensingMaskImageSource = s.Result);
            //}
        }

        #endregion
    }
}
