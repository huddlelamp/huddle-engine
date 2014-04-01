using Emgu.CV;
using Emgu.CV.Structure;
using System;

namespace Huddle.Engine.Data
{
    public class GrayByteImage : BaseImageData<Gray, Byte>
    {
        #region properties

        #endregion

        #region ctor
        public GrayByteImage(string key, Image<Gray, Byte> image)
            : base(key, image)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new GrayByteImage(Key, Image.Copy());
        }
    }
}
