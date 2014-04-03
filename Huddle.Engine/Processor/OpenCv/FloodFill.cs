using System;
using System.Drawing;
using System.Windows.Navigation;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.GPU;
using Emgu.CV.Structure;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Flood Fill", "FloodFill")]
    public class FloodFill : BaseImageProcessor<Gray, float>
    {
        #region properties

        /// <summary>
        /// The <see cref="Difference" /> property's name.
        /// </summary>
        public const string DifferencePropertyName = "Difference";

        private float _difference = 25.0f;

        /// <summary>
        /// Sets and gets the Difference property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float Difference
        {
            get
            {
                return _difference;
            }

            set
            {
                if (_difference == value)
                {
                    return;
                }

                RaisePropertyChanging(DifferencePropertyName);
                _difference = value;
                RaisePropertyChanged(DifferencePropertyName);
            }
        }

        #endregion

        public override Image<Gray, float> ProcessAndView(Image<Gray, float> image)
        {
            var imageCopy = image.Copy();

            //var gpuImage = new GpuImage<Gray, float>(image);

            //Image<Gray, byte> grayImage = new Image<Gray, byte>(image.Width, image.Height);
            //if (GpuInvoke.HasCuda)
            //{
            //    var gpuGrayImage = gpuImage.Convert<Gray, byte>();
            //    grayImage = gpuGrayImage.ToImage();
            //}
            //else
            //grayImage = image.Convert<Gray, byte>();

            //Convert the image to grayscale and filter out the noise
            //var grayImage = image.Convert<Gray, byte>(new Image<Gray, Gray>(image.Width, image.Height), (f, gray) =>
            //{
            //    return 123;
            //});

            //for (int y = 0; y < image.Height; y++)
            //{
            //    for (int x = 0; x < image.Width; x++)
            //    {
            //        byte val = (byte)image[y, x].Intensity;
            //        grayImage[y, x] = new Gray(val);
            //    }
            //}

            //grayImage._EqualizeHist();
            //grayImage.Erode(10);//.Dilate(2);

            // Dispose old image
            //image.Dispose();

            //if (GaussianPyramidDownUpDecomposition)
            //    grayImage = grayImage.PyrDown().PyrUp();

            //grayImage = grayImage.SmoothGaussian(15);

            MCvConnectedComp comp;
            CvInvoke.cvFloodFill(imageCopy.Ptr, new Point(image.Width / 2, image.Height / 2), new MCvScalar(0), new MCvScalar(Difference), new MCvScalar(Difference), out comp, CONNECTIVITY.EIGHT_CONNECTED, FLOODFILL_FLAG.DEFAULT, IntPtr.Zero);

            //var cannyEdges = grayImage.Canny(Threshold, ThresholdLinking);

            //var output = new Image<Gray, float>(image.Width, image.Height);

            //using (var storage = new MemStorage())
            //{
            //    //var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, IsRetrieveExternal ? RETR_TYPE.CV_RETR_EXTERNAL : RETR_TYPE.CV_RETR_LIST, storage);
            //    //Parallel.ForEach(IterateContours(contours, storage), contour =>
            //    //{

            //    //});
            //    for (
            //        var contours = cannyEdges.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage);
            //        contours != null;
            //        contours = contours.HNext)
            //    {
            //        var currentContour = contours.ApproxPoly(contours.Perimeter*0.05, storage);                  

            //            output.Draw(currentContour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), new Gray(255), 2);
            //    }
            //}

            var image1 = imageCopy.Convert<Gray, float>();

            // Dispose gray image
            image.Dispose();

            return image1;
        }
    }
}
