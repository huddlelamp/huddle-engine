using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Extensions;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor
{
    [ViewTemplate("Data Renderer", "DataRenderer")]
    public class DataRenderer : BaseProcessor
    {
        #region static fields

        public static MCvFont EmguFont = new MCvFont(FONT.CV_FONT_HERSHEY_DUPLEX, 0.3, 0.3);
        public static MCvFont EmguFontBig = new MCvFont(FONT.CV_FONT_HERSHEY_DUPLEX, 1.0, 1.0);

        #endregion

        #region properties

        #region DebugOutputBitmapSource

        /// <summary>
        /// The <see cref="DebugOutputBitmapSource" /> property's name.
        /// </summary>
        public const string DebugOutputBitmapSourcePropertyName = "DebugOutputBitmapSource";

        private BitmapSource _debugOutputBitmapSource;

        /// <summary>
        /// Sets and gets the DebugOutputBitmapSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource DebugOutputBitmapSource
        {
            get
            {
                return _debugOutputBitmapSource;
            }

            set
            {
                if (_debugOutputBitmapSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DebugOutputBitmapSourcePropertyName);
                _debugOutputBitmapSource = value;
                RaisePropertyChanged(DebugOutputBitmapSourcePropertyName);
            }
        }

        #endregion

        #endregion

        #region private members

        private VideoWriter _videoWriter;

        private const int Width = 1280;
        private const int Height = 720;

        private Image<Rgb, byte> _debugOutputImage;

        #endregion

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            _debugOutputImage = new Image<Rgb, byte>(Width, Height);

            var rgbImage = dataContainer.OfType<RgbImageData>().ToArray();

            if (!rgbImage.Any()) return null;

            _debugOutputImage += rgbImage.First().Image.Copy();

            var devices = dataContainer.OfType<Device>().ToArray();
            //var unknownDevices = dataContainer.OfType<Device>().Where(d => !d.IsIdentified).ToArray();
            var hands = dataContainer.OfType<Hand>().ToArray();

            foreach (var device in devices)
            {
                var polyline = new List<Point>();
                foreach (var point in device.Shape.Points)
                {
                    var x = point.X * Width;
                    var y = point.Y * Height;

                    polyline.Add(new Point((int)x, (int)y));
                }

                var centerX = (int)(device.SmoothedCenter.X / 320 * Width);
                var centerY = (int)(device.SmoothedCenter.Y / 240 * Height);

                _debugOutputImage.DrawPolyline(polyline.ToArray(), true, device.IsIdentified ? Rgbs.Red : Rgbs.White, 5);

                if (device.IsIdentified)
                    _debugOutputImage.Draw(string.Format("Id {0}", device.DeviceId), ref EmguFontBig, new Point(centerX, centerY), Rgbs.Red);
            }

            foreach (var hand in hands)
            {
                var resizedHandSegment = hand.Segment.Resize(_debugOutputImage.Width, _debugOutputImage.Height, INTER.CV_INTER_CUBIC).Mul(255);

                //_debugOutputImage = _debugOutputImage.Copy(resizedHandSegment.Not());
                _debugOutputImage = _debugOutputImage.AddWeighted(resizedHandSegment.Convert<Rgb, byte>(), 1.0, 0.5, 0.0);

                resizedHandSegment.Dispose();

                var point = new Point((int)(hand.RelativeCenter.X * Width), (int)(hand.RelativeCenter.Y * Height));
                var labelPoint = new Point((int)(hand.RelativeCenter.X * Width + 30), (int)(hand.RelativeCenter.Y * Height));

                _debugOutputImage.Draw(new CircleF(point, 10), Rgbs.Red, 6);
                _debugOutputImage.Draw(string.Format("Id {0} (d={1:F0})", hand.Id, hand.Depth), ref EmguFontBig, labelPoint, Rgbs.Red);
            }

            var debugOutputImageCopy = _debugOutputImage.Copy();
            Task.Factory.StartNew(() =>
            {
                var bitmapSource = debugOutputImageCopy.ToBitmapSource(true);
                debugOutputImageCopy.Dispose();
                return bitmapSource;
            }).ContinueWith(t => DebugOutputBitmapSource = t.Result);

            Stage(new RgbImageData(this, "DataRenderer", _debugOutputImage.Copy()));

            if (_videoWriter != null)
                _videoWriter.WriteFrame(_debugOutputImage.Convert<Bgr, byte>());

            _debugOutputImage.Dispose();

            Push();

            return base.PreProcess(dataContainer);
        }

        public override IData Process(IData data)
        {
            return data;
        }

        public override void Start()
        {
            _videoWriter = new VideoWriter("DataRenderer.avi", CvInvoke.CV_FOURCC('D', 'I', 'V', 'X'), 25, Width, Height, true);

            base.Start();
        }

        public override void Stop()
        {
            if (_videoWriter != null)
            {
                _videoWriter.Dispose();
                _videoWriter = null;
            }

            base.Stop();
        }

        public override Bitmap[] TakeSnapshots()
        {
            return new[]
            {
                DebugOutputBitmapSource.BitmapFromSource()
            };
        }
    }
}
