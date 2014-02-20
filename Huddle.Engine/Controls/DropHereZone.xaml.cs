using System.Windows;
using System.Windows.Controls;

namespace Huddle.Engine.Controls
{
    /// <summary>
    /// Interaction logic for DropHereZone.xaml
    /// </summary>
    public partial class DropHereZone : UserControl
    {
        #region events

        #endregion

        #region dependeny properties

        #region DropInfo

        public string DropInfo
        {
            get { return (string)GetValue(DropInfoProperty); }
            set { SetValue(DropInfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DropInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DropInfoProperty =
            DependencyProperty.Register("DropInfo", typeof(string), typeof(DropHereZone), new PropertyMetadata("Drop here")); 

        #endregion

        #endregion

        public DropHereZone()
        {
            InitializeComponent();
        }
    }
}
