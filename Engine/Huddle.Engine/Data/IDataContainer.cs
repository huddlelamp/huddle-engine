using System;
using System.Collections.Generic;
using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
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
