using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Huddle.Engine.Converter;
using Huddle.Engine.Processor;
using Huddle.Engine.Processor.OpenCv.Struct;
using Newtonsoft.Json;

namespace Huddle.Engine.Data
{
    public sealed class Proximity : BaseData
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

        private Ratio _rgbImageToDisplayRatio = Ratio.Identity;

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
                State = State,
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
            // Clear all presences.
            foreach (var presence in Presences)
                presence.Dispose();
            Presences.Clear();
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            //       
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237  
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherProximity = obj as Proximity;
            if (otherProximity == null) return false;

            return Equals(Location, otherProximity.Location) &&
                   Equals(Orientation, otherProximity.Orientation) &&
                   Presences.SequenceEqual(otherProximity.Presences);
        }

// override object.GetHashCode
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{0}={{Type={1},State={2},Identity={3},Location={4},Orientation={5},Distance={6},Movement={7}}}", GetType().Name, Type, State, Identity, Location, Orientation, Distance, Movement);
        }
    }
}
