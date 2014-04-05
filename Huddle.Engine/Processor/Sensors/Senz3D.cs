using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Emgu.CV.VideoSurveillance;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.OpenCv;
using Huddle.Engine.Util;
using Color = System.Drawing.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.Sensors
{
    [ViewTemplate("Senz3D", "Senz3D", "/Huddle.Engine;component/Resources/kinect.png")]
    public class Senz3D : BaseProcessor
    {
        #region private fields

        private UtilMPipeline _pp;

        private PXCMCapture.Device _device;

        private bool _isRunning;

        private long _frameId = -1;

        private double _minDepth = Double.MaxValue;
        private double _maxDepth = -1.0;

        private Rectangle _rgbInDepthROI = new Rectangle(0, 0, 0, 0);

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

        #region UVMapImageSource

        /// <summary>
        /// The <see cref="UVMapImageSource" /> property's name.
        /// </summary>
        public const string UVMapImageSourcePropertyName = "UVMapImageSource";

        private BitmapSource _uvMapImageSource = null;

        /// <summary>
        /// Sets and gets the ConfidenceMapImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource UVMapImageSource
        {
            get
            {
                return _uvMapImageSource;
            }

            set
            {
                if (_uvMapImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(UVMapImageSourcePropertyName);
                _uvMapImageSource = value;
                RaisePropertyChanged(UVMapImageSourcePropertyName);
            }
        }

        #endregion

        #region RgbOfDepthImageSource

        /// <summary>
        /// The <see cref="RgbOfDepthImageSource" /> property's name.
        /// </summary>
        public const string RgbOfDepthImageSourcePropertyName = "RgbOfDepthImageSource";

        private BitmapSource _RgbOfDepthImageSource = null;

        /// <summary>
        /// Sets and gets the ConfidenceMapImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource RgbOfDepthImageSource
        {
            get
            {
                return _RgbOfDepthImageSource;
            }

            set
            {
                if (_RgbOfDepthImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(RgbOfDepthImageSourcePropertyName);
                _RgbOfDepthImageSource = value;
                RaisePropertyChanged(RgbOfDepthImageSourcePropertyName);
            }
        }

        #endregion

        #region DepthOfRgbImageSource

        /// <summary>
        /// The <see cref="DepthOfRgbImageSource" /> property's name.
        /// </summary>
        public const string DepthOfRgbImageSourcePropertyName = "DepthOfRgbImageSource";

        private BitmapSource _DepthOfRgbImageSource = null;

        /// <summary>
        /// Sets and gets the ConfidenceMapImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource DepthOfRgbImageSource
        {
            get
            {
                return _DepthOfRgbImageSource;
            }

            set
            {
                if (_DepthOfRgbImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthOfRgbImageSourcePropertyName);
                _DepthOfRgbImageSource = value;
                RaisePropertyChanged(DepthOfRgbImageSourcePropertyName);
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
        [XmlAttribute]
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

        #region UvMapChecked

        /// <summary>
        /// The <see cref="UvMapChecked" /> property's name.
        /// </summary>
        public const string UvMapCheckedPropertyName = "UvMapChecked";

        private bool _uvMapCheckedProperty = false;

        /// <summary>
        /// Sets and gets the UvMapChecked property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool UvMapChecked
        {
            get
            {
                return _uvMapCheckedProperty;
            }

            set
            {
                if (_uvMapCheckedProperty == value)
                {
                    return;
                }

                RaisePropertyChanging(UvMapCheckedPropertyName);
                _uvMapCheckedProperty = value;
                RaisePropertyChanged(UvMapCheckedPropertyName);
            }
        }

        #endregion

        #region RgbOfDepthChecked

        /// <summary>
        /// The <see cref="RgbOfDepthCheckedProperty" /> property's name.
        /// </summary>
        public const string RgbOfDepthCheckedPropertyName = "RgbOfDepthCheckedProperty";

        private bool _rgbOfDepthCheckedProperty = false;

        /// <summary>
        /// Sets and gets the RgbOfDepthCheckedProperty property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool RgbOfDepthChecked
        {
            get
            {
                return _rgbOfDepthCheckedProperty;
            }

            set
            {
                if (_rgbOfDepthCheckedProperty == value)
                {
                    return;
                }

                RaisePropertyChanging(RgbOfDepthCheckedPropertyName);
                _rgbOfDepthCheckedProperty = value;
                RaisePropertyChanged(RgbOfDepthCheckedPropertyName);
            }
        }

        #endregion

        #region DepthOfRgbChecked

        /// <summary>
        /// The <see cref="DepthOfRgbChecked" /> property's name.
        /// </summary>
        public const string DepthOfRgbCheckedPropertyName = "DepthOfRgbChecked";

        private bool _depethOfRgbChecked = false;

        /// <summary>
        /// Sets and gets the DepthOfRgbChecked property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool DepthOfRgbChecked
        {
            get
            {
                return _depethOfRgbChecked;
            }

            set
            {
                if (_depethOfRgbChecked == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthOfRgbCheckedPropertyName);
                _depethOfRgbChecked = value;
                RaisePropertyChanged(DepthOfRgbCheckedPropertyName);
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

        #region UVMapImageFrameTime
        /// <summary>
        /// The <see cref="UVMapImageFrameTime" /> property's name.
        /// </summary>
        public const string UVMapImageFrameTimePropertyName = "UVMapImageFrameTime";

        private long _UVMapImageFrameTime = 0;

        /// <summary>
        /// Sets and gets the UVMapImageFrameTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long UVMapImageFrameTime
        {
            get
            {
                return _UVMapImageFrameTime;
            }

            set
            {
                if (_UVMapImageFrameTime == value)
                {
                    return;
                }

                RaisePropertyChanging(UVMapImageFrameTimePropertyName);
                _UVMapImageFrameTime = value;
                RaisePropertyChanged(UVMapImageFrameTimePropertyName);
            }
        }

        #endregion

        #region RgbOfDepthImageFrameTime
        /// <summary>
        /// The <see cref="RgbOfDepthImageFrameTime" /> property's name.
        /// </summary>
        public const string RgbOfDepthImageFrameTimePropertyName = "RgbOfDepthImageFrameTime";

        private long _myProperty = 0;

        /// <summary>
        /// Sets and gets the RgbOfDepthImageFrameTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long RgbOfDepthImageFrameTime
        {
            get
            {
                return _myProperty;
            }

            set
            {
                if (_myProperty == value)
                {
                    return;
                }

                RaisePropertyChanging(RgbOfDepthImageFrameTimePropertyName);
                _myProperty = value;
                RaisePropertyChanged(RgbOfDepthImageFrameTimePropertyName);
            }
        }
        #endregion

        #region DepthOfRgbImageFrameTime
        /// <summary>
        /// The <see cref="DepthOfRgbImageFrameTime" /> property's name.
        /// </summary>
        public const string DepthOfRgbImageFrameTimePropertyName = "DepthOfRgbImageFrameTime";

        private long _DepthOfRgbImageFrameTime = 0;

        /// <summary>
        /// Sets and gets the DepthOfRgbImageFrameTime property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long DepthOfRgbImageFrameTime
        {
            get
            {
                return _DepthOfRgbImageFrameTime;
            }

            set
            {
                if (_DepthOfRgbImageFrameTime == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthOfRgbImageFrameTimePropertyName);
                _DepthOfRgbImageFrameTime = value;
                RaisePropertyChanged(DepthOfRgbImageFrameTimePropertyName);
            }
        }
        #endregion

        #region MinDepthValue

        /// <summary>
        /// The <see cref="MinDepthValue" /> property's name.
        /// </summary>
        public const string MinDepthValuePropertyName = "MinDepthValue";

        private float _minDepthThreshold = 0.0f;

        /// <summary>
        /// Sets and gets the MinDepthValue property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float MinDepthValue
        {
            get
            {
                return _minDepthThreshold;
            }

            set
            {
                if (_minDepthThreshold == value)
                {
                    return;
                }

                RaisePropertyChanging(MinDepthValuePropertyName);
                _minDepthThreshold = value;
                RaisePropertyChanged(MinDepthValuePropertyName);
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

        #endregion

        #region ctor

        public Senz3D()
        {
            PXCMSession session;
            var sts = PXCMSession.CreateInstance(out session);

            Debug.Assert(sts >= pxcmStatus.PXCM_STATUS_NO_ERROR, "could not create session instance");

            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case DepthConfidenceThresholdPropertyName:
                        if (_device != null)
                            _device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_CONFIDENCE_THRESHOLD, _depthConfidenceThreshold);
                        break;

                    case ColorImageProfilePropertyName:
                        //Stop();
                        _rgbInDepthROI = new Rectangle(0, 0, 0, 0);
                        //Thread.Sleep(2000);
                        //Start();
                        break;
                }
            };
        }

        #endregion

        public override IData Process(IData data)
        {
            return null;
        }

        #region override methods

        public override void Start()
        {
            var ctx = SynchronizationContext.Current;
            var thread = new Thread(() => DoRendering(ctx));
            thread.Start();
            Thread.Sleep(5);
        }

        public override void Stop()
        {
            _isRunning = false;
        }

        #endregion

        #region private methods

        private void DoRendering(SynchronizationContext ctx)
        {
            _isRunning = true;

            /* UtilMPipeline works best for synchronous color and depth streaming */
            _pp = new UtilMPipeline();

            /* Set Input Source */
            _pp.capture.SetFilter("DepthSense Device 325V2");

            /* Set Color & Depth Resolution */
            PXCMCapture.VideoStream.ProfileInfo cinfo = GetConfiguration(PXCMImage.ColorFormat.COLOR_FORMAT_RGB32);
            _pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_RGB32, cinfo.imageInfo.width, cinfo.imageInfo.height);
            _pp.capture.SetFilter(ref cinfo); // only needed to set FPS

            PXCMCapture.VideoStream.ProfileInfo dinfo2 = GetConfiguration(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH);
            _pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, dinfo2.imageInfo.width, dinfo2.imageInfo.height);
            _pp.capture.SetFilter(ref dinfo2); // only needed to set FPS

            /* Initialization */
            if (!_pp.Init())
                ctx.Send(state => ThrowException("Could not initialize Senz3D hardware"), null);

            _device = _pp.capture.device;
            _pp.capture.device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_CONFIDENCE_THRESHOLD, DepthConfidenceThreshold);

            while (_isRunning)
            {
                /* If raw depth is needed, disable smoothing */
                _pp.capture.device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SMOOTHING, DepthSmoothing ? 1 : 0);

                /* Wait until a frame is ready */
                if (!_pp.AcquireFrame(true)) break;
                if (_pp.IsDisconnected()) break;

                /* Get RGB color image */
                Stopwatch sw = Stopwatch.StartNew();
                var color = _pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_COLOR);
                var colorBitmap = GetRgb32Pixels(color);
                var colorImage = new Image<Rgb, byte>(colorBitmap);
                var colorImageCopy = colorImage.Copy();
                ColorImageFrameTime = sw.ElapsedMilliseconds;

                /* Get depth image */
                sw.Restart();
                var depth = _pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_DEPTH);
                var depthImageAndConfidence = GetHighPrecisionDepthImage(depth);
                var depthImage = (Image<Gray, float>)depthImageAndConfidence[0];
                var depthImageCopy = depthImage.Copy();
                var confidenceMapImage = (Image<Rgb, Byte>)depthImageAndConfidence[1];
                var confidenceMapImageCopy = confidenceMapImage.Copy();
                DepthImageFrameTime = sw.ElapsedMilliseconds;
                ConfidenceMapImageFrameTime = 0;

                bool getRgbInDepthROI = false;
                /* if rgbInDepthROI is undefined get uvmap and rgbofdepth and rgbInDepthROI */
                if (_rgbInDepthROI.Left == 0 && _rgbInDepthROI.Right == 0 && _rgbInDepthROI.Width == 0 &&
                    _rgbInDepthROI.Height == 0)
                {
                    getRgbInDepthROI = true;
                }


                /* Get UV map */
                Image<Rgb, float> uvMapImage, uvMapImageCopy;
                if (UvMapChecked || getRgbInDepthROI)
                {
                    sw.Restart();
                    uvMapImage = GetDepthUVMap(depth);
                    uvMapImageCopy = uvMapImage.Copy();
                    UVMapImageFrameTime = sw.ElapsedMilliseconds;
                }
                else
                {
                    uvMapImage = null;
                    uvMapImageCopy = null;
                    UVMapImageFrameTime = -1;
                }

                /* Get RgbOfDepth */
                Image<Rgb, byte> rgbOfDepthImage, rgbOfDepthImageCopy;
                if ((RgbOfDepthChecked && uvMapImage != null) || getRgbInDepthROI)
                {
                    sw.Restart();
                    if (getRgbInDepthROI)
                    {
                        var tl = new Point();
                        var br = new Point();
                        rgbOfDepthImage = GetRgbOfDepthPixels(depthImage, colorImage, uvMapImage, true, ref _rgbInDepthROI);
                        getRgbInDepthROI = false;
                    }
                    else
                        rgbOfDepthImage = GetRgbOfDepthPixels(depthImage, colorImage, uvMapImage);

                    rgbOfDepthImageCopy = rgbOfDepthImage.Copy();
                    RgbOfDepthImageFrameTime = sw.ElapsedMilliseconds;
                }
                else
                {
                    rgbOfDepthImage = null;
                    rgbOfDepthImageCopy = null;
                    RgbOfDepthImageFrameTime = -1;
                }

                /* Get DepthOfRGB */
                Image<Gray, float> depthOfRgbImage, depthOfRgbImageCopy;
                if (DepthOfRgbChecked && uvMapImage != null)
                {
                    sw.Restart();
                    depthOfRgbImage = GetDepthOfRGBPixels(depthImage, colorImage, uvMapImage);
                    depthOfRgbImageCopy = depthOfRgbImage.Copy();
                    DepthOfRgbImageFrameTime = sw.ElapsedMilliseconds;
                }
                else
                {
                    depthOfRgbImage = null;
                    depthOfRgbImageCopy = null;
                    DepthOfRgbImageFrameTime = -1;
                }

                float prop;
                _pp.QueryCapture()
                    .QueryDevice()
                    .QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_LOW_CONFIDENCE_VALUE, out prop);
                var lowConfidence = (int)prop;
                _pp.QueryCapture()
                    .QueryDevice()
                    .QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SATURATION_VALUE, out prop);
                var saturation = (int)prop;

                _pp.ReleaseFrame();

                if (IsRenderContent)
                {
                    /* draw color image */
                    DispatcherHelper.RunAsync(() =>
                    {
                        ColorImageSource = colorImageCopy.ToBitmapSource();
                        colorImageCopy.Dispose();
                    });


                    /* draw depth image */
                    DispatcherHelper.RunAsync(() =>
                    {
                        DepthImageSource = depthImageCopy.ToGradientBitmapSource(lowConfidence, saturation);
                        depthImageCopy.Dispose();
                    });


                    /* draw confidence map */
                    DispatcherHelper.RunAsync(() =>
                    {
                        ConfidenceMapImageSource = confidenceMapImageCopy.ToBitmapSource();
                        confidenceMapImageCopy.Dispose();
                    });


                    /* draw uvmap */
                    if (uvMapImage != null)
                    {
                        DispatcherHelper.RunAsync(() =>
                        {
                            UVMapImageSource = uvMapImageCopy.ToBitmapSource();
                            uvMapImageCopy.Dispose();
                        });
                    }
                    else
                    {
                        UVMapImageSource = null;
                    }


                    /* draw rgbofdepth */
                    if (rgbOfDepthImage != null)
                    {
                        DispatcherHelper.RunAsync(() =>
                        {
                            RgbOfDepthImageSource = rgbOfDepthImageCopy.ToBitmapSource();
                            rgbOfDepthImageCopy.Dispose();
                        });
                    }
                    else
                    {
                        RgbOfDepthImageSource = null;
                    }


                    /* draw depthofrgb */
                    if (depthOfRgbImage != null)
                    {
                        DispatcherHelper.RunAsync(() =>
                        {
                            DepthOfRgbImageSource = depthOfRgbImageCopy.ToGradientBitmapSource(lowConfidence, saturation);
                            depthOfRgbImageCopy.Dispose();
                        });
                    }
                    else
                    {
                        DepthOfRgbImageSource = null;
                    }
                }


                /* publish results */
                DispatcherHelper.RunAsync(() =>
                {
                    var dc = new DataContainer(++_frameId, DateTime.Now)
                    {
                        new RgbImageData(this, "color", colorImage),
                        new GrayFloatImage(this, "depth", depthImage),
                        new RgbImageData(this, "confidence", confidenceMapImage),
                    };

                    if (uvMapImage != null) dc.Add(new RgbFloatImage(this, "uvmap", uvMapImage));
                    if (rgbOfDepthImage != null) dc.Add(new RgbImageData(this, "rgbofdepth", rgbOfDepthImage));
                    if (depthOfRgbImage != null) dc.Add(new GrayFloatImage(this, "depthofrgb", depthOfRgbImage));
                    Publish(dc);
                });

                Thread.Sleep(1000 / (Fps > 0 ? Fps : 1));
            }

            _pp.Close();
            _pp.Dispose();
        }

        private static void ThrowException(string message)
        {
            throw new Exception(message);
        }

        private static int Align16(uint width)
        {
            return ((int)((width + 15) / 16)) * 16;
        }

        private Bitmap GetRgb32Pixels(PXCMImage image)
        {
            var cwidth = Align16(image.info.width); /* aligned width */
            var cheight = (int)image.info.height;

            PXCMImage.ImageData cdata;
            byte[] cpixels;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_RGB32, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cpixels = cdata.ToByteArray(0, cwidth * cheight * 4);
                image.ReleaseAccess(ref cdata);
            }
            else
            {
                cpixels = new byte[cwidth * cheight * 4];
            }

            var width = (int)image.info.width;
            var height = (int)image.info.height;

            Bitmap bitmap;
            lock (this)
            {
                bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                Marshal.Copy(cpixels, 0, data.Scan0, width * height * 4);
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        private IImage[] GetHighPrecisionDepthImage(PXCMImage depthImage)
        {
            var inputWidth = Align16(depthImage.info.width);  /* aligned width */
            var inputHeight = (int)depthImage.info.height;

            var returnImages = new IImage[2];
            returnImages[0] = new Image<Gray, float>(inputWidth, inputHeight);
            returnImages[1] = new Image<Rgb, Byte>(inputWidth, inputHeight);

            PXCMImage.ImageData cdata;
            if (depthImage.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, out cdata) <
                pxcmStatus.PXCM_STATUS_NO_ERROR) return returnImages;

            float prop;
            _pp.QueryCapture().QueryDevice().QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_LOW_CONFIDENCE_VALUE, out prop);
            var lowConfidence = (int)prop;
            _pp.QueryCapture().QueryDevice().QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SATURATION_VALUE, out prop);
            var saturation = (int)prop;

            var depthValues = cdata.ToShortArray(0, inputWidth * inputHeight);
            depthImage.ReleaseAccess(ref cdata);

            var minValue = MinDepthValue;
            var maxValue = MaxDepthValue;

            var depthReturnImage = ((Image<Gray, float>)returnImages[0]);
            var confidenceReturnImage = ((Image<Rgb, Byte>)returnImages[1]);
            var depthReturnImageData = depthReturnImage.Data;
            var confidenceReturnImageData = confidenceReturnImage.Data;

            Parallel.For(0, inputHeight, y =>
            {
                for (int x = 0; x < inputWidth; x++)
                {
                    float depth = depthValues[y * inputWidth + x];
                    if (depth != lowConfidence && depth != saturation)
                    {
                        var test = (depth - minValue) / (maxValue - minValue);

                        if (test < 0)
                            test = 0.0f;
                        else if (test > 1.0)
                            test = 1.0f;

                        test *= 255.0f;

                        depthReturnImageData[y, x, 0] = test;
                    }
                    else
                    {
                        depthReturnImageData[y, x, 0] = depth;
                        confidenceReturnImageData[y, x, 0] = 255;
                    }
                }
            });
            return returnImages;
        }


        private Image<Rgb, float> GetDepthUVMap(PXCMImage image)
        {
            var inputWidth = (int)image.info.width;
            var inputHeight = (int)image.info.height;

            var _uvMap = new Image<Rgb, float>(inputWidth, inputHeight);

            PXCMImage.ImageData cdata;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, out cdata) <
                pxcmStatus.PXCM_STATUS_NO_ERROR) return _uvMap;

            var uv = new float[inputHeight * inputWidth * 2];

            // read UV
            var pData = cdata.buffer.planes[2];
            Marshal.Copy(pData, uv, 0, inputWidth * inputHeight * 2);
            image.ReleaseAccess(ref cdata);

            Parallel.For(0, uv.Length / 2, i =>
            {
                int j = i * 2;
                //Console.WriteLine(j + ": " + uv[j] * 255.0 + ", " + uv[j + 1] * 255.0);
                _uvMap[(j / 2) / inputWidth, (j / 2) % inputWidth] = new Rgb(uv[j] * 255.0, uv[j + 1] * 255.0, 0);
            });

            return _uvMap;
        }

        private Image<Rgb, byte> GetRgbOfDepthPixels(Image<Gray, float> depth, Image<Rgb, byte> rgb,
            Image<Rgb, float> uvmap)
        {
            Rectangle dummyRect = new Rectangle();
            return GetRgbOfDepthPixels(depth, rgb, uvmap, false, ref dummyRect);
        }

        private Image<Rgb, byte> GetRgbOfDepthPixels(Image<Gray, float> depth, Image<Rgb, byte> rgb, Image<Rgb, float> uvmap,
            bool getRgbContour, ref Rectangle rgbInDepthRect)
        {
            var resImg = new Image<Rgb, byte>(depth.Width, depth.Height);

            // number of rgb pixels per depth pixel
            int regWidth = rgb.Width / depth.Width;
            int regHeight = rgb.Height / depth.Height;
            int rgbWidth = rgb.Width;
            int rgbHeight = rgb.Height;
            float xfactor = 1.0f / 255.0f * rgbWidth;
            float yfactor = 1.0f / 255.0f * rgbHeight;
            var uvmapData = uvmap.Data;
            var rgbData = rgb.Data;
            var resImgData = resImg.Data;

            Image<Gray, byte> contourImg = null;
            byte[, ,] contourImgData = null;
            if (getRgbContour)
            {
                // dummy image to extract contour of RGB image in depth image
                contourImg = new Image<Gray, byte>(depth.Width, depth.Height);
                contourImgData = contourImg.Data;
            }

            Parallel.For(0, depth.Height, y =>
            {
                for (int x = 0; x < depth.Width; x++)
                {
                    int xindex = (int)(uvmapData[y, x, 0] * xfactor + 0.5);
                    int yindex = (int)(uvmapData[y, x, 1] * yfactor + 0.5);

                    double rsum = 0, gsum = 0, bsum = 0;
                    int pixelcount = 0;
                    for (int rx = xindex - regWidth / 2; rx < xindex + regWidth / 2; rx++)
                    {
                        for (int ry = yindex - regHeight / 2; ry < yindex + regHeight / 2; ry++)
                        {
                            if (rx > 0 && ry > 0 && rx < rgbWidth && ry < rgbHeight)
                            {
                                rsum += rgbData[ry, rx, 0];
                                gsum += rgbData[ry, rx, 1];
                                bsum += rgbData[ry, rx, 2];
                                pixelcount++;
                            }
                        }
                    }
                    resImgData[y, x, 0] = (byte)(rsum / pixelcount);
                    resImgData[y, x, 1] = (byte)(gsum / pixelcount);
                    resImgData[y, x, 2] = (byte)(bsum / pixelcount);
                    if ((resImgData[y, x, 0] + resImgData[y, x, 1] + resImgData[y, x, 2]) > 0.01)
                    {
                        if (getRgbContour) contourImgData[y, x, 0] = 255;
                    }
                }
            });

            if (getRgbContour)
            {
                using (var storage = new MemStorage())
                {
                    for (var contours = contourImg.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                        RETR_TYPE.CV_RETR_EXTERNAL, storage); contours != null; contours = contours.HNext)
                    {
                        var currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                        if (currentContour.Area > 160 * 120)
                        {
                            _rgbInDepthROI = currentContour.BoundingRectangle;
                            //contourImg.Draw(_rgbInDepthROI, new Gray(122.0), 5);
                            //return contourImg.Convert<Rgb, Byte>();

                            Stage(new ROI(this, "rgbInDepthROI")
                            {
                                RoiRectangle = _rgbInDepthROI
                            });
                            Push();

                            Log("Identified rgbInDepthROI as {0}", _rgbInDepthROI);
                            return resImg;
                        }
                    }
                }
            }

            return resImg;
        }


        private Image<Gray, float> GetDepthOfRGBPixels(Image<Gray, float> depth, Image<Rgb, byte> rgb, Image<Rgb, float> uvmap)
        {
            float prop;
            _pp.QueryCapture().QueryDevice().QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_LOW_CONFIDENCE_VALUE, out prop);
            var lowConfidence = (int)prop;
            _pp.QueryCapture().QueryDevice().QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SATURATION_VALUE, out prop);
            var saturation = (int)prop;

            // create RGB-sized image
            var retdepth = new Image<Gray, float>(rgb.Width, rgb.Height, new Gray(lowConfidence));
            var retdepthWidth = retdepth.Width;
            var retdepthHeight = retdepth.Height;

            var uvmapWidth = uvmap.Width;
            var uvmapHeight = uvmap.Height;

            var depthData = depth.Data;
            var uvmapData = uvmap.Data;

            float xfactor = 1.0f / 255.0f * retdepthWidth;
            float yfactor = 1.0f / 255.0f * retdepthHeight;

            //for (int uvy = 0; uvy < uvmapHeight - 1; uvy++)
            Parallel.For(0, uvmapHeight - 1, uvy =>
            {

                //for (int uvx = 0; uvx < uvmapWidth - 1; uvx++)
                Parallel.For(0, uvmapWidth - 1, uvx =>
                {
                    // for each point in UVmap create two triangles that connect this point with the right/bottom neighbors                   

                    var pts1 = new Point[3];
                    var d1 = new float[]
                    {
                        depthData[uvy, uvx, 0],
                        depthData[uvy, uvx + 1, 0],
                        depthData[uvy + 1, uvx, 0]
                    };

                    double d1avg = 0;
                    int count = 0;
                    for (int i = 0; i < d1.Length; i++)
                    {
                        if (d1[i] != saturation && d1[i] != lowConfidence)
                        {
                            d1avg += d1[i];
                            count++;
                        }
                    }
                    if (count > 0)
                        d1avg = d1avg / (float)count;
                    else
                        d1avg = lowConfidence;

                    var pts2 = new Point[3];
                    var d2 = new float[]
                    {
                        depthData[uvy, uvx + 1, 0],
                        depthData[uvy + 1, uvx + 1, 0],
                        depthData[uvy + 1, uvx, 0]
                    };

                    double d2avg = 0;
                    count = 0;
                    for (int i = 0; i < d2.Length; i++)
                    {
                        if (d2[i] != saturation && d2[i] != lowConfidence)
                        {
                            d2avg += d2[i];
                            count++;
                        }
                    }
                    if (count > 0)
                        d2avg = d2avg / (float)count;
                    else
                        d2avg = lowConfidence;


                    bool outofbounds = false;

                    // get points for triangle 1 (top left)
                    pts1[0].X = (int)(uvmapData[uvy, uvx, 0] * xfactor + 0.5);
                    outofbounds |= pts1[0].X < 0 || pts1[0].X > retdepthWidth;

                    pts1[0].Y = (int)(uvmapData[uvy, uvx, 1] * yfactor + 0.5);
                    outofbounds |= pts1[0].Y < 0 || pts1[0].Y > retdepthHeight;

                    pts1[1].X = (int)(uvmapData[uvy, uvx + 1, 0] * xfactor + 0.5) - 1;
                    outofbounds |= pts1[1].X < 0 || pts1[1].X > retdepthWidth;

                    pts1[1].Y = (int)(uvmapData[uvy, uvx + 1, 1] * yfactor + 0.5) - 1;
                    outofbounds |= pts1[1].Y < 0 || pts1[1].Y > retdepthHeight;

                    pts1[2].X = (int)(uvmapData[uvy + 1, uvx, 0] * xfactor + 0.5);
                    outofbounds |= pts1[2].X < 0 || pts1[2].X > retdepthWidth;

                    pts1[2].Y = (int)(uvmapData[uvy + 1, uvx, 1] * yfactor + 0.5) - 1;
                    outofbounds |= pts1[2].Y < 0 || pts1[2].Y > retdepthHeight;

                    if (!outofbounds)
                        retdepth.FillConvexPoly(pts1, new Gray(d1avg));

                    // get points for triangle 2 (bottom right)
                    outofbounds = false;

                    pts2[0].X = pts1[1].X;
                    outofbounds |= pts2[0].X < 0 || pts2[0].X > retdepthWidth;

                    pts2[0].Y = pts1[1].Y;
                    outofbounds |= pts2[0].Y < 0 || pts2[0].Y > retdepthHeight;

                    pts2[1].X = (int)(uvmapData[uvy + 1, uvx + 1, 0] * xfactor + 0.5);
                    outofbounds |= pts2[1].X < 0 || pts2[1].X > retdepthWidth;

                    pts2[1].Y = (int)(uvmapData[uvy + 1, uvx + 1, 1] * yfactor + 0.5) - 1;
                    outofbounds |= pts2[1].Y < 0 || pts2[1].Y > retdepthHeight;

                    pts2[2].X = pts1[2].X;
                    outofbounds |= pts2[2].X < 0 || pts2[2].X > retdepthWidth;

                    pts2[2].Y = pts1[2].Y;
                    outofbounds |= pts2[2].Y < 0 || pts2[2].Y > retdepthHeight;

                    if (!outofbounds)
                        retdepth.FillConvexPoly(pts2, new Gray(d2avg));

                });
            });

            return retdepth;
        }

        private PXCMCapture.VideoStream.ProfileInfo GetConfiguration(PXCMImage.ColorFormat format)
        {
            var pinfo = new PXCMCapture.VideoStream.ProfileInfo { imageInfo = { format = format } };

            if (((int)format & (int)PXCMImage.ImageType.IMAGE_TYPE_COLOR) != 0)
            {
                if (ColorImageProfile.Equals("1280 x 720"))
                {
                    pinfo.imageInfo.width = 1280;
                    pinfo.imageInfo.height = 720;
                }
                else if (ColorImageProfile.Equals("640 x 480"))
                {
                    pinfo.imageInfo.width = 640;
                    pinfo.imageInfo.height = 480;
                }

                pinfo.frameRateMin.numerator = 15;
                pinfo.frameRateMax.numerator = 30;
                pinfo.frameRateMin.denominator = pinfo.frameRateMax.denominator = 1;
            }
            else
            {
                pinfo.imageInfo.width = 320;
                pinfo.imageInfo.height = 240;

                pinfo.frameRateMin.numerator = 30;
                pinfo.frameRateMin.denominator = 1;
            }

            return pinfo;
        }

        #endregion
    }
}
