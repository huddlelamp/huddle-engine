using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.GPU;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Command;
using Huddle.Engine.Properties;
using Huddle.Engine.Util;
using Point = System.Windows.Point;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("BackgroundSubtraction", "BackgroundSubtraction")]
    public class BackgroundSubtraction : BaseImageProcessor<Gray, float>
    {
        #region private fields

        private Image<Gray, float> _backgroundImage;

        #endregion

        #region commands

        public RelayCommand SubtractCommand { get; private set; }

        #endregion

        #region properties

        #region LowCutOffDepth

        /// <summary>
        /// The <see cref="LowCutOffDepth" /> property's name.
        /// </summary>
        public const string LowCutOffDepthPropertyName = "LowCutOffDepth";

        private float _lowCutOffDepth = 5.0f;

        /// <summary>
        /// Sets and gets the LowCutOffDepth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float LowCutOffDepth
        {
            get
            {
                return _lowCutOffDepth;
            }

            set
            {
                if (_lowCutOffDepth == value)
                {
                    return;
                }

                RaisePropertyChanging(LowCutOffDepthPropertyName);
                _lowCutOffDepth = value;
                RaisePropertyChanged(LowCutOffDepthPropertyName);
            }
        }

        #endregion

        #region HighCutOffDepth

        /// <summary>
        /// The <see cref="HighCutOffDepth" /> property's name.
        /// </summary>
        public const string HighCutOffDepthPropertyName = "HighCutOffDepth";

        private float _highCutOffDepth = 1000.0f;

        /// <summary>
        /// Sets and gets the HighCutOffDepth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float HighCutOffDepth
        {
            get
            {
                return _highCutOffDepth;
            }

            set
            {
                if (_highCutOffDepth == value)
                {
                    return;
                }

                RaisePropertyChanging(HighCutOffDepthPropertyName);
                _highCutOffDepth = value;
                RaisePropertyChanged(HighCutOffDepthPropertyName);
            }
        }

        #endregion

        #endregion

        #region public properties

        #region ctor

        public BackgroundSubtraction()
        {
            SubtractCommand = new RelayCommand(() =>
            {
                _backgroundImage = null;
            });
        }

        #endregion

        #endregion

        public override Image<Gray, float> PreProcess(Image<Gray, float> image)
        {
            return image;
        }

        public override Image<Gray, float> ProcessAndView(Image<Gray, float> image)
        {
            if (_backgroundImage == null)
            {
                _backgroundImage = image.Copy();
                return null;
            }

            var cleanImage = image.Copy().Sub(_backgroundImage);

            var cleanerImage = new Image<Gray, byte>(image.Width, image.Height);
            for (int y = 0; y < cleanImage.Height; y++)
            {
                for (int x = 0; x < cleanImage.Width; x++)
                {
                    byte val = (byte)cleanImage[y, x].Intensity;

                    //cleanerImage[y, x] = new Gray(val);

                    if (val > LowCutOffDepth && val < HighCutOffDepth)
                    {
                        cleanerImage[y, x] = image[y, x];
                    }
                    else
                    {
                        cleanerImage[y, x] = new Gray(0);
                    }
                }
            }

            cleanerImage = cleanerImage.Erode(1).Dilate(1);

            return cleanerImage.Convert<Gray, float>();
        }
    }
}
