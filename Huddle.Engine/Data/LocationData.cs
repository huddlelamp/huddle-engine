using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public abstract class LocationData : BaseData
    {
        #region properties

        #region Id

        /// <summary>
        /// The <see cref="Id" /> property's name.
        /// </summary>
        public const string IdPropertyName = "Id";

        private string _id = string.Empty;

        /// <summary>
        /// Sets and gets the Id property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Id
        {
            get
            {
                return _id;
            }

            set
            {
                if (_id == value)
                {
                    return;
                }

                RaisePropertyChanging(IdPropertyName);
                _id = value;
                RaisePropertyChanged(IdPropertyName);
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

        #region RelativeX

        /// <summary>
        /// The <see cref="RelativeX" /> property's name.
        /// </summary>
        public const string RelativeXPropertyName = "RelativeX";

        private double _relativeX = 0.0;

        /// <summary>
        /// Sets and gets the RelativeX property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double RelativeX
        {
            get
            {
                return _relativeX;
            }

            set
            {
                if (_relativeX == value)
                {
                    return;
                }

                RaisePropertyChanging(RelativeXPropertyName);
                _relativeX = value;
                RaisePropertyChanged(RelativeXPropertyName);
            }
        }

        #endregion

        #region RelativeY

        /// <summary>
        /// The <see cref="RelativeY" /> property's name.
        /// </summary>
        public const string RelativeYPropertyName = "RelativeY";

        private double _relativeY = 0.0;

        /// <summary>
        /// Sets and gets the RelativeY property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double RelativeY
        {
            get
            {
                return _relativeY;
            }

            set
            {
                if (_relativeY == value)
                {
                    return;
                }

                RaisePropertyChanging(RelativeYPropertyName);
                _relativeY = value;
                RaisePropertyChanged(RelativeYPropertyName);
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

        protected LocationData(IProcessor source, string key)
            : base(source, key)
        {

        }
    }
}
