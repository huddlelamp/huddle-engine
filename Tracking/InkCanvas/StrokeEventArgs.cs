using System;
using System.Windows.Ink;

namespace Tools.FlockingDevice.Tracking.InkCanvas
{
    public class StrokeEventArgs : EventArgs
    {
        #region properties

        public Device Device { get; set; }

        public Stroke Stroke { get; set; }

        #endregion

        public StrokeEventArgs(Device device, Stroke s)
        {
            Device = device;
            Stroke = s;
        }
    }

    public enum Device
    {
        Stylus,
        StylusInverted,
        Touch,
        Mouse
    }
}
