using System.Windows;
using Huddle.Engine.Filter;
using Huddle.Engine.Processor;
using Huddle.Engine.Processor.Complex.PolygonIntersection;
using Huddle.Engine.Processor.OpenCv.Struct;
using Newtonsoft.Json;
using WPoint = System.Windows.Point;

namespace Huddle.Engine.Data
{
    public class Device : BaseData
    {
        #region private members

        private readonly ISmoothing _smoothing = new OneEuroSmoothing();

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

        #region Center

        /// <summary>
        /// The <see cref="Center" /> property's name.
        /// </summary>
        public const string CenterPropertyName = "Center";

        private WPoint _center;

        /// <summary>
        /// Sets and gets the Center property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public WPoint Center
        {
            get
            {
                return _center;
            }

            set
            {
                if (_center == value)
                {
                    return;
                }

                _isSmoothedCenter = false;

                RaisePropertyChanging(CenterPropertyName);
                _center = value;
                RaisePropertyChanged(CenterPropertyName);
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

        private double _angle;

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
                if (_angle == value)
                {
                    return;
                }

                _isSmoothedAngle = false;

                RaisePropertyChanging(AnglePropertyName);
                _angle = value;
                RaisePropertyChanged(AnglePropertyName);
            }
        }

        #endregion

        #region SmoothedCenter

        private bool _isSmoothedCenter = false;
        private Point _smoothedCenter;

        public WPoint SmoothedCenter
        {
            get
            {
                if (_isSmoothedCenter) return _smoothedCenter;

                _smoothedCenter = _smoothing.SmoothPoint(Center);
                _isSmoothedCenter = true;
                return _smoothedCenter;
            }
        }

        #endregion

        #region SmoothedAngle

        private bool _isSmoothedAngle = false;
        private double _smoothedAngle;

        public double SmoothedAngle
        {
            get
            {
                if (_isSmoothedAngle) return _smoothedAngle;

                _smoothedAngle = _smoothing.SmoothAngle(Angle);
                _isSmoothedAngle = true;
                return _smoothedAngle;
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
                DeviceId = DeviceId,
                BlobId = BlobId,
                IsIdentified = IsIdentified,
                Center = Center,
                Angle = Angle,
                State = State,
                Shape = Shape,
                Area = Area,
                RgbImageToDisplayRatio = RgbImageToDisplayRatio
            };

            return device;
        }

        public override void Dispose()
        {
            
        }
    }
}
