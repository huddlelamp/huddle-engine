using System.Windows;
using System.Windows.Controls;
using Huddle.Engine.Util;

namespace Huddle.Engine.Controls
{
    /// <summary>
    /// Interaction logic for RemovableExpander.xaml
    /// </summary>
    public partial class RemovableExpander : Expander
    {
        #region Routed Events

        #region Remove

        /// <summary>
        /// Remove Routed Event
        /// </summary>
        public static readonly RoutedEvent RemoveEvent = EventManager.RegisterRoutedEvent("Remove",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RemovableExpander));

        /// <summary>
        /// Occurs when ...
        /// </summary>
        public event RoutedEventHandler Remove
        {
            add { AddHandler(RemoveEvent, value); }
            remove { RemoveHandler(RemoveEvent, value); }
        }

        /// <summary>
        /// A helper method to raise the Remove event.
        /// </summary>
        protected RoutedEventArgs RaiseRemoveEvent()
        {
            return RaiseRemoveEvent(this);
        }

        /// <summary>
        /// A static helper method to raise the Remove event on a target element.
        /// </summary>
        /// <param name="target">UIElement or ContentElement on which to raise the event</param>
        internal static RoutedEventArgs RaiseRemoveEvent(DependencyObject target)
        {
            if (target == null) return null;

            RoutedEventArgs args = new RoutedEventArgs();
            args.RoutedEvent = RemoveEvent;
            RoutedEventHelper.RaiseEvent(target, args);
            return args;
        }

        #endregion

        #endregion

        public RemovableExpander()
        {
            InitializeComponent();
        }

        private void RemoveSite_OnClick(object sender, RoutedEventArgs e)
        {
            RaiseRemoveEvent(this);
        }
    }
}
