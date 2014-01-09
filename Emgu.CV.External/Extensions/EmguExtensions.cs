using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using Emgu.CV.Structure;

namespace Emgu.CV.External.Extensions
{
    public static class EmguExtensions
    {
        public static BitmapSource ToBitmapSource(this IImage image)
        {
            using (var bitmap = image.Bitmap)
            {
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Bmp);

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();

                    return bitmapImage;
                }
            }
        }

        public static IImage Copy(this IImage image)
        {
            return image.CallInternalMethod<IImage>("Copy");
        }

        public static int Width(this IImage image)
        {
            return image.CallInternalProperty<int>("Width");
        }

        public static int Height(this IImage image)
        {
            return image.CallInternalProperty<int>("Height");
        }

        public static void Draw(this IImage image, string message, ref MCvFont font, Point bottomLeft, IColor color)
        {
            image.CallInternalMethod("Draw", new[] { false, true, false, false }, new object[] { message, font, bottomLeft, color });
        }

        public static Image<TColor, TDepth> Convert<TColor, TDepth>(this IImage image)
            where TColor : struct, IColor
            where TDepth : new()
        {
            return image.CallInternalMethod<Image<TColor, TDepth>>("Convert");
        }

        internal static void CallInternalMethod(this IImage image, string methodName, bool[] refTypes, params object[] parameters)
        {
            var types = parameters.Select((t, i) => !refTypes[i] ? t.GetType() : t.GetType().MakeByRefType()).ToArray();

            var type = image.GetType();

            var methodInfo = type.GetMethod(methodName, types);

            methodInfo.Invoke(image, parameters);
        }

        internal static void CallInternalMethod(this IImage image, string methodName, params object[] parameters)
        {
            var types = parameters.Select(parameter => parameter.GetType()).ToArray();

            var type = image.GetType();

            var methodInfo = type.GetMethod(methodName, types);

            methodInfo.Invoke(image, parameters);
        }

        internal static TResult CallInternalMethod<TResult>(this IImage image, string methodName, params object[] parameters)
        {
            var types = parameters.Select(parameter => parameter.GetType()).ToArray();

            var type = image.GetType();

            var methodInfo = type.GetMethod(methodName, types);

            return (TResult)methodInfo.Invoke(image, parameters);
        }

        internal static TResult CallInternalProperty<TResult>(this IImage image, string propertyName)
        {
            var type = image.GetType();

            var propertyInfo = type.GetProperty(propertyName, typeof(TResult));

            return (TResult)propertyInfo.GetValue(image);
        }

        //public static TResult CallInternal<TResult, TColor, TDepth>(this IImage image, Func<Image<TColor, TDepth>, TResult> func)
        //    where TColor : struct, IColor
        //    where TDepth : new()
        //{
        //    var castImage = image as Image<TColor, TDepth>;

        //    if (castImage == null)
        //        throw new InvalidCastException();

        //    return func.Invoke(castImage);
        //}

        //public static Image<TColor, TDepth> Cast<TColor, TDepth>(this IImage image)
        //    where TColor : struct, IColor
        //    where TDepth : new()
        //{
        //    return image as Image<TColor, TDepth>;
        //}
    }
}
