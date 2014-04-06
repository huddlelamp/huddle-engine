using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Extensions;
using Huddle.Engine.Processor.OpenCv.Struct;
using Huddle.Engine.Util;
using PolygonIntersection;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Find Contours 2", "FindContours")]
    public class FindContours2 : RgbProcessor
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

        #region MinDetectRightAngles

        /// <summary>
        /// The <see cref="MinDetectRightAngles" /> property's name.
        /// </summary>
        public const string MinDetectRightAnglesPropertyName = "MinDetectRightAngles";

        private int _minDetectRightAngles = 3;

        /// <summary>
        /// Sets and gets the MinDetectRightAngles property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MinDetectRightAngles
        {
            get
            {
                return _minDetectRightAngles;
            }

            set
            {
                if (_minDetectRightAngles == value)
                {
                    return;
                }

                RaisePropertyChanging(MinDetectRightAnglesPropertyName);
                _minDetectRightAngles = value;
                RaisePropertyChanged(MinDetectRightAnglesPropertyName);
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

        #region IntegrationDistance

        /// <summary>
        /// The <see cref="IntegrationDistance" /> property's name.
        /// </summary>
        public const string IntegrationDistancePropertyName = "IntegrationDistance";

        private double _integrationDistance = 0.1;

        /// <summary>
        /// Sets and gets the IntegrationDistance property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double IntegrationDistance
        {
            get
            {
                return _integrationDistance;
            }

            set
            {
                if (_integrationDistance == value)
                {
                    return;
                }

                RaisePropertyChanging(IntegrationDistancePropertyName);
                _integrationDistance = value;
                RaisePropertyChanged(IntegrationDistancePropertyName);
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
            var width = image.Width;
            var height = image.Height;

            var now = DateTime.Now;

            _objects.RemoveAll(o => (now - o.LastUpdate).TotalMilliseconds > Timeout);

            var outputImage = new Image<Rgb, byte>(width, height, Rgbs.Black);

            //Convert the image to grayscale and filter out the noise
            var grayImage = image.Convert<Gray, Byte>();

            using (var storage = new MemStorage())
            {
                //var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, IsRetrieveExternal ? RETR_TYPE.CV_RETR_EXTERNAL : RETR_TYPE.CV_RETR_LIST, storage);
                //Parallel.ForEach(IterateContours(contours, storage), contour =>
                //{

                //});
                for (var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, IsRetrieveExternal ? RETR_TYPE.CV_RETR_EXTERNAL : RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
                {
                    var currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);

                    if (currentContour.Area > MinContourArea && currentContour.Area < MaxContourArea) //only consider contours with area greater than 250
                    {
                        outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.BlueTorquoise, 2);

                        if (currentContour.Total >= 4) //The contour has 4 vertices.
                        {
                            #region determine if all the angles in the contour are within [80, 100] degree
                            bool isRectangle = true;
                            Point[] pts = currentContour.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                            var rightAngle = 0;
                            for (int i = 0; i < edges.Length; i++)
                            {
                                var angle = Math.Abs(edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));


                                if (angle < MinAngle || angle > MaxAngle)
                                {
                                    //isRectangle = false;
                                    break;
                                }
                                else
                                {
                                    rightAngle++;
                                }
                            }

                            if (rightAngle < MinDetectRightAngles)
                                isRectangle = false;

                            #endregion

                            //Log("Is Rectangle: {0}", isRectangle);

                            if (isRectangle)
                            {
                                var cCenter = currentContour.GetMinAreaRect().center;
                                var point = new Point((int)cCenter.X, (int)cCenter.Y);

                                RawObject obj = null;

                                if (_objects.Count > 0)
                                    obj = _objects.Aggregate((curmin, p) => p.EstimatedCenter.Length(point) < curmin.EstimatedCenter.Length(point) ? p : curmin);

                                if (obj != null && obj.EstimatedCenter.Length(point) < MaxDistanceRestoreId)
                                {
                                    obj.LastUpdate = DateTime.Now;
                                    obj.Center = new Point((int)cCenter.X, (int)cCenter.Y);
                                    obj.Bounds = currentContour.BoundingRectangle;
                                    obj.Shape = currentContour.GetMinAreaRect();
                                    obj.Polygon = new Polygon(contours.ToArray(), width, height);
                                    obj.Points = pts;
                                }
                                else
                                {
                                    var minAreaRect = currentContour.GetMinAreaRect();

                                    _objects.Add(new RawObject
                                    {
                                        Id = GetNextId(),
                                        LastUpdate = DateTime.Now,
                                        Center = new Point((int)minAreaRect.center.X, (int)minAreaRect.center.Y),
                                        Bounds = currentContour.BoundingRectangle,
                                        Shape = minAreaRect,
                                        Polygon = new Polygon(contours.ToArray(), width, height),
                                        Points = pts
                                    });
                                }
                            }
                        }
                    }
                }
            }

            foreach (var rawObject in _objects)
            {
                if (IsRenderContent)
                {
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

                    outputImage.Draw(string.Format("Id {0}", rawObject.Id), ref EmguFontBig, new Point((int)rawObject.Shape.center.X, (int)rawObject.Shape.center.Y), Rgbs.White);
                }

                var bounds = rawObject.Bounds;
                var estimatedCenter = rawObject.EstimatedCenter;
                Stage(new BlobData(this, "DeviceBlob")
                {
                    Id = rawObject.Id,
                    X = estimatedCenter.X / (double)width,
                    Y = estimatedCenter.Y / (double)height,
                    Angle = rawObject.Shape.angle,
                    Shape = rawObject.Shape,
                    Polygon = rawObject.Polygon,
                    Area = new Rect
                    {
                        X = bounds.X / (double)width,
                        Y = bounds.Y / (double)height,
                        Width = bounds.Width / (double)width,
                        Height = bounds.Height / (double)height,
                    }
                });
            }

            Push();

            grayImage.Dispose();

            return outputImage;
        }

        private static long GetNextId()
        {
            return ++_id;
        }

        private IEnumerable<Contour<Point>> IterateContours(Contour<Point> contours, MemStorage storage)
        {
            for (; contours != null; contours = contours.HNext)
            {
                yield return contours.ApproxPoly(contours.Perimeter * 0.05, storage);
            }
        }
    }
}
