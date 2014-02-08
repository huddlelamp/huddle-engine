using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Tools.FlockingDevice.Tracking.Util
{
    public static class TreeHelper
    {
        #region Finding children in the tree

        /// <summary>
        /// Tries to find the child with the given Type T of a given object in the visual tree. Returns null if no child of Type T is found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T TryFindChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T childOfChild = TryFindChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        /// <summary>
        /// Tries to find all children of Type T of a given object in the visual tree. Returns an empty list if no child was found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<T> TryFindChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            List<T> children = new List<T>();

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                {
                    children.Add((T)child);
                }
                else
                {
                    IEnumerable<T> childOfChild = TryFindChildren<T>(child);
                    if (childOfChild != null)
                        children.AddRange(childOfChild);
                }
            }
            return children;
        }

        #endregion

        #region Finding parents in the tree

        /// <summary>
        /// Tries to find a parent of the given object with type T. If the end of the tree is reached null is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="child"></param>
        /// <returns></returns>
        public static T TryFindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we’ve reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                //use recursion to proceed with next level
                return TryFindParent<T>(parentObject);
            }
        }

        /// <summary>
        /// Returns a list of all parents of type T.
        /// If no parent element of type T is found, returns an empty list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="child"></param>
        /// <returns></returns>
        public static IEnumerable<T> TryFindParents<T>(DependencyObject child) where T : DependencyObject
        {
            List<T> parents = new List<T>();

            T parent = TreeHelper.TryFindParent<T>(child);

            while (parent != null)
            {
                parents.Add(parent);
                parent = TryFindParent<T>(parent);
            }

            return parents;
        }

        /// <summary>
        /// Tries to find a parent FrameworkElement by name. Returns null if none could be found.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FrameworkElement TryFindParentByName(FrameworkElement child, String name)
        {
            if (child == null)
                throw new ArgumentNullException("child");

            DependencyObject parent = child.Parent;

            //if the end of the tree is reached return null
            if (child.Parent == null)
                return null;

            FrameworkElement parentFE = parent as FrameworkElement;

            //if the parent is no FrameworkElement return null
            if (parentFE == null)
                return null;

            //if parent name matches searched name return parent, else go one step higher
            if (parentFE.Name == name)
                return parentFE;
            else return
                TryFindParentByName(parentFE, name);
        }

        #endregion

        #region matrix calculations

        /// <summary>
        /// Calculates a matrix to transform window coordinates into the element's coordinate system.
        /// </summary>
        /// <param name="element">Target element.</param>
        /// <returns>Transformation matrix.</returns>
        public static MatrixTransform GetGlobalTransformation(Visual element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            Window window = Window.GetWindow(element);
            return element.TransformToAncestor(window) as MatrixTransform;
        }

        /// <summary>
        /// Calculates the element's rendered size.
        /// </summary>
        /// <param name="element">The element to measure.</param>
        /// <returns>The rendered size.</returns>
        public static Size GetRenderedSize(FrameworkElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            double width = element.ActualWidth;
            double height = element.ActualHeight;

            MatrixTransform t = GetGlobalTransformation(element);

            Point upperLeft = t.Transform(new Point(0, 0));
            Point upperRight = t.Transform(new Point(width, 0));
            Point lowerLeft = t.Transform(new Point(0, height));
            Point lowerRight = t.Transform(new Point(width, height));

            List<Point> allBoundingPoints = new List<Point>() { upperLeft, upperRight, lowerLeft, lowerRight };

            var minBoundingX = allBoundingPoints.Min(point => point.X);
            var minBoundingY = allBoundingPoints.Min(point => point.Y);
            var maxBoundingX = allBoundingPoints.Max(point => point.X);
            var maxBoundingY = allBoundingPoints.Max(point => point.Y);

            Size s = new Size(maxBoundingX - minBoundingX, maxBoundingY - minBoundingY);

            return s;
        }

        #endregion
    }
}
