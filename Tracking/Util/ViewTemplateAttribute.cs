using System;

namespace Tools.FlockingDevice.Tracking.Util
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ViewTemplateAttribute : Attribute
    {
        #region const

        private const string DefaultIcon = @"/FlockingDevice.Tracking;component/Resources/drag-icon.png";

        #endregion

        #region properties

        public string Name { get; set; }

        public string Template { get; set; }

        public string Icon { get; set; }

        public Type Type { get; set; }

        #endregion

        #region ctor

        public ViewTemplateAttribute(string name, string template, string icon = DefaultIcon)
        {
            Name = name;
            Template = template;
            Icon = icon;
        }

        #endregion
    }
}
