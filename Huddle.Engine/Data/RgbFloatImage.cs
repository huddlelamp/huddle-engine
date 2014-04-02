using Emgu.CV;
using Emgu.CV.Structure;

namespace Huddle.Engine.Data
{
    public class RgbFloatImage : BaseImageData<Rgb, float>
    {
        #region properties

        #endregion

        #region ctor
        public RgbFloatImage(string key, Image<Rgb, float> image)
            : base(key, image)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new RgbFloatImage(Key, Image.Copy());
        }
    }
}
