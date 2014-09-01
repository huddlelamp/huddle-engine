using System.Runtime.Serialization;
using System.Xml.Serialization;
using Huddle.Engine.Data;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Model
{
    [XmlRoot]
    public class Pipeline : BaseProcessor
    {
        #region properties

        #region Scale

        /// <summary>
        /// The <see cref="Scale" /> property's name.
        /// </summary>
        public const string ScalePropertyName = "Scale";

        private double _scale = 1.0;

        /// <summary>
        /// Sets and gets the Scale property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Scale
        {
            get
            {
                return _scale;
            }

            set
            {
                if (_scale == value)
                {
                    return;
                }

                RaisePropertyChanging(ScalePropertyName);
                _scale = value;
                RaisePropertyChanged(ScalePropertyName);
            }
        }

        #endregion

        #region Fps

        /// <summary>
        /// The <see cref="Fps" /> property's name.
        /// </summary>
        public const string FpsPropertyName = "Fps";

        private double _fps;

        /// <summary>
        /// Sets and gets the Fps property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public double Fps
        {
            get
            {
                return _fps;
            }

            set
            {
                if (_fps == value)
                {
                    return;
                }

                RaisePropertyChanging(FpsPropertyName);
                _fps = value;
                RaisePropertyChanged(FpsPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public Pipeline()
        {

        }

        #endregion

        public override IData Process(IData data)
        {
            return null;
        }
    }
}
