using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor.OpenCv
{
    [ViewTemplate("Kalman", "KalmanTemplate")]
    public class Kalman : BaseProcessor
    {
        public override IData Process(IData data)
        {
            throw new NotImplementedException();
        }
    }
}
