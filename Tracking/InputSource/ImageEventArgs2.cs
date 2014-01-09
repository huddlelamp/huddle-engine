using System;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Tools.FlockingDevice.Tracking.InputSource
{
    public class ImageEventArgs2 : EventArgs
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

        public ImageEventArgs2(Image<Rgb, byte> colorImage, Image<Rgb, byte> depthImage, long elapsedTime)
        {
            ColorImage = colorImage;
            DepthImage = depthImage;
            ElapsedTime = elapsedTime;
        }

        #endregion
    }
}
