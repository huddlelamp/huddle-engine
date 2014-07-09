using Emgu.CV;
using Emgu.CV.Structure;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Not", "Not")]
    public class Not : RgbProcessor
    {
        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            return image.Not();
        }
    }
}
