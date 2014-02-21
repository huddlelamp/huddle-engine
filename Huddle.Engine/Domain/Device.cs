using GalaSoft.MvvmLight;
using Huddle.Engine.Data;

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

        #endregion

        #region ctor

        public Device(string key)
            : base(key)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new Device(Key)
            {
                Angle = Angle,
                DeviceId = DeviceId,
                BlobId = BlobId,
                IsIdentified = IsIdentified,
                X = X,
                Y = Y
            };
        }

        public override void Dispose()
        {
            
        }
    }
}
