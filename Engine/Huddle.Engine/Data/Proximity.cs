using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Media3D;
using Huddle.Engine.Converter;
using Huddle.Engine.Processor;
using Newtonsoft.Json;

namespace Huddle.Engine.Data
{
    public class Proximity : BaseData
    {
        #region properties

        #region Type

        /// <summary>
        /// The <see cref="Type" /> property's name.
        /// </summary>
        public const string TypePropertyName = "Type";

        private string _type = string.Empty;

        /// <summary>
        /// Sets and gets the Type property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Type
        {
            get
            {
                return _type;
            }

            set
            {
                if (_type == value)
                {
                    return;
                }

                RaisePropertyChanging(TypePropertyName);
                _type = value;
                RaisePropertyChanged(TypePropertyName);
            }
        }

        #endregion

        #region Distance

        /// <summary>
        /// The <see cref="Distance" /> property's name.
        /// </summary>
        public const string DistancePropertyName = "Distance";

        private double _distance = 0.0;

        /// <summary>
        /// Sets and gets the Distance property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Distance
        {
            get
            {
                return _distance;
            }

            set
            {
                if (_distance == value)
                {
                    return;
                }

                RaisePropertyChanging(DistancePropertyName);
                _distance = value;
                RaisePropertyChanged(DistancePropertyName);
            }
        }

        #endregion

        #region Orientation

        /// <summary>
        /// The <see cref="Orientation" /> property's name.
        /// </summary>
        public const string OrientationPropertyName = "Orientation";

        private double _orientation = 0.0;

        /// <summary>
        /// Sets and gets the Orientation property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Orientation
        {
            get
            {
                return _orientation;
            }

            set
            {
                if (_orientation == value)
                {
                    return;
                }

                RaisePropertyChanging(OrientationPropertyName);
                _orientation = value;
                RaisePropertyChanged(OrientationPropertyName);
            }
        }

        #endregion

        #region Movement

        /// <summary>
        /// The <see cref="Movement" /> property's name.
        /// </summary>
        public const string MovementPropertyName = "Movement";

        private double _movement = 0.0;

        /// <summary>
        /// Sets and gets the Movement property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Movement
        {
            get
            {
                return _movement;
            }

            set
            {
                if (_movement == value)
                {
                    return;
                }

                RaisePropertyChanging(MovementPropertyName);
                _movement = value;
                RaisePropertyChanged(MovementPropertyName);
            }
        }

        #endregion

        #region Identity

        /// <summary>
        /// The <see cref="Identity" /> property's name.
        /// </summary>
        public const string IdentityPropertyName = "Identity";

        private string _indentity = string.Empty;

        /// <summary>
        /// Sets and gets the Identity property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Identity
        {
            get
            {
                return _indentity;
            }

            set
            {
                if (_indentity == value)
                {
                    return;
                }

                RaisePropertyChanging(IdentityPropertyName);
                _indentity = value;
                RaisePropertyChanged(IdentityPropertyName);
            }
        }

        #endregion

        #region Location

        /// <summary>
        /// The <see cref="Location" /> property's name.
        /// </summary>
        public const string LocationPropertyName = "Location";

        private Point3D _location = new Point3D();

        /// <summary>
        /// Sets and gets the Location property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [JsonConverter(typeof(Point3DToJsonArrayConverter))]
        public Point3D Location
        {
            get
            {
                return _location;
            }

            set
            {
                if (_location == value)
                {
                    return;
                }

                RaisePropertyChanging(LocationPropertyName);
                _location = value;
                RaisePropertyChanged(LocationPropertyName);
            }
        }

        #endregion

        #region Presences

        /// <summary>
        /// The <see cref="Presences" /> property's name.
        /// </summary>
        public const string PresencesPropertyName = "Presences";

        private List<Proximity> _presences = new List<Proximity>();

        /// <summary>
        /// Sets and gets the Presences property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public List<Proximity> Presences
        {
            get
            {
                return _presences;
            }

            set
            {
                if (_presences == value)
                {
                    return;
                }

                RaisePropertyChanging(PresencesPropertyName);
                _presences = value;
                RaisePropertyChanged(PresencesPropertyName);
            }
        }

        #endregion

        #region RgbImageToDisplayRatio

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

        public Proximity(IProcessor source, string type, string key)
            : base(source, key)
        {
            Type = type;
        }

        public override IData Copy()
        {
            return new Proximity(Source, Type, Key)
            {
                Distance = Distance,
                Identity = Identity,
                Location = Location,
                Movement = Movement,
                Orientation = Orientation,
                Presences = new List<Proximity>(Presences),
                RgbImageToDisplayRatio = RgbImageToDisplayRatio
            };
        }

        public override void Dispose()
        {
            // ignore
        }

        public override string ToString()
        {
            return string.Format("{0}={{Type={1},Identity={2},Location={3},Orientation={4},Distance={5},Movement={6}}}", GetType().Name, Type, Identity, Location, Orientation, Distance, Movement);
        }
    }
}
