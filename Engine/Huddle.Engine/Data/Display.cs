using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public class Display: LocationData
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

        #endregion

        public Display(IProcessor source, string key) : base(source, key)
        {
        }

        public override IData Copy()
        {
            return new Display(Source, Key)
            {
                Center = Center,
                Angle = Angle,
                RgbImageToDisplayRatio = RgbImageToDisplayRatio
            };
        }

        public override void Dispose()
        {

        }
    }
}
