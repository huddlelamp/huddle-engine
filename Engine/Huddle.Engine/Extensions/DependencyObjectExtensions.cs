using System.Windows;
using System.Windows.Media;

namespace Huddle.Engine.Extensions
{
    public static class DependencyObjectExtensions
    {
        /// <summary>
        /// Recursive method to walk up the visual tree to return an ancestor type of the supplied type.
        /// </summary>
        /// <typeparam name="T">Type of ancestor to search for.</typeparam>
        /// <param name="current">Type to start search from.</param>
        /// <returns></returns>
        public static T FindAncestor<T>(this DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            } while (current != null);
            return null;
        }  
    }
}
