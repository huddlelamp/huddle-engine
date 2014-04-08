using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Fill Convex Hulls", "FillConvexHulls")]
    public class FillConvexHulls : RgbProcessor
    {
        #region properties

        #region MinContourArea

        /// <summary>
        /// The <see cref="MinContourArea" /> property's name.
        /// </summary>
        public const string MinContourAreaPropertyName = "MinContourArea";

        private int _minContourArea = 200;

        /// <summary>
        /// Sets and gets the MinContourArea property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MinContourArea
        {
            get
            {
                return _minContourArea;
            }

            set
            {
                if (_minContourArea == value)
                {
                    return;
                }

                RaisePropertyChanging(MinContourAreaPropertyName);
                _minContourArea = value;
                RaisePropertyChanged(MinContourAreaPropertyName);
            }
        }

        #endregion

        #region IsRetrieveExternal

        /// <summary>
        /// The <see cref="IsRetrieveExternal" /> property's name.
        /// </summary>
        public const string IsRetrieveExternalPropertyName = "IsRetrieveExternal";

        private bool _isRetrieveExternal = false;

        /// <summary>
        /// Sets and gets the IsRetrieveExternal property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsRetrieveExternal
        {
            get
            {
                return _isRetrieveExternal;
            }

            set
            {
                if (_isRetrieveExternal == value)
                {
                    return;
                }

                RaisePropertyChanging(IsRetrieveExternalPropertyName);
                _isRetrieveExternal = value;
                RaisePropertyChanged(IsRetrieveExternalPropertyName);
            }
        }

        #endregion

        #region DebugImageSource

        /// <summary>
        /// The <see cref="DebugImageSource" /> property's name.
        /// </summary>
        public const string DebugImageSourcePropertyName = "DebugImageSource";

        private BitmapSource _debugImageSource;

        /// <summary>
        /// Sets and gets the DebugImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public BitmapSource DebugImageSource
        {
            get
            {
                return _debugImageSource;
            }

            set
            {
                if (_debugImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DebugImageSourcePropertyName);
                _debugImageSource = value;
                RaisePropertyChanged(DebugImageSourcePropertyName);
            }
        }

        #endregion

        #endregion

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var outputImage = new Image<Rgb, byte>(image.Size.Width, image.Size.Height, Rgbs.Black);
            var debugImage = outputImage.Copy();

            //Convert the image to grayscale and filter out the noise
            var grayImage = image.Convert<Gray, Byte>();

            using (var storage = new MemStorage())
            {
                for (var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, IsRetrieveExternal ? RETR_TYPE.CV_RETR_EXTERNAL : RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
                {
                    var currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);

                    //Console.WriteLine("AREA {0}", currentContour.Area);

                    //if (currentContour.Area > MinContourArea) //only consider contours with area greater than 250
                    //{
                        //outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.White, 2);
                        outputImage.FillConvexPoly(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE).ToArray(), Rgbs.White);

                        if (IsRenderContent)
                            debugImage.FillConvexPoly(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE).ToArray(), Rgbs.White);    
                    //}
                    //else
                    //{
                    //    if (IsRenderContent)
                    //        debugImage.FillConvexPoly(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE).ToArray(), Rgbs.Red);
                    //}
                }
            }

            Task.Factory.StartNew(() =>
            {
                var bitmapSource = debugImage.ToBitmapSource(true);
                debugImage.Dispose();
                return bitmapSource;
            }).ContinueWith(t => DebugImageSource = t.Result);

            grayImage.Dispose();

            return outputImage;
        }
    }
}
