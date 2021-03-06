﻿using Huddle.Engine.Processor;

namespace Huddle.Engine.Data
{
    public sealed class Digital : BaseData
    {
        #region properties

        #region Value

        /// <summary>
        /// The <see cref="Value" /> property's name.
        /// </summary>
        public const string ValuePropertyName = "Value";

        private bool _value = false;

        /// <summary>
        /// Sets and gets the Value property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool Value
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

        public Digital(IProcessor source, string key)
            : base(source, key)
        {
        }

        public override IData Copy()
        {
            return new Digital(Source, Key)
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
