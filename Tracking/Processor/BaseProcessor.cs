using System.Collections.Generic;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Data;

namespace Tools.FlockingDevice.Tracking.Processor
{
    public abstract class BaseProcessor : ObservableObject, IProcessor
    {
        #region properties

        #region FiendlyName

        public virtual string FriendlyName
        {
            get
            {
                return GetType().Name;
            }
        }

        #endregion

        #endregion

        public List<IData> Process(List<IData> allData)
        {
            allData.RemoveAll(d =>
            {
                var imageData = d as RgbImageData;

                if (imageData != null)
                    return Process(imageData) == null;

                return false;
            });

            return allData;
        }

        protected virtual IData Process(RgbImageData data)
        {
            return data;
        }
    }
}
