using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using Tools.FlockingDevice.Tracking.Data;

namespace Tools.FlockingDevice.Tracking.Processor
{
    public interface IProcessor
    {
        #region public properties

        string FriendlyName { get; }

        ObservableCollection<BaseProcessor> Children { get; set; }

        #endregion

        #region Processing

        void Start();

        void Stop();

        void Publish(IDataContainer data);

        void Process(IDataContainer data);

        IData Process(IData data);

        #endregion
    }
}
