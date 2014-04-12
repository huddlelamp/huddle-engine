using System;
using System.Drawing;
using System.Linq;
using Huddle.Engine.Processor;
using Huddle.Engine.Processor.OpenCv.Filter;

namespace Huddle.Engine.Data
{
    public class Hand : LocationData
    {
        #region private members

        private readonly KalmanFilter _kalmanFilter = new KalmanFilter();

        private const int SlidingSize = 5;
        private int _slidingPointerX = 0;
        private int _slidingPointerY = 0;
        private int _slidingPointerDepth = 0;
        private double[] _slidingX = new double[SlidingSize];
        private double[] _slidingY = new double[SlidingSize];
        private double[] _slidingDepth = new double[SlidingSize];

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

        private Point _center = new Point();

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

        #region RelativeX

        /// <summary>
        /// The <see cref="RelativeX" /> property's name.
        /// </summary>
        public new const string RelativeXPropertyName = "RelativeX";

        private double _relativeX = 0.0;

        /// <summary>
        /// Sets and gets the RelativeX property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public new double RelativeX
        {
            get
            {
                return _relativeX;
            }

            set
            {
                _slidingX[++_slidingPointerX % SlidingSize] = value;

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
        public new const string RelativeYPropertyName = "RelativeY";

        private double _relativeY = 0.0;

        /// <summary>
        /// Sets and gets the RelativeY property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public new double RelativeY
        {
            get
            {
                return _relativeY;
            }

            set
            {
                _slidingY[++_slidingPointerY % SlidingSize] = value;

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

        #region Depth

        /// <summary>
        /// The <see cref="Depth" /> property's name.
        /// </summary>
        public const string DepthPropertyName = "Depth";

        private float _depth = 0.0f;

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
                _slidingDepth[++_slidingPointerDepth % SlidingSize] = value;

                if (_depth == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthPropertyName);
                _depth = value;
                RaisePropertyChanged(DepthPropertyName);
            }
        }

        #endregion

        #region SlidingX

        public double SlidingX
        {
            get 
            {
                return _slidingPointerX < SlidingSize ? _slidingX[_slidingPointerX] : _slidingX.Average();
            }
        }

        #endregion

        #region SlidingY

        public double SlidingY
        {
            get 
            {
                return _slidingPointerY < SlidingSize ? _slidingY[_slidingPointerY] : _slidingY.Average();
            }
        }

        #endregion

        #region SlidingDepth

        public double SlidingDepth
        {
            get 
            {
                return _slidingPointerDepth < SlidingSize ? _slidingDepth[_slidingPointerDepth] : _slidingDepth.Average();
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

        #endregion

        #region ctor

        public Hand(IProcessor source, string key, long id, Point center)
            : base(source, key)
        {
            Id = id;
            Center = center;
        }

        #endregion

        #region PredictedCenter

        public Point PredictedCenter
        {
            get
            {
                return _kalmanFilter.GetPredictedPoint(Center);
            }
        }

        #endregion

        #region EstimatedCenter

        public Point EstimatedCenter
        {
            get
            {
                return _kalmanFilter.GetEstimatedPoint(Center);
            }
        }

        #endregion

        public override IData Copy()
        {
            var hand = new Hand(Source, Key, Id, Center)
            {
                X = X,
                Y = Y,
                Angle = Angle,
                LastUpdate = LastUpdate,
                Depth = Depth,
                Center = Center,
                RelativeX = RelativeX,
                RelativeY = RelativeY
            };
            Array.Copy(_slidingX, hand._slidingX, SlidingSize);
            Array.Copy(_slidingY, hand._slidingY, SlidingSize);
            Array.Copy(_slidingDepth, hand._slidingDepth, SlidingSize);
            hand._slidingPointerX = _slidingPointerX;
            hand._slidingPointerY = _slidingPointerY;
            hand._slidingPointerDepth = _slidingPointerDepth;

            return hand;
        }

        public override void Dispose()
        {
        }
    }
}
