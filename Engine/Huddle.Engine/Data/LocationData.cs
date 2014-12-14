using System.Linq;
using System.Windows;
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

        #region Center

        /// <summary>
        /// The <see cref="Center" /> property's name.
        /// </summary>
        public const string CenterPropertyName = "Center";

        private Point _center;

        /// <summary>
        /// Sets and gets the Center property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Point Center
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

                RaisePropertyChanging(CenterPropertyName);
                _center = value;
                RaisePropertyChanged(CenterPropertyName);
            }
        }

        #endregion

        #region RelativeCenter

        /// <summary>
        /// The <see cref="RelativeCenter" /> property's name.
        /// </summary>
        public const string RelativeCenterPropertyName = "RelativeCenter";

        private Point _relativeCenter;

        /// <summary>
        /// Sets and gets the RelativeCenter property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Point RelativeCenter
        {
            get
            {
                return _relativeCenter;
            }

            set
            {
                if (_relativeCenter == value)
                {
                    return;
                }

                RaisePropertyChanging(RelativeCenterPropertyName);
                _relativeCenter = value;
                RaisePropertyChanged(RelativeCenterPropertyName);
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
