using System.Drawing;
using System.Windows;

namespace Huddle.Engine.Data
{
    public class ROI : BaseData
    {
        #region properties

        #region RoiRectangle
        /// <summary>
        /// The <see cref="RoiRectangle" /> property's name.
        /// </summary>
        public const string RoiRectanglePropertyName = "RoiRectangle";

        private Rectangle _roiRectangleProperty = new Rectangle();

        /// <summary>
        /// Sets and gets the RoiRectangle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Rectangle RoiRectangle
        {
            get
            {
                return _roiRectangleProperty;
            }

            set
            {
                if (_roiRectangleProperty == value)
                {
                    return;
                }

                RaisePropertyChanging(RoiRectanglePropertyName);
                _roiRectangleProperty = value;
                RaisePropertyChanged(RoiRectanglePropertyName);
            }
        }
        #endregion

        #endregion

        #region ctor

        public ROI(string key)
            : base(key)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new ROI(Key)
            {
                RoiRectangle = RoiRectangle
            };
        }

        public override void Dispose()
        {
            
        }
    }
}
