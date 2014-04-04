using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor
{
    [ViewTemplate("Data Viewer", "DataViewer")]
    public class DataViewer : BaseProcessor
    {
        #region properties

        #region DataStats

        /// <summary>
        /// The <see cref="DataStats" /> property's name.
        /// </summary>
        public const string DataStatsPropertyName = "DataStats";

        private ObservableCollection<Stat> _dataStats = new ObservableCollection<Stat>();

        /// <summary>
        /// Sets and gets the DataStats property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public ObservableCollection<Stat> DataStats
        {
            get
            {
                return _dataStats;
            }

            set
            {
                if (_dataStats == value)
                {
                    return;
                }

                RaisePropertyChanging(DataStatsPropertyName);
                _dataStats = value;
                RaisePropertyChanged(DataStatsPropertyName);
            }
        }

        #endregion

        #endregion

        public override IData Process(IData data)
        {
            DispatcherHelper.RunAsync(() =>
            {
                try
                {
                    var stat = _dataStats.Single(s => s.Type == data.GetType());
                    stat.Count++;
                }
                catch (Exception)
                {
                    DataStats.Add(new Stat
                    {
                        Type = data.GetType(),
                        Count = 1
                    });
                }
            });

            return data;
        }
    }

    public class Stat : ObservableObject
    {
        #region properties

        #region Type

        /// <summary>
        /// The <see cref="Type" /> property's name.
        /// </summary>
        public const string TypePropertyName = "Type";

        private Type _type = null;

        /// <summary>
        /// Sets and gets the Type property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Type Type
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

        #region Count

        /// <summary>
        /// The <see cref="Count" /> property's name.
        /// </summary>
        public const string CountPropertyName = "Count";

        private int _count = 0;

        /// <summary>
        /// Sets and gets the Count property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }

            set
            {
                if (_count == value)
                {
                    return;
                }

                RaisePropertyChanging(CountPropertyName);
                _count = value;
                RaisePropertyChanged(CountPropertyName);
            }
        }

        #endregion

        #endregion
    }
}
