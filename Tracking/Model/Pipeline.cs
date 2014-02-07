using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.Model
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
