using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Tools.FlockingDevice.Tracking.Properties;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
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
            if (FlipHorizontal)
                image = image.Flip(FLIP.HORIZONTAL);
            if (FlipVertical)
                image = image.Flip(FLIP.VERTICAL);

            return image.Copy(ROI);
        }
    }
}
