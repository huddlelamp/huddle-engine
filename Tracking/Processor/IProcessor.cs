using System.Collections.Generic;
using System.Windows.Documents;
using Tools.FlockingDevice.Tracking.Data;

namespace Tools.FlockingDevice.Tracking.Processor
{
    public interface IProcessor
    {
        #region public properties

        string FriendlyName { get; }

        #endregion

        List<IData> Process(List<IData> allData);
    }
}
