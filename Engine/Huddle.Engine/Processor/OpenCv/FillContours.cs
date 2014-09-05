using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Fill Contours", "FillContours")]
    public class FillContours : RgbProcessor
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
        [IgnoreDataMember]
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
                    var currentContour = contours.ApproxPoly(contours.Perimeter * 0.015, storage);

                    //Console.WriteLine("AREA {0}", currentContour.Area);

                    //if (currentContour.Area > MinContourArea) //only consider contours with area greater than 250
                    //{

                    //outputImage.FillConvexPoly(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE).ToArray(), Rgbs.White);

                    //var p1 = currentContour[0];
                    //currentContour.Push(p1);

                    //outputImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.White, Rgbs.Yellow, 2, -1);

                    outputImage.Draw(currentContour, Rgbs.White, -1);

                    if (IsRenderContent)
                    {

                        //debugImage.DrawPolyline(currentContour.GetMoments().);

                        //debugImage.FillConvexPoly(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE).ToArray(), Rgbs.White);

                        //debugImage.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.White, 4);

                        //debugImage.FillConvexPoly(currentContour.ToArray(), Rgbs.Yellow);


                        //debugImage.Draw(contours, Rgbs.Green, 3);

                        //debugImage = debugImage.Dilate(1);

                        //foreach (var defect in currentContour.GetConvexityDefacts(storage, ORIENTATION.CV_CLOCKWISE).ToArray())
                        //{
                        //    debugImage.Draw(new LineSegment2D(defect.StartPoint, defect.EndPoint), Rgbs.Red, 3);

                        //} 
                    }
                    //}
                    //else
                    //{
                    //    if (IsRenderContent)
                    //        debugImage.FillConvexPoly(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE).ToArray(), Rgbs.Red);
                    //}
                }
            }

            ////Convert the image to grayscale and filter out the noise
            //var grayImage2 = outputImage.Convert<Gray, Byte>();

            //using (var storage = new MemStorage())
            //{
            //    for (var contours = grayImage2.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
            //    {
            //        var currentContour = contours.ApproxPoly(contours.Perimeter * 0.001, storage);

            //        #region determine if all the angles in the contour are within [80, 100] degree

            //        var pts = currentContour.ToArray();
            //        var edges = PointCollection.PolyLine(pts, true);

            //        for (int i = 0; i < edges.Length; i++)
            //        {
            //            for (int j = 0; j < edges.Length; j++)
            //            {
            //                var angle = Math.Abs(edges[i].GetExteriorAngleDegree(edges[j]));

            //                if (angle < 95 || angle > 85)
            //                {
            //                    outputImage.Draw(edges[i], Rgbs.Yellow, 2);
            //                    outputImage.Draw(edges[j], Rgbs.Green, 2);
            //                }
            //            }
            //        }

            //        #endregion
            //    }
            //}

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
