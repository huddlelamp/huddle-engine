using Emgu.CV;
using Emgu.CV.Structure;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("Background Subtraction", "BackgroundSubtraction")]
    public class BackgroundSubtraction : BaseImageProcessor<Gray, float>
    {
        public override Image<Gray, float> ProcessAndView(Image<Gray, float> image)
        {
            //throw new System.NotImplementedException();
        }
    }
}
