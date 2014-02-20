using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Huddle.Engine.Util
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

                    try
                    {
                        return element.FindResource(viewAttribute.Template) as DataTemplate;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(@"DataTemplate not found for {0}. Add template to App.xaml.", viewAttribute.Template);
                    }
                }
            }
            catch
            {
                Console.WriteLine(@"DataTemplate not found for {0}. Add template to App.xaml.", item.GetType().Name);
            }
            return base.SelectTemplate(item, container);
        }
    }
}
