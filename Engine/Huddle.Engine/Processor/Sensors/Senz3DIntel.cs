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

namespace Huddle.Engine.Processor.Sensors
{
    [ViewTemplate("Senz3D (Intel)", "Senz3DIntel", "/Huddle.Engine;component/Resources/kinect.png")]
    public class Senz3DIntel : BaseProcessor
    {
        #region private fields

        private UtilMPipeline _pp;

        private PXCMCapture.Device _device;

        private bool _isRunning;

        private long _frameId = -1;

        private Rectangle _rgbInDepthROI = Rectangle.Empty;

        #region Adaptive Sensing

        private bool _alternate;

        private Image<Gray, float> _depthImageLow;
        private Image<Gray, float> _depthImageHigh;

        private Image<Rgb, byte> _confidenceImageLow;
        private Image<Rgb, byte> _confidenceImageHigh;

        private Image<Gray, byte> _adaptiveSensingMaskImage;

        #endregion

        #endregion

        #region commands

        public RelayCommand UpdateAdaptiveSensingMaskCommand { get; private set; }

        #endregion

        #region properties

        #region IsAdaptiveSensing

        /// <summary>
        /// The <see cref="IsAdaptiveSensing" /> property's name.
        /// </summary>
        public const string IsAdaptiveSensingPropertyName = "IsAdaptiveSensing";

        private bool _isAdaptiveSensing = true;

        /// <summary>
        /// Sets and gets the IsAdaptiveSensing property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsAdaptiveSensing
        {
            get
            {
                return _isAdaptiveSensing;
            }

            set
            {
                if (_isAdaptiveSensing == value)
                {
                    return;
                }

                RaisePropertyChanging(IsAdaptiveSensingPropertyName);
                _isAdaptiveSensing = value;
                RaisePropertyChanged(IsAdaptiveSensingPropertyName);
            }
        }

        #endregion

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

        #endregion

        #region ctor

        public Senz3DIntel()
        {
            UpdateAdaptiveSensingMaskCommand = new RelayCommand(OnUpdateAdaptiveSensingMask);

            PXCMSession session;
            var sts = PXCMSession.CreateInstance(out session);

            Debug.Assert(sts >= pxcmStatus.PXCM_STATUS_NO_ERROR, "could not create session instance");

            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case DepthConfidenceThresholdPropertyName:
                        if (_device != null)
                            _device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_CONFIDENCE_THRESHOLD, DepthConfidenceThreshold);
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

        private void OnUpdateAdaptiveSensingMask()
        {
            _adaptiveSensingMaskImage = CreateConfidenceMask(_confidenceImageHigh);
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
                _isRunning = true;
                Thread.Sleep(5);
                var thread = new Thread(GrabFrames);
                thread.Start();
            }
        }

        public override void Stop()
        {
            _isRunning = false;
            _alternate = false;
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
            /* UtilMPipeline works best for synchronous color and depth streaming */
            _pp = new UtilMPipeline();

            /* Set Input Source */
            _pp.capture.SetFilter("DepthSense Device 325V2");

            /* Set Color & Depth Resolution */
            var cinfo = GetConfiguration(PXCMImage.ColorFormat.COLOR_FORMAT_RGB32);
            _pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_RGB32, cinfo.imageInfo.width, cinfo.imageInfo.height);
            _pp.capture.SetFilter(ref cinfo); // only needed to set FPS

            var dinfo2 = GetConfiguration(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH);
            _pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, dinfo2.imageInfo.width, dinfo2.imageInfo.height);
            _pp.capture.SetFilter(ref dinfo2); // only needed to set FPS

            /* Initialization */
            if (!_pp.Init())
            {
                Log("Could not initialize Senz3D hardware");
                HasErrorState = true;
                return false;
            }

            var capture = _pp.capture;
            _device = capture.device;
            _device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_CONFIDENCE_THRESHOLD, DepthConfidenceThreshold);
            _device.QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_LOW_CONFIDENCE_VALUE, out EmguExtensions.LowConfidence);
            _device.QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SATURATION_VALUE, out EmguExtensions.Saturation);

            return true;
        }

        private void GrabFrames()
        {
            while (_isRunning)
            {
                _alternate = !_alternate;

                var confidenceThreshold = DepthConfidenceThreshold;
                if (IsAdaptiveSensing)
                    confidenceThreshold = _alternate ? DepthConfidenceThresholdHigh : DepthConfidenceThreshold;

                /* If raw depth is needed, disable smoothing */
                _device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SMOOTHING, DepthSmoothing ? 1 : 0);
                _device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_CONFIDENCE_THRESHOLD, confidenceThreshold);

                /* Wait until a frame is ready */
                if (!_pp.AcquireFrame(true)) break;
                if (_pp.IsDisconnected()) break;

                /* Get RGB color image */
                Stopwatch sw = Stopwatch.StartNew();
                var color = _pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_COLOR);
                var colorBitmap = Senz3DUtils.GetRgb32Pixels(color);
                var colorImage = new Image<Rgb, byte>(colorBitmap);
                ColorImageFrameTime = sw.ElapsedMilliseconds;

                /* Get depth image */
                sw.Restart();
                var depth = _pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_DEPTH);
                var depthImageAndConfidence = Senz3DUtils.GetHighPrecisionDepthImage(depth, MinDepthValue, MaxDepthValue);

                // do adaptive sensing (alternating low/high confidence threshold) on depth image if enabled
                if (IsAdaptiveSensing)
                    depthImageAndConfidence = PerformAdaptiveSensing(depthImageAndConfidence);

                var depthImage = (Image<Gray, float>)depthImageAndConfidence[0];
                var confidenceMapImage = (Image<Rgb, Byte>)depthImageAndConfidence[1];

                DepthImageFrameTime = sw.ElapsedMilliseconds;
                ConfidenceMapImageFrameTime = 0;

                // alignment of rgb and depth image (depth images field of view is larger
                //than rgb image field of view and therefore needs to be 'cropped' properly.
                if (_rgbInDepthROI == Rectangle.Empty)
                    PushAlignedRgbAndDepthImageROI(depth, depthImage, colorImage);

                _pp.ReleaseFrame();

                if (IsRenderContent)
                    RenderImages(colorImage, depthImage, confidenceMapImage);

                // publish images finally
                PublishImages(colorImage, depthImage, confidenceMapImage);
            }

            _pp.Close();
            _pp.Dispose();
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

        private IImage[] PerformAdaptiveSensing(IImage[] depthImageAndConfidence)
        {
            var depthImage = (Image<Gray, float>)depthImageAndConfidence[0];
            var confidenceMapImage = (Image<Rgb, Byte>)depthImageAndConfidence[1];

            if (_alternate)
            {
                // dispose old depth image (low confidence threshold)
                if (_depthImageLow != null)
                    _depthImageLow.Dispose();

                _depthImageLow = depthImage.Copy();

                // dispose old confidence image (low confidence threshold)
                if (_confidenceImageLow != null)
                    _confidenceImageLow.Dispose();

                _confidenceImageLow = confidenceMapImage.Copy();
            }
            else
            {
                // dispose old depth image (high confidence threshold)
                if (_depthImageHigh != null)
                    _depthImageHigh.Dispose();

                _depthImageHigh = depthImage.Copy();

                // dispose old confidence image (high confidence threshold)
                if (_confidenceImageHigh != null)
                    _confidenceImageHigh.Dispose();

                _confidenceImageHigh = confidenceMapImage.Copy();

                if (_adaptiveSensingMaskImage == null)
                {
                    _adaptiveSensingMaskImage = CreateConfidenceMask(_confidenceImageHigh);
                }
            }

            if (_depthImageLow != null && _depthImageHigh != null)
            {
                CvInvoke.cvCopy(_depthImageHigh.Ptr, _depthImageLow.Ptr, _adaptiveSensingMaskImage.Ptr);
                depthImage.Dispose();
                depthImageAndConfidence[0] = _depthImageLow;
            }

            if (_confidenceImageLow != null && _confidenceImageHigh != null)
            {
                CvInvoke.cvCopy(_confidenceImageHigh.Ptr, _confidenceImageLow.Ptr, _adaptiveSensingMaskImage.Ptr);
                confidenceMapImage.Dispose();
                depthImageAndConfidence[1] = _confidenceImageLow;
            }

            return depthImageAndConfidence;
        }

        /// <summary>
        /// Creates a mask of the confidence image with a high confidence value. The mask is later used to merge
        /// the alternating low / high confidence image.
        /// </summary>
        /// <param name="confidenceImage"></param>
        /// <returns></returns>
        private Image<Gray, byte> CreateConfidenceMask(Image<Rgb, byte> confidenceImage)
        {
            var floodFillImage = confidenceImage.Convert<Gray, byte>();

            // TODO Not necessarily required to extract spot with valid depth values in low confidence depth image (high).
            CvInvoke.cvThreshold(floodFillImage.Ptr, floodFillImage.Ptr, 10, 250, THRESH.CV_THRESH_BINARY);

            // Mask need to be two pixels bigger than the source image.
            var width = confidenceImage.Width();
            var height = confidenceImage.Height();

            var shrinkMaskROI = new Rectangle(1, 1, width, height);
            var mask = new Image<Gray, byte>(width + 2, height + 2);

            var seedPoint = new Point(width / 2, height / 2);

            MCvConnectedComp comp;

            // Flood fill segment with lowest pixel value to allow for next segment on next iteration.
            CvInvoke.cvFloodFill(floodFillImage.Ptr,
                seedPoint,
                new MCvScalar(255.0),
                new MCvScalar(10),
                new MCvScalar(10),
                out comp,
                CONNECTIVITY.EIGHT_CONNECTED,
                FLOODFILL_FLAG.DEFAULT,
                mask.Ptr);

            mask = mask.Dilate(6).Erode(15);
            mask.ROI = shrinkMaskROI;

            mask = mask.Mul(255);
            var maskCopy = mask.Copy();
            Task.Factory.StartNew(() =>
            {
                var bitmap = maskCopy.ToBitmapSource(true);
                maskCopy.Dispose();
                return bitmap;
            }).ContinueWith(s => AdaptiveSensingMaskImageSource = s.Result);

            return mask;
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

        /// <summary>
        /// Aligns the rgb and depth image, calculates the aligned ROI and pushes it through the pipeline.
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="depthImage"></param>
        /// <param name="colorImage"></param>
        private void PushAlignedRgbAndDepthImageROI(PXCMImage depth, Image<Gray, float> depthImage, Image<Rgb, byte> colorImage)
        {
            /* Get UV map */
            var uvMapImage = Senz3DUtils.GetDepthUvMap(depth);

            /* Get RgbOfDepth */
            Senz3DUtils.GetRgbOfDepthPixels(depthImage, colorImage, uvMapImage, true, ref _rgbInDepthROI);
            Stage(new ROI(this, "rgbInDepthROI")
            {
                RoiRectangle = _rgbInDepthROI
            });
            Push();

            Log("Identified rgbInDepthROI as {0}", _rgbInDepthROI);
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

                pinfo.frameRateMin.numerator = 1;
                pinfo.frameRateMax.numerator = 25;
                pinfo.frameRateMin.denominator = pinfo.frameRateMax.denominator = 1;
            }
            else
            {
                pinfo.imageInfo.width = 320;
                pinfo.imageInfo.height = 240;

                pinfo.frameRateMin.numerator = 25;
                pinfo.frameRateMin.denominator = 1;
            }

            return pinfo;
        }

        #endregion
    }
}
