using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;

namespace Emgu.CV.External.Extensions
{
    public static class EmguExtensions
    {
        #region static members

        public const float DefaultLowConfidence = 32001.0f;
        public const float DefaultSaturation = 32002.0f;

        public static float LowConfidence = DefaultLowConfidence;
        public static float Saturation = DefaultSaturation;

        #endregion

        private static readonly Rgb[] Gradient = CreateGradient();

        private static Rgb[] CreateGradient()
        {
            var gradient = new Rgb[4 * 256];
            int i = 0;

            /* Blue 0,0,255 --> Cyan 0,255,255 */
            for (int j = 0; j < 256; j++) gradient[i++] = new Rgb(0, j, 255);

            /* Cyan 0,255,255 --> Green 0,255,0  */
            for (int j = 0; j < 256; j++) gradient[i++] = new Rgb(0, 255, 255 - j);

            /* Green 0,255,0 --> Yellow 255,255,0 */
            for (int j = 0; j < 256; j++) gradient[i++] = new Rgb(j, 255, 0);

            /* Yellow 255,255,0 --> Red 255,0,0 */
            for (int j = 0; j < 256; j++) gradient[i++] = new Rgb(255, 255 - j, 0);

            return gradient;
        }

        public static BitmapSource ToBitmapSource(this IImage image, bool freeze = false)
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

                    if (freeze)
                        bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
        }
        public static BitmapSource ToGradientBitmapSourceForHandTracking(this Image<Gray, float> image, bool freeze = true, float lowConfidence = DefaultLowConfidence, float saturation = DefaultSaturation)
        {
            var width = image.Width;
            var height = image.Height;

            var gradientImage = new Image<Rgb, byte>(width, height, Rgbs.White);
            var imageData = image.Data;


            Parallel.For(0, height, y =>
            {
                Rgb color;
                for (var x = 0; x < width; x++)
                {
                    var depth = imageData[y, x, 0];

                    if (depth > 10000)
                    {
                        color = Rgbs.Black;
                    }
                    else
                    {
                        var index = (int)((Gradient.Length - 1) * (255.0 - depth) / 255.0);

                        if (index < 0 || index > Gradient.Length - 1)
                            color = Rgbs.White;
                        else
                            color = Gradient[index];
                    }

                    gradientImage[y, x] = color;
                }
            });

            return gradientImage.ToBitmapSource();
        }

        public static BitmapSource ToGradientBitmapSource(this Image<Gray, float> image, bool freeze = false, float lowConfidence = DefaultLowConfidence, float saturation = DefaultSaturation)
        {
            var width = image.Width;
            var height = image.Height;

            var imageData = image.Data;
            var gradientImage = new Image<Rgb, byte>(width, height, Rgbs.White);

            Parallel.For(0, height, y =>
            {
                Rgb color;
                for (int x = 0; x < width; x++)
                {
                    var depth = imageData[y, x, 0];

                    if (depth == LowConfidence)
                    {
                        color = Rgbs.Black;
                    }
                    else if (depth == Saturation)
                    {
                        color = Rgbs.White;
                    }
                    else
                    {
                        var index = (int)((Gradient.Length - 1) * (255.0 - depth) / 255.0);
                        if (index < 0) index = 0;
                        if (index > Gradient.Length) index = Gradient.Length - 1;
                        color = Gradient[index];
                    }
                    gradientImage[y, x] = color;
                }
            });

            return gradientImage.ToBitmapSource(freeze);
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

        public static double Length(this PointF self, PointF point)
        {
            return Math.Sqrt(Math.Pow(self.X - point.X, 2) + Math.Pow(self.Y - point.Y, 2));
        }
    }
}
