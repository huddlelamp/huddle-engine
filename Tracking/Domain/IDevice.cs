using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools.FlockingDevice.Tracking.Data;

namespace Tools.FlockingDevice.Tracking.Domain
{
    public interface IDevice : IData
    {
        #region properties

        int Id { get; set; }

        double X { get; set; }

        double Y { get; set; }

        double Angle { get; set; }

        #endregion
    }
}
