using System.Collections.Generic;
using System.Windows.Documents;

namespace Tools.FlockingDevice.Tracking.Data
{
    public class Digital : BaseData
    {
        #region properties

        #region Id

        /// <summary>
        /// The <see cref="Id" /> property's name.
        /// </summary>
        public const string IdPropertyName = "Id";

        private string _id = string.Empty;

        /// <summary>
        /// Sets and gets the Id property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Id
        {
            get
            {
                return _id;
            }

            set
            {
                if (_id == value)
                {
                    return;
                }

                RaisePropertyChanging(IdPropertyName);
                _id = value;
                RaisePropertyChanged(IdPropertyName);
            }
        }

        #endregion

        #region NotFor

        /// <summary>
        /// The <see cref="NotFor" /> property's name.
        /// </summary>
        public const string NotForPropertyName = "NotFor";

        private List<string> _notFor = new List<string>();

        /// <summary>
        /// Sets and gets the NotFor property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public List<string> NotFor
        {
            get
            {
                return _notFor;
            }

            set
            {
                if (_notFor == value)
                {
                    return;
                }

                RaisePropertyChanging(NotForPropertyName);
                _notFor = value;
                RaisePropertyChanged(NotForPropertyName);
            }
        }

        #endregion

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

        public Digital(string key) : base(key)
        {
        }

        public override IData Copy()
        {
            return new Digital(Key)
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
