using System;
using System.Linq;
using System.Windows;
using Huddle.Engine.Extensions;
using Huddle.Engine.Processor;
using Huddle.Engine.Processor.Complex.PolygonIntersection;
using Huddle.Engine.Processor.OpenCv.Filter;
using Huddle.Engine.Processor.OpenCv.Struct;
using Newtonsoft.Json;

namespace Huddle.Engine.Data
{
    public class Device : BaseData
    {
        #region private members

        private readonly KalmanFilter _kalmanFilter = new KalmanFilter();

        private const int SlidingSize = 5;
        private int _slidingPointerX = -1;
        private int _slidingPointerY = -1;
        private int _slidingPointerAngle = -1;
        private readonly double[] _slidingX = new double[SlidingSize];
        private readonly double[] _slidingY = new double[SlidingSize];
        private readonly double[] _slidingAngle = new double[SlidingSize];

        #endregion

        #region properties

        #region BlobId

        /// <summary>
        /// The <see cref="BlobId" /> property's name.
        /// </summary>
        public const string BlobIdPropertyName = "BlobId";

        private long _blobId;

        /// <summary>
        /// Sets and gets the BlobId property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long BlobId
        {
            get
            {
                return _blobId;
            }

            set
            {
                if (_blobId == value)
                {
                    return;
                }

                RaisePropertyChanging(BlobIdPropertyName);
                _blobId = value;
                RaisePropertyChanged(BlobIdPropertyName);
            }
        }

        #endregion

        #region DeviceId

        /// <summary>
        /// The <see cref="DeviceId" /> property's name.
        /// </summary>
        public const string DeviceIdPropertyName = "DeviceId";

        private string _deviceId = string.Empty;

        /// <summary>
        /// Sets and gets the DeviceId property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string DeviceId
        {
            get
            {
                return _deviceId;
            }

            set
            {
                if (_deviceId == value)
                {
                    return;
                }

                RaisePropertyChanging(DeviceIdPropertyName);
                _deviceId = value;
                RaisePropertyChanged(DeviceIdPropertyName);
            }
        }

        #endregion

        #region IsIdentified

        /// <summary>
        /// The <see cref="IsIdentified" /> property's name.
        /// </summary>
        public const string IsIdentifiedPropertyName = "IsIdentified";

        private bool _isIdentified = false;

        /// <summary>
        /// Sets and gets the IsIdentified property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [JsonIgnore]
        public bool IsIdentified
        {
            get
            {
                return _isIdentified;
            }

            set
            {
                if (_isIdentified == value)
                {
                    return;
                }

                RaisePropertyChanging(IsIdentifiedPropertyName);
                _isIdentified = value;
                RaisePropertyChanged(IsIdentifiedPropertyName);
            }
        }

        #endregion

        #region X

        /// <summary>
        /// The <see cref="X" /> property's name.
        /// </summary>
        public const string XPropertyName = "X";

        private double _x = 0.0;

        /// <summary>
        /// Sets and gets the X property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }

            set
            {
                _slidingX[++_slidingPointerX % SlidingSize] = value;
                
                if (_x == value)
                {
                    return;
                }

                RaisePropertyChanging(XPropertyName);
                _x = value;
                RaisePropertyChanged(XPropertyName);
            }
        }

        #endregion

        #region Y

        /// <summary>
        /// The <see cref="Y" /> property's name.
        /// </summary>
        public const string YPropertyName = "Y";

        private double _y = 0.0;

        /// <summary>
        /// Sets and gets the Y property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }

            set
            {
                _slidingY[++_slidingPointerY % SlidingSize] = value;

                if (_y == value)
                {
                    return;
                }

                RaisePropertyChanging(YPropertyName);
                _y = value;
                RaisePropertyChanged(YPropertyName);
            }
        }

        #endregion

        #region State

        /// <summary>
        /// The <see cref="State" /> property's name.
        /// </summary>
        public const string StatePropertyName = "State";

        private TrackingState _state = TrackingState.NotTracked;

        /// <summary>
        /// Sets and gets the State property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public TrackingState State
        {
            get
            {
                return _state;
            }

            set
            {
                if (_state == value)
                {
                    return;
                }

                RaisePropertyChanging(StatePropertyName);
                _state = value;
                RaisePropertyChanged(StatePropertyName);
            }
        }

        #endregion

        #region Angle

        /// <summary>
        /// The <see cref="Angle" /> property's name.
        /// </summary>
        public const string AnglePropertyName = "Angle";

        private double _angle = 0.0;

        /// <summary>
        /// Sets and gets the Angle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Angle
        {
            get
            {
                return _angle;
            }

            set
            {
                _slidingAngle[++_slidingPointerAngle % SlidingSize] = Math.Round(value);

                if (_angle == value)
                {
                    return;
                }

                RaisePropertyChanging(AnglePropertyName);
                _angle = value;
                RaisePropertyChanged(AnglePropertyName);
            }
        }

        #endregion

        #region SlidingX

        public double SlidingX
        {
            get
            {
                return _slidingPointerX < SlidingSize ? _slidingX[_slidingPointerX] : _slidingX.Median();
                return EstimatedPoint.X;
                return _slidingPointerX < SlidingSize ? _slidingX[_slidingPointerX] : _slidingX.Average();
            }
        }

        #endregion

        #region SlidingY

        public double SlidingY
        {
            get
            {
                return _slidingPointerY < SlidingSize ? _slidingY[_slidingPointerY] : _slidingY.Median();
                return EstimatedPoint.Y;
                return _slidingPointerY < SlidingSize ? _slidingY[_slidingPointerY] : _slidingY.Average();
            }
        }

        #endregion

        #region SlidingAngle

        public double SlidingAngle
        {
            get
            {
                return _slidingPointerAngle < SlidingSize ? _slidingAngle[_slidingPointerAngle] : _slidingAngle.Median();
                return _slidingPointerAngle < SlidingSize ? _slidingAngle[_slidingPointerAngle] : _slidingAngle.Average();
            }
        }

        #endregion

        #region EstimatedPoint

        public Point EstimatedPoint
        {
            get
            {
                const double factor = 10000.0;

                var estimatedPoint = _kalmanFilter.GetEstimatedPoint(new System.Drawing.Point((int)(X * factor), (int)(Y * factor)));

                return new Point(estimatedPoint.X / factor, estimatedPoint.Y / factor);
            }
        }

        #endregion

        #region LastBlobAngle

        /// <summary>
        /// The <see cref="LastBlobAngle" /> property's name.
        /// </summary>
        public const string LastBlobAnglePropertyName = "LastBlobAngle";

        private double _lastBlobAngle = 0.0;

        /// <summary>
        /// Sets and gets the LastBlobAngle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [JsonIgnore]
        public double LastBlobAngle
        {
            get
            {
                return _lastBlobAngle;
            }

            set
            {
                if (_lastBlobAngle == value)
                {
                    return;
                }

                RaisePropertyChanging(LastBlobAnglePropertyName);
                _lastBlobAngle = value;
                RaisePropertyChanged(LastBlobAnglePropertyName);
            }
        }

        #endregion

        #region Shape

        /// <summary>
        /// The <see cref="Shape" /> property's name.
        /// </summary>
        public const string ShapePropertyName = "Shape";

        private Polygon _shape;

        /// <summary>
        /// Sets and gets the Shape property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Polygon Shape
        {
            get
            {
                return _shape;
            }

            set
            {
                if (_shape == value)
                {
                    return;
                }

                RaisePropertyChanging(ShapePropertyName);
                _shape = value;
                RaisePropertyChanged(ShapePropertyName);
            }
        }

        #endregion

        #region Area

        /// <summary>
        /// The <see cref="Area" /> property's name.
        /// </summary>
        public const string AreaPropertyName = "Area";

        private Rect _area = Rect.Empty;

        /// <summary>
        /// Sets and gets the Area property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Rect Area
        {
            get
            {
                return _area;
            }

            set
            {
                if (_area == value)
                {
                    return;
                }

                RaisePropertyChanging(AreaPropertyName);
                _area = value;
                RaisePropertyChanged(AreaPropertyName);
            }
        }

        #endregion

        #region RgbImageToDeviceRatio

        /// <summary>
        /// The <see cref="RgbImageToDisplayRatio" /> property's name.
        /// </summary>
        public const string RgbImageToDisplayRatioPropertyName = "RgbImageToDisplayRatio";

        private Ratio _rgbImageToDisplayRatio = Ratio.Empty;

        /// <summary>
        /// Sets and gets the RgbImageToDisplayRatio property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Ratio RgbImageToDisplayRatio
        {
            get
            {
                return _rgbImageToDisplayRatio;
            }

            set
            {
                if (_rgbImageToDisplayRatio.Equals(value))
                {
                    return;
                }

                RaisePropertyChanging(RgbImageToDisplayRatioPropertyName);
                _rgbImageToDisplayRatio = value;
                RaisePropertyChanged(RgbImageToDisplayRatioPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public Device(IProcessor source, string key)
            : base(source, key)
        {
        }

        #endregion

        public override IData Copy()
        {
            var device = new Device(Source, Key)
            {
                Angle = Angle,
                DeviceId = DeviceId,
                BlobId = BlobId,
                IsIdentified = IsIdentified,
                X = X,
                Y = Y,
                State = State,
                Shape = Shape,
                Area = Area,
                RgbImageToDisplayRatio = RgbImageToDisplayRatio
            };
            Array.Copy(_slidingX, device._slidingX, SlidingSize);
            Array.Copy(_slidingY, device._slidingY, SlidingSize);
            Array.Copy(_slidingAngle, device._slidingAngle, SlidingSize);
            device._slidingPointerX = _slidingPointerX;
            device._slidingPointerY = _slidingPointerY;
            device._slidingPointerAngle = _slidingPointerAngle;

            return device;
        }

        public override void Dispose()
        {
            
        }
    }
}
