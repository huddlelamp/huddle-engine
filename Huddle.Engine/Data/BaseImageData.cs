using Emgu.CV;

namespace Huddle.Engine.Data
{
    public abstract class BaseImageData<TColor, TDepth> : BaseData
        where TColor : struct, IColor
        where TDepth : new()
    {
        #region properties

        #region Image

        private Image<TColor, TDepth> _image;

        public Image<TColor, TDepth> Image
        {
            get
            {
                return _image;
            }

            set
            {
                if (_image != null)
                    _image.Dispose();

                _image = value;
            }
        }

        #endregion

        #endregion

        #region ctor

        protected BaseImageData(string key, Image<TColor, TDepth> image)
            : base(key)
        {
            Image = image;
        }

        #endregion

        //public override IData Copy()
        //{
        //    var type = GetType();
        //    var copy = Activator.CreateInstance(type, Key, Image.Copy()) as BaseImageData<TColor, TDepth>;
        //    return copy;
        //}

        public override void Dispose()
        {
            Image.Dispose();
        }
    }
}
