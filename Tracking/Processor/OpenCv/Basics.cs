using System;
using System.Drawing;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Tools.FlockingDevice.Tracking.Properties;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor.OpenCv
{
    [XmlType]
    [ViewTemplate("Basics")]
    public class Basics : RgbProcessor
    {
        #region public properties

        #region FriendlyName

        public override string FriendlyName
        {
            get
            {
                return "Basic Processor";
            }
        }

        #endregion

        #region ROI

        /// <summary>
        /// The <see cref="ROI" /> property's name.
        /// </summary>
        public const string ROIPropertyName = "ROI";

        private Rectangle _roi = Settings.Default.ROI;

        /// <summary>
        /// Sets and gets the ROI property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore]
        public Rectangle ROI
        {
            get
            {
                return _roi;
            }

            set
            {
                if (_roi == value)
                {
                    return;
                }

                RaisePropertyChanging(ROIPropertyName);
                _roi = value;
                RaisePropertyChanged(ROIPropertyName);
            }
        }

        #endregion

        #region ROIString

        private readonly RectangleConverter _doiConverter = new RectangleConverter();

        [XmlAttribute(ROIPropertyName)]
        public string ROIString
        {
            get { return (string)_doiConverter.ConvertTo(ROI, typeof(string)); }
            set
            {
                var newROI = _doiConverter.ConvertFrom(value);
                if (newROI != null) ROI = (Rectangle)newROI;
            }
        }

        #endregion

        #region FlipVertical

        /// <summary>
        /// The <see cref="FlipVertical" /> property's name.
        /// </summary>
        public const string FlipVerticalPropertyName = "FlipVertical";

        private bool _flipVertical = Settings.Default.FlipVertical;

        /// <summary>
        /// Sets and gets the FlipVertical property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool FlipVertical
        {
            get
            {
                return _flipVertical;
            }

            set
            {
                if (_flipVertical == value)
                {
                    return;
                }

                RaisePropertyChanging(FlipVerticalPropertyName);
                _flipVertical = value;
                RaisePropertyChanged(FlipVerticalPropertyName);
            }
        }

        #endregion

        #region FlipHorizontal

        /// <summary>
        /// The <see cref="FlipHorizontal" /> property's name.
        /// </summary>
        public const string FlipHorizontalPropertyName = "FlipHorizontal";

        private bool _flipHorizontal = Settings.Default.FlipHorizontal;

        /// <summary>
        /// Sets and gets the FlipHorizontal property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool FlipHorizontal
        {
            get
            {
                return _flipHorizontal;
            }

            set
            {
                if (_flipHorizontal == value)
                {
                    return;
                }

                RaisePropertyChanging(FlipHorizontalPropertyName);
                _flipHorizontal = value;
                RaisePropertyChanged(FlipHorizontalPropertyName);
            }
        }

        #endregion

        #endregion

        public override Image<Rgb, byte> PreProcess(Image<Rgb, byte> image0)
        {
            var image = base.PreProcess(image0);

            image.Draw(ROI, Rgbs.Red, 1);

            return image;
        }

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            // mirror image
            try
            {
                var imageCopy = image.Copy(ROI);

                if (FlipHorizontal)
                    imageCopy = imageCopy.Flip(FLIP.HORIZONTAL);
                if (FlipVertical)
                    imageCopy = imageCopy.Flip(FLIP.VERTICAL);

                return imageCopy;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
