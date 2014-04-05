using Emgu.CV;
using Emgu.CV.Structure;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public class RgbImageData : BaseImageData<Rgb, byte>
    {
        #region properties

        #endregion

        #region ctor
        public RgbImageData(IProcessor source, string key, Image<Rgb, byte> image)
            : base(source, key, image)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new RgbImageData(Source, Key, Image.Copy());
        }
    }
}
