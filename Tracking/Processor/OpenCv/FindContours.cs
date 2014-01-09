using System;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Threading;
using Tools.FlockingDevice.Tracking.Properties;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [ViewTemplate("FindContours")]
    public class FindContours : RgbProcessor
    {
        #region private fields

        private readonly MemStorage _storage = new MemStorage();

        #endregion

        #region properties

        #region IsFillContours

        /// <summary>
        /// The <see cref="IsFillContours" /> property's name.
        /// </summary>
        public const string IsFillContoursPropertyName = "IsFillContours";

        private bool _isFillContours;

        /// <summary>
        /// Sets and gets the IsFillContours property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsFillContours
        {
            get
            {
                return _isFillContours;
            }

            set
            {
                if (_isFillContours == value)
                {
                    return;
                }

                RaisePropertyChanging(IsFillContoursPropertyName);
                _isFillContours = value;
                RaisePropertyChanged(IsFillContoursPropertyName);
            }
        }

        #endregion

        #region SingleTabletSize

        /// <summary>
        /// The <see cref="SingleTabletSize" /> property's name.
        /// </summary>
        public const string SingleTabletSizePropertyName = "SingleTabletSize";

        private double _singleTabletSize = Settings.Default.SingleTabletSize;

        /// <summary>
        /// Sets and gets the SingleTabletSize property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double SingleTabletSize
        {
            get
            {
                return _singleTabletSize;
            }

            set
            {
                if (_singleTabletSize == value)
                {
                    return;
                }

                RaisePropertyChanging(SingleTabletSizePropertyName);
                _singleTabletSize = value;
                RaisePropertyChanged(SingleTabletSizePropertyName);
            }
        }

        #endregion

        #region ApproxPolyAccuracy

        /// <summary>
        /// The <see cref="ApproxPolyAccuracy" /> property's name.
        /// </summary>
        public const string ApproxPolyAccuracyPropertyName = "ApproxPolyAccuracy";

        private double _approxPolyAccuracy = Settings.Default.ApproxPolyAccuracy;

        /// <summary>
        /// Sets and gets the ApproxPolyAccuracy property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double ApproxPolyAccuracy
        {
            get
            {
                return _approxPolyAccuracy;
            }

            set
            {
                if (_approxPolyAccuracy == value)
                {
                    return;
                }

                RaisePropertyChanging(ApproxPolyAccuracyPropertyName);
                _approxPolyAccuracy = value;
                RaisePropertyChanged(ApproxPolyAccuracyPropertyName);
            }
        }

        #endregion

        #endregion

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var outputImage = new Image<Rgb, byte>(image.Size.Width, image.Size.Height, Rgbs.Black);

            #region second path

            //var triangleList = new List<Triangle2DF>();
            //var boxList = new List<MCvBox2D>(); //a box is a rotated rectangle

            //for (var contours2 = cannyEdges.FindContours(); contours2 != null; contours2 = contours2.HNext)
            //{
            //    var currentContour2 = contours2.ApproxPoly(contours2.Perimeter * ApproxPolyAccuracy, _storage);

            //    var hull2 = currentContour2.GetConvexHull(ORIENTATION.CV_CLOCKWISE);

            //    if (currentContour2.Area > 250) //only consider contours with area greater than 250
            //    {
            //        if (currentContour2.Total == 3) //The contour has 3 vertices, it is a triangle
            //        {
            //            var pts = currentContour2.ToArray();
            //            triangleList.Add(new Triangle2DF(
            //                pts[0],
            //                pts[1],
            //                pts[2]
            //                ));

            //            outputImage.FillConvexPoly(hull2.ToArray(), new Rgb(0, 255, 0));
            //        }
            //        else if (currentContour2.Total == 4) //The contour has 4 vertices.
            //        {
            //            #region determine if all the angles in the contour are within [80, 100] degree

            //            var isRectangle = true;
            //            var pts = currentContour2.ToArray();
            //            var edges = PointCollection.PolyLine(pts, true);

            //            for (int i = 0; i < edges.Length; i++)
            //            {
            //                double angle = Math.Abs(
            //                    edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));
            //                if (angle < 80 || angle > 100)
            //                {
            //                    isRectangle = false;
            //                    break;
            //                }
            //            }

            //            #endregion

            //            if (isRectangle)
            //            {
            //                outputImage.FillConvexPoly(hull2.ToArray(), new Rgb(0, 255, 255));
            //                boxList.Add(currentContour2.GetMinAreaRect());
            //            }
            //        }
            //    }
            //    _storage.Clear();
            //}

            #endregion

            var grayImage = image.Convert<Gray, byte>();

            for (var contours = grayImage.FindContours(); contours != null; contours = contours.HNext)
            {
                var currentContour = contours.ApproxPoly(contours.Perimeter * ApproxPolyAccuracy, _storage);

                var hull1 = currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE);

                if (Math.Abs(hull1.Area - SingleTabletSize) < 1000)
                {
                    if (IsFillContours)
                        outputImage.FillConvexPoly(hull1.ToArray(), Rgbs.White);
                    else
                        outputImage.DrawPolyline(hull1.ToArray(), true, Rgbs.White, 1);
                }
                else if (Math.Abs(hull1.Area - SingleTabletSize * 2) < 1000 * 1.5)
                {
                    if (IsFillContours)
                        outputImage.FillConvexPoly(hull1.ToArray(), Rgbs.Yellow);
                    else
                        outputImage.DrawPolyline(hull1.ToArray(), true, Rgbs.Yellow, 1);
                }
                //else
                //{
                //    if (IsFillContours)
                //        outputImage.FillConvexPoly(hull1.ToArray(), Rgbs.Red);
                //    else
                //        outputImage.DrawPolyline(hull1.ToArray(), true, Rgbs.Red, 1);
                //}

                _storage.Clear();
            }

            grayImage.Dispose();

            return outputImage;
        }
    }
}
