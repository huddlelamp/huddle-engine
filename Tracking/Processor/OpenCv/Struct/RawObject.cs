using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight;

namespace Tools.FlockingDevice.Tracking.Processor.OpenCv.Struct
{
    public class RawObject : ObservableObject
    {
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
    }
}
