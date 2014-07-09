using Emgu.CV.Structure;

namespace Huddle.Engine.Processor
{
    public abstract class RgbProcessor : BaseImageProcessor<Rgb, byte>
    {
        #region ctor

        protected RgbProcessor()
            : base(false)
        {

        }

        protected RgbProcessor(bool enableVideoWriter)
            : base(enableVideoWriter)
        {
            
        }

        #endregion
    }
}
