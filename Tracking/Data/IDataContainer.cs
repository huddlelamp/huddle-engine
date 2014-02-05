using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.Data
{
    public interface IDataContainer : IList<IData>, IDisposable
    {
        long FrameId { get; }
        
        DateTime Timestamp { get; }

        List<BaseProcessor> Trail { get; }
        
        List<IDataContainer> Siblings { get; }
        
        IDataContainer Copy();
    }
}
