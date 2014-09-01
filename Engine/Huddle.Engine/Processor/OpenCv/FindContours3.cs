using System;
using System.Collections.Generic;
using System.Windows;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.Complex.PolygonIntersection;
using Huddle.Engine.Processor.OpenCv.Struct;
using Huddle.Engine.Util;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Find Contours3", "FindContours")]
    public class FindContours3 : RgbProcessor
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

        #region IsDrawAllContours

        /// <summary>
        /// The <see cref="IsDrawAllContours" /> property's name.
        /// </summary>
        public const string IsDrawAllContoursPropertyName = "IsDrawAllContours";

        private bool _isDrawAllContours = false;

        /// <summary>
        /// Sets and gets the IsDrawAllContours property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsDrawAllContours
        {
            get
            {
                return _isDrawAllContours;
            }

            set
            {
                if (_isDrawAllContours == value)
                {
                    return;
                }

                RaisePropertyChanging(IsDrawAllContoursPropertyName);
                _isDrawAllContours = value;
                RaisePropertyChanged(IsDrawAllContoursPropertyName);
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

        #region ctor

        public FindContours3()
            : base(false)
        {
            
        }

        #endregion

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            var now = DateTime.Now;

            _objects.RemoveAll(o => (now - o.LastUpdate).TotalMilliseconds > Timeout);

            var outputImage = new Image<Rgb, byte>(imageWidth, imageHeight, Rgbs.Black);

            //Convert the image to grayscale and filter out the noise
            var grayImage = image.Convert<Gray, Byte>();

            using (var storage = new MemStorage())
            {
                for (var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, IsRetrieveExternal ? RETR_TYPE.CV_RETR_EXTERNAL : RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
                {
                    var currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);

                    if (currentContour.Area > MinContourArea) //only consider contours with area greater than
                    {
                        if (IsRenderContent && IsDrawAllContours && currentContour.Area < MaxContourArea)
                            outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.BlueTorquoise, 2);

                        if (currentContour.Total >= 4) //The contour has 4 vertices.
                        {
                            #region determine if all the angles in the contour are within [80, 100] degree

                            var points = new List<Point>();

                            bool isRectangle = true;
                            Point[] pts = currentContour.ToArray();
                            LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                            LineSegment2D longestEdge = edges[0];
                            var longestEdgeLength = 0.0;
                            var rightAngle = 0;
                            for (int i = 0; i < edges.Length; i++)
                            {
                                var edge = edges[(i + 1) % edges.Length];

                                // Assumption is that the longest edge defines the width of the tracked device in the blob
                                if (edge.Length > longestEdgeLength)
                                {
                                    longestEdgeLength = edge.Length;
                                    longestEdge = edge;
                                }

                                var angle = Math.Abs(edge.GetExteriorAngleDegree(edges[i]));

                                points.Add(edge.P1);

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
                                bool updated = false;

                                var area = currentContour.GetMinAreaRect(storage);

                                foreach (var o in _objects)
                                {
                                    var oCenter = o.Shape.center;
                                    var cCenter = currentContour.GetMinAreaRect().center;

                                    //var distance = currentContour.Distance(shapeCenter);

                                    //var distance2 = oCenter.Length(cCenter);
                                    var distance2 = Math.Sqrt(Math.Pow(oCenter.X - cCenter.X, 2) + Math.Pow(oCenter.Y - cCenter.Y, 2));

                                    //Log("Distance {0}", distance2);

                                    if (distance2 < MaxDistanceRestoreId)
                                    //if (currentContour.BoundingRectangle.IntersectsWith(o.Bounds))
                                    {
                                        o.LastUpdate = DateTime.Now;
                                        o.Center = new Point((int)cCenter.X, (int)cCenter.Y);
                                        o.Bounds = currentContour.BoundingRectangle;
                                        o.Shape = area;
                                        o.Polygon = new Polygon(points.ToArray(), imageWidth, imageHeight);
                                        o.Points = pts;
                                        o.DeviceToCameraRatio = ((longestEdgeLength / imageWidth)*0.3 + o.DeviceToCameraRatio)*0.7;
                                        o.LongestEdge = longestEdge;

                                        updated = true;
                                    }
                                }

                                //_objects.RemoveAll(o => currentContour.BoundingRectangle.IntersectsWith(o.Bounds));

                                if (!updated)
                                {
                                    var minAreaRect = currentContour.GetMinAreaRect();

                                    _objects.Add(new RawObject
                                    {
                                        Id = NextId(),
                                        LastUpdate = DateTime.Now,
                                        Center = new Point((int)minAreaRect.center.X, (int)minAreaRect.center.Y),
                                        Bounds = currentContour.BoundingRectangle,
                                        Shape = minAreaRect,
                                        Polygon = new Polygon(points.ToArray(), imageWidth, imageHeight),
                                        Points = pts,
                                        DeviceToCameraRatio = longestEdgeLength / imageWidth,
                                        LongestEdge = longestEdge
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
                        outputImage.Draw(rawObject.Shape, Rgbs.Yellow, 2);

                    if (IsDrawCenter)
                    {
                        var circle = new CircleF(rawObject.Center, 2);
                        outputImage.Draw(circle, Rgbs.Green, 3);
                    }

                    if (IsDrawCenter)
                    {
                        var circle = new CircleF(rawObject.EstimatedCenter, 2);
                        outputImage.Draw(circle, Rgbs.Blue, 3);
                    }

                    //if (IsRenderContent)
                    //{
                    //    outputImage.Draw(rawObject.LongestEdge, Rgbs.Green, 5);
                    //    outputImage.Draw(string.Format("Length {0:F1}", rawObject.LongestEdge.Length), ref EmguFont, rawObject.LongestEdge.P1, Rgbs.Green);
                    //}

                    outputImage.Draw(string.Format("Id {0}", rawObject.Id), ref EmguFontBig, new Point((int)rawObject.Shape.center.X, (int)rawObject.Shape.center.Y), Rgbs.White);
                }

                var bounds = rawObject.Bounds;
                var estimatedCenter = rawObject.EstimatedCenter;
                Stage(new BlobData(this, "DeviceBlob")
                {
                    Id = rawObject.Id,
                    X = estimatedCenter.X / (double)imageWidth,
                    Y = estimatedCenter.Y / (double)imageHeight,
                    Angle = rawObject.Shape.angle,
                    //Angle = rawObject.SlidingAngle,
                    Shape = rawObject.Shape,
                    Polygon = rawObject.Polygon,
                    Area = new Rect
                    {
                        X = bounds.X / (double)imageWidth,
                        Y = bounds.Y / (double)imageHeight,
                        Width = bounds.Width / (double)imageWidth,
                        Height = bounds.Height / (double)imageHeight,
                    }
                });
            }

            Push();

            grayImage.Dispose();

            return outputImage;
        }
    }
}
