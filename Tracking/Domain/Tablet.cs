using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools.FlockingDevice.Tracking.Data;

namespace Tools.FlockingDevice.Tracking.Domain
{
    public class Tablet : AbstractDevice
    {
        public Tablet(string key) : base(key)
        {
        }

        public override IData Copy()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
