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

namespace Tools.FlockingDevice.Tracking.Pages
{
    /// <summary>
    /// Interaction logic for PipelinePage.xaml
    /// </summary>
    public partial class PipelinePage
    {
        public PipelinePage()
        {
            InitializeComponent();
        }

        private void UIElement_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine();

            e.Handled = true;
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine();

            e.MouseDevice.Capture(sender as IInputElement);

            e.Handled = true;
        }
    }
}
