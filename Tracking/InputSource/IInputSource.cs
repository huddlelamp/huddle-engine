using System;

namespace Tools.FlockingDevice.Tracking.InputSource
{
    public interface IInputSource : IDisposable
    {
        #region events

        event EventHandler<ImageEventArgs2> ImageReady;

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
