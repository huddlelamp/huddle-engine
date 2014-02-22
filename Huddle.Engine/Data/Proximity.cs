using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Huddle.Engine.Domain;

namespace Huddle.Engine.Data
{
    public class Proximity : BaseData
    {
        #region properties

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

        private Point _location = new Point();

        /// <summary>
        /// Sets and gets the Location property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Point Location
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

        #endregion

        public Proximity(string key) : base(key)
        {
        }

        public override IData Copy()
        {
            return new Proximity(Key)
            {
                Distance = Distance,
                Identity = Identity,
                Location = Location,
                Movement = Movement,
                Orientation = Orientation,
                Presences = new List<Proximity>(Presences)
            };
        }

        public override void Dispose()
        {
            // ignore
        }

        public override string ToString()
        {
            return string.Format("{0}={{Identity={1},Location={2},Orientation={3},Distance={4},Movement={5}}}", GetType().Name, Identity, Location, Orientation, Distance, Movement);
        }
    }
}
