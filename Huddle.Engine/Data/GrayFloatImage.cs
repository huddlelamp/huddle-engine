using Emgu.CV;
using Emgu.CV.Structure;

namespace Huddle.Engine.Data
{
    public class GrayFloatImage : BaseImageData<Gray, float>
    {
        #region properties

        #endregion

        #region ctor
        public GrayFloatImage(string key, Image<Gray, float> image)
            : base(key, image)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new GrayFloatImage(Key, Image.Copy());
        }
    }
}
