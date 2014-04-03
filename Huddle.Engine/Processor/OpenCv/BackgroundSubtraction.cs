using System;
using System.Drawing;
using System.Threading.Tasks;
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

        #region IsSmoothGaussianEnabled

        /// <summary>
        /// The <see cref="IsSmoothGaussianEnabled" /> property's name.
        /// </summary>
        public const string IsSmoothGaussianEnabledPropertyName = "IsSmoothGaussianEnabled";

        private bool _isSmoothGaussianEnabled = false;

        /// <summary>
        /// Sets and gets the IsSmoothGaussianEnabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSmoothGaussianEnabled
        {
            get
            {
                return _isSmoothGaussianEnabled;
            }

            set
            {
                if (_isSmoothGaussianEnabled == value)
                {
                    return;
                }

                RaisePropertyChanging(IsSmoothGaussianEnabledPropertyName);
                _isSmoothGaussianEnabled = value;
                RaisePropertyChanged(IsSmoothGaussianEnabledPropertyName);
            }
        }

        #endregion

        #region NumDilate

        /// <summary>
        /// The <see cref="NumDilate" /> property's name.
        /// </summary>
        public const string NumDilatePropertyName = "NumDilate";

        private int _numDilate = 2;

        /// <summary>
        /// Sets and gets the NumDilate property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public int NumDilate
        {
            get
            {
                return _numDilate;
            }

            set
            {
                if (_numDilate == value)
                {
                    return;
                }

                RaisePropertyChanging(NumDilatePropertyName);
                _numDilate = value;
                RaisePropertyChanged(NumDilatePropertyName);
            }
        }

        #endregion

        #region NumErode

        /// <summary>
        /// The <see cref="NumErode" /> property's name.
        /// </summary>
        public const string NumErodePropertyName = "NumErode";

        private int _numErode = 2;

        /// <summary>
        /// Sets and gets the NumErode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public int NumErode
        {
            get
            {
                return _numErode;
            }

            set
            {
                if (_numErode == value)
                {
                    return;
                }

                RaisePropertyChanging(NumErodePropertyName);
                _numErode = value;
                RaisePropertyChanged(NumErodePropertyName);
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

            var width = image.Width;
            var height = image.Height;

            var lowCutOffDepth = LowCutOffDepth;
            var highCutOffDepth = HighCutOffDepth;

            var cleanImage = image.Copy().Sub(_backgroundImage);

            var cleanerImage = new Image<Gray, byte>(width, height);
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    var val = (byte)cleanImage.Data[y, x, 0];

                    byte value;
                    if (val > lowCutOffDepth && val < highCutOffDepth)
                        value = (byte)image.Data[y, x, 0];
                    else
                        value = 0;

                    cleanerImage.Data[y, x, 0] = value;
                }
            });

            cleanerImage = cleanerImage.Erode(NumErode).Dilate(NumDilate);

            //if (IsSmoothGaussianEnabled)
            //    cleanerImage = cleanerImage.SmoothGaussian(3);

            return cleanerImage.Convert<Gray, float>();
        }
    }
}
