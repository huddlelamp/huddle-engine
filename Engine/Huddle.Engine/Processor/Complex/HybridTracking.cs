using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("Hybrid Tracking", "HybridTracking")]
    public class HybridTracking : RgbProcessor
    {
        #region properties

        #region CannyEdgesThreshold

        /// <summary>
        /// The <see cref="CannyEdgesThreshold" /> property's name.
        /// </summary>
        public const string CannyEdgesPropertyName = "CannyEdgesThreshold";

        private int _cannyEdgesThreshold = 110;

        /// <summary>
        /// Sets and gets the CannyEdgesThreshold property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int CannyEdgesThreshold
        {
            get
            {
                return _cannyEdgesThreshold;
            }

            set
            {
                if (_cannyEdgesThreshold == value)
                {
                    return;
                }

                RaisePropertyChanging(CannyEdgesPropertyName);
                _cannyEdgesThreshold = value;
                RaisePropertyChanged(CannyEdgesPropertyName);
            }
        }

        #endregion

        #region CannyEdgesThresholdLinking

        /// <summary>
        /// The <see cref="CannyEdgesThresholdLinking" /> property's name.
        /// </summary>
        public const string CannyEdgesThresholdLinkingPropertyName = "CannyEdgesThresholdLinking";

        private int _cannyEdgesThresholdLinking = 200;

        /// <summary>
        /// Sets and gets the CannyEdgesThresholdLinking property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int CannyEdgesThresholdLinking
        {
            get
            {
                return _cannyEdgesThresholdLinking;
            }

            set
            {
                if (_cannyEdgesThresholdLinking == value)
                {
                    return;
                }

                RaisePropertyChanging(CannyEdgesThresholdLinkingPropertyName);
                _cannyEdgesThresholdLinking = value;
                RaisePropertyChanged(CannyEdgesThresholdLinkingPropertyName);
            }
        }

        #endregion

        #region BinaryThreshold

        /// <summary>
        /// The <see cref="BinaryThreshold" /> property's name.
        /// </summary>
        public const string BinaryThresholdPropertyName = "BinaryThreshold";

        private byte _binaryThreshold = 127;

        /// <summary>
        /// Sets and gets the BinaryThreshold property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public byte BinaryThreshold
        {
            get
            {
                return _binaryThreshold;
            }

            set
            {
                if (_binaryThreshold == value)
                {
                    return;
                }

                RaisePropertyChanging(BinaryThresholdPropertyName);
                _binaryThreshold = value;
                RaisePropertyChanged(BinaryThresholdPropertyName);
            }
        }

        #endregion

        #region BinaryThresholdMaxValue

        /// <summary>
        /// The <see cref="BinaryThresholdMax" /> property's name.
        /// </summary>
        public const string BinaryThresholdMaxPropertyName = "BinaryThresholdMax";

        private byte _binaryThresholdMax = 255;

        /// <summary>
        /// Sets and gets the BinaryThresholdMax property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public byte BinaryThresholdMax
        {
            get
            {
                return _binaryThresholdMax;
            }

            set
            {
                if (_binaryThresholdMax == value)
                {
                    return;
                }

                RaisePropertyChanging(BinaryThresholdMaxPropertyName);
                _binaryThresholdMax = value;
                RaisePropertyChanged(BinaryThresholdMaxPropertyName);
            }
        }

        #endregion

        #region IsBinaryThresholdInv

        /// <summary>
        /// The <see cref="IsBinaryThresholdInv" /> property's name.
        /// </summary>
        public const string IsBinaryThresholdInvPropertyName = "IsBinaryThresholdInv";

        private bool _isBinaryThresholdInv = true;

        /// <summary>
        /// Sets and gets the IsBinaryThresholdInv property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsBinaryThresholdInv
        {
            get
            {
                return _isBinaryThresholdInv;
            }

            set
            {
                if (_isBinaryThresholdInv == value)
                {
                    return;
                }

                RaisePropertyChanging(IsBinaryThresholdInvPropertyName);
                _isBinaryThresholdInv = value;
                RaisePropertyChanged(IsBinaryThresholdInvPropertyName);
            }
        }

        #endregion

        #region GaussianPyramidDownUpDecomposition

        /// <summary>
        /// The <see cref="GaussianPyramidDownUpDecomposition" /> property's name.
        /// </summary>
        public const string GaussianPyramidDownUpDecompositionPropertyName = "GaussianPyramidDownUpDecomposition";

        private bool _gaussianPyramidDownUpDecomposition = true;

        /// <summary>
        /// Sets and gets the GaussianPyramidDownUpDecomposition property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool GaussianPyramidDownUpDecomposition
        {
            get
            {
                return _gaussianPyramidDownUpDecomposition;
            }

            set
            {
                if (_gaussianPyramidDownUpDecomposition == value)
                {
                    return;
                }

                RaisePropertyChanging(GaussianPyramidDownUpDecompositionPropertyName);
                _gaussianPyramidDownUpDecomposition = value;
                RaisePropertyChanged(GaussianPyramidDownUpDecompositionPropertyName);
            }
        }

        #endregion

        #region IsSmoothGaussian

        /// <summary>
        /// The <see cref="IsSmoothGaussian" /> property's name.
        /// </summary>
        public const string IsSmoothGaussianPropertyName = "IsSmoothGaussian";

        private bool _isSmoothGaussian = false;

        /// <summary>
        /// Sets and gets the IsSmoothGaussian property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSmoothGaussian
        {
            get
            {
                return _isSmoothGaussian;
            }

            set
            {
                if (_isSmoothGaussian == value)
                {
                    return;
                }

                RaisePropertyChanging(IsSmoothGaussianPropertyName);
                _isSmoothGaussian = value;
                RaisePropertyChanged(IsSmoothGaussianPropertyName);
            }
        }

        #endregion

        #region SmoothGaussian

        /// <summary>
        /// The <see cref="SmoothGaussian" /> property's name.
        /// </summary>
        public const string SmoothGaussianPropertyName = "SmoothGaussian";

        private int _smoothGaussian = 1;

        /// <summary>
        /// Sets and gets the SmoothGaussian property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int SmoothGaussian
        {
            get
            {
                return _smoothGaussian;
            }

            set
            {
                if (_smoothGaussian == value)
                {
                    return;
                }

                RaisePropertyChanging(SmoothGaussianPropertyName);
                _smoothGaussian = value;
                RaisePropertyChanged(SmoothGaussianPropertyName);
            }
        }

        #endregion

        #region Hsv

        /// <summary>
        /// The <see cref="H" /> property's name.
        /// </summary>
        public const string HPropertyName = "H";

        private byte _h = 0;

        /// <summary>
        /// Sets and gets the H property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public byte H
        {
            get
            {
                return _h;
            }

            set
            {
                if (_h == value)
                {
                    return;
                }

                RaisePropertyChanging(HPropertyName);
                _h = value;
                RaisePropertyChanged(HPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="S" /> property's name.
        /// </summary>
        public const string SPropertyName = "S";

        private byte _s = 0;

        /// <summary>
        /// Sets and gets the S property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public byte S
        {
            get
            {
                return _s;
            }

            set
            {
                if (_s == value)
                {
                    return;
                }

                RaisePropertyChanging(SPropertyName);
                _s = value;
                RaisePropertyChanged(SPropertyName);
            }
        }

        /// <summary>
        /// The <see cref="V" /> property's name.
        /// </summary>
        public const string VPropertyName = "V";

        private byte _v = 0;

        /// <summary>
        /// Sets and gets the V property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public byte V
        {
            get
            {
                return _v;
            }

            set
            {
                if (_v == value)
                {
                    return;
                }

                RaisePropertyChanging(VPropertyName);
                _v = value;
                RaisePropertyChanged(VPropertyName);
            }
        }

        #endregion

        #endregion

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var binaryThreshold = new Gray(BinaryThreshold);
            var binaryThresholdMax = new Gray(BinaryThresholdMax);
            var isBinaryThresholdInv = IsBinaryThresholdInv;

            var imageCopy = image.Copy();

            ////var mask = imageCopy.InRange(new Rgb(100, 100, 100), Rgbs.White);
            ////imageCopy.SetValue(Rgbs.Black, mask);

            ////var hsvImage = image.Convert<Hsv, byte>();
            ////hsvImage.InRange(new Hsv(H, S, V), Rgbs.White);

            ////imageCopy._GammaCorrect(1.8d);

            //var outputImage = new Image<Rgb, byte>(image.Width, image.Height);

            var grayImage = imageCopy.Convert<Gray, byte>();

            var thresholdImage = isBinaryThresholdInv ?
                grayImage.ThresholdBinaryInv(binaryThreshold, binaryThresholdMax) :
                grayImage.ThresholdBinary(binaryThreshold, binaryThresholdMax);

            //var outputImage = new Image<Rgb, byte>(image.Width, image.Height);

            //outputImage.Add(grayImage.Convert<Rgb, byte>());

            //using (var storage = new MemStorage())
            //{
            //    for (
            //        var contours = thresholdImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
            //            RETR_TYPE.CV_RETR_EXTERNAL);
            //        contours != null;
            //        contours = contours.HNext)
            //    {
            //        var currentContour = contours.ApproxPoly(contours.Perimeter * 0.09, storage);

            //        //if (currentContour.Area > MinContourArea && currentContour.Area < MaxContourArea)
            //        //    //only consider contours with area greater than 250
            //        //{
            //        if (currentContour.Total == 4)
            //        outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.BlueTorquoise, 2);
            //        //}
            //    }
            //}

            return thresholdImage.Convert<Rgb, byte>();

            //return outputImage;

            // APPROACH 2:
            //var thresholdImage = image.Canny(CannyEdgesThreshold, CannyEdgesThresholdLinking);

            //if (GaussianPyramidDownUpDecomposition)
            //    thresholdImage = thresholdImage.PyrDown().PyrUp();

            //if (IsSmoothGaussian)
            //    thresholdImage = thresholdImage.SmoothGaussian(SmoothGaussian);

            //thresholdImage = thresholdImage.ThresholdBinary(new Gray(BinaryThreshold), new Gray(BinaryThresholdMax));

            ////var thresholdImage = grayImage.Canny(BinaryThreshold, BinaryThresholdMax);

            ////var outputImage = new Image<Rgb, byte>(image.Width, image.Height);

            ////outputImage.Add(grayImage.Convert<Rgb, byte>());

            //if (IsBinaryThresholdInv)
            //    thresholdImage = thresholdImage.Not();

            //return thresholdImage.Convert<Rgb, byte>();
        }

        public override IDataContainer PostProcess(IDataContainer dataContainer)
        {
            return base.PostProcess(dataContainer);
        }
    }
}
