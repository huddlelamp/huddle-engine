using System;
using System.Drawing;
using Huddle.Engine.Processor.OpenCv.Filter;

namespace Huddle.Engine.Data
{
    public class Hand : LocationData
    {
        #region private fields

        private readonly KalmanFilter _kalmanFilter = new KalmanFilter();

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

        public Hand(string key, long id, Point center)
            : base(key)
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
            return new Hand(Key, Id, Center)
            {
                X = X,
                Y = Y,
                Angle = Angle,
                LastUpdate = LastUpdate,
                Depth = Depth
            };
        }

        public override void Dispose()
        {
        }
    }
}
