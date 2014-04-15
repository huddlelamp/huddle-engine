using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Huddle.Engine.Processor;
using Huddle.Engine.Processor.Complex;

namespace Huddle.Engine.Data
{
    public class Marker: LocationData
    {
        #region properties

        #region RgbImageToDisplayRatio

        /// <summary>
        /// The <see cref="RgbImageToDisplayRatio" /> property's name.
        /// </summary>
        public const string RgbImageToDisplayRatioPropertyName = "RgbImageToDisplayRatio";

        private Ratio _rgbImageToDisplayRatio = Ratio.Empty;

        /// <summary>
        /// Sets and gets the RgbImageToDisplayRatio property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Ratio RgbImageToDisplayRatio
        {
            get
            {
                return _rgbImageToDisplayRatio;
            }

            set
            {
                if (_rgbImageToDisplayRatio.Equals(value))
                {
                    return;
                }

                RaisePropertyChanging(RgbImageToDisplayRatioPropertyName);
                _rgbImageToDisplayRatio = value;
                RaisePropertyChanged(RgbImageToDisplayRatioPropertyName);
            }
        }

        #endregion

        #region EnclosingRectangle

        /// <summary>
        /// The <see cref="EnclosingRectangle" /> property's name.
        /// </summary>
        public const string EnclosingRectanglePropertyName = "EnclosingRectangle";

        private EnclosingRectangle _enclosingRectangle;

        /// <summary>
        /// Sets and gets the EnclosingRectangle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public EnclosingRectangle EnclosingRectangle
        {
            get
            {
                return _enclosingRectangle;
            }

            set
            {
                if (_enclosingRectangle == value)
                {
                    return;
                }

                RaisePropertyChanging(EnclosingRectanglePropertyName);
                _enclosingRectangle = value;
                RaisePropertyChanged(EnclosingRectanglePropertyName);
            }
        }

        #endregion

        #endregion

        public Marker(IProcessor source, string key) : base(source, key)
        {
        }

        public override IData Copy()
        {
            return new Marker(Source, Key)
            {
                Id = Id,
                X = X,
                Y = Y,
                Angle = Angle,
                RgbImageToDisplayRatio = RgbImageToDisplayRatio,
                EnclosingRectangle = EnclosingRectangle,
                RelativeX = RelativeX,
                RelativeY = RelativeY,
            };
        }

        public override void Dispose()
        {

        }
    }
}
