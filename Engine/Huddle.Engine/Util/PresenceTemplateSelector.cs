using System.Windows;
using System.Windows.Controls;
using Huddle.Engine.Data;

namespace Huddle.Engine.Util
{
    public class PresenceTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DeviceTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var device = item as Device;

            if (device != null)
            {
                return DeviceTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
