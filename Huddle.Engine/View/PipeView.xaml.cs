using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Huddle.Engine.View
{
    /// <summary>
    /// Interaction logic for PipeView.xaml
    /// </summary>
    public partial class PipeView : UserControl
    {
        public PipeView()
        {
            InitializeComponent();
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return base.HitTestCore(hitTestParameters);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return base.HitTestCore(hitTestParameters);
        }

        private void VisualPath_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            //throw new System.NotImplementedException();
            Console.WriteLine();
        }
    }
}
