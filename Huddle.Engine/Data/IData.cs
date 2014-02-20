using System;

namespace Huddle.Engine.Data
{
    public interface IData : IDisposable
    {
        string Key { get; }

        IData Copy();
    }
}
