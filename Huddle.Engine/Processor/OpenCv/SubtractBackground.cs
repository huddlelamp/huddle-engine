using Emgu.CV;
using Emgu.CV.Structure;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.OpenCv
{
    [ViewTemplate("SubtractBackground", "SubtractBackground")]
    public class SubtractBackground : RgbProcessor
    {
        #region private fields

        private Image<Gray, byte> _backgroundImage;

        private int _collectedBackgroundImages = 0;

        #endregion

        public override void Start()
        {
            _collectedBackgroundImages = 0;
            _backgroundImage = null;

            base.Start();
        }

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var imageCopy = image.Convert<Gray, byte>();

            //if (BuildingBackgroundImage(image)) return null;
            if (_backgroundImage == null)
            {
                _backgroundImage = imageCopy;
                return null;
            }

            return imageCopy.Sub(_backgroundImage).Convert<Rgb, byte>();
        }
        //private bool BuildingBackgroundImage(Image<Rgb, byte> image)
        //{
        //    if (_backgroundImage == null)
        //    {
        //        _backgroundImage = image.Copy();
        //        return true;
        //    }

        //    if (++_collectedBackgroundImages < 30)
        //    {
        //        _backgroundImage.RunningAvg(image, 0.8);
        //        return true;
        //    }

        //    return false;
        //}
    }
}
