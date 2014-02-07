using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.FlockingDevice.Tracking.Model
{
    public interface ILocator : INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region properties

        double X { get; set; }
        double Y { get; set; }
        double Angle { get; set; }

        #endregion
    }
}
