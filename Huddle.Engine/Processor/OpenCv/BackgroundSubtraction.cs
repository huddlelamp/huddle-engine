﻿using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Extensions;
using Huddle.Engine.Util;

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

        #region MinHandArmArea

        /// <summary>
        /// The <see cref="MinHandArmArea" /> property's name.
        /// </summary>
        public const string MinHandArmAreaPropertyName = "MinHandArmArea";

        private int _minHandArmArea = 1500;

        /// <summary>
        /// Sets and gets the MinHandArmArea property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MinHandArmArea
        {
            get
            {
                return _minHandArmArea;
            }

            set
            {
                if (_minHandArmArea == value)
                {
                    return;
                }

                RaisePropertyChanging(MinHandArmAreaPropertyName);
                _minHandArmArea = value;
                RaisePropertyChanged(MinHandArmAreaPropertyName);
            }
        }

        #endregion

        #region FloodFillMask

        /// <summary>
        /// The <see cref="FloodFillMask" /> property's name.
        /// </summary>
        public const string FloodFillMaskPropertyName = "FloodFillMask";

        private BitmapSource _floodFillMask = null;

        /// <summary>
        /// Sets and gets the FloodFillMask property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public BitmapSource FloodFillMask
        {
            get
            {
                return _floodFillMask;
            }

            set
            {
                if (_floodFillMask == value)
                {
                    return;
                }

                RaisePropertyChanging(FloodFillMaskPropertyName);
                _floodFillMask = value;
                RaisePropertyChanged(FloodFillMaskPropertyName);
            }
        }

        #endregion

        #region FloodFillDifference

        /// <summary>
        /// The <see cref="FloodFillDifference" /> property's name.
        /// </summary>
        public const string FloodFillDifferencePropertyName = "FloodFillDifference";

        private float _floodFillDifference = 25.0f;

        /// <summary>
        /// Sets and gets the FloodFillDifference property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float FloodFillDifference
        {
            get
            {
                return _floodFillDifference;
            }

            set
            {
                if (_floodFillDifference == value)
                {
                    return;
                }

                RaisePropertyChanging(FloodFillDifferencePropertyName);
                _floodFillDifference = value;
                RaisePropertyChanged(FloodFillDifferencePropertyName);
            }
        }

        #endregion

        #region ContourAccuracy

        /// <summary>
        /// The <see cref="ContourAccuracy" /> property's name.
        /// </summary>
        public const string ContourAccuracyPropertyName = "ContourAccuracy";

        private double _contourAccuracy = 0.05;

        /// <summary>
        /// Sets and gets the ContourAccuracy property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double ContourAccuracy
        {
            get
            {
                return _contourAccuracy;
            }

            set
            {
                if (_contourAccuracy == value)
                {
                    return;
                }

                RaisePropertyChanging(ContourAccuracyPropertyName);
                _contourAccuracy = value;
                RaisePropertyChanged(ContourAccuracyPropertyName);
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

            var minHandArmArea = MinHandArmArea;
            var contourAccuracy = ContourAccuracy;

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

            cleanerImage = cleanerImage.PyrUp().PyrDown();

            var output = cleanerImage.Copy();

            //if (IsSmoothGaussianEnabled)
            //    cleanerImage = cleanerImage.SmoothGaussian(3);

            double[] minValues;
            double[] maxValues;
            Point[] minLocations;
            Point[] maxLocations;
            cleanerImage.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

            //if (maxLocations.Length > 1)
            //    Console.WriteLine();

            var i = 0;

            var color = 0.0f;

            var outputImage = new Image<Rgb, byte>(width + 2, height + 2);

            while (!minValues[0].Equals(maxValues[0]) && i < 100)
            {
                //Flood(cleanerImage, maxLocations[0], 0.0f);

                MCvConnectedComp comp0;
                CvInvoke.cvFloodFill(cleanerImage.Ptr, maxLocations[0], new MCvScalar(0.0f), new MCvScalar(FloodFillDifference), new MCvScalar(FloodFillDifference), out comp0, CONNECTIVITY.EIGHT_CONNECTED, FLOODFILL_FLAG.DEFAULT, IntPtr.Zero);

                if (comp0.area > minHandArmArea)
                {
                    var mask = new Image<Gray, byte>(width + 2, height + 2);
                    MCvConnectedComp comp1;
                    CvInvoke.cvFloodFill(output.Ptr, maxLocations[0], new MCvScalar(color += 25), new MCvScalar(FloodFillDifference), new MCvScalar(FloodFillDifference), out comp1, CONNECTIVITY.EIGHT_CONNECTED, FLOODFILL_FLAG.DEFAULT, mask.Ptr);

                    Console.WriteLine();

                    mask = mask.Mul(255);

                    outputImage += mask.Convert<Rgb, byte>();

                    using (var storage = new MemStorage())
                    {
                        var contours = mask.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_EXTERNAL, storage);

                        CvInvoke.cvDrawContours(outputImage.Ptr, contours.Ptr, new MCvScalar(128), new MCvScalar(80), 1, 2, LINE_TYPE.EIGHT_CONNECTED, new Point());

                        for ( ; contours != null; contours = contours.HNext)
                        {

                            var currentContour = contours.ApproxPoly(contours.Perimeter * contourAccuracy, 1, storage);

                            using (var storage2 = new MemStorage())
                            {
                                var defacts = currentContour.GetConvexityDefacts(storage2, ORIENTATION.CV_CLOCKWISE).ToArray();

                                foreach (var defact in defacts)
                                {
                                    outputImage.Draw(new CircleF(defact.DepthPoint, 3), Rgbs.Red, 3);
                                    outputImage.Draw(new CircleF(defact.StartPoint, 3), Rgbs.Yellow, 3);
                                    outputImage.Draw(new CircleF(defact.EndPoint, 3), Rgbs.Blue, 3);
                                }
                            }


                            outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.BlueTorquoise, 2);

                            var area = currentContour.GetMinAreaRect();

                            var center = area.center;

                            var x = (float)(center.X + (50 * Math.Cos(area.angle.DegreeToRadians())));
                            var y = (float)(center.Y + (50 * Math.Sin(area.angle.DegreeToRadians())));

                            var toPoint = new PointF(x, y);

                            outputImage.Draw(new LineSegment2DF(center, toPoint), Rgbs.Green, 2);
                        }
                    }
                }

                //Flood(output, maxLocations[0], color += 50);
                //cleanerImage.Data[maxLocations[0].Y, maxLocations[0].X, 0] = 255.0;
                cleanerImage.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
                i++;
            }

            var outputImageCopy = outputImage.Copy();
            DispatcherHelper.RunAsync(() =>
            {
                FloodFillMask = outputImageCopy.ToBitmapSource();
                outputImageCopy.Dispose();
            });

            return output.Convert<Gray, float>();
        }

        private void Flood(IImage image, Point maxLocation, float color)
        {

        }
    }
}
