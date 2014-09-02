using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
                if (_dataStats.All(s => s.Type != data.GetType()))
                {
                    var stat = new Stat
                           {
                               Type = data.GetType(),
                               Count = 1
                           };
                    stat.Watch.Start();
                    DataStats.Add(stat);
                }
                else
                {
                    var stat = _dataStats.Single(s => s.Type == data.GetType());
                    stat.Fps = 1000.0 / stat.Watch.ElapsedMilliseconds;
                    stat.Watch.Restart();
                    stat.Count++;
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

        #region Fps

        /// <summary>
        /// The <see cref="Fps" /> property's name.
        /// </summary>
        public const string FpsPropertyName = "Fps";

        private double _fps;

        /// <summary>
        /// Sets and gets the Fps property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public double Fps
        {
            get
            {
                return _fps;
            }

            set
            {
                if (_fps == value)
                {
                    return;
                }

                RaisePropertyChanging(FpsPropertyName);
                _fps = value;
                RaisePropertyChanged(FpsPropertyName);
            }
        }

        #endregion

        #region Stopwatch

        /// <summary>
        /// The <see cref="Watch" /> property's name.
        /// </summary>
        public const string WatchPropertyName = "Watch";

        private Stopwatch _watch = new Stopwatch();

        /// <summary>
        /// Sets and gets the Watch property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Stopwatch Watch
        {
            get
            {
                return _watch;
            }

            set
            {
                if (_watch == value)
                {
                    return;
                }

                RaisePropertyChanging(WatchPropertyName);
                _watch = value;
                RaisePropertyChanged(WatchPropertyName);
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
