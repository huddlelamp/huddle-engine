using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Tools.FlockingDevice.Tracking.Extensions;

namespace Tools.FlockingDevice.Tracking.InkCanvas
{
    public class AdvancedInkCanvas : Canvas
    {
        public AdvancedInkCanvas()
        {
            Loaded += OnLoaded;
        }

        #region private fields

        private StrokeVisual _mouseStrokeVisual;

        private StrokeVisualAdorner _strokeVisualAdorner;

        private AdornerLayer _adornerLayer;

        private bool _drawing;

        #endregion private fields

        #region events

        public event EventHandler<StrokeEventArgs> StrokeCollected;

        #endregion events

        #region dp

        #region IsDrawingEnabled

        public bool IsDrawingEnabled
        {
            get { return (bool)GetValue(IsDrawingEnabledProperty); }
            set { SetValue(IsDrawingEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsDrawingEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsDrawingEnabledProperty = DependencyProperty.Register("IsDrawingEnabled", typeof(bool), typeof(AdvancedInkCanvas), new UIPropertyMetadata(false, OnIsDrawingEnabledChanged));

        private static void OnIsDrawingEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var inkCanvas = sender as AdvancedInkCanvas;
            if (inkCanvas == null) return;

            var newValue = (bool)e.NewValue;

            if (newValue)
            {
                inkCanvas.RemoveEventHandler();
                inkCanvas.AddEventHandler();
            }
            else
            {
                inkCanvas.RemoveEventHandler();
            }
        }

        #endregion IsDrawingEnabled

        #region IsMouseEnabled

        /// <summary>
        /// IsMouseEnabled Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsMouseEnabledProperty = DependencyProperty.Register("IsMouseEnabled", typeof(bool), typeof(AdvancedInkCanvas), new FrameworkPropertyMetadata(false, OnIsMouseEnabledChanged));

        /// <summary>
        /// Gets or sets the IsMouseEnabled property. This dependency property 
        /// indicates ....
        /// </summary>
        public bool IsMouseEnabled
        {
            get { return (bool)GetValue(IsMouseEnabledProperty); }
            set { SetValue(IsMouseEnabledProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsMouseEnabled property.
        /// </summary>
        private static void OnIsMouseEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (AdvancedInkCanvas)d;
            var oldIsMouseEnabled = (bool)e.OldValue;
            var newIsMouseEnabled = target.IsMouseEnabled;
            target.OnIsMouseEnabledChanged(oldIsMouseEnabled, newIsMouseEnabled);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the IsMouseEnabled property.
        /// </summary>
        protected virtual void OnIsMouseEnabledChanged(bool oldIsMouseEnabled, bool newIsMouseEnabled)
        {
        }

        #endregion

        #region Scale

        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        #endregion

        #endregion dp

        #region private methods

        private void AddEventHandler()
        {
            //TouchDown += OnTouchDown;
            //TouchMove += OnTouchMove;
            //TouchUp += OnTouchUp;
            ////TouchLeave += OnTouchUp;

            //StylusDown += OnStylusDown;
            //StylusMove += OnStylusMove;
            //StylusUp += OnStylusUp;
            //StylusOutOfRange += OnStylusOutOfRange;
            //StylusInAirMove += OnStylusInAirMove;

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
        }

        private void RemoveEventHandler()
        {
            //TouchDown -= OnTouchDown;
            //TouchMove -= OnTouchMove;
            //TouchUp -= OnTouchUp;
            //TouchLeave -= OnTouchUp;

            //StylusDown -= OnStylusDown;
            //StylusMove -= OnStylusMove;
            //StylusUp -= OnStylusUp;

            MouseDown -= OnMouseDown;
            MouseMove -= OnMouseMove;
            MouseUp -= OnMouseUp;
        }

        #endregion private methods

        #region event handling

        private void OnLoaded(object sender, EventArgs e)
        {
            if (_strokeVisualAdorner == null)
            {
                _adornerLayer = AdornerLayer.GetAdornerLayer(this);
                _strokeVisualAdorner = new StrokeVisualAdorner(this);
                _adornerLayer.Add(_strokeVisualAdorner);
            }
        }

        #region Touch Events

        //private void OnTouchDown(object sender, TouchEventArgs e)
        //{
        //    if (e.TouchDevice.IsFingerRecognized() && e.Source == this)
        //    {
        //        var currentPoint = TouchExtensions.GetCenterPosition(e.TouchDevice, this);
        //        var sv = new StrokeVisual(currentPoint, Colors.Black);

        //        sv.TouchUp += OnTouchUp;
        //        sv.TouchMove += OnTouchMove;

        //        _strokeVisualAdorner.Add(sv);

        //        TouchExtensions.SetUserData(e.TouchDevice, StrokeVisualKey, sv);

        //        e.TouchDevice.Capture(sv);

        //        e.Handled = true;
        //    }
        //}

        //private void OnTouchMove(object sender, TouchEventArgs e)
        //{
        //    if (e.TouchDevice.IsFingerRecognized())
        //    {
        //        var currentPoint = TouchExtensions.GetCenterPosition(e.TouchDevice, this);

        //        var sv = e.TouchDevice.GetUserData<StrokeVisual>(StrokeVisualKey);
        //        if (sv != null)
        //            sv.AddPoint(currentPoint);

        //        e.Handled = true;
        //    }
        //}

        //private void OnTouchUp(object sender, TouchEventArgs e)
        //{
        //    var sv = e.TouchDevice.GetUserData<StrokeVisual>(StrokeVisualKey);

        //    if (sv != null)
        //    {
        //        sv.TouchUp -= OnTouchUp;
        //        sv.TouchLeave -= OnTouchUp;

        //        _strokeVisualAdorner.Remove(sv);

        //        var points = new StylusPointCollection(sv.Points);

        //        var s = new Stroke(points);

        //        if (s.GetBounds().Width > 10 || s.GetBounds().Height > 10)
        //        {
        //            if (StrokeCollected != null)
        //                StrokeCollected(this, new StrokeEventArgs(Device.Touch, s));
        //        }
        //        e.TouchDevice.Capture(null);

        //        e.Handled = true;
        //    }
        //}

        #endregion

        #region Stylus Events

        //private StrokeVisual _sv;
        //private bool _inverted = false;

        //private void OnStylusDown(object sender, StylusEventArgs e)
        //{
        //    if (!Equals(e.Source, this) || !e.StylusDevice.GetIsStylusRecognized()) return;

        //    _strokeVisualAdorner.HideGestureHelp();

        //    var currentPoint = e.StylusDevice.GetPosition(this);

        //    _inverted = e.Inverted;
        //    _sv = new StrokeVisual(currentPoint, !e.Inverted ? Colors.DeepPink : Colors.DeepSkyBlue);

        //    _sv.StylusUp += OnStylusUp;
        //    _sv.StylusMove += OnStylusMove;

        //    _strokeVisualAdorner.Add(_sv);

        //    e.StylusDevice.Capture(_sv);

        //    e.Handled = true;
        //}

        //private void OnStylusMove(object sender, StylusEventArgs e)
        //{
        //    if (!e.StylusDevice.GetIsStylusRecognized()) return;

        //    // Use in air to show available gestures
        //    if (!e.InAir)
        //    {
        //        var currentPoint = e.StylusDevice.GetPosition(this);

        //        if (_sv != null)
        //            _sv.AddPoint(currentPoint);

        //        e.Handled = true;
        //    }
        //}

        //private void OnStylusUp(object sender, StylusEventArgs e)
        //{
        //    if (_sv == null || !e.StylusDevice.GetIsStylusRecognized()) return;

        //    _sv.StylusUp -= OnStylusUp;
        //    _sv.StylusMove -= OnStylusUp;

        //    _strokeVisualAdorner.Remove(_sv);

        //    var points = new StylusPointCollection(_sv.Points);

        //    _sv = null;

        //    var s = new Stroke(points);

        //    if (s.GetBounds().Width > 10 || s.GetBounds().Height > 10)
        //    {
        //        if (StrokeCollected != null)
        //            StrokeCollected(this, new StrokeEventArgs(_inverted ? Device.StylusInverted : Device.Stylus, s));
        //    }
        //    e.StylusDevice.Capture(null);

        //    e.Handled = true;
        //}

        //void OnStylusInAirMove(object sender, StylusEventArgs e)
        //{
        //    _strokeVisualAdorner.ShowGestureHelp(e.GetPosition(null));
        //}

        //void OnStylusOutOfRange(object sender, StylusEventArgs e)
        //{
        //    _strokeVisualAdorner.HideGestureHelp();
        //}

        #endregion

        #region Mouse Events

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released || (Keyboard.Modifiers > 0))
                return;

            _mouseStrokeVisual = new StrokeVisual(e.GetPosition(this), Colors.Black);
            _strokeVisualAdorner.Add(_mouseStrokeVisual);
            _drawing = true;
            CaptureMouse();

            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_drawing)
            {
                var currentPoint = e.GetPosition(this);
                _mouseStrokeVisual.AddPoint(currentPoint);

                e.Handled = true;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_drawing)
            {
                _strokeVisualAdorner.Remove(_mouseStrokeVisual);

                var points = new StylusPointCollection(_mouseStrokeVisual.Points);

                var s = new Stroke(points);

                if (StrokeCollected != null)
                    StrokeCollected(this, new StrokeEventArgs(Device.Mouse, s));

                _drawing = false;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        #endregion

        #endregion event handling

        #region static

        private static readonly object StrokeVisualKey = new Object();
        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register("Scale", typeof (double), typeof (AdvancedInkCanvas), new PropertyMetadata(default(double)));

        #endregion static
    }

    internal class StrokeVisualAdorner : Adorner
    {
        private readonly Canvas _strokeVisualCanvas;

        private readonly Canvas _dummyCanvas;
        private readonly UserControl _gestureHelp;

        public StrokeVisualAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            //var s = Settings.Default;

            _strokeVisualCanvas = new Canvas
            {
                //Width = s.DisplayWidth,
                //Height = s.DisplayHeight
            };

            _visuals = new VisualCollection(this);

            AddVisualChild(_strokeVisualCanvas);

            _gestureHelp = new GestureHelpControl { Visibility = Visibility.Collapsed };

            _dummyCanvas = new Canvas
            {
                //Width = s.DisplayWidth,
                //Height = s.DisplayHeight
            };
            _dummyCanvas.Children.Add(_gestureHelp);

            _visuals.Add(_dummyCanvas);
        }

        public void ShowGestureHelp(Point position)
        {
            Canvas.SetLeft(_gestureHelp, position.X - _gestureHelp.ActualWidth / 2);
            Canvas.SetTop(_gestureHelp, position.Y - _gestureHelp.ActualHeight / 2);

            _gestureHelp.Visibility = Visibility.Visible;
        }

        public void HideGestureHelp()
        {
            _gestureHelp.Visibility = Visibility.Collapsed;
        }

        public void Add(StrokeVisual sv)
        {
            //_strokeVisualCanvas.Targets.Add(sv);
            _visuals.Add(sv);
        }

        public void Remove(StrokeVisual sv)
        {
            //_strokeVisualCanvas.Targets.Remove(sv);
            _visuals.Remove(sv);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _strokeVisualCanvas.Arrange(new Rect(finalSize));
            _dummyCanvas.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _strokeVisualCanvas.Measure(constraint);
            _dummyCanvas.Measure(constraint);
            return _strokeVisualCanvas.DesiredSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }

        private readonly VisualCollection _visuals;

        protected override int VisualChildrenCount
        {
            get { return _visuals.Count; }
        }
    }
}