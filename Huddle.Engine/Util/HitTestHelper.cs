using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Huddle.Engine.Util
{

    public static class HitTestHelper
    {
        #region Point Hit Testing - Topmost Element

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="point"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static DependencyObject GetTopmostElementAtPoint(Visual reference, Point point, Type targetType)
        {
            HitTestResult result = VisualTreeHelper.HitTest(reference, point);

            if (result.VisualHit.GetType() == targetType)
                return result.VisualHit;
            else
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static T GetTopmostElementAtPoint<T>(Visual reference, Point point) where T : DependencyObject
        {
            HitTestResult result = VisualTreeHelper.HitTest(reference, point);

            if (result.VisualHit == null)
                return null;
            if (result.VisualHit is T)
                return result.VisualHit as T;
            else
                return TreeHelper.TryFindParent<T>(result.VisualHit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static DependencyObject GetTopmostElementAtPoint(Visual reference, Point point)
        {
            HitTestResult result = VisualTreeHelper.HitTest(reference, point);

            return result.VisualHit;
        }

        #endregion

        #region Point Hit Testing - All Elements

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="point"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public static IEnumerable<DependencyObject> GetElementsAtPoint(Visual reference, Point point, Type targetType)
        {
            List<DependencyObject> results = new List<DependencyObject>();

            HitTestFilterCallback filter = (o) =>
            {
                return HitTestFilterBehavior.Continue;
            };

            HitTestResultCallback result = (r) =>
            {
                if (r.VisualHit.GetType() == targetType)
                    results.Add(r.VisualHit);

                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(reference, filter, result, new PointHitTestParameters(point));

            return results;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetElementsAtPoint<T>(Visual reference, Point point) where T : DependencyObject
        {
            List<T> results = new List<T>();

            HitTestFilterCallback filter = (o) =>
            {
                return HitTestFilterBehavior.Continue;
            };

            HitTestResultCallback result = (r) =>
            {
                if (r.VisualHit is T)
                    results.Add(r.VisualHit as T);

                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(reference, filter, result, new PointHitTestParameters(point));

            return results;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static IEnumerable<DependencyObject> GetElementsAtPoint(Visual reference, Point point)
        {
            List<DependencyObject> results = new List<DependencyObject>();

            HitTestFilterCallback filter = (o) =>
            {
                return HitTestFilterBehavior.Continue;
            };

            HitTestResultCallback result = (r) =>
            {
                if (r.VisualHit != null)
                    results.Add(r.VisualHit);

                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(reference, filter, result, new PointHitTestParameters(point));

            return results;
        }

        #endregion

        #region Geometry Hit Testing

        /// <summary>
        /// Returns a collection of Elements of type targetType that are located in the area of the Geometry geo in the subtree of the UIElement reference
        /// </summary>
        /// <param name="geo">the geometry which will be tested</param>
        /// <param name="reference">specifies the subtree that will be tested</param>
        /// <param name="targetType">specifies the type of elements that will be returned (subclasses of this type will also be returned)</param>
        /// <returns>A collection of Elements that are found by hittesting with the given parameters</returns>
        public static IEnumerable<DependencyObject> GetElementsInGeometry(Geometry geo, Visual reference, Type targetType)
        {
            List<DependencyObject> results = new List<DependencyObject>();

            HitTestFilterCallback filter = (o) =>
            {
                Type matchType = o.GetType();

                //if we find an element of correct type, we can ignore its subtree
                //if we find an element of incorrect type, we can ignore it, but must check its children

                if (matchType == targetType || matchType.IsSubclassOf(targetType))
                    return HitTestFilterBehavior.ContinueSkipChildren;
                else
                    return HitTestFilterBehavior.ContinueSkipSelf;
            };

            HitTestResultCallback result = (r) =>
            {
                GeometryHitTestResult ghr = r as GeometryHitTestResult;

                if (ghr.IntersectionDetail == IntersectionDetail.Empty)
                    return HitTestResultBehavior.Continue;

                Type matchType = r.VisualHit.GetType();

                //again we check the type of the element, just to be sure that only correct types go into our collection

                if (matchType == targetType || matchType.IsSubclassOf(targetType))
                    results.Add(r.VisualHit);

                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(reference, filter, result, new GeometryHitTestParameters(geo));

            return results;
        }

        /// <summary>
        /// Returns a collection of Elements of type T that are located in the area of the Geometry geo in the subtree of the UIElement reference
        /// </summary>
        /// <typeparam name="T">specifies the type of elements that will be returned (subclasses of this type will also be returned)</typeparam>
        /// <param name="geo">the geometry which will be tested</param>
        /// <param name="reference">specifies the subtree that will be tested</param>
        /// <returns>A collection of Elements that are found by hittesting with the given parameters</returns>
        public static IEnumerable<T> GetElementsInGeometry<T>(Geometry geo, Visual reference) where T : DependencyObject
        {
            var results = new List<T>();

            if (geo == null)
                return results;

            HitTestFilterCallback filter = o =>
                {
                    //if we find an element of correct type, we can ignore its subtree
                    //if we find an element of incorrect type, we can ignore it, but must check its children

                    if (o is T)
                        return HitTestFilterBehavior.ContinueSkipChildren;

                    return HitTestFilterBehavior.ContinueSkipSelf;
                };

            HitTestResultCallback result = r =>
            {
                //again we check the type of the element, just to be sure that only correct types go into our collection

                var ghr = r as GeometryHitTestResult;

                if (ghr.IntersectionDetail == IntersectionDetail.Empty)
                    return HitTestResultBehavior.Continue;

                if (r.VisualHit is T)
                    results.Add(r.VisualHit as T);

                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(reference, filter, result, new GeometryHitTestParameters(geo));

            return results;
        }

        /// <summary>
        /// Returns a collection of Elements that are located in the area of the Geometry geo in the subtree of the UIElement reference
        /// Note: Elements will be put into the collection in the order that they appear in the visual tree (aka ZOrder)
        /// </summary>
        /// <param name="geo">the geometry which will be tested</param>
        /// <param name="reference">specifies the subtree that will be tested</param>
        /// <returns>A collection of Elements that are found by hittesting with the given parameters</returns>
        public static IEnumerable<DependencyObject> GetElementsInGeometry(Geometry geo, Visual reference)
        {
            var results = new List<DependencyObject>();

            HitTestFilterCallback filter = (o) =>
            {
                return HitTestFilterBehavior.Continue;
            };

            HitTestResultCallback result = (r) =>
            {
                if (r.VisualHit != null)
                    results.Add(r.VisualHit);

                return HitTestResultBehavior.Continue;
            };

            VisualTreeHelper.HitTest(reference, filter, result, new GeometryHitTestParameters(geo));

            return results;
        }

        #endregion
    }


}
