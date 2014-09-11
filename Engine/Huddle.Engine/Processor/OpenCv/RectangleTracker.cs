using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Extensions;
using Huddle.Engine.Processor.Complex;
using Huddle.Engine.Processor.Complex.PolygonIntersection;
using Huddle.Engine.Processor.OpenCv.Struct;
using Huddle.Engine.Util;
using Point = System.Drawing.Point;
using Polygon = Huddle.Engine.Processor.Complex.PolygonIntersection.Polygon;
using Rectangle = System.Drawing.Rectangle;
using Vector = Huddle.Engine.Processor.Complex.PolygonIntersection.Vector;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Rectangle Tracker", "RectangleTracker")]
    public class RectangleTracker : RgbProcessor
    {
        #region private fields

        private readonly List<RectangularObject> _objects = new List<RectangularObject>();

        private Image<Gray, float> _depthImage;

        #endregion

        #region properties

        #region BlobType

        /// <summary>
        /// The <see cref="BlobType" /> property's name.
        /// </summary>
        public const string BlobTypePropertyName = "BlobType";

        private string _blobType = string.Empty;

        /// <summary>
        /// Sets and gets the BlobType property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string BlobType
        {
            get
            {
                return _blobType;
            }

            set
            {
                if (_blobType == value)
                {
                    return;
                }

                RaisePropertyChanging(BlobTypePropertyName);
                _blobType = value;
                RaisePropertyChanged(BlobTypePropertyName);
            }
        }

        #endregion

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

        #region IsUpdateOccludedRectangles

        /// <summary>
        /// The <see cref="IsUpdateOccludedRectangles" /> property's name.
        /// </summary>
        public const string IsUpdateOccludedRectanglesPropertyName = "IsUpdateOccludedRectangles";

        private bool _isUpdateOccludedRectangles = true;

        /// <summary>
        /// Sets and gets the IsUpdateOccludedRectangles property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsUpdateOccludedRectangles
        {
            get
            {
                return _isUpdateOccludedRectangles;
            }

            set
            {
                if (_isUpdateOccludedRectangles == value)
                {
                    return;
                }

                RaisePropertyChanging(IsUpdateOccludedRectanglesPropertyName);
                _isUpdateOccludedRectangles = value;
                RaisePropertyChanged(IsUpdateOccludedRectanglesPropertyName);
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

        #region FixMaskErode

        /// <summary>
        /// The <see cref="FixMaskErode" /> property's name.
        /// </summary>
        public const string FixMaskErodePropertyName = "FixMaskErode";

        private int _fixMaskErode = 0;

        /// <summary>
        /// Sets and gets the FixMaskErode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int FixMaskErode
        {
            get
            {
                return _fixMaskErode;
            }

            set
            {
                if (_fixMaskErode == value)
                {
                    return;
                }

                RaisePropertyChanging(FixMaskErodePropertyName);
                _fixMaskErode = value;
                RaisePropertyChanged(FixMaskErodePropertyName);
            }
        }

        #endregion

        #region FixMaskDilate

        /// <summary>
        /// The <see cref="FixMaskDilate" /> property's name.
        /// </summary>
        public const string FixMaskDilatePropertyName = "FixMaskDilate";

        private int _fixMaskDilate = 0;

        /// <summary>
        /// Sets and gets the FixMaskDilate property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int FixMaskDilate
        {
            get
            {
                return _fixMaskDilate;
            }

            set
            {
                if (_fixMaskDilate == value)
                {
                    return;
                }

                RaisePropertyChanging(FixMaskDilatePropertyName);
                _fixMaskDilate = value;
                RaisePropertyChanged(FixMaskDilatePropertyName);
            }
        }

        #endregion

        #region IsFirstErodeThenDilateFixMask

        /// <summary>
        /// The <see cref="IsFirstErodeThenDilateFixMask" /> property's name.
        /// </summary>
        public const string IsFirstErodeThenDilateFixMaskPropertyName = "IsFirstErodeThenDilateFixMask";

        private bool _isFirstErodeThenDilateFixMask = true;

        /// <summary>
        /// Sets and gets the IsFirstErodeThenDilateFixMask property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsFirstErodeThenDilateFixMask
        {
            get
            {
                return _isFirstErodeThenDilateFixMask;
            }

            set
            {
                if (_isFirstErodeThenDilateFixMask == value)
                {
                    return;
                }

                RaisePropertyChanging(IsFirstErodeThenDilateFixMaskPropertyName);
                _isFirstErodeThenDilateFixMask = value;
                RaisePropertyChanged(IsFirstErodeThenDilateFixMaskPropertyName);
            }
        }

        #endregion

        #region DepthPatchesErode

        /// <summary>
        /// The <see cref="DepthPatchesErode" /> property's name.
        /// </summary>
        public const string DepthPatchesErodePropertyName = "DepthPatchesErode";

        private int _depthPatchesErode = 4;

        /// <summary>
        /// Sets and gets the DepthPatchesErode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int DepthPatchesErode
        {
            get
            {
                return _depthPatchesErode;
            }

            set
            {
                if (_depthPatchesErode == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthPatchesErodePropertyName);
                _depthPatchesErode = value;
                RaisePropertyChanged(DepthPatchesErodePropertyName);
            }
        }

        #endregion

        #region DepthPatchesDilate

        /// <summary>
        /// The <see cref="DepthPatchesDilate" /> property's name.
        /// </summary>
        public const string DepthPatchesDilatePropertyName = "DepthPatchesDilate";

        private int _depthPatchesDilate = 4;

        /// <summary>
        /// Sets and gets the DepthPatchesDilate property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int DepthPatchesDilate
        {
            get
            {
                return _depthPatchesDilate;
            }

            set
            {
                if (_depthPatchesDilate == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthPatchesDilatePropertyName);
                _depthPatchesDilate = value;
                RaisePropertyChanged(DepthPatchesDilatePropertyName);
            }
        }

        #endregion

        #region IsFirstErodeThenDilateDepthPatches

        /// <summary>
        /// The <see cref="IsFirstErodeThenDilateDepthPatches" /> property's name.
        /// </summary>
        public const string IsFirstErodeThenDilateDepthPatchesPropertyName = "IsFirstErodeThenDilateDepthPatches";

        private bool _isFirstErodeThenDilateDepthPatches = true;

        /// <summary>
        /// Sets and gets the IsFirstErodeThenDilateDepthPatches property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsFirstErodeThenDilateDepthPatches
        {
            get
            {
                return _isFirstErodeThenDilateDepthPatches;
            }

            set
            {
                if (_isFirstErodeThenDilateDepthPatches == value)
                {
                    return;
                }

                RaisePropertyChanging(IsFirstErodeThenDilateDepthPatchesPropertyName);
                _isFirstErodeThenDilateDepthPatches = value;
                RaisePropertyChanged(IsFirstErodeThenDilateDepthPatchesPropertyName);
            }
        }

        #endregion

        #region Image Sources

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

        #region DepthPatchesImageSource

        /// <summary>
        /// The <see cref="DepthPatchesImageSource" /> property's name.
        /// </summary>
        public const string DepthPatchesImageSourcePropertyName = "DepthPatchesImageSource";

        private BitmapSource _depthPatchesImageSource;

        /// <summary>
        /// Sets and gets the DepthPatchesImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource DepthPatchesImageSource
        {
            get
            {
                return _depthPatchesImageSource;
            }

            set
            {
                if (_depthPatchesImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthPatchesImageSourcePropertyName);
                _depthPatchesImageSource = value;
                RaisePropertyChanged(DepthPatchesImageSourcePropertyName);
            }
        }

        #endregion

        #region DepthFixedImageSource

        /// <summary>
        /// The <see cref="DepthFixedImageSource" /> property's name.
        /// </summary>
        public const string DepthFixedImageSourcePropertyName = "DepthFixedImageSource";

        private BitmapSource _depthFixedImageSource;

        /// <summary>
        /// Sets and gets the DepthFixedImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource DepthFixedImageSource
        {
            get
            {
                return _depthFixedImageSource;
            }

            set
            {
                if (_depthFixedImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthFixedImageSourcePropertyName);
                _depthFixedImageSource = value;
                RaisePropertyChanged(DepthFixedImageSourcePropertyName);
            }
        }

        #endregion

        #region FixMaskImageSource

        /// <summary>
        /// The <see cref="FixMaskImageSource" /> property's name.
        /// </summary>
        public const string FixMaskImageSourcePropertyName = "FixMaskImageSource";

        private BitmapSource _fixMaskImageSource;

        /// <summary>
        /// Sets and gets the FixMaskImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource FixMaskImageSource
        {
            get
            {
                return _fixMaskImageSource;
            }

            set
            {
                if (_fixMaskImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(FixMaskImageSourcePropertyName);
                _fixMaskImageSource = value;
                RaisePropertyChanged(FixMaskImageSourcePropertyName);
            }
        }

        #endregion

        #endregion

        #endregion

        #region ctor

        public RectangleTracker()
            : base(false)
        {

        }

        #endregion

        //public override IDataContainer PreProcess(IDataContainer dataContainer)
        //{
        //    if (dataContainer.OfType<GrayFloatImage>().Any())
        //        Console.WriteLine("Depth Image Frame: {0}", dataContainer.FrameId);
        //    else if (dataContainer.OfType<RgbImageData>().Any())
        //        Console.WriteLine("Confidence Image Frame: {0}", dataContainer.FrameId);

        //    return dataContainer;
        //}

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

            // Get time for current processing.
            var now = DateTime.Now;

            // Remove all objects, which have a last update past the timeout threshold.
            _objects.RemoveAll(o => (now - o.LastUpdate).TotalMilliseconds > Timeout);

            // Reset tracking state of all objects in the previous frame
            foreach (var o in _objects)
                o.State = TrackingState.NotTracked;

            // Needed to be wrapped in closure -> required by Parallel.ForEach below.
            Image<Rgb, byte>[] outputImage = { new Image<Rgb, byte>(imageWidth, imageHeight, Rgbs.Black) };

            var threadSafeObjects = _objects.ToArray();

            if (threadSafeObjects.Length > 0)
            {
                // Try to identify objects even if they are connected tightly (without a gap).
                //Parallel.ForEach(threadSafeObjects, obj => FindObjectByBlankingKnownObjects(image, ref outputImage[0], now, threadSafeObjects, obj)); // TODO Parallel.ForEach does not work :(
                foreach (var foundObjects in threadSafeObjects.Select(obj => FindObjectByBlankingKnownObjects(false, image, ref outputImage[0], now, threadSafeObjects, obj, true)))
                {
                    _objects.AddRange(foundObjects);

                    if (foundObjects.Any())
                        Log("Updated but also found {0} new objects {1}", foundObjects.Length, foundObjects);
                }

                // Update occluded objects. It tries to find not yet identified and maybe occluded objects.
                if (IsUpdateOccludedRectangles)
                    UpdateOccludedObjects(image, ref outputImage[0], now, threadSafeObjects);

                // Try to find new objects.
                var foundNewObjects = FindObjectByBlankingKnownObjects(false, image, ref outputImage[0], now, _objects.ToArray());
                _objects.AddRange(foundNewObjects);

                if (foundNewObjects.Any())
                    Log("Found {0} new objects {1}", foundNewObjects.Length, foundNewObjects);
            }
            else
            {
                // Find yet unidentified objects
                var foundObjects = FindObjectByBlankingKnownObjects(false, image, ref outputImage[0], now, _objects.ToArray());
                _objects.AddRange(foundObjects);

                if (foundObjects.Any())
                    Log("Found {0} new objects {1}", foundObjects.Length, foundObjects);
            }

            foreach (var obj in _objects.ToArray())
            {
                if (IsRenderContent)
                {
                    if (IsFillContours)
                        outputImage[0].FillConvexPoly(obj.Points, Rgbs.Yellow);

                    if (IsDrawContours)
                    {
                        Rgb color;
                        switch (obj.State)
                        {
                            case TrackingState.Tracked:
                                color = Rgbs.Green;
                                break;
                            case TrackingState.Occluded:
                                color = Rgbs.Yellow;
                                break;
                            case TrackingState.NotTracked:
                                color = Rgbs.Red;
                                break;
                            default:
                                color = Rgbs.Cyan;
                                break;
                        }
                        outputImage[0].Draw(obj.Shape, color, 2);
                    }

                    if (IsDrawCenter)
                    {
                        var circle = new CircleF(obj.Center, 2);
                        outputImage[0].Draw(circle, Rgbs.Green, 3);
                    }

                    if (IsDrawCenter)
                    {
                        var circle = new CircleF(obj.EstimatedCenter, 2);
                        outputImage[0].Draw(circle, Rgbs.Blue, 3);
                    }

                    outputImage[0].Draw(string.Format("Id {0}", obj.Id), ref EmguFont, new Point((int)obj.Shape.center.X, (int)obj.Shape.center.Y), Rgbs.White);
                }

                var bounds = obj.Bounds;
                var estimatedCenter = obj.EstimatedCenter;
                Stage(new BlobData(this, BlobType)
                {
                    Id = obj.Id,
                    X = estimatedCenter.X / (double)imageWidth,
                    Y = estimatedCenter.Y / (double)imageHeight,
                    State = obj.State,
                    Angle = obj.Shape.angle,
                    //Angle = rawObject.SlidingAngle,
                    Shape = obj.Shape,
                    Polygon = obj.Polygon,
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
        /// <param name="occlusionTracking"></param>
        /// <param name="image"></param>
        /// <param name="outputImage"></param>
        /// <param name="objects"></param>
        /// <param name="obj"></param>
        /// <param name="updateTime"></param>
        /// <param name="useROI"></param>
        private RectangularObject[] FindObjectByBlankingKnownObjects(bool occlusionTracking, Image<Rgb, byte> image, ref Image<Rgb, byte> outputImage, DateTime updateTime, RectangularObject[] objects, RectangularObject obj = null, bool useROI = false)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            var objectsToBlank = obj != null ? objects.Where(o => o != obj) : objects;

            // Blank previous objects from previous frame
            var blankedImage = image.Copy();
            foreach (var otherObject in objectsToBlank)
            {
                blankedImage.Draw(otherObject.Shape, Rgbs.Black, -1);
            }

            var blankedImageGray = blankedImage.Convert<Gray, Byte>();

            var roi = blankedImage.ROI;
            if (useROI)
            {
                const int threshold = 20;
                var b = obj.Bounds;

                roi = new Rectangle(b.X - threshold, b.Y - threshold, b.Width + 2 * threshold, b.Height + 2 * threshold);

                //blankedImageGray.ROI = roi;

                if (IsRenderContent)
                {
                    outputImage.Draw(roi, Rgbs.AquaSky, 2);
                }

                var maskImage = new Image<Gray, byte>(imageWidth, imageHeight);
                maskImage.Draw(roi, new Gray(255), -1);

                CvInvoke.cvAnd(blankedImageGray.Ptr, maskImage.Ptr, blankedImageGray.Ptr, IntPtr.Zero);
            }

            blankedImageGray = blankedImageGray.Erode(2);

            if (IsRenderContent && occlusionTracking)
            {
                #region Render Depth Fixed Image

                var debugImageCopy = blankedImageGray.Copy();
                Task.Factory.StartNew(() =>
                {
                    var bitmapSource = debugImageCopy.ToBitmapSource(true);
                    debugImageCopy.Dispose();
                    return bitmapSource;
                }).ContinueWith(t => DebugImageSource = t.Result);

                #endregion
            }

            //var oldROI = outputImage.ROI;
            //outputImage.ROI = roi;

            var newObjects = FindRectangles(occlusionTracking, blankedImageGray, ref outputImage, updateTime, objects, imageWidth, imageHeight);

            //outputImage.ROI = oldROI;

            // Remove objects that intersect with previous objects.
            var filteredObjects = newObjects.ToList();
            foreach (var newObject in newObjects)
            {
                if (objects.Any(o =>
                            {
                                var r = PolygonCollisionUtils.PolygonCollision(o.Polygon, newObject.Polygon, Vector.Empty);
                                return r.WillIntersect;
                            }))
                {
                    filteredObjects.Remove(newObject);
                }
            }

            blankedImageGray.Dispose();

            return filteredObjects.ToArray();
        }

        /// <summary>
        /// Tries to find occluded objects based on their previous positions.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="updateTime"></param>
        /// <param name="outputImage"></param>
        /// <param name="objects"></param>
        private void UpdateOccludedObjects(Image<Rgb, byte> image, ref Image<Rgb, byte> outputImage, DateTime updateTime, RectangularObject[] objects)
        {
            var occludedObjects = objects.Where(o => !Equals(o.LastUpdate, updateTime)).ToArray();

            // ignore if no objects are occluded but continue in case is render content set true to update debug view
            if (occludedObjects.Length < 1 || _depthImage == null)
                return;

            var enclosedOutputImage = outputImage;
            Parallel.ForEach(occludedObjects, obj => UpdateOccludedObject(image, ref enclosedOutputImage, updateTime, objects, obj));
        }

        /// <summary>
        /// Tries to find the occluded object based on its previous position.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="updateTime"></param>
        /// <param name="outputImage"></param>
        /// <param name="objects"></param>
        /// <param name="obj"></param>
        private void UpdateOccludedObject(Image<Rgb, byte> image, ref Image<Rgb, byte> outputImage, DateTime updateTime, RectangularObject[] objects, RectangularObject obj)
        {
            var imageWidth = image.Width;
            var imageHeight = image.Height;

            var mask = new Image<Gray, byte>(imageWidth, imageHeight);
            var depthPatchesImage = new Image<Gray, float>(imageWidth, imageHeight);

            // create mask for objects previousl location
            mask.Draw(obj.Shape, new Gray(1), -1);

            var depthMapBinary = _depthImage.ThresholdBinaryInv(new Gray(255), new Gray(255));
            var depthMap = depthMapBinary;

            if (depthMap.Width != imageWidth || depthMap.Height != imageHeight)
            {
                var resizedDepthMap = new Image<Gray, float>(imageWidth, imageHeight);
                CvInvoke.cvResize(depthMap.Ptr, resizedDepthMap.Ptr, INTER.CV_INTER_CUBIC);
                depthMap.Dispose();
                depthMap = resizedDepthMap;
            }

            if (IsFirstErodeThenDilateFixMask)
                mask = mask.Erode(FixMaskErode).Dilate(FixMaskDilate);
            else
                mask = mask.Dilate(FixMaskDilate).Erode(FixMaskErode);

            if (IsRenderContent)
            {
                #region Render Fix Mask Image

                var maskCopy = mask.Mul(255).Copy();
                Task.Factory.StartNew(() =>
                {
                    var bitmapSource = maskCopy.ToBitmapSource(true);
                    maskCopy.Dispose();
                    return bitmapSource;
                }).ContinueWith(t => FixMaskImageSource = t.Result);

                #endregion
            }

            CvInvoke.cvCopy(depthMap.Ptr, depthPatchesImage.Ptr, mask);

            var repairedPixels = depthPatchesImage.CountNonzero()[0];
            var totalPixels = obj.Shape.size.Width * obj.Shape.size.Height;
            var factorOfRepairedPixels = (double)repairedPixels / totalPixels;
            //Console.WriteLine("{0}% pixels repaired.", factorOfRepairedPixels * 100);

            // Do not account for entire occlusion at this time to avoid phantom objects even if the device is not present anymore.
            if (factorOfRepairedPixels > 0.95) return;

            // Erode and dilate depth patches image to remove small pixels around device borders.
            if (IsFirstErodeThenDilateDepthPatches)
            {
                CvInvoke.cvErode(depthPatchesImage.Ptr, depthPatchesImage.Ptr, IntPtr.Zero, DepthPatchesErode);
                CvInvoke.cvDilate(depthPatchesImage.Ptr, depthPatchesImage.Ptr, IntPtr.Zero, DepthPatchesDilate);
            }
            else
            {
                CvInvoke.cvDilate(depthPatchesImage.Ptr, depthPatchesImage.Ptr, IntPtr.Zero, DepthPatchesDilate);
                CvInvoke.cvErode(depthPatchesImage.Ptr, depthPatchesImage.Ptr, IntPtr.Zero, DepthPatchesErode);
            }

            if (IsRenderContent)
            {
                #region Render Depth Patches Image

                var depthPatchesImageCopy = depthPatchesImage.Copy();
                Task.Factory.StartNew(() =>
                {
                    var bitmapSource = depthPatchesImageCopy.ToBitmapSource(true);
                    depthPatchesImageCopy.Dispose();
                    return bitmapSource;
                }).ContinueWith(t => DepthPatchesImageSource = t.Result);

                #endregion
            }

            // ??? Clip depth patches image again to avoid depth fixed rectangles to grow.
            //CvInvoke.cvCopy(depthPatchesImage.Ptr, depthPatchesImage.Ptr, mask);

            var debugImage3 = depthPatchesImage.Convert<Rgb, byte>();

            var depthFixedImage = image.Or(debugImage3);
            //fixedImage = fixedImage.Erode(2);

            if (IsRenderContent)
            {

                #region Render Depth Fixed Image

                var depthFixedImageCopy = depthFixedImage.Copy();
                Task.Factory.StartNew(() =>
                {
                    var bitmapSource = depthFixedImageCopy.ToBitmapSource(true);
                    depthFixedImageCopy.Dispose();
                    return bitmapSource;
                }).ContinueWith(t => DepthFixedImageSource = t.Result);

                #endregion
            }

            FindObjectByBlankingKnownObjects(true, depthFixedImage, ref outputImage, updateTime, objects, obj, true);
        }

        /// <summary>
        /// Find rectangles in image and add possible rectangle candidates as temporary but known objects or updates
        /// existing objects from previous frames.
        /// </summary>
        /// <param name="occlusionTracking"></param>
        /// <param name="grayImage"></param>
        /// <param name="outputImage"></param>
        /// <param name="updateTime"></param>
        /// <param name="objects"></param>
        private RectangularObject[] FindRectangles(bool occlusionTracking, Image<Gray, byte> grayImage, ref Image<Rgb, byte> outputImage, DateTime updateTime, RectangularObject[] objects, int imageWidth, int imageHeight)
        {
            var newObjects = new List<RectangularObject>();

            var pixels = imageWidth * imageHeight;

            var diagonal = Math.Sqrt(Math.Pow(imageWidth, 2) + Math.Pow(imageHeight, 2));

            var maxRestoreDistance = (MaxRestoreDistance / 100.0) * diagonal;

            using (var storage = new MemStorage())
            {
                for (var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, IsRetrieveExternal ? RETR_TYPE.CV_RETR_EXTERNAL : RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
                {
                    var lowApproxContour = contours.ApproxPoly(contours.Perimeter * 0.015, storage);

                    if (IsRenderContent && IsDrawAllContours)
                        outputImage.Draw(lowApproxContour, Rgbs.FuchsiaRose, 1);

                    if (lowApproxContour.Area > ((MinContourArea / 100.0) * pixels) && lowApproxContour.Area < ((MaxContourArea / 100.0) * pixels)) //only consider contours with area greater than
                    {
                        if (IsRenderContent && IsDrawAllContours)
                            outputImage.Draw(lowApproxContour, Rgbs.BlueTorquoise, 1);
                        //outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.BlueTorquoise, 2);

                        // Continue with next contour if current contour is not a rectangle.
                        List<Point> points;
                        if (!IsPlausibleRectangle(lowApproxContour, MinAngle, MaxAngle, MinDetectRightAngles, out points)) continue;

                        var highApproxContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                        if (IsRenderContent && IsDrawAllContours)
                            outputImage.Draw(highApproxContour, Rgbs.Yellow, 1);

                        var rectangle = highApproxContour.BoundingRectangle;
                        var minAreaRect = highApproxContour.GetMinAreaRect(storage);
                        var polygon = new Polygon(points.ToArray(), imageWidth, imageHeight);
                        var contourPoints = highApproxContour.ToArray();

                        if (!UpdateObject(occlusionTracking, highApproxContour, maxRestoreDistance, rectangle, minAreaRect, polygon, contourPoints, updateTime, objects))
                        {
                            newObjects.Add(CreateObject(NextId(), rectangle, minAreaRect, polygon, contourPoints, updateTime));
                        }
                    }
                }
            }

            return newObjects.ToArray();
        }

        /// <summary>
        /// Determine if all the angles in the contour are within min/max angle.
        /// </summary>
        /// <param name="contour"></param>
        /// <param name="minAngle"></param>
        /// <param name="maxAngle"></param>
        /// <param name="minDetectAngles"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        private bool IsPlausibleRectangle(Seq<Point> contour, int minAngle, int maxAngle, int minDetectAngles, out List<Point> points)
        {
            points = new List<Point>();

            if (contour.Total < minDetectAngles) return false; //The contour has less than 3 vertices.

            var pts = contour.ToArray();
            var edges = PointCollection.PolyLine(pts, true);

            var rightAngle = 0;
            for (var i = 0; i < edges.Length; i++)
            {
                var edge1 = edges[i];
                var edge2 = edges[(i + 1) % edges.Length];

                var edgeRatio = (edge1.Length / edge2.Length);

                points.Add(edge1.P1);

                var angle = Math.Abs(edge1.GetExteriorAngleDegree(edge2));

                // stop if an angle is not in min/max angle range, no need to continue
                // also stop if connected edges are more than double in ratio
                if ((angle < minAngle || angle > maxAngle) ||
                     (edgeRatio > 3.0 || 1 / edgeRatio > 3.0))
                {
                    continue;
                }

                rightAngle++;
            }

            return rightAngle >= minDetectAngles;
        }

        /// <summary>
        /// Adds a new temporary object that will be used to identify itself in the preceeding frames.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="boundingRectangle"></param>
        /// <param name="minAreaRect"></param>
        /// <param name="polygon"></param>
        /// <param name="points"></param>
        /// <param name="updateTime"></param>
        private static RectangularObject CreateObject(long id, Rectangle boundingRectangle, MCvBox2D minAreaRect, Polygon polygon, Point[] points, DateTime updateTime)
        {
            return new RectangularObject
            {
                Id = id,
                State = TrackingState.Tracked,
                LastUpdate = updateTime,
                Center = new Point((int)minAreaRect.center.X, (int)minAreaRect.center.Y),
                Bounds = boundingRectangle,
                Shape = minAreaRect,
                LastAngle = minAreaRect.angle,
                Polygon = polygon,
                Points = points,
            };
        }

        /// <summary>
        /// Updates an object if it finds an object from last frame at a max restore distance.
        /// </summary>
        /// <param name="occluded"></param>
        /// <param name="objectContour"></param>
        /// <param name="maxRestoreDistance"></param>
        /// <param name="boundingRectangle"></param>
        /// <param name="minAreaRect"></param>
        /// <param name="polygon"></param>
        /// <param name="points"></param>
        /// <param name="updateTime"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        private static bool UpdateObject(bool occluded, Contour<Point> objectContour, double maxRestoreDistance, Rectangle boundingRectangle, MCvBox2D minAreaRect, Polygon polygon, Point[] points, DateTime updateTime, IEnumerable<RectangularObject> objects)
        {
            var candidate = GetObjectCandidate(objects, objectContour, minAreaRect, maxRestoreDistance);

            if (candidate == null) return false;

            //var dAngle = Math.Abs(candidate.Shape.angle - minAreaRect.angle);
            //Console.WriteLine(minAreaRect.angle);

            var oldAngle = candidate.Shape.angle;
            var deltaAngle = minAreaRect.angle - candidate.LastAngle;

            // this is a hack but it works pretty good
            if (deltaAngle > 45)
            {
                deltaAngle -= 90;
                //return;
            }
            else if (deltaAngle < -45)
            {
                deltaAngle += 90;
            }

            // add current shape to compute the average shape.
            if (candidate.ApplyShapeAverage(minAreaRect))
            {
                // added shape for candidate shape estimation based on average shape
            }

            // create new candidate shape based on its previous shape size and the new center point and orientation.
            // This keeps the objects shape constant and avoids growing shapes when devices are connected closely or
            // an objects occludes the device.
            var shape = new MCvBox2D(minAreaRect.center, candidate.Size, oldAngle + deltaAngle);

            candidate.State = occluded ? TrackingState.Occluded : TrackingState.Tracked;
            candidate.LastUpdate = updateTime;
            candidate.Center = new Point((int)shape.center.X, (int)shape.center.Y);
            candidate.Bounds = boundingRectangle;
            candidate.Shape = shape;
            candidate.LastAngle = minAreaRect.angle;
            candidate.Polygon = polygon;
            candidate.Points = points;

            return true;
        }

        private static RectangularObject GetObjectCandidate(IEnumerable<RectangularObject> objects, Contour<Point> objectContour, MCvBox2D shape, double maxRestoreDistance)
        {
            RectangularObject candidate = null;
            var leastDistance = double.MaxValue;
            foreach (var obj in objects)
            {
                // check current contour to last center point distance (checking last contour with current center point does not work because of MemStorage
                // which will lead to an inconsistent last contour after last image has been processed completely and after storage is disposed.
                var distanceToContour = CvInvoke.cvPointPolygonTest(objectContour.Ptr, obj.Shape.center, true);

                var oCenter = obj.Shape.center;
                var distance = Math.Sqrt(Math.Pow(oCenter.X - shape.center.X, 2) + Math.Pow(oCenter.Y - shape.center.Y, 2));

                // distance < 0 means the point is outside of the contour.
                if (distanceToContour < 0 || leastDistance < distance) continue;

                //if (distanceToContour )

                candidate = obj;
                leastDistance = distance;
            }

            if (leastDistance > maxRestoreDistance || candidate == null)
                return null;

            return candidate;
        }
    }
}
