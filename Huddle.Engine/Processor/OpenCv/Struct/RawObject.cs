using System;
using System.Drawing;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight;
using Huddle.Engine.Processor.OpenCv.Filter;

namespace Huddle.Engine.Processor.OpenCv.Struct
{
    public class RawObject : ObservableObject
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

        #region Center

        /// <summary>
        /// The <see cref="Center" /> property's name.
        /// </summary>
        public const string CenterPropertyName = "Center";

        private Point _center = Point.Empty;

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

        #region Shape

        /// <summary>
        /// The <see cref="Shape" /> property's name.
        /// </summary>
        public const string ShapePropertyName = "Shape";

        private MCvBox2D _shape;

        /// <summary>
        /// Sets and gets the Shape property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public MCvBox2D Shape
        {
            get
            {
                return _shape;
            }

            set
            {
                if (Equals(_shape, value))
                {
                    return;
                }

                RaisePropertyChanging(ShapePropertyName);
                _shape = value;
                RaisePropertyChanged(ShapePropertyName);
            }
        }

        #endregion

        #region Bounds

        /// <summary>
        /// The <see cref="Bounds" /> property's name.
        /// </summary>
        public const string BoundsPropertyName = "Bounds";

        private Rectangle _bounds;

        /// <summary>
        /// Sets and gets the Bounds property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Rectangle Bounds
        {
            get
            {
                return _bounds;
            }

            set
            {
                if (_bounds == value)
                {
                    return;
                }

                RaisePropertyChanging(BoundsPropertyName);
                _bounds = value;
                RaisePropertyChanged(BoundsPropertyName);
            }
        }

        #endregion

        #region Points

        /// <summary>
        /// The <see cref="Points" /> property's name.
        /// </summary>
        public const string PointsPropertyName = "Points";

        private Point[] _points;

        /// <summary>
        /// Sets and gets the Points property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Point[] Points
        {
            get
            {
                return _points;
            }

            set
            {
                if (_points == value)
                {
                    return;
                }

                RaisePropertyChanging(PointsPropertyName);
                _points = value;
                RaisePropertyChanged(PointsPropertyName);
            }
        }

        #endregion

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
    }
}
