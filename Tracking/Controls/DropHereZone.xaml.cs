using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tools.FlockingDevice.Tracking.Controls
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
