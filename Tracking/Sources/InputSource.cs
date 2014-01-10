using System;
using System.Xml.Serialization;
using Tools.FlockingDevice.Tracking.Sources.Senz3D;

namespace Tools.FlockingDevice.Tracking.Sources
{
    [XmlInclude(typeof(Senz3Dv2InputSource))]
    public abstract class InputSource : IInputSource
    {
        public abstract void Dispose();

        public abstract event EventHandler<ImageEventArgs> ImageReady;
        public abstract string FriendlyName { get; }
        public abstract float DepthConfidenceThreshold { get; set; }
        public abstract bool DepthSmoothing { get; set; }
        public abstract bool FlipVertical { get; set; }
        public abstract bool FlipHorizontal { get; set; }
        public abstract void Start();
        public abstract void Stop();
        public abstract void Pause();
        public abstract void Resume();
    }
}
