using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;

namespace Tools.FlockingDevice.Tracking.Util
{
    internal static class MVVMUtil
    {
        public static string CreateView(object model)
        {
            var type = model.GetType();

            var viewAttribute = type.GetCustomAttribute<ViewTemplateAttribute>();

            return viewAttribute.TemplateName;
        }
    }

    public class ViewTemplateAttribute : Attribute
    {
        #region properties

        public string TemplateName { get; private set; }

        #endregion

        #region ctor

        public ViewTemplateAttribute(string templateName)
        {
            TemplateName = templateName;
        }

        #endregion
    }
}
