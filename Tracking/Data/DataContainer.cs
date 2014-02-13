using System;
using System.Collections.Generic;
using System.Linq;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.Data
{
    public class DataContainer : List<IData>, IDataContainer //SynchronizedCollection<IData>, IDataContainer
    {
        #region private fields

        private object _lock = new object();

        #endregion

        #region properties

        #region FrameId

        public long FrameId { get; set; }

        #endregion

        #region Timestamp

        public DateTime Timestamp { get; private set; }

        #endregion

        #endregion

        #region ctor

        public DataContainer(long frameId, DateTime timestamp)
        {
            FrameId = frameId;
            Timestamp = timestamp;
        }

        #endregion

        public List<BaseProcessor> Trail { get; private set; }

        public List<IDataContainer> Siblings { get; private set; }

        public IDataContainer Copy()
        {
            var copy = new DataContainer(FrameId, Timestamp)
            {
                Siblings = new List<IDataContainer>()
            };

            if (Siblings == null)
                Siblings = new List<IDataContainer>();

            lock (_lock)
            {
                Siblings.Add(copy);
            }

            copy.Siblings.Add(this);

            foreach (var data in ToArray())
            {
                var dataCopy = data.Copy();

                if (dataCopy != null)
                    copy.Add(dataCopy);
            }

            return copy;
        }

        public void Dispose()
        {
            //lock (_lock)
            //{
            //    if (Siblings != null)
            //    {
            //        foreach (var sibling in Siblings.ToArray())
            //            sibling.Siblings.Remove(this);

            //        if (Siblings.Any())
            //            Siblings.Clear();
            //    } 
            //}

            //if (Trail != null && Trail.Any())
            //    Trail.Clear();

            if (Count > 0)
                foreach (var data in this)
                    data.Dispose();
        }
    }
}
