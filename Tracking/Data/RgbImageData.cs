using Emgu.CV;
using Emgu.CV.Structure;

namespace Tools.FlockingDevice.Tracking.Data
{
    public class RgbImageData : BaseImageData<Rgb, byte>
    {
        #region properties

        #endregion

        #region ctor
        public RgbImageData(string key, Image<Rgb, byte> image)
            : base(key, image)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new RgbImageData(Key, Image.Copy());
        }
    }
}
