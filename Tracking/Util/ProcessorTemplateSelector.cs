using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Tools.FlockingDevice.Tracking.Util
{
    public class ProcessorTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            try
            {
                var element = container as FrameworkElement;

                if (element != null && item != null)
                {
                    var type = item.GetType();

                    var viewAttribute = type.GetCustomAttribute<ViewTemplateAttribute>();

                    return element.FindResource(viewAttribute.TemplateName) as DataTemplate;
                }
            }
            catch
            {
                // empty
            }
            return base.SelectTemplate(item, container);
        }
    }
}
