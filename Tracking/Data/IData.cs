using System;

namespace Tools.FlockingDevice.Tracking.Data
{
    public interface IData : IDisposable
    {
        string Key { get; }

        IData Copy();
    }
}
