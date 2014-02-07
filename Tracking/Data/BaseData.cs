using GalaSoft.MvvmLight;

namespace Tools.FlockingDevice.Tracking.Data
{
    public abstract class BaseData : ObservableObject, IData
    {
        #region properties

        #region Key

        public string Key { get; set; }

        #endregion

        #endregion

        #region ctor

        protected BaseData(string key)
        {
            Key = key;
        }

        #endregion

        public abstract IData Copy();

        public abstract void Dispose();
    }
}
