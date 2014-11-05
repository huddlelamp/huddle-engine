using System.Drawing;
using System.Windows;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public sealed class ROI : BaseData
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

        public ROI(IProcessor source, string key)
            : base(source, key)
        {
        }

        #endregion

        public override IData Copy()
        {
            return new ROI(Source, Key)
            {
                RoiRectangle = RoiRectangle
            };
        }

        public override void Dispose()
        {
            
        }
    }
}
