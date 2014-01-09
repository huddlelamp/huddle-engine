using Emgu.CV;
using Emgu.CV.Structure;

namespace Tools.FlockingDevice.Tracking.InputSource
{
    public class ColorImageEventArgs : ImageEventArgs
    {
        #region properties

        public Image<Rgb, byte> Image { get; private set; }

        #endregion

        #region ctor

        public ColorImageEventArgs(Image<Rgb, byte> image, long elapsedTime)
            : base(elapsedTime)
        {
            Image = image;
        }

        #endregion
    }
}
