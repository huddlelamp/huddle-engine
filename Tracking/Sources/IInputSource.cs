using System;
using System.Xml.Serialization;
using Tools.FlockingDevice.Tracking.Sources.Senz3D;

namespace Tools.FlockingDevice.Tracking.Sources
{
    [XmlInclude(typeof(Senz3DInputSource))]
    public interface IInputSource : IDisposable
    {
        #region events

        event EventHandler<ImageEventArgs> ImageReady;

        #endregion

        #region properties

        string FriendlyName { get; }

        #endregion

        void Start();

        void Stop();

        void Pause();

        void Resume();
    }
}
