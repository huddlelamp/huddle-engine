﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    public class PipeViewModel : ObservableObject
    {
        #region ctor

        public PipeViewModel(BaseProcessor source, BaseProcessor target)
        {
            Source = source;
            Debug.Assert(Source != null, "Source != null");
            
            if (Source == null)
                throw new Exception("Source == null");
            
            Destination = target;
            Debug.Assert(Destination != null, "Destination != null");

            if (Destination == null)
                throw new Exception("Destination == null");

            //Make sure we get the updates of the source and destination nodes when they are being moved around
            RegisterChangeNotificationAction(Source, "X", e => OnSourcePositionChanged());
            RegisterChangeNotificationAction(Source, "Y", e => OnSourcePositionChanged());
            RegisterChangeNotificationAction(Destination, "X", e => OnDestinationPositionChanged());
            RegisterChangeNotificationAction(Destination, "Y", e => OnDestinationPositionChanged());

            Update();
        }

        #endregion

        #region Update Register Functions

        private readonly List<IDisposable> _changeNotificationActionSubscriptions = new List<IDisposable>();
        protected void AddChangeNotificationActionSubscription(IDisposable subscription)
        {
            _changeNotificationActionSubscriptions.Add(subscription);
        }

        /// <summary>
        /// !Subscription muss manuell zum Disposen per <see cref="AddChangeNotificationActionSubscription"/> eingetragen werden
        /// </summary>
        protected IObservable<PropertyChangedEventArgs> RegisterChangeNotificationAction(INotifyPropertyChanged source, String sourceProperty)
        {
            return Observable.FromEventPattern<PropertyChangedEventArgs>(source, "PropertyChanged").Where(e => e.EventArgs.PropertyName == sourceProperty).Select(e => e.EventArgs);
        }
        protected IDisposable RegisterChangeNotificationAction(INotifyPropertyChanged source, String sourceProperty, Action<PropertyChangedEventArgs> action)
        {
            var subscription = Observable.FromEventPattern<PropertyChangedEventArgs>(source, "PropertyChanged").Where(e => e.EventArgs.PropertyName == sourceProperty).Subscribe(e => action(e.EventArgs));
            AddChangeNotificationActionSubscription(subscription);
            return subscription;
        }

        #endregion

        protected virtual void OnSourcePositionChanged()
        {
            UpdateArrowDashes();
        }

        protected virtual void OnDestinationPositionChanged()
        {
            UpdateArrowDashes();
        }

        internal Point? GetNearestIntersectionPoint(LineGeometry geometry)
        {
            var l1X1 = geometry.StartPoint.X;
            var l1Y1 = geometry.StartPoint.Y;

            var l1X2 = geometry.EndPoint.X;
            var l1Y2 = geometry.EndPoint.Y;

            var l2X1 = Source.X;
            var l2Y1 = Source.Y;

            var l2X2 = Destination.X;
            var l2Y2 = Destination.Y;

            // Denominator for ua and ub are the same, so store this calculation
            double d = (l2Y2 - l2Y1) * (l1X2 - l1X1) - (l2X2 - l2X1) * (l1Y2 - l1Y1);
        
            //n_a and n_b are calculated as seperate values for readability
            double n_a = (l2X2 - l2X1) * (l1Y1 - l2Y1) - (l2Y2 - l2Y1) * (l1X1 - l2X1);

            double n_b = (l1X2 - l1X1) * (l1Y1 - l2Y1) - (l1Y2 - l1Y1) * (l1X1 - l2X1);

            // Make sure there is not a division by zero - this also indicates that
            // the lines are parallel.  
            // If n_a and n_b were both equal to zero the lines would be on top of each 
            // other (coincidental).  This check is not done because it is not 
            // necessary for this implementation (the parallel check accounts for this).
            if (d == 0)
                return null;

            // Calculate the intermediate fractional point that the lines potentially intersect.
            double ua = n_a / d;
            double ub = n_b / d;

            // The fractional point will be between 0 and 1 inclusive if the lines
            // intersect.  If the fractional calculation is larger than 1 or smaller
            // than 0 the lines would need to be longer to intersect.
            if (ua >= 0.0 && ua <= 1.0 && ub >= 0.0 && ub <= 1.0)
            {
                Point intersection = new Point();
                intersection.X = l1X1 + (ua * (l1X2 - l1X1));
                intersection.Y = l1Y1 + (ua * (l1Y2 - l1Y1));
                return intersection;
            }
            return null;
        }

        private void UpdateArrowDashes()
        {
            var ax = Source.X;
            var ay = Source.Y;

            var bx = Destination.X;
            var by = Destination.Y;

            var dx = ax - bx;
            var dy = ay - by;

            var lineLength = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));

            //Normalize
            dx = dx / lineLength;
            dy = dy / lineLength;

            var t = dx * (bx - ax) + dy * (by - ay);

            var ex = t * dx + ax;
            var ey = t * dy + ay;

            var distance = Math.Sqrt(Math.Pow(ex - bx, 2) + Math.Pow(ey - by, 2));

            double radius = 22.5;

            // TODO: Bad design!!!
            //if (Destination is QueryCanvasFacetNodeViewModel)
            //    radius = Settings.Default.NodeRadius;
            //else if (Destination is QueryCanvasTextNodeViewModel)
            //    radius = Settings.Default.TextSnippetWidth;
            //else if (Destination is QueryCanvasResultTokenViewModel)
            //    radius = Settings.Default.ResultTokenRadius;

            var dt = Math.Sqrt(Math.Pow(radius, 2) - Math.Pow(distance, 2));

            //point 1
            var fx = (t - dt) * dx + ax;
            var fy = (t - dt) * dy + ay;

            var fDstX = ax - fx;
            var fDstY = ay - fy;
            var fDistance = Math.Sqrt(Math.Pow(fDstX, 2) + Math.Pow(fDstY, 2));

            //point 2
            var gx = (t + dt) * dx + ax;
            var gy = (t + dt) * dy + ay;

            var gDstX = ax - gx;
            var gDstY = ay - gy;
            var gDistance = Math.Sqrt(Math.Pow(gDstX, 2) + Math.Pow(gDstY, 2));

            Point selectedPoint = fDistance < gDistance ? new Point(fx, fy) : new Point(gx, gy);

            ArrowDash1StartPoint = selectedPoint;
            ArrowDash2StartPoint = selectedPoint;

            Vector diff = new Point(Source.X, Source.Y) - new Point(Destination.X, Destination.Y);

            double alpha = Math.Atan2(diff.Y, diff.X);

            ArrowDash1EndPoint = new Point(selectedPoint.X + 30 * Math.Cos(alpha + Math.PI / 5.0), selectedPoint.Y + 30 * Math.Sin(alpha + Math.PI / 5.0));
            ArrowDash2EndPoint = new Point(selectedPoint.X + 30 * Math.Cos(alpha - Math.PI / 5.0), selectedPoint.Y + 30 * Math.Sin(alpha - Math.PI / 5.0));
        }

        //private void UpdateLinkWidth()
        //{
        //    LinkWidth = Math.Log10(QueryCount + 1.0) / Math.Log10(DataProvider.TotalResultCount + 1.0) * 12.0 + 2.0;
        //}

        #region Source

        private BaseProcessor _source = null;

        /// <summary>
        /// Gets or sets the Source property. This observable property 
        /// indicates ....
        /// </summary>
        public BaseProcessor Source
        {
            get { return _source; }
            set
            {
                if (_source != value)
                {
                    _source = value;
                    RaisePropertyChanged("Source");
                }
            }
        }

        #endregion

        #region Destination

        private BaseProcessor _destination = null;

        /// <summary>
        /// Gets or sets the Destination property. This observable property 
        /// indicates ....
        /// </summary>
        public BaseProcessor Destination
        {
            get { return _destination; }
            set
            {
                if (_destination != value)
                {
                    _destination = value;
                    RaisePropertyChanged("Destination");
                }
            }
        }

        #endregion

        #region ArrowDash1StartPoint

        private Point _arrowDash1StartPoint = new Point();

        /// <summary>
        /// Gets or sets the ArrowDash1StartPoint property. This observable property 
        /// indicates ....
        /// </summary>
        public Point ArrowDash1StartPoint
        {
            get { return _arrowDash1StartPoint; }
            set
            {
                if (_arrowDash1StartPoint != value)
                {
                    _arrowDash1StartPoint = value;
                    RaisePropertyChanged("ArrowDash1StartPoint");
                }
            }
        }

        #endregion

        #region ArrowDash1EndPoint

        private Point _arrowDash1EndPoint = new Point();

        /// <summary>
        /// Gets or sets the ArrowDash1EndPoint property. This observable property 
        /// indicates ....
        /// </summary>
        public Point ArrowDash1EndPoint
        {
            get { return _arrowDash1EndPoint; }
            set
            {
                if (_arrowDash1EndPoint != value)
                {
                    _arrowDash1EndPoint = value;
                    RaisePropertyChanged("ArrowDash1EndPoint");
                }
            }
        }

        #endregion

        #region ArrowDash2StartPoint

        private Point _arrowDash2StartPoint = new Point();

        /// <summary>
        /// Gets or sets the ArrowDash2StartPoint property. This observable property 
        /// indicates ....
        /// </summary>
        public Point ArrowDash2StartPoint
        {
            get { return _arrowDash2StartPoint; }
            set
            {
                if (_arrowDash2StartPoint != value)
                {
                    _arrowDash2StartPoint = value;
                    RaisePropertyChanged("ArrowDash2StartPoint");
                }
            }
        }

        #endregion

        #region ArrowDash2EndPoint

        private Point _arrowDash2EndPoint = new Point();

        /// <summary>
        /// Gets or sets the ArrowDash2EndPoint property. This observable property 
        /// indicates ....
        /// </summary>
        public Point ArrowDash2EndPoint
        {
            get { return _arrowDash2EndPoint; }
            set
            {
                if (_arrowDash2EndPoint != value)
                {
                    _arrowDash2EndPoint = value;
                    RaisePropertyChanged("ArrowDash2EndPoint");
                }
            }
        }

        #endregion

        #region LinkWidth

        private double _linkWidth = 14;

        /// <summary>
        /// Gets or sets the LinkWidth property. This observable property 
        /// indicates ....
        /// </summary>
        public double LinkWidth
        {
            get { return _linkWidth; }
            set
            {
                if (_linkWidth != value)
                {
                    _linkWidth = value;
                    RaisePropertyChanged("LinkWidth");
                }
            }
        }

        #endregion

        internal void Update()
        {
            //QueryCount = Source.QueryCount;
            //UpdateLinkWidth();
            UpdateArrowDashes();
        }
    }
}
