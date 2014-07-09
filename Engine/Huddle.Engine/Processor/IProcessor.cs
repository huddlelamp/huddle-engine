using System.Collections.ObjectModel;
using Huddle.Engine.Data;

namespace Huddle.Engine.Processor
{
    public interface IProcessor
    {
        #region public properties

        ObservableCollection<BaseProcessor> Targets { get; set; }

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
