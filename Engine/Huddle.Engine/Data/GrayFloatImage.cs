using Emgu.CV;
using Emgu.CV.Structure;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public sealed class GrayFloatImage : BaseImageData<Gray, float>
    {
        #region properties

        #endregion

        #region ctor
        public GrayFloatImage(IProcessor source, string key, Image<Gray, float> image)
            : base(source, key, image)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new GrayFloatImage(Source, Key, Image.Copy());
        }
    }
}
