using Emgu.CV;

namespace Tools.FlockingDevice.Tracking.Data
{
    public abstract class ImageData<TColor, TDepth> : BaseData
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

        protected ImageData(string key, Image<TColor, TDepth> image)
            : base(key)
        {
            Image = image;
        }

        #endregion
    }
}
