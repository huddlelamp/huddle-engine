using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.Complex.PolygonIntersection;
using Huddle.Engine.Processor.OpenCv.Struct;
using Huddle.Engine.Util;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Find Contours", "FindContours")]
    public class FindContours : RgbProcessor
    {
        #region private fields

        private readonly List<RawObject> _objects = new List<RawObject>();

        private Image<Gray, float> _depthImage; 

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

        private double _minContourArea = 5.0;

        /// <summary>
        /// Sets and gets the MinContourArea property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double MinContourArea
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

        private double _maxContourArea = 10.0;

        /// <summary>
        /// Sets and gets the MaxContourArea property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double MaxContourArea
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

        #region MaxRestoreDistance

        /// <summary>
        /// The <see cref="MaxRestoreDistance" /> property's name.
        /// </summary>
        public const string MaxRestoreDistancePropertyName = "MaxRestoreDistance";

        private double _maxRestoreDistance = 5.0;

        /// <summary>
        /// Sets and gets the MaxRestoreDistance property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double MaxRestoreDistance
        {
            get
            {
                return _maxRestoreDistance;
            }

            set
            {
                if (_maxRestoreDistance == value)
                {
                    return;
                }

                RaisePropertyChanging(MaxRestoreDistancePropertyName);
                _maxRestoreDistance = value;
                RaisePropertyChanged(MaxRestoreDistancePropertyName);
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

        #region DebugImageSource

        /// <summary>
        /// The <see cref="DebugImageSource" /> property's name.
        /// </summary>
        public const string DebugImageSourcePropertyName = "DebugImageSource";

        private BitmapSource _debugImageSource;

        /// <summary>
        /// Sets and gets the DebugImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource DebugImageSource
        {
            get
            {
                return _debugImageSource;
            }

            set
            {
                if (_debugImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DebugImageSourcePropertyName);
                _debugImageSource = value;
                RaisePropertyChanged(DebugImageSourcePropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public FindContours()
            : base(false)
        {

        }

        #endregion

        public override IData Process(IData data)
        {
            var depthImageData = data as GrayFloatImage;
            if (depthImageData != null && Equals(depthImageData.Key, "depth"))
            {
                if (_depthImage != null)
                    _depthImage.Dispose();

                _depthImage = depthImageData.Image.Copy();
            }

            return base.Process(data);
        }

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            var now = DateTime.Now;

            _objects.RemoveAll(o => (now - o.LastUpdate).TotalMilliseconds > Timeout);

            #region Debug Stuff

            // #### DEBUG
            //var debugImage = image.Copy();
            var debugImage = new Image<Rgb, byte>(imageWidth, imageHeight);
            var debugImage2 = new Image<Gray, float>(imageWidth, imageHeight);
            if (_depthImage != null)
            {
                var depthMapBinary = _depthImage.ThresholdBinaryInv(new Gray(255), new Gray(255));
                var depthMap = depthMapBinary;//.Mul(255);

                //mask = mask.ThresholdBinary(new Gray(10), new Gray(250));

                //debugImage = depthMap.Convert<Rgb, byte>();//mask.Convert<Rgb, byte>();

                var mask = new Image<Gray, byte>(imageWidth, imageHeight);
                foreach (var obj in _objects)
                {
                    mask.Draw(obj.Shape, new Gray(1), -1);
                    //mask = mask.Mul(255);
                }

                CvInvoke.cvCopy(depthMap.Ptr, debugImage2.Ptr, mask);
                debugImage2 = debugImage2.Erode(2).Dilate(2);

                var debugImage3 = debugImage2.Convert<Rgb, byte>();

                //debugImage = image.Or(debugImage3);
                //debugImage = debugImage.Dilate(2).Erode(2);

                //image = image.Or(debugImage3);
                //image = image.Dilate(2).Erode(2);
            }

            //foreach (var rawObject in _objects)
            //{
            //    debugImage.Draw(rawObject.Shape, Rgbs.Mimosa, -1);
            //}

            //Task.Factory.StartNew(() =>
            //{
            //    var bitmapSource = debugImage.ToBitmapSource(true);
            //    debugImage.Dispose();
            //    return bitmapSource;
            //}).ContinueWith(t => DebugImageSource = t.Result);
            // #### DEBUG

            #endregion

            // Needed to be wrapped in closure -> required by Parallel.ForEach below.
            Image<Rgb, byte>[] outputImage = { new Image<Rgb, byte>(imageWidth, imageHeight, Rgbs.Black) };

            // Try to identify objects event if they are connected tightly (without a gap).
            Parallel.ForEach(_objects, obj => FindObjectByBlankingKnownObjects(image, ref outputImage[0], now, obj));

            // Find yet unidentified objects
            FindObjectByBlankingKnownObjects(image, ref outputImage[0], now);

            // Tries to find not yet identified and maybe occluded objects.
            FindOccludedObjects(image, now, ref outputImage[0]);

            foreach (var rawObject in _objects)
            {
                if (IsRenderContent)
                {
                    if (IsFillContours)
                        outputImage[0].FillConvexPoly(rawObject.Points, Rgbs.Yellow);

                    if (IsDrawContours)
                        outputImage[0].Draw(rawObject.Shape, Rgbs.Red, 2);

                    if (IsDrawCenter)
                    {
                        var circle = new CircleF(rawObject.Center, 2);
                        outputImage[0].Draw(circle, Rgbs.Green, 3);
                    }

                    if (IsDrawCenter)
                    {
                        var circle = new CircleF(rawObject.EstimatedCenter, 2);
                        outputImage[0].Draw(circle, Rgbs.Blue, 3);
                    }

                    outputImage[0].Draw(string.Format("Id {0}", rawObject.Id), ref EmguFont, new Point((int)rawObject.Shape.center.X, (int)rawObject.Shape.center.Y), Rgbs.White);
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

            return outputImage[0];
        }

        /// <summary>
        /// Find an object by blanking out known objects except for the parameter object in the
        /// source image. If obj == null it will blank out all objects.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="outputImage"></param>
        /// <param name="obj"></param>
        private void FindObjectByBlankingKnownObjects(Image<Rgb, byte> image, ref Image<Rgb, byte> outputImage, DateTime updateTime, RawObject obj = null)
        {
            var objectsToBlank = obj != null ? _objects.Where(o => o != obj).ToArray() : _objects.ToArray();

            // Blank previous objects from previous frame
            var blankedImage = image.Copy();
            foreach (var otherObject in objectsToBlank)
            {
                blankedImage.Draw(otherObject.Shape, Rgbs.Black, -1);
            }

            var blankedImageGray = blankedImage.Convert<Gray, Byte>();

            FindRectangles(blankedImageGray, ref outputImage, updateTime);

            blankedImageGray.Dispose();
        }

        /// <summary>
        /// Tries to find objects that are occluded.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="updateTime"></param>
        /// <param name="outputImage"></param>
        private void FindOccludedObjects(Image<Rgb, byte> image, DateTime updateTime, ref Image<Rgb, byte> outputImage)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            var mask = new Image<Gray, byte>(imageWidth, imageHeight);

            var occludedObjects = _objects.Where(o => !Equals(o.LastUpdate, updateTime)).ToArray();
            Console.WriteLine("Occluded Objects: {0}", occludedObjects.Length);
            foreach (var obj in occludedObjects)
                mask.Draw(obj.Shape, new Gray(1), -1);

            if (_depthImage == null) return;

            var occludedPartsImage = new Image<Gray, float>(imageWidth, imageHeight);

            var depthMapBinary = _depthImage.ThresholdBinaryInv(new Gray(255), new Gray(255));
            var depthMap = depthMapBinary;

            CvInvoke.cvCopy(depthMap.Ptr, occludedPartsImage.Ptr, mask);
            occludedPartsImage = occludedPartsImage.Erode(2).Dilate(2);

            var debugImage3 = occludedPartsImage.Convert<Rgb, byte>();

            var fixedImage = image.Or(debugImage3);
            fixedImage = fixedImage.Dilate(2).Erode(2);

            var debugImage = fixedImage.Copy();
            Task.Factory.StartNew(() =>
            {
                var bitmapSource = debugImage.ToBitmapSource(true);
                debugImage.Dispose();
                return bitmapSource;
            }).ContinueWith(t => DebugImageSource = t.Result);

            var outputImageEnclosed = outputImage;
            Parallel.ForEach(_objects, obj => FindObjectByBlankingKnownObjects(fixedImage, ref outputImageEnclosed, updateTime, obj));
        }

        /// <summary>
        /// Find rectangles in image and add possible rectangle candidates as temporary but known objects or updates
        /// existing objects from previous frames.
        /// </summary>
        /// <param name="grayImage"></param>
        /// <param name="outputImage"></param>
        /// <param name="updateTime"></param>
        private void FindRectangles(Image<Gray, byte> grayImage, ref Image<Rgb, byte> outputImage, DateTime updateTime)
        {
            var imageWidth = grayImage.Width;
            var imageHeight = grayImage.Height;
            var pixels = imageWidth * imageHeight;

            var diagonal = Math.Sqrt(Math.Pow(imageWidth, 2) + Math.Pow(imageHeight, 2));

            var maxRestoreDistance = (MaxRestoreDistance / 100.0) * diagonal;

            using (var storage = new MemStorage())
            {
                for (var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, IsRetrieveExternal ? RETR_TYPE.CV_RETR_EXTERNAL : RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
                {
                    var currentContour = contours.ApproxPoly(contours.Perimeter * 0.015, storage);

                    if (currentContour.Area > ((MinContourArea / 100.0) * pixels) && currentContour.Area < ((MaxContourArea / 100.0) * pixels)) //only consider contours with area greater than
                    {
                        if (IsRenderContent && IsDrawAllContours)
                            outputImage.Draw(currentContour, Rgbs.BlueTorquoise, 2);
                        //outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.BlueTorquoise, 2);

                        // Continue with next contour if current contour is not a rectangle.
                        List<Point> points;
                        if (!IsRectangle(currentContour, MinAngle, MaxAngle, MinDetectRightAngles, out points)) continue;

                        var rectangle = currentContour.BoundingRectangle;
                        var minAreaRect = currentContour.GetMinAreaRect(storage);
                        var polygon = new Polygon(points.ToArray(), imageWidth, imageHeight);
                        var contourPoints = currentContour.ToArray();

                        if (!UpdateObject(maxRestoreDistance, rectangle, minAreaRect, polygon, contourPoints, updateTime))
                        {
                            AddObject(rectangle, minAreaRect, polygon, contourPoints, updateTime);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine if all the angles in the contour are within min/max angle.
        /// </summary>
        /// <param name="contour"></param>
        /// <param name="minAngle"></param>
        /// <param name="maxAngle"></param>
        /// <param name="minDetectAngles"></param>
        /// <returns></returns>
        private bool IsRectangle(Seq<Point> contour, int minAngle, int maxAngle, int minDetectAngles, out List<Point> points)
        {
            points = new List<Point>();

            if (contour.Total >= 3) //The contour has 4 vertices.
            {
                var pts = contour.ToArray();
                var edges = PointCollection.PolyLine(pts, true);

                var rightAngle = 0;
                for (var i = 0; i < edges.Length; i++)
                {
                    var edge1 = edges[i];
                    var edge2 = edges[(i + 1) % edges.Length];

                    points.Add(edge1.P1);

                    var angle = Math.Abs(edge1.GetExteriorAngleDegree(edge2));

                    // stop if an angle is not in min/max angle range, no need to continue
                    if (angle < minAngle || angle > maxAngle)
                    {
                        points.Clear();
                        return false;
                    }

                    rightAngle++;
                }

                return rightAngle >= minDetectAngles;
            }
            return false;
        }

        /// <summary>
        /// Adds a new temporary object that will be used to identify itself in the preceeding frames.
        /// </summary>
        /// <param name="boundingRectangle"></param>
        /// <param name="minAreaRect"></param>
        /// <param name="polygon"></param>
        /// <param name="points"></param>
        /// <param name="updateTime"></param>
        private void AddObject(Rectangle boundingRectangle, MCvBox2D minAreaRect, Polygon polygon, Point[] points, DateTime updateTime)
        {
            _objects.Add(new RawObject
            {
                Id = NextId(),
                LastUpdate = updateTime,
                Center = new Point((int)minAreaRect.center.X, (int)minAreaRect.center.Y),
                Bounds = boundingRectangle,
                Shape = minAreaRect,
                Polygon = polygon,
                Points = points,
            });
        }

        /// <summary>
        /// Updates an object if it finds an object from last frame at a max restore distance.
        /// </summary>
        /// <param name="maxRestoreDistance"></param>
        /// <param name="boundingRectangle"></param>
        /// <param name="minAreaRect"></param>
        /// <param name="polygon"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        private bool UpdateObject(double maxRestoreDistance, Rectangle boundingRectangle, MCvBox2D minAreaRect, Polygon polygon, Point[] points, DateTime updateTime)
        {
            foreach (var o in _objects)
            {
                var oCenter = o.Shape.center;
                var cCenter = minAreaRect.center;

                var distance = Math.Sqrt(Math.Pow(oCenter.X - cCenter.X, 2) + Math.Pow(oCenter.Y - cCenter.Y, 2));

                if (!(distance < maxRestoreDistance)) continue;

                o.LastUpdate = updateTime;
                o.Center = new Point((int)cCenter.X, (int)cCenter.Y);
                o.Bounds = boundingRectangle;
                o.Shape = minAreaRect;
                o.Polygon = polygon;
                o.Points = points;

                return true;
            }
            return false;
        }
    }
}
