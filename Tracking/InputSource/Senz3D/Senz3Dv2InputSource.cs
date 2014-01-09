using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.InputSource.Senz3D
{
    [ViewTemplate("Senz3DInputSource")]
    public class Senz3Dv2InputSource : IInputSource
    {
        #region events

        public event EventHandler<ImageEventArgs2> ImageReady;

        #endregion

        #region private fields

        private PXCMSession _session;

        private UtilMPipeline pp;

        private PXCMCapture.Device _device;

        private bool _isRunning;

        private long _lastImageFrameTime = -1;

        #endregion

        private float _depthConfidenceThreshold = -1;

        public string FriendlyName {
            get
            {
                return "Senz3D v2 Input Source";
            }
        }

        public float DepthConfidenceThreshold
        {
            get
            {
                if (_device != null)
                    _device.QueryProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_CONFIDENCE_THRESHOLD, out _depthConfidenceThreshold);

                return _depthConfidenceThreshold;
            }
            set
            {
                _depthConfidenceThreshold = value;

                if (_device != null)
                    _device.SetProperty(PXCMCapture.Device.Property.PROPERTY_DEPTH_CONFIDENCE_THRESHOLD, _depthConfidenceThreshold);
            }

        }
        public bool DepthSmoothing { get; set; }
        public bool FlipVertical { get; set; }
        public bool FlipHorizontal { get; set; }

        #region ctor

        public Senz3Dv2InputSource()
        {
            var sts = PXCMSession.CreateInstance(out _session);

            Debug.Assert(sts >= pxcmStatus.PXCM_STATUS_NO_ERROR, "could not create session instance");
        }

        #endregion

        public void Start()
        {
            var thread = new Thread(DoRendering);
            thread.Start();
            Thread.Sleep(5);
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Pause()
        {
            _isRunning = false;
        }

        public void Resume()
        {
            Start();
        }

        public void Dispose()
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
                PXCMImage color = pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_COLOR);
                PXCMImage depth = pp.QueryImage(PXCMImage.ImageType.IMAGE_TYPE_DEPTH);

                var colorBitmap = GetRgb32Pixels(color);
                var depthBitmap = GetRgb32Pixels(depth);

                pp.ReleaseFrame();

                Image<Rgb, byte> colorImage = new Image<Rgb, byte>(colorBitmap);
                Image<Rgb, byte> depthImage = new Image<Rgb, byte>(depthBitmap);

                if (ImageReady != null)
                    ImageReady(this, new ImageEventArgs2(colorImage, depthImage, diffImageFrameTime));
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

        private PXCMCapture.VideoStream.ProfileInfo GetConfiguration(PXCMImage.ColorFormat format)
        {
            PXCMCapture.VideoStream.ProfileInfo pinfo = new PXCMCapture.VideoStream.ProfileInfo();
            pinfo.imageInfo.format = format;


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
