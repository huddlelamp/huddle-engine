using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Extensions;
using Huddle.Engine.Processor.OpenCv.Filter;
using Huddle.Engine.Processor.OpenCv.Struct;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("BackgroundSubtraction", "BackgroundSubtraction")]
    public class BackgroundSubtraction : BaseImageProcessor<Gray, float>
    {
        #region private fields

        private Image<Gray, float> _backgroundImage;

        private int _collectedBackgroundImages = 0;

        private readonly List<Hand> _palms = new List<Hand>();

        private static long _id = -1;

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

        #region HandLocationSamples

        /// <summary>
        /// The <see cref="HandLocationSamples" /> property's name.
        /// </summary>
        public const string HandLocationSamplesPropertyName = "HandLocationSamples";

        private int _handLocationSamples = 30;

        /// <summary>
        /// Sets and gets the HandLocationSamples property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int HandLocationSamples
        {
            get
            {
                return _handLocationSamples;
            }

            set
            {
                if (_handLocationSamples == value)
                {
                    return;
                }

                RaisePropertyChanging(HandLocationSamplesPropertyName);
                _handLocationSamples = value;
                RaisePropertyChanged(HandLocationSamplesPropertyName);
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
        [IgnoreDataMember]
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

        #endregion

        #region ctor

        public BackgroundSubtraction()
        {
            SubtractCommand = new RelayCommand(() =>
            {
                _backgroundImage = null;
            });
        }

        #endregion

        public override void Start()
        {
            _collectedBackgroundImages = 0;

            base.Start();
        }

        public override Image<Gray, float> ProcessAndView(Image<Gray, float> image)
        {
            if (BuildingBackgroundImage(image)) return null;

            var now = DateTime.Now;

            _palms.RemoveAll(p => (now - p.LastUpdate).TotalMilliseconds > 150);

            var width = image.Width;
            var height = image.Height;

            var lowCutOffDepth = LowCutOffDepth;
            var highCutOffDepth = HighCutOffDepth;

            // This image is used to segment object from background
            var imageRemovedBackground = image.Sub(_backgroundImage);

            // This image is necessary for using FloodFill to avoid filling background
            // (segmented objects are shifted back to original depth location after background subtraction)
            var imageWithOriginalDepth = new Image<Gray, byte>(width, height);

            var imageData = image.Data;
            var imageRemovedBackgroundData = imageRemovedBackground.Data;
            var imageWithOriginalDepthData = imageWithOriginalDepth.Data;

            Parallel.For(0, height, y =>
            {
                byte originalDepthValue;
                for (var x = 0; x < width; x++)
                {
                    // DON'T REMOVE CAST (it is necessary!!! :) )
                    var depthValue = (byte)imageRemovedBackgroundData[y, x, 0];

                    if (depthValue > lowCutOffDepth && depthValue < highCutOffDepth)
                        originalDepthValue = (byte)imageData[y, x, 0];
                    else
                        originalDepthValue = 0;

                    imageWithOriginalDepthData[y, x, 0] = originalDepthValue;
                }
            });

            imageRemovedBackground.Dispose();

            var imageWithOriginalDepthCopy = imageWithOriginalDepth.Copy();

            // Remove noise (background noise)
            imageWithOriginalDepth = imageWithOriginalDepth
                .Erode(NumErode)
                .Dilate(NumDilate)
                .PyrUp()
                .PyrDown();

            var targetImage = imageWithOriginalDepth.Copy();

            double[] minValues;
            double[] maxValues;
            Point[] minLocations;
            Point[] maxLocations;
            imageWithOriginalDepth.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

            var minHandArmArea = MinHandArmArea;
            var maxHandArmArea = width * height - 1000;

            var shrinkMaskROI = new Rectangle(1, 1, width, height);

            var color = 0.0f;

            var debugOutput = new Image<Rgb, byte>(width, height);

            MCvConnectedComp comp;
            for (var i = 0; !minValues[0].Equals(maxValues[0]) && i < 100;
                imageWithOriginalDepth.MinMax(out minValues, out maxValues, out minLocations, out maxLocations), i++)
            {
                var mask = new Image<Gray, byte>(width + 2, height + 2);

                CvInvoke.cvFloodFill(imageWithOriginalDepth.Ptr,
                    maxLocations[0],
                    new MCvScalar(0.0f),
                    new MCvScalar(FloodFillDifference),
                    new MCvScalar(FloodFillDifference),
                    out comp,
                    CONNECTIVITY.EIGHT_CONNECTED,
                    FLOODFILL_FLAG.DEFAULT,
                    mask.Ptr);

                if (comp.area > minHandArmArea && comp.area < maxHandArmArea)
                {
                    mask.ROI = shrinkMaskROI;
                    var maskCopy = mask.Copy();
                    var coloredMask = maskCopy.Mul(color += 25);

                    var colorMaskData = coloredMask.Data;
                    var targetImageData = targetImage.Data;

                    // This does not work therefore the following manual approach is used.
                    //targetImage = targetImage.And(coloredMask);

                    Parallel.For(0, height, y0 =>
                    {
                        for (var x0 = 0; x0 < width; x0++)
                        {
                            var maskValue = colorMaskData[y0, x0, 0];
                            if (maskValue > 0)
                                targetImageData[y0, x0, 0] = maskValue;
                        }
                    });

                    debugOutput += maskCopy.Mul(255).Convert<Rgb, byte>();

                    coloredMask.Dispose();

                    var segment = mask.Mul(imageWithOriginalDepthCopy);// .Dilate(2);

                    int x;
                    int y;
                    float depth;
                    FindHandLocation(ref segment, ref mask, out x, out y, out depth);
                    UpdateHand(ref x, ref y, ref depth);

                    segment.Dispose();
                }
            }

            foreach (var palm in _palms)
            {
                debugOutput.Draw(new CircleF(new PointF(palm.Center.X, palm.Center.Y), 5), Rgbs.Red, 3);
                debugOutput.Draw(new CircleF(new PointF(palm.EstimatedCenter.X, palm.EstimatedCenter.Y), 5), Rgbs.Green, 3);

                debugOutput.Draw(string.Format("Id {0} ({1})", palm.Id, palm.Depth), ref EmguFontBig, new Point(palm.EstimatedCenter.X, palm.EstimatedCenter.Y), Rgbs.White);
            }

            var debugOutputCopy = debugOutput.Copy();
            DispatcherHelper.RunAsync(() =>
            {
                FloodFillMask = debugOutputCopy.ToBitmapSource();
                debugOutputCopy.Dispose();
            });

            return targetImage.Convert<Gray, float>();
        }

        private bool BuildingBackgroundImage(Image<Gray, float> image)
        {
            if (_backgroundImage == null)
            {
                _backgroundImage = image.Copy();
                return true;
            }

            if (++_collectedBackgroundImages < 50)
            {
                _backgroundImage.RunningAvg(image, 0.8);
                return true;
            }

            return false;
        }

        #region Hand Processing

        /// <summary>
        /// Finds hand location in a segmented image. It assumes that the pixel with the highest depth value
        /// is the tip of the hand (highest value means closest to the static surface). It uses a sample of
        /// 30 highest depth values to compute the average x/y coordinate, which will be returned.
        /// </summary>
        /// <param name="handSegment"></param>
        /// <param name="handMask"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void FindHandLocation(ref Image<Gray, byte> handSegment, ref Image<Gray, byte> handMask, out int x, out int y, out float depth)
        {
            var samples = HandLocationSamples;

            var xs = new int[samples];
            var ys = new int[samples];

            var minVal = double.MaxValue;
            var maxVal = 0.0;
            var minLoc = new Point();
            var maxLoc = new Point();

            for (var j = 0; j < samples; j++)
            {
                CvInvoke.cvMinMaxLoc(handSegment, ref minVal, ref maxVal, ref minLoc, ref maxLoc, handMask);

                var maxX = maxLoc.X;
                var maxY = maxLoc.Y;

                xs[j] = maxX;
                ys[j] = maxY;

                handSegment.Data[maxY, maxX, 0] = 0;
            }

            x = (int)xs.Average();
            y = (int)ys.Average();
            depth = handSegment.Data[y, x, 0];
        }

        /// <summary>
        /// Updates a hand from previous frames or adds a new hand if not found in history.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void UpdateHand(ref int x, ref int y, ref float depth)
        {
            var now = DateTime.Now;
            depth = 255 - depth;

            Hand hand;
            var point = new Point(x, y);

            if (_palms.Count > 0)
            {
                hand = _palms.Aggregate((curmin, p) => p.EstimatedCenter.Length(point) < curmin.EstimatedCenter.Length(point) ? p : curmin);
            }
            else
            {
                hand = new Hand(GetNextId(), point)
                {
                    Depth = depth,
                    LastUpdate = now
                };
                _palms.Add(hand);
            }

            if (hand.EstimatedCenter.Length(point) < 50)
            {
                hand.Center = point;
                hand.Depth = depth;
                hand.LastUpdate = now;
            }
            else
            {
                hand = new Hand(GetNextId(), point)
                {
                    Depth = depth,
                    LastUpdate = now
                };
                _palms.Add(hand);
            }
        }

        #endregion

        private static long GetNextId()
        {
            return ++_id;
        }
    }
}
