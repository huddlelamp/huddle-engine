using System;

namespace Tools.FlockingDevice.Tracking.InputSource
{
    public abstract class ImageEventArgs : EventArgs
    {
        #region properties

        public long ElapsedTime { get; private set; }

        public double Fps
        {
            get
            {
                return 1000.0 / ElapsedTime;
            }
        }

        #endregion

        #region ctor

        protected ImageEventArgs(long elapsedTime)
        {
            ElapsedTime = elapsedTime;
        }

        #endregion
    }
}
