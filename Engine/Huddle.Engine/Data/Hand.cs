using System;
using Emgu.CV;
using Emgu.CV.Structure;
using Huddle.Engine.Filter;
using Huddle.Engine.Processor;
using WPoint = System.Windows.Point;

namespace Huddle.Engine.Data
{
    public sealed class Hand : LocationData
    {
        #region private members

        private readonly ISmoothing _smoothing = SmoothingFilterFactory.CreateDefault();

        #endregion

        #region properties

        #region Id

        /// <summary>
        /// The <see cref="Id" /> property's name.
        /// </summary>
        public const string IdPropertyName = "Id";

        private long _id = -1;

        /// <summary>
        /// Sets and gets the Id property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long Id
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
        public new const string RelativeCenterPropertyName = "RelativeCenter";

        private WPoint _relativeCenter;

        /// <summary>
        /// Sets and gets the RelativeCenter property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public new WPoint RelativeCenter
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

        #region Depth

        /// <summary>
        /// The <see cref="Depth" /> property's name.
        /// </summary>
        public const string DepthPropertyName = "Depth";

        private float _depth;

        /// <summary>
        /// Sets and gets the Depth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float Depth
        {
            get
            {
                return _depth;
            }

            set
            {
                if (_depth == value)
                {
                    return;
                }

                _isSmoothedDepth = false;

                RaisePropertyChanging(DepthPropertyName);
                _depth = value;
                RaisePropertyChanged(DepthPropertyName);
            }
        }

        #endregion

        #region SmoothedDepth

        private bool _isSmoothedDepth;
        private double _smoothedDepth;

        public double SlidingDepth
        {
            get 
            {
                if (_isSmoothedDepth) return _smoothedDepth;

                _smoothedDepth = _smoothing.SmoothDepth(Depth);
                _isSmoothedDepth = true;
                return _smoothedDepth;
            }
        }

        #endregion

        #region LastUpdate

        /// <summary>
        /// The <see cref="LastUpdate" /> property's name.
        /// </summary>
        public const string LastUpdatePropertyName = "LastUpdate";

        private DateTime _lastUpdate;

        /// <summary>
        /// Sets and gets the LastUpdate property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public DateTime LastUpdate
        {
            get
            {
                return _lastUpdate;
            }

            set
            {
                if (_lastUpdate == value)
                {
                    return;
                }

                RaisePropertyChanging(LastUpdatePropertyName);
                _lastUpdate = value;
                RaisePropertyChanged(LastUpdatePropertyName);
            }
        }

        #endregion

        #region Segment

        /// <summary>
        /// The <see cref="Segment" /> property's name.
        /// </summary>
        public const string SegmentPropertyName = "Segment";

        private Image<Gray, byte> _segment;

        /// <summary>
        /// Sets and gets the Segment property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Image<Gray, byte> Segment
        {
            get
            {
                return _segment;
            }

            set
            {
                if (_segment == value)
                {
                    return;
                }

                RaisePropertyChanging(SegmentPropertyName);
                _segment = value;
                RaisePropertyChanged(SegmentPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public Hand(IProcessor source, string key, long id, WPoint center)
            : base(source, key)
        {
            Id = id;
            Center = center;
        }

        #endregion

        #region SmoothedCenter

        public WPoint SmoothedCenter
        {
            get
            {
                return _smoothing.SmoothPoint(Center);
            }
        }

        #endregion

        public override IData Copy()
        {
            var hand = new Hand(Source, Key, Id, Center)
            {
                LastUpdate = LastUpdate,
                Depth = Depth,
                Center = Center,
                Angle = Angle,
                RelativeCenter = RelativeCenter,
                Segment = Segment != null ? Segment.Copy() : null
            };

            return hand;
        }

        public override void Dispose()
        {
        }
    }
}
