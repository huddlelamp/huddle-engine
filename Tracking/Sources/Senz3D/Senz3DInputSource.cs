using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Sources.Senz3D
{
    [ViewTemplate("Senz3DInputSource")]
    public class Senz3DInputSource : InputSource
    {
        #region events

        public override event EventHandler<ImageEventArgs> ImageReady;

        #endregion

        #region private fields

        private PXCMSession _session;

        private UtilMPipeline pp;

        private PXCMCapture.Device _device;

        private bool _isRunning;

        private long _lastImageFrameTime = -1;

        #endregion

        #region properties

        #region FriendlyName

        public override string FriendlyName
        {
            get
            {
                return "Senz3D Input Source";
            }
        }

        #endregion

        #region DepthConfidenceThreshold

        /// <summary>
        /// The <see cref="DepthConfidenceThreshold" /> property's name.
        /// </summary>
        public const string DepthConfidenceThresholdPropertyName = "DepthConfidenceThreshold";

        private float _depthConfidenceThreshold = -1;

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

        #endregion

        #region ctor

        public Senz3DInputSource()
        {
            var sts = PXCMSession.CreateInstance(out _session);

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

        public override void Start()
        {
            var thread = new Thread(DoRendering);
            thread.Start();
            Thread.Sleep(5);
        }

        public override void Stop()
        {
            _isRunning = false;
        }

        public override void Pause()
        {
            _isRunning = false;
        }

        public override void Resume()
        {
            Start();
        }

        public override void Dispose()
        {
            Stop();
        }

        #region private methods

        private void DoRendering()
        {
            _isRunning = true;

            //PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
            //desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            //desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;

            //for (uint i = 0; ; i++)
            //{
            //    PXCMSession.ImplDesc desc1;
            //    if (_session.QueryImpl(ref desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
            //    PXCMCapture capture;
            //    if (_session.CreateImpl<PXCMCapture>(ref desc1, PXCMCapture.CUID, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;
            //    for (uint j = 0; ; j++)
            //    {
            //        PXCMCapture.DeviceInfo dinfo;
            //        if (capture.QueryDevice(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

            //        Console.WriteLine(dinfo.name.get());
            //    }
            //    capture.Dispose();
            //}

            /* UtilMPipeline works best for synchronous color and depth streaming */
            pp = new UtilMPipeline();

            /* Set Input Source */
            pp.capture.SetFilter("DepthSense Device 325V2");

            /* Set Color & Depth Resolution */
            PXCMCapture.VideoStream.ProfileInfo cinfo = GetConfiguration(PXCMImage.ColorFormat.COLOR_FORMAT_RGB32);
            pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_RGB32, cinfo.imageInfo.width, cinfo.imageInfo.height);
            pp.capture.SetFilter(ref cinfo); // only needed to set FPS

            PXCMCapture.VideoStream.ProfileInfo dinfo2 = GetConfiguration(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH);
            pp.EnableImage(PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, dinfo2.imageInfo.width, dinfo2.imageInfo.height);
            pp.capture.SetFilter(ref dinfo2); // only needed to set FPS

            /* Initialization */
            if (!pp.Init())
                throw new Exception("Could not initialize Senz3D hardware");

            _device = pp.capture.device;
            pp.capture.device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_CONFIDENCE_THRESHOLD, DepthConfidenceThreshold);

            while (_isRunning)
            {
                var currentImageFrameTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                var diffImageFrameTime = currentImageFrameTime - _lastImageFrameTime;
                _lastImageFrameTime = currentImageFrameTime;

                /* If raw depth is needed, disable smoothing */
                pp.capture.device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SMOOTHING, DepthSmoothing ? 1 : 0);

                /* Wait until a frame is ready */
                if (!pp.AcquireFrame(true)) break;
                if (pp.IsDisconnected()) break;

                /* Display images */
                var color = pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_COLOR);
                var depth = pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_DEPTH);

                var colorBitmap = GetRgb32Pixels(color);
                var depthBitmap = GetRgb32Pixels(depth);
                var depthConfidenceBitmap = GetDepthConfidencePixels(depth);

                pp.ReleaseFrame();

                var colorImage = new Image<Rgb, byte>(colorBitmap);
                var depthImage = new Image<Rgb, byte>(depthBitmap);

                if (ImageReady != null)
                {
                    ImageReady(this, new ImageEventArgs(new Dictionary<string, Image<Rgb, byte>>
                                                                                         {
                                                                                            {"color", colorImage},
                                                                                            {"depth", depthImage},
                                                                                            {"confidence", depthConfidenceBitmap}
                                                                                         }, diffImageFrameTime));
                }
            }

            pp.Close();
            pp.Dispose();
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

        double minDepth = Double.MaxValue;
        double maxDepth = -1.0;

        private Image<Rgb, byte> GetDepthConfidencePixels(PXCMImage image)
        {
            var cwidth = Align16(image.info.width); /* aligned width */
            var cheight = (int)image.info.height;

            var inputWidth = (int)image.info.width;
            var inputHeight = (int)image.info.height;

            var confidencePixels = new Image<Rgb, byte>(inputWidth, inputHeight);

            float prop;
            pp.QueryCapture().QueryDevice().QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_LOW_CONFIDENCE_VALUE, out prop);
            var LOW_CONFIDENCE = (int)prop;
            pp.QueryCapture().QueryDevice().QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_SATURATION_VALUE, out prop);
            var SATURATION = (int)prop;

            PXCMImage.ImageData cdata;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                for (int y = 0; y < inputHeight; y++)
                {
                    for (int x = 0; x < inputWidth; x++)
                    {
                        // read Depth
                        var pData = cdata.buffer.planes[0] + (y * inputWidth + x) * 2;
                        double depth = Marshal.ReadInt16(pData, 0);

                        if (depth == LOW_CONFIDENCE) // low confidence
                        {
                            confidencePixels[y, x] = new Rgb(255, 255, 255);
                        }
                        else
                        {
                            if (depth == SATURATION) // saturated
                            {
                                confidencePixels[y, x] = new Rgb(255, 0, 0);
                            }
                            else
                            {
                                confidencePixels[y, x] = new Rgb(0, depth / 32768.0 * 255.0, 0);

                                if (depth < minDepth) minDepth = depth;
                                if (depth > maxDepth) maxDepth = depth;
                            }
                        }
                    }
                }

                image.ReleaseAccess(ref cdata);
            }

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
