using System.Windows;
using System.Windows.Input;

namespace Huddle.Engine.Pages
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
