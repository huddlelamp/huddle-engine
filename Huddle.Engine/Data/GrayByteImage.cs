using Emgu.CV;
using Emgu.CV.Structure;
using System;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public class GrayByteImage : BaseImageData<Gray, Byte>
    {
        #region properties

        #endregion

        #region ctor
        public GrayByteImage(IProcessor source, string key, Image<Gray, Byte> image)
            : base(source, key, image)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new GrayByteImage(Source, Key, Image.Copy());
        }
    }
}
