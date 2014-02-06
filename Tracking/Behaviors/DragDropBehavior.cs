using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Tools.FlockingDevice.Tracking.Behaviors
{
    public class DragDropBehavior : Behavior<FrameworkElement>
    {
        #region private fields

        private Window _window;

        private Point _position;

        #endregion

        #region dependency properties

        #region X

        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        // Using a DependencyProperty as the backing store for X.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(DragDropBehavior), new PropertyMetadata(0.0));

        #endregion

        #region Y

        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Y.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(DragDropBehavior), new PropertyMetadata(0.0));

        #endregion

        #region Angle

        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(DragDropBehavior), new PropertyMetadata(0.0));

        #endregion

        #region Container

        public IInputElement Container
        {
            get { return (IInputElement)GetValue(ContainerProperty); }
            set { SetValue(ContainerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Container.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContainerProperty =
            DependencyProperty.Register("Container", typeof(IInputElement), typeof(DragDropBehavior), new PropertyMetadata(null));

        #endregion

        #endregion

        #region properties

        #region IsRotationEnabled

        private bool _isRotationEnabled = true;

        public bool IsRotationEnabled
        {
            get { return _isRotationEnabled; }
            set { _isRotationEnabled = value; }
        }

        #endregion

        #endregion

        #region overrides

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.MouseDown += OnInputDown;
            AssociatedObject.MouseUp += OnInputUp;
            AssociatedObject.MouseMove += OnInputMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseDown -= OnInputDown;
            AssociatedObject.MouseUp -= OnInputUp;
            AssociatedObject.MouseMove -= OnInputMove;

            base.OnDetaching();
        }

        #endregion

        #region event handling

        private void OnInputDown(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            _window = Window.GetWindow(AssociatedObject);

            e.Handled = true;

            _position = e.GetPosition(null);

            e.MouseDevice.Capture(AssociatedObject, CaptureMode.SubTree);
        }

        private void OnInputUp(object sender, MouseEventArgs e)
        {
            AssociatedObject.ReleaseMouseCapture();

            e.Handled = true;
        }

        private void OnInputMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            #region Movement

            var currentPosition = e.GetPosition(null);
            var delta = currentPosition - _position;

            X += delta.X;
            Y += delta.Y;

            _position = currentPosition;

            #endregion

            #region Rotation

            //Angle += e.DeltaManipulation.Rotation;

            #endregion
        }

        #endregion
    }
}