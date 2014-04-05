using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.External.Extensions;
using Emgu.CV.GPU;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.Sensors
{
    [ViewTemplate("Video Capture", "VideoCaptureTemplate")]
    public class VideoCapture : BaseProcessor
    {
        #region private fields

        private Capture _capture;

        private bool _isCapturing = false;

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
        [IgnoreDataMember]
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

        public override IData Process(IData data)
        {
            throw new NotImplementedException();
        }

        public override void Start()
        {
            _capture = new Capture();

            _isCapturing = true;

            new Thread(() =>
                       {
                           while (_isCapturing)
                           {
                               var frame = _capture.QueryFrame();
                               if (frame == null) continue;

                               var image = frame.Convert<Rgb, byte>();

                               var imageCopy = image.Copy();
                               DispatcherHelper.CheckBeginInvokeOnUI(() =>
                                                                     {
                                                                         ColorImageSource = imageCopy.ToBitmapSource();
                                                                         imageCopy.Dispose();
                                                                     });

                               var rgbImage = new RgbImageData(this, "color", image);
                               Stage(rgbImage);
                               Push();

                               Thread.Sleep(1000 / Fps);
                           }
                       }).Start();

            base.Start();
        }

        public override void Stop()
        {
            _isCapturing = false;

            // free camera resources
            if (_capture != null)
                _capture.Dispose();

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                ColorImageSource = null;
            });

            base.Stop();
        }
    }
}
