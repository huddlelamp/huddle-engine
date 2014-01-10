using System;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Sources.Senz3D;

namespace Tools.FlockingDevice.Tracking.Sources
{
    [XmlInclude(typeof(Senz3Dv2InputSource))]
    public abstract class InputSource : ObservableObject, IInputSource
    {
        public abstract void Dispose();

        public abstract event EventHandler<ImageEventArgs> ImageReady;

        public abstract string FriendlyName { get; }
        public abstract void Start();
        public abstract void Stop();
        public abstract void Pause();
        public abstract void Resume();

        #region properties

        #region IsRenderColorImage

        /// <summary>
        /// The <see cref="IsRenderColorImage" /> property's name.
        /// </summary>
        public const string IsRenderColorImagePropertyName = "IsRenderColorImage";

        private bool _isRenderColorImage = true;

        /// <summary>
        /// Sets and gets the IsRenderColorImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool IsRenderColorImage
        {
            get
            {
                return _isRenderColorImage;
            }

            set
            {
                if (_isRenderColorImage == value)
                {
                    return;
                }

                RaisePropertyChanging(IsRenderColorImagePropertyName);
                _isRenderColorImage = value;
                RaisePropertyChanged(IsRenderColorImagePropertyName);
            }
        }

        #endregion

        #region IsRenderDepthImage

        /// <summary>
        /// The <see cref="IsRenderDepthImage" /> property's name.
        /// </summary>
        public const string IsRenderDepthImagePropertyName = "IsRenderDepthImage";

        private bool _isRenderDepthImage = true;

        /// <summary>
        /// Sets and gets the IsRenderDepthImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool IsRenderDepthImage
        {
            get
            {
                return _isRenderDepthImage;
            }

            set
            {
                if (_isRenderDepthImage == value)
                {
                    return;
                }

                RaisePropertyChanging(IsRenderDepthImagePropertyName);
                _isRenderDepthImage = value;
                RaisePropertyChanged(IsRenderDepthImagePropertyName);
            }
        }

        #endregion

        #endregion
    }
}
