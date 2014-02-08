using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Emgu.CV.VideoSurveillance;
using Tools.FlockingDevice.Tracking.Properties;

namespace Tools.FlockingDevice.Tracking.Processor.OpenCv
{
    //[ViewTemplate("Blob Tracker", "BlobTracker")]
    public class BlobTracker : RgbProcessor
    {
        #region private fields

        private static MCvFont _font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_SIMPLEX, 0.3, 0.3);

        private readonly BlobTrackerAuto<Rgb> _blobTracker;

        #endregion

        #region properties

        #region IsOutputOnInputImage

        /// <summary>
        /// The <see cref="IsOutputOnInputImage" /> property's name.
        /// </summary>
        public const string IsOutputOnInputImagePropertyName = "IsOutputOnInputImage";

        private bool _isOutputOnInputImage = Settings.Default.IsOutputOnInputImage;

        /// <summary>
        /// Sets and gets the IsOutputOnInputImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool IsOutputOnInputImage
        {
            get
            {
                return _isOutputOnInputImage;
            }

            set
            {
                if (_isOutputOnInputImage == value)
                {
                    return;
                }

                RaisePropertyChanging(IsOutputOnInputImagePropertyName);
                _isOutputOnInputImage = value;
                RaisePropertyChanged(IsOutputOnInputImagePropertyName);
            }
        }

        #endregion

        #region IsBoundingBoxEnabled

        /// <summary>
        /// The <see cref="IsBoundingBoxEnabled" /> property's name.
        /// </summary>
        public const string IsBoundingBoxEnabledPropertyName = "IsBoundingBoxEnabled";

        private bool _isBoundingBoxEnabled = Settings.Default.IsBoundingBoxEnabled;

        /// <summary>
        /// Sets and gets the IsBoundingBoxEnabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool IsBoundingBoxEnabled
        {
            get
            {
                return _isBoundingBoxEnabled;
            }

            set
            {
                if (_isBoundingBoxEnabled == value)
                {
                    return;
                }

                RaisePropertyChanging(IsBoundingBoxEnabledPropertyName);
                _isBoundingBoxEnabled = value;
                RaisePropertyChanged(IsBoundingBoxEnabledPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public BlobTracker()
        {
            _blobTracker = new BlobTrackerAuto<Rgb>();
        }

        #endregion

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var mask = image.Convert<Gray, byte>();

            var outputImage = IsOutputOnInputImage ? image.Copy() : new Image<Rgb, byte>(image.Width, image.Height);

            _blobTracker.Process(image, mask);

            //var currentDevices = new List<IDevice>();

            foreach (var blob in _blobTracker)
            {
                var rect = new Rectangle((int)(blob.Center.X - blob.Size.Width / 2f), (int)(blob.Center.Y - blob.Size.Height / 2f), (int)blob.Size.Width, (int)blob.Size.Height);

                if (IsBoundingBoxEnabled)
                    outputImage.Draw(rect, Rgbs.White, 2);

                outputImage.Draw(blob.ID.ToString(CultureInfo.InvariantCulture), ref _font, Point.Round(blob.Center), Rgbs.Green);

                //if (_devices.All(d => d.Id != blob.ID))
                //{
                //    var device = new Smartphone("")
                //    {
                //        Id = blob.ID,
                //        X = blob.Center.X,
                //        Y = blob.Center.Y
                //    };

                //    _devices.Add(device);
                //    currentDevices.Add(device);
                //    if (DeviceEnter != null)
                //        DeviceEnter(this, new DeviceEventArgs(device));
                //}
                //else
                //{
                //    var device = _devices.Single(d => d.Id == blob.ID);
                //    device.X = blob.Center.X;
                //    device.Y = blob.Center.Y;

                //    currentDevices.Add(device);
                //    if (DeviceMove != null)
                //        DeviceMove(this, new DeviceEventArgs(device));
                //}
            }

            //if (_devices.Any() && currentDevices.Any())
            //    foreach (var device in _devices.Except(currentDevices).ToList())
            //    {
            //        if (DeviceLeave != null)
            //            DeviceLeave(this, new DeviceEventArgs(device));

            //        _devices.Remove(device);
            //    }
            //else if (!currentDevices.Any())
            //{
            //    foreach (var device in _devices.Except(currentDevices).ToList())
            //    {
            //        if (DeviceLeave != null)
            //            DeviceLeave(this, new DeviceEventArgs(device));
            //    }
            //    _devices.Clear();
            //}

            return outputImage;
        }
    }
}
