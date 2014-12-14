using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Huddle.Engine.Processor.OpenCv.Struct
{
    public enum TrackingState
    {
        NotTracked = 0,
        Tracked = 1,
        Occluded = 2
    }
}
