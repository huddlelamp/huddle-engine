using System;
using System.Xml.Serialization;
using Tools.FlockingDevice.Tracking.Sources.Senz3D;

namespace Tools.FlockingDevice.Tracking.Sources
{
    [XmlInclude(typeof(Senz3Dv2InputSource))]
    public interface IInputSource : IDisposable
    {
        #region events

        event EventHandler<ImageEventArgs> ImageReady;

        #endregion

        #region properties

        string FriendlyName { get; }

        float DepthConfidenceThreshold { get; set; }

        bool DepthSmoothing { get; set; }

        bool FlipVertical { get; set; }

        bool FlipHorizontal { get; set; }

        #endregion

        void Start();

        void Stop();

        void Pause();

        void Resume();
    }
}
