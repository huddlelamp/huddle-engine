using System;
using System.Windows.Controls;
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
            return new GeometryHitTestResult(this, HitTestPath.RenderedGeometry.FillContainsWithDetail(hitTestParameters.HitGeometry));
        }
    }
}
