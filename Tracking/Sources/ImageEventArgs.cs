using System;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Tools.FlockingDevice.Tracking.Sources
{
    public class ImageEventArgs : EventArgs
    {
        #region properties

        public Image<Rgb, byte> ColorImage { get; private set; }

        public Image<Rgb, byte> DepthImage { get; private set; }

        public long ElapsedTime { get; private set; }

        public double Fps
        {
            get
            {
                return 1000.0 / ElapsedTime;
            }
        }

        #endregion

        #region ctor

        public ImageEventArgs(Image<Rgb, byte> colorImage, Image<Rgb, byte> depthImage, long elapsedTime)
        {
            ColorImage = colorImage;
            DepthImage = depthImage;
            ElapsedTime = elapsedTime;
        }

        #endregion
    }
}
