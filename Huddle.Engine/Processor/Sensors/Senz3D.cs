using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.External.Extensions;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

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

        #endregion

        #region properties

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

                /* Display images */
                var color = _pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_COLOR);
                var depth = _pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_DEPTH);

                var colorBitmap = GetRgb32Pixels(color);
                var depthBitmap = GetRgb32Pixels(depth);
                var confidenceMapImage = GetDepthConfidencePixels(depth);

                _pp.ReleaseFrame();

                var colorImage = new Image<Rgb, byte>(colorBitmap);
                var depthImage = new Image<Rgb, byte>(depthBitmap);

                var colorImageCopy = colorImage.Copy();
                var depthImageCopy = depthImage.Copy();
                var confidenceMapImageCopy = confidenceMapImage.Copy();
                DispatcherHelper.RunAsync(() =>
                {
                    ColorImageSource = colorImageCopy.ToBitmapSource();
                    colorImageCopy.Dispose();

                    DepthImageSource = depthImageCopy.ToBitmapSource();
                    depthImageCopy.Dispose();

                    ConfidenceMapImageSource = confidenceMapImageCopy.ToBitmapSource();
                    confidenceMapImageCopy.Dispose();
                });

                Publish(new DataContainer(++_frameId, DateTime.Now)
                {
                    new RgbImageData("color", colorImage),
                    new RgbImageData("depth", depthImage),
                    new RgbImageData("confidence", confidenceMapImage)
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

        private Image<Rgb, byte> GetDepthConfidencePixels(PXCMImage image)
        {
            var inputWidth = (int)image.info.width;
            var inputHeight = (int)image.info.height;

            var confidencePixels = new Image<Rgb, byte>(inputWidth, inputHeight);

            float prop;
            _pp.QueryCapture().QueryDevice().QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_LOW_CONFIDENCE_VALUE, out prop);
            var lowConfidence = (int)prop;
            _pp.QueryCapture().QueryDevice().QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SATURATION_VALUE, out prop);
            var saturation = (int)prop;

            PXCMImage.ImageData cdata;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, out cdata) <
                pxcmStatus.PXCM_STATUS_NO_ERROR) return confidencePixels;

            for (var y = 0; y < inputHeight; y++)
            {
                for (var x = 0; x < inputWidth; x++)
                {
                    // read Depth
                    var pData = cdata.buffer.planes[0] + (y * inputWidth + x) * 2;
                    double depth = Marshal.ReadInt16(pData, 0);

                    if (depth == lowConfidence) // low confidence
                    {
                        confidencePixels[y, x] = new Rgb(255, 255, 255);
                    }
                    else
                    {
                        if (depth == saturation) // saturated
                        {
                            confidencePixels[y, x] = new Rgb(255, 0, 0);
                        }
                        else
                        {
                            confidencePixels[y, x] = new Rgb(0, depth / 32768.0 * 255.0, 0);

                            if (depth < _minDepth) _minDepth = depth;
                            if (depth > _maxDepth) _maxDepth = depth;
                        }
                    }
                }
            }

            image.ReleaseAccess(ref cdata);

            return confidencePixels;
        }

        private static PXCMCapture.VideoStream.ProfileInfo GetConfiguration(PXCMImage.ColorFormat format)
        {
            var pinfo = new PXCMCapture.VideoStream.ProfileInfo { imageInfo = { format = format } };

            if (((int)format & (int)PXCMImage.ImageType.IMAGE_TYPE_COLOR) != 0)
            {
                pinfo.imageInfo.width = 640;
                pinfo.imageInfo.height = 480;

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
