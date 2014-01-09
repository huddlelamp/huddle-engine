using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tools.FlockingDevice.Tracking.Domain
{
    public delegate void DeviceEventHandler(object sender, DeviceEventArgs e);

    /// <summary>
    /// Provides custom event args for the Device event.
    /// </summary>
    public class DeviceEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// DeviceEventArgsEventArgs Constructor
        /// </summary>
        /// <param name="device">Specifies the value for the Device property.</param>
        internal DeviceEventArgs(IDevice device)
        {
            Device = device;
        }

        /// <summary>
        /// Gets the value of the Device property.
        /// This property indicates ....
        /// </summary>
        public IDevice Device { get; private set; }
    }
}
