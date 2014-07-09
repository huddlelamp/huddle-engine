using Emgu.CV;
using Emgu.CV.Structure;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public class RgbFloatImage : BaseImageData<Rgb, float>
    {
        #region properties

        #endregion

        #region ctor
        public RgbFloatImage(IProcessor source, string key, Image<Rgb, float> image)
            : base(source, key, image)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new RgbFloatImage(Source, Key, Image.Copy());
        }
    }
}
