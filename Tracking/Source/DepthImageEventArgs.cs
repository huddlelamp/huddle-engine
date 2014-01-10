using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Tools.FlockingDevice.Tracking.Source
{
    public class DepthImageEventArgs : ImageEventArgs
    {
        #region properties

        public Image<Gray, double> Image { get; private set; }

        public double MinDepth { get; private set; }

        public double MaxDepth { get; private set; }

        #endregion

        #region ctor

        public DepthImageEventArgs(Image<Gray, double> image, long elapsedTime)
            : base(elapsedTime)
        {
            Image = image;

            double[] minValues;
            double[] maxValues;
            Point[] minLocations;
            Point[] maxLocations;
            image.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

            MinDepth = minValues.Single();
            MaxDepth = maxValues.Single();
        }

        #endregion
    }
}
