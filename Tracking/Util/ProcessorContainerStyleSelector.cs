using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Tools.FlockingDevice.Tracking.Processor;
using Tools.FlockingDevice.Tracking.ViewModel;

namespace Tools.FlockingDevice.Tracking.Util
{
    public class ProcessorContainerStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is ProcessorViewModelBase<BaseProcessor>)
                return BaseProcessorStyle;

            return null;
        }

        public Style BaseProcessorStyle { get; set; }
    }
}
