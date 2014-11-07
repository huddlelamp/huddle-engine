using GalaSoft.MvvmLight;
using Huddle.Engine.Processor;
using Newtonsoft.Json;

namespace Huddle.Engine.Data
{
    public abstract class BaseData : ObservableObject, IData
    {
        #region properties

        #region Source

        /// <summary>
        /// The <see cref="Source" /> property's name.
        /// </summary>
        public const string SourcePropertyName = "Source";

        private IProcessor _source;

        /// <summary>
        /// Sets and gets the Source property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [JsonIgnore]
        public IProcessor Source
        {
            get
            {
                return _source;
            }

            set
            {
                if (_source == value)
                {
                    return;
                }

                RaisePropertyChanging(SourcePropertyName);
                _source = value;
                RaisePropertyChanged(SourcePropertyName);
            }
        }

        #endregion

        #region Key

        /// <summary>
        /// The <see cref="Key" /> property's name.
        /// </summary>
        public const string KeyPropertyName = "Key";

        private string _key = string.Empty;

        /// <summary>
        /// Sets and gets the Key property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [JsonIgnore]
        public string Key
        {
            get
            {
                return _key;
            }

            set
            {
                if (_key == value)
                {
                    return;
                }

                RaisePropertyChanging(KeyPropertyName);
                _key = value;
                RaisePropertyChanged(KeyPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        protected BaseData(IProcessor source, string key)
        {
            Source = source;
            Key = key;
        }

        #endregion

        public abstract IData Copy();

        public abstract void Dispose();

        #region override methods

        public override string ToString()
        {
            return string.Format("{0}: {1}", base.ToString(), Source.GetType().Name);
        }

        #endregion
    }
}
