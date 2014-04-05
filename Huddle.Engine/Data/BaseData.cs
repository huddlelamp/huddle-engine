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

        public string Key { get; set; }

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
    }
}
