using System.Collections.Generic;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Data;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.Threading;

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

        #region Messages

        /// <summary>
        /// The <see cref="Messages" /> property's name.
        /// </summary>
        public const string MessagesPropertyName = "Messages";

        private ObservableCollection<string> _messages = new ObservableCollection<string>();

        /// <summary>
        /// Sets and gets the Messages property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore]
        public ObservableCollection<string> Messages
        {
            get
            {
                return _messages;
            }

            set
            {
                if (_messages == value)
                {
                    return;
                }

                RaisePropertyChanging(MessagesPropertyName);
                _messages = value;
                RaisePropertyChanged(MessagesPropertyName);
            }
        }

        #endregion

        #endregion

        public virtual List<IData> Process(List<IData> allData)
        {
            allData.RemoveAll(d => Process(d) == null);

            return allData;
        }

        protected virtual IData Process(IData data)
        {
            return data;
        }

        protected void Log(string format, params object[] args)
        {
            DispatcherHelper.RunAsync(() =>
            {
                if (Messages.Count > 100)
                    Messages.RemoveAt(100);

                Messages.Insert(0, string.Format(format, args));
            });
        }
    }
}
