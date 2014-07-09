using System.Windows;
using System.Windows.Controls;
using Huddle.Engine.Processor;
using Huddle.Engine.ViewModel;

namespace Huddle.Engine.Util
{
    public class ProcessorContainerStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is ProcessorViewModelBase<BaseProcessor>)
                return FilterStyle;
            else if (item is PipeViewModel)
                return PipeStyle;

            return null;
        }

        public Style FilterStyle { get; set; }

        public Style PipeStyle { get; set; }
    }
}
