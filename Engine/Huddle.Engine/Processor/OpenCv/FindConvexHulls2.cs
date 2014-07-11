﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.OpenCv.Struct;
using Huddle.Engine.Util;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Find Convex Hulls 2", "FindConvexHullsTemplate")]
    public class FindConvexHulls2 : RgbProcessor
    {
        #region private fields

        private static long _id = 0;

        private readonly List<RawObject> _objects = new List<RawObject>();

        #endregion

        #region properties

        #region MinAngle

        /// <summary>
        /// The <see cref="MinAngle" /> property's name.
        /// </summary>
        public const string MinAnglePropertyName = "MinAngle";

        private int _minAngle = 80;

        /// <summary>
        /// Sets and gets the MinAngle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MinAngle
        {
            get
            {
                return _minAngle;
            }

            set
            {
                if (_minAngle == value)
                {
                    return;
                }

                RaisePropertyChanging(MinAnglePropertyName);
                _minAngle = value;
                RaisePropertyChanged(MinAnglePropertyName);
            }
        }

        #endregion

        #region MaxAngle

        /// <summary>
        /// The <see cref="MaxAngle" /> property's name.
        /// </summary>
        public const string MaxAnglePropertyName = "MaxAngle";

        private int _maxAngle = 100;

        /// <summary>
        /// Sets and gets the MaxAngle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MaxAngle
        {
            get
            {
                return _maxAngle;
            }

            set
            {
                if (_maxAngle == value)
                {
                    return;
                }

                RaisePropertyChanging(MaxAnglePropertyName);
                _maxAngle = value;
                RaisePropertyChanged(MaxAnglePropertyName);
            }
        }

        #endregion

        #region MinContourArea

        /// <summary>
        /// The <see cref="MinContourArea" /> property's name.
        /// </summary>
        public const string MinContourAreaPropertyName = "MinContourArea";

        private int _minContourArea = 200;

        /// <summary>
        /// Sets and gets the MinContourArea property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MinContourArea
        {
            get
            {
                return _minContourArea;
            }

            set
            {
                if (_minContourArea == value)
                {
                    return;
                }

                RaisePropertyChanging(MinContourAreaPropertyName);
                _minContourArea = value;
                RaisePropertyChanged(MinContourAreaPropertyName);
            }
        }

        #endregion

        #region MaxContourArea

        /// <summary>
        /// The <see cref="MaxContourArea" /> property's name.
        /// </summary>
        public const string MaxContourAreaPropertyName = "MaxContourArea";

        private int _maxContourArea = 3000;

        /// <summary>
        /// Sets and gets the MaxContourArea property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MaxContourArea
        {
            get
            {
                return _maxContourArea;
            }

            set
            {
                if (_maxContourArea == value)
                {
                    return;
                }

                RaisePropertyChanging(MaxContourAreaPropertyName);
                _maxContourArea = value;
                RaisePropertyChanged(MaxContourAreaPropertyName);
            }
        }

        #endregion

        #region Timeout

        /// <summary>
        /// The <see cref="Timeout" /> property's name.
        /// </summary>
        public const string TimeoutPropertyName = "Timeout";

        private int _timeout = 500;

        /// <summary>
        /// Sets and gets the Timeout property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int Timeout
        {
            get
            {
                return _timeout;
            }

            set
            {
                if (_timeout == value)
                {
                    return;
                }

                RaisePropertyChanging(TimeoutPropertyName);
                _timeout = value;
                RaisePropertyChanged(TimeoutPropertyName);
            }
        }

        #endregion

        #region IsFillContours

        /// <summary>
        /// The <see cref="IsFillContours" /> property's name.
        /// </summary>
        public const string IsFillContoursPropertyName = "IsFillContours";

        private bool _isFillContours;

        /// <summary>
        /// Sets and gets the IsFillContours property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool IsFillContours
        {
            get
            {
                return _isFillContours;
            }

            set
            {
                if (_isFillContours == value)
                {
                    return;
                }

                RaisePropertyChanging(IsFillContoursPropertyName);
                _isFillContours = value;
                RaisePropertyChanged(IsFillContoursPropertyName);
            }
        }

        #endregion

        #region IsDrawContours

        /// <summary>
        /// The <see cref="IsDrawContours" /> property's name.
        /// </summary>
        public const string IsDrawContoursPropertyName = "IsDrawContours";

        private bool _isDrawContours = true;

        /// <summary>
        /// Sets and gets the IsDrawContours property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsDrawContours
        {
            get
            {
                return _isDrawContours;
            }

            set
            {
                if (_isDrawContours == value)
                {
                    return;
                }

                RaisePropertyChanging(IsDrawContoursPropertyName);
                _isDrawContours = value;
                RaisePropertyChanged(IsDrawContoursPropertyName);
            }
        }

        #endregion

        #region IsDrawCenter

        /// <summary>
        /// The <see cref="IsDrawCenter" /> property's name.
        /// </summary>
        public const string IsDrawCenterPropertyName = "IsDrawCenter";

        private bool _isDrawCenter = true;

        /// <summary>
        /// Sets and gets the IsDrawCenter property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsDrawCenter
        {
            get
            {
                return _isDrawCenter;
            }

            set
            {
                if (_isDrawCenter == value)
                {
                    return;
                }

                RaisePropertyChanging(IsDrawCenterPropertyName);
                _isDrawCenter = value;
                RaisePropertyChanged(IsDrawCenterPropertyName);
            }
        }

        #endregion

        #region IsRetrieveExternal

        /// <summary>
        /// The <see cref="IsRetrieveExternal" /> property's name.
        /// </summary>
        public const string IsRetrieveExternalPropertyName = "IsRetrieveExternal";

        private bool _isRetrieveExternal = false;

        /// <summary>
        /// Sets and gets the IsRetrieveExternal property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsRetrieveExternal
        {
            get
            {
                return _isRetrieveExternal;
            }

            set
            {
                if (_isRetrieveExternal == value)
                {
                    return;
                }

                RaisePropertyChanging(IsRetrieveExternalPropertyName);
                _isRetrieveExternal = value;
                RaisePropertyChanged(IsRetrieveExternalPropertyName);
            }
        }

        #endregion

        #region MaxDistanceRestoreId

        /// <summary>
        /// The <see cref="MaxDistanceRestoreId" /> property's name.
        /// </summary>
        public const string MaxDistanceRestoreIdPropertyName = "MaxDistanceRestoreId";

        private double _maxDistanceRestoreId = 10.0;

        /// <summary>
        /// Sets and gets the MaxDistanceRestoreId property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double MaxDistanceRestoreId
        {
            get
            {
                return _maxDistanceRestoreId;
            }

            set
            {
                if (_maxDistanceRestoreId == value)
                {
                    return;
                }

                RaisePropertyChanging(MaxDistanceRestoreIdPropertyName);
                _maxDistanceRestoreId = value;
                RaisePropertyChanged(MaxDistanceRestoreIdPropertyName);
            }
        }

        #endregion

        #region IsSubtractConfidenceImage

        /// <summary>
        /// The <see cref="IsSubtractConfidenceImage" /> property's name.
        /// </summary>
        public const string IsSubtractConfidenceImagePropertyName = "IsSubtractConfidenceImage";

        private bool _isSubtractConfidenceImage = false;

        /// <summary>
        /// Sets and gets the IsSubtractConfidenceImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsSubtractConfidenceImage
        {
            get
            {
                return _isSubtractConfidenceImage;
            }

            set
            {
                if (_isSubtractConfidenceImage == value)
                {
                    return;
                }

                RaisePropertyChanging(IsSubtractConfidenceImagePropertyName);
                _isSubtractConfidenceImage = value;
                RaisePropertyChanged(IsSubtractConfidenceImagePropertyName);
            }
        }

        #endregion

        #endregion

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var now = DateTime.Now;

            _objects.RemoveAll(o => (now - o.LastUpdate).TotalMilliseconds > Timeout);

            var outputImage = new Image<Rgb, byte>(image.Size.Width, image.Size.Height, Rgbs.Black);

            //Convert the image to grayscale and filter out the noise
            var grayImage = image.Convert<Gray, Byte>();

            using (var storage = new MemStorage())
            {
                for (var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, IsRetrieveExternal ? RETR_TYPE.CV_RETR_EXTERNAL : RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
                {
                    var currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);

                    //Console.WriteLine("AREA {0}", currentContour.Area);

                    if (currentContour.Total == 4 && currentContour.Area > MinContourArea && currentContour.Area < MaxContourArea) //only consider contours with area greater than 250
                    {
                        //outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.White, 2);
                        outputImage.FillConvexPoly(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE).ToArray(), Rgbs.White);
                        outputImage.Draw(string.Format("{0}", currentContour.Area), ref EmguFont, currentContour.BoundingRectangle.Location, Rgbs.Red);
                    }
                }
            }
            
            foreach (var rawObject in _objects)
            {
                //var orig = currentContour.BoundingRectangle.Location;
                //var pred = _kalmanFilter.GetPredictedPoint(orig);

                //Log("Original={0}, Predicted={1}", orig, pred);

                //outputImage.Draw(new CircleF(new PointF(orig.X, orig.Y), 5), Rgbs.Red, 5);

                if (IsFillContours)
                    outputImage.FillConvexPoly(rawObject.Points, Rgbs.Yellow);

                if (IsDrawContours)
                    outputImage.Draw(rawObject.Shape, Rgbs.Red, 2);

                if (IsDrawCenter)
                {
                    var circle = new CircleF(rawObject.Center, 3);
                    outputImage.Draw(circle, Rgbs.Green, 3);
                }

                if (IsDrawCenter)
                {
                    var circle = new CircleF(rawObject.EstimatedCenter, 3);
                    outputImage.Draw(circle, Rgbs.Blue, 3);
                }

                //outputImage.Draw(string.Format("Angle {0}", rawObject.Shape.angle), ref EmguFont, new Point((int)rawObject.Shape.center.X, (int)rawObject.Shape.center.Y), Rgbs.White);

                outputImage.Draw(string.Format("Id {0}", rawObject.Id), ref EmguFontBig, new Point((int)rawObject.Shape.center.X, (int)rawObject.Shape.center.Y), Rgbs.White);

                Stage(new BlobData(this, string.Format("FindContours Id{0}", rawObject.Id))
                {
                    Id = rawObject.Id,
                    X = (rawObject.EstimatedCenter.X) / (double) image.Width,
                    Y = (rawObject.EstimatedCenter.Y) / (double) image.Height,
                    Angle = rawObject.Shape.angle,
                    Area = new Rect
                    {
                        X = rawObject.Bounds.X / (double)image.Width,
                        Y = rawObject.Bounds.X / (double)image.Height,
                        Width = rawObject.Bounds.X / (double)image.Width,
                        Height = rawObject.Bounds.X / (double)image.Height,
                    }
                });
            }

            //Push();

            grayImage.Dispose();

            return outputImage;
        }

        private static long GetNextId()
        {
            return ++_id;
        }
    }
}