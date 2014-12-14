using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public sealed class Disconnected : BaseData
    {
        #region properties

        #region Value

        /// <summary>
        /// The <see cref="Value" /> property's name.
        /// </summary>
        public const string ValuePropertyName = "Value";

        private string _value = null;

        /// <summary>
        /// Sets and gets the Value property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (_value == value)
                {
                    return;
                }

                RaisePropertyChanging(ValuePropertyName);
                _value = value;
                RaisePropertyChanged(ValuePropertyName);
            }
        }

        #endregion

        #endregion

        public Disconnected(IProcessor source, string key)
            : base(source, key)
        {
        }

        public override IData Copy()
        {
            return new Disconnected(Source, Key)
            {
                Value = Value
            };
        }

        public override void Dispose()
        {
            // ignore
        }
    }
}
