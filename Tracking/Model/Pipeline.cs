using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.InputSource;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.Model
{
    [XmlType]
    public class Pipeline : ObservableObject
    {
        #region InputSource

        /// <summary>
        /// The <see cref="InputSource" /> property's name.
        /// </summary>
        public const string InputSourcePropertyName = "InputSource";

        private IInputSource _inputSource;

        /// <summary>
        /// Sets and gets the InputSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlElement]
        public IInputSource InputSource
        {
            get
            {
                return _inputSource;
            }

            set
            {
                if (_inputSource == value)
                {
                    return;
                }

                RaisePropertyChanging(InputSourcePropertyName);
                _inputSource = value;
                RaisePropertyChanged(InputSourcePropertyName);
            }
        }

        #endregion

        #region ColorImageProcessors

        /// <summary>
        /// The <see cref="ColorImageProcessors" /> property's name.
        /// </summary>
        public const string ColorImageProcessorsPropertyName = "ColorImageProcessors";

        private ObservableCollection<RgbProcessor> _colorImageProcessors = new ObservableCollection<RgbProcessor>();

        /// <summary>
        /// Sets and gets the ColorImageProcessors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlArray("ColorImageProcessors")]
        [XmlArrayItem("Processor")]
        public ObservableCollection<RgbProcessor> ColorImageProcessors
        {
            get
            {
                return _colorImageProcessors;
            }

            set
            {
                if (_colorImageProcessors == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorImageProcessorsPropertyName);
                _colorImageProcessors = value;
                RaisePropertyChanged(ColorImageProcessorsPropertyName);
            }
        }

        #endregion

        #region DepthImageProcessors

        /// <summary>
        /// The <see cref="DepthImageProcessors" /> property's name.
        /// </summary>
        public const string DepthImageProcessorsPropertyName = "DepthImageProcessors";

        private ObservableCollection<RgbProcessor> _depthImageProcessors = new ObservableCollection<RgbProcessor>();

        /// <summary>
        /// Sets and gets the DepthImageProcessors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlArray("DepthImageProcessors")]
        [XmlArrayItem("Processor")]
        public ObservableCollection<RgbProcessor> DepthImageProcessors
        {
            get
            {
                return _depthImageProcessors;
            }

            set
            {
                if (_depthImageProcessors == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthImageProcessorsPropertyName);
                _depthImageProcessors = value;
                RaisePropertyChanged(DepthImageProcessorsPropertyName);
            }
        }

        #endregion

        #region ctor

        public Pipeline()
        {
            
        }

        #endregion
    }
}
