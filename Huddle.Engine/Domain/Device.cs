using System.Drawing;
using System.Windows;
using GalaSoft.MvvmLight;
using Huddle.Engine.Data;
using Huddle.Engine.Processor;
using Huddle.Engine.Processor.Complex.PolygonIntersection;
using Newtonsoft.Json;

namespace Huddle.Engine.Domain
{
    public class Device : BaseData
    {
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

        #region DeviceToCameraRatio

        /// <summary>
        /// The <see cref="DeviceToCameraRatio" /> property's name.
        /// </summary>
        public const string DeviceToCameraRatioPropertyName = "DeviceToCameraRatio";

        private double _deviceToCameraRatio = 0.0;

        /// <summary>
        /// Sets and gets the DeviceToCameraRatio property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double DeviceToCameraRatio
        {
            get
            {
                return _deviceToCameraRatio;
            }

            set
            {
                if (_deviceToCameraRatio == value)
                {
                    return;
                }

                RaisePropertyChanging(DeviceToCameraRatioPropertyName);
                _deviceToCameraRatio = value;
                RaisePropertyChanged(DeviceToCameraRatioPropertyName);
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
            return new Device(Source, Key)
            {
                Angle = Angle,
                DeviceId = DeviceId,
                BlobId = BlobId,
                IsIdentified = IsIdentified,
                X = X,
                Y = Y,
                Shape = Shape,
                Area = Area,
                DeviceToCameraRatio = DeviceToCameraRatio
            };
        }

        public override void Dispose()
        {
            
        }
    }
}
