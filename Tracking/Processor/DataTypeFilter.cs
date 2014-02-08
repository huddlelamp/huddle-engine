using System.Runtime.Serialization;
using System.Xml.Serialization;
using Tools.FlockingDevice.Tracking.Data;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [ViewTemplate("Data Type Filter", "DataTypeFilter")]
    public class DataTypeFilter : BaseProcessor
    {
        #region properties

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

        #region Type

        [IgnoreDataMember]
        public static string[] Types
        {
            get
            {
                return new[]
                {
                    typeof(BaseData).FullName,
                    typeof(RgbImageData).FullName,
                    typeof(BlobData).FullName,
                    typeof(LocationData).FullName
                };
            }
        }

        /// <summary>
        /// The <see cref="Type" /> property's name.
        /// </summary>
        public const string TypePropertyName = "Type";

        private string _type;

        /// <summary>
        /// Sets and gets the Type property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string Type
        {
            get
            {
                return _type;
            }

            set
            {
                if (_type == value)
                {
                    return;
                }

                RaisePropertyChanging(TypePropertyName);
                _type = value;
                RaisePropertyChanged(TypePropertyName);
            }
        }

        #endregion

        #endregion

        public override IData Process(IData data)
        {
            if (Type != null && !Equals(Type, data.GetType().FullName))
                return null;

            if (!string.IsNullOrWhiteSpace(Key) && !Equals(Key, data.Key))
                return null;

            return data;
        }
    }
}
