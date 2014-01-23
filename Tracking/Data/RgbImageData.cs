using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Tools.FlockingDevice.Tracking.Data
{
    public class RgbImageData : ImageData<Rgb, byte>
    {
        #region properties

        #endregion

        #region ctor
        public RgbImageData(Image<Rgb, byte> image)
            : base(image)
        {
        }

        #endregion
    }
}
