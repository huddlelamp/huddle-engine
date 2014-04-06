using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Model;
using Huddle.Engine.Properties;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor
{
    [KnownType("GetKnownTypes")]
    public abstract class BaseProcessor : ObservableObject, IProcessor, ILocator
    {
        #region member fields

        private Thread _processingThread;

        // A bounded collection. It can hold no more than 100 items at once.
        private BlockingCollection<IDataContainer> _dataQueue = new BlockingCollection<IDataContainer>(100);

        private bool _processing;

        private readonly object _stagedDataLock = new object();

        protected readonly List<IData> StagedData = new List<IData>();

        private long _frameId;

        private long _id = 0L;

        #endregion

        #region properties

        #region X

        /// <summary>
        /// The <see cref="X" /> property's name.
        /// </summary>
        public const string XPropertyName = "X";

        private double _x;

        /// <summary>
        /// Sets and gets the X property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public double X
        {
            get
            {
                return _x;
            }

            set
            {
                if (_x == value)
                {
                    return;
                }

                RaisePropertyChanging(XPropertyName);
                _x = value;
                RaisePropertyChanged(XPropertyName);
            }
        }

        #endregion

        #region Y

        /// <summary>
        /// The <see cref="Y" /> property's name.
        /// </summary>
        public const string YPropertyName = "Y";

        private double _y;

        /// <summary>
        /// Sets and gets the Y property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public double Y
        {
            get
            {
                return _y;
            }

            set
            {
                if (_y == value)
                {
                    return;
                }

                RaisePropertyChanging(YPropertyName);
                _y = value;
                RaisePropertyChanged(YPropertyName);
            }
        }

        #endregion

        #region Angle

        /// <summary>
        /// The <see cref="Angle" /> property's name.
        /// </summary>
        public const string AnglePropertyName = "Angle";

        private double _angle;

        /// <summary>
        /// Sets and gets the Angle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public double Angle
        {
            get
            {
                return _angle;
            }

            set
            {
                if (_angle == value)
                {
                    return;
                }

                RaisePropertyChanging(AnglePropertyName);
                _angle = value;
                RaisePropertyChanged(AnglePropertyName);
            }
        }

        #endregion

        #region ZIndex

        /// <summary>
        /// The <see cref="ZIndex" /> property's name.
        /// </summary>
        public const string ZIndexPropertyName = "ZIndex";

        private int _zIndex = 0;

        /// <summary>
        /// Sets and gets the ZIndex property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public int ZIndex
        {
            get
            {
                return _zIndex;
            }

            set
            {
                if (_zIndex == value)
                {
                    return;
                }

                RaisePropertyChanging(ZIndexPropertyName);
                _zIndex = value;
                RaisePropertyChanged(ZIndexPropertyName);
            }
        }

        #endregion

        #region Name

        private ViewTemplateAttribute _metadata;

        public ViewTemplateAttribute Metadata
        {
            get
            {
                if (_metadata != null) return _metadata;
                var type = GetType();
                var attribute = type.GetCustomAttribute<ViewTemplateAttribute>();
                return _metadata = attribute;
            }
        }

        #endregion

        #region IsRenderContent

        /// <summary>
        /// The <see cref="IsRenderContent" /> property's name.
        /// </summary>
        public const string IsRenderContentPropertyName = "IsRenderContent";

        private bool _isRenderContent = false;

        /// <summary>
        /// Sets and gets the IsRenderContent property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool IsRenderContent
        {
            get
            {
                return _isRenderContent;
            }

            set
            {
                if (_isRenderContent == value)
                {
                    return;
                }

                RaisePropertyChanging(IsRenderContentPropertyName);
                _isRenderContent = value;
                RaisePropertyChanged(IsRenderContentPropertyName);
            }
        }

        #endregion

        #region Sources

        /// <summary>
        /// The <see cref="Sources" /> property's name.
        /// </summary>
        public const string SourcesPropertyName = "Sources";

        private ObservableCollection<BaseProcessor> _sources = new ObservableCollection<BaseProcessor>();

        /// <summary>
        /// Sets and gets the Sources property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlArray("Sources")]
        [XmlArrayItem("Processor")]
        public ObservableCollection<BaseProcessor> Sources
        {
            get
            {
                return _sources;
            }

            set
            {
                if (_sources == value)
                {
                    return;
                }

                RaisePropertyChanging(SourcesPropertyName);
                _sources = value;
                RaisePropertyChanged(SourcesPropertyName);
            }
        }

        #endregion

        #region Targets

        /// <summary>
        /// The <see cref="Targets" /> property's name.
        /// </summary>
        public const string TargetsPropertyName = "Targets";

        private ObservableCollection<BaseProcessor> _targets = new ObservableCollection<BaseProcessor>();

        /// <summary>
        /// Sets and gets the Targets property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlArray("Targets")]
        [XmlArrayItem("Processor")]
        public ObservableCollection<BaseProcessor> Targets
        {
            get
            {
                return _targets;
            }

            set
            {
                if (_targets == value)
                {
                    return;
                }

                RaisePropertyChanging(TargetsPropertyName);
                _targets = value;
                RaisePropertyChanged(TargetsPropertyName);
            }
        }

        #endregion

        #region Logs

        /// <summary>
        /// The <see cref="Logs" /> property's name.
        /// </summary>
        public const string LogsPropertyName = "Logs";

        private ObservableCollection<string> _logs = new ObservableCollection<string>();

        /// <summary>
        /// Sets and gets the Logs property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public ObservableCollection<string> Logs
        {
            get
            {
                return _logs;
            }

            set
            {
                if (_logs == value)
                {
                    return;
                }

                RaisePropertyChanging(LogsPropertyName);
                _logs = value;
                RaisePropertyChanged(LogsPropertyName);
            }
        }

        #endregion

        #region HasErrorState

        /// <summary>
        /// The <see cref="HasErrorState" /> property's name.
        /// </summary>
        public const string HasErrorStatePropertyName = "HasErrorState";

        private bool _hasErrorState = false;

        /// <summary>
        /// Sets and gets the HasErrorState property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public bool HasErrorState
        {
            get
            {
                return _hasErrorState;
            }

            set
            {
                if (_hasErrorState == value)
                {
                    return;
                }

                RaisePropertyChanging(HasErrorStatePropertyName);
                _hasErrorState = value;
                RaisePropertyChanged(HasErrorStatePropertyName);
            }
        }

        #endregion

        #region Benchmark

        /// <summary>
        /// The <see cref="Benchmark" /> property's name.
        /// </summary>
        public const string BenchmarkPropertyName = "Benchmark";

        private Benchmark _benchmark = new Benchmark();

        /// <summary>
        /// Sets and gets the Benchmark property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public Benchmark Benchmark
        {
            get
            {
                return _benchmark;
            }

            set
            {
                if (_benchmark == value)
                {
                    return;
                }

                RaisePropertyChanging(BenchmarkPropertyName);
                _benchmark = value;
                RaisePropertyChanged(BenchmarkPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        protected BaseProcessor()
        {
        }

        #endregion

        #region Processing

        /* For autonumbering anonymous threads. */
        private static int _threadInitNumber;
        private static int NextThreadNumber()
        {
            return _threadInitNumber++;
        }

        public virtual void Start()
        {
            HasErrorState = false;
            _dataQueue = new BlockingCollection<IDataContainer>(_dataQueue.BoundedCapacity);

            if (_processing)
                return;

            _processing = true;

            _processingThread = new Thread(() =>
            {
                while (!_dataQueue.IsCompleted)
                {
                    IDataContainer dataContainer;
                    try
                    {
                        dataContainer = _dataQueue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        _processing = false;
                        break;
                    }

                    if (dataContainer == null)
                        throw new Exception("Empty data container has been queued");

                    if (Settings.Default.IsBenchmarkEnabled)
                        _benchmark.StartMeasurement();

                    dataContainer = PreProcess(dataContainer);

                    if (dataContainer != null)
                        dataContainer = ProcessInternal(dataContainer);

                    if (dataContainer != null)
                        dataContainer = PostProcess(dataContainer);

                    if (Settings.Default.IsBenchmarkEnabled)
                        _benchmark.StopMeasurement();

                    if (dataContainer != null)
                        Publish(dataContainer);
                }
            })
            {
                Name = string.Format(@"{0}{1}", GetType().Name, NextThreadNumber()),
                Priority = Settings.Default.ProcessingThreadPriority,
                IsBackground = true
            };
            _processingThread.Start();
        }

        public virtual void Stop()
        {
            // If it is not processing just return.
            if (!_processing)
                return;

            _processing = false;

            // Notify processing thread about completion.
            _dataQueue.CompleteAdding();

            //if (_processingThread != null)
            //    _processingThread.Join();
        }

        public void Publish(IDataContainer dataContainer)
        {
            if (!Targets.Any())
            {
                dataContainer.Dispose();
                return;
            }

            if (Targets.Count > 1)
                for (var i = 0; i < Targets.Count - 1; i++)
                {
                    var dataContainerCopy = dataContainer.Copy();
                    Targets[i].Process(dataContainerCopy);
                }

            Targets.Last().Process(dataContainer);
        }

        /// <summary>
        /// Pre process data.
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <returns></returns>
        public virtual IDataContainer PreProcess(IDataContainer dataContainer)
        {
            return dataContainer;
        }

        public void Process(IDataContainer dataContainer)
        {
            // Pipe data through if processing is turned off
            if (!_processing)
            {
                Publish(dataContainer);
                return;
            }

            //if (HasErrorState && _dataQueue.Count < 5)
            //{
            //    HasErrorState = false;
            //}

            _benchmark.QueueCount = _dataQueue.Count;

            // Add data container to processing queue.
            if (!_dataQueue.IsCompleted)
                try
                {
                    if (_dataQueue.Count == _dataQueue.BoundedCapacity)
                    { 
                        _dataQueue = new BlockingCollection<IDataContainer>(_dataQueue.BoundedCapacity);
                        HasErrorState = true;
                    }

                    _dataQueue.Add(dataContainer);

#if DEBUG
                    //Console.WriteLine("QUEUE: {0}, IsProcessing: {1} Name: {2}", _dataQueue.Count, _processing, GetType().Name);
#endif
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
        }

        private IDataContainer ProcessInternal(IDataContainer dataContainer)
        {
            var allData = dataContainer.ToArray();

            // Set new data to keep data container's meta information.
            dataContainer.Clear();

            Parallel.ForEach(allData, data =>
            {
                IData processedData = null;
                try
                {
                    processedData = Process(data);
                }
                catch (Exception e)
                {
                    Log("Processing error {0}", e.ToString());
                }

                if (processedData == null)
                    return;

                dataContainer.Add(processedData);
            });

            return dataContainer.Any() ? dataContainer : null;
        }

        /// <summary>
        /// Post process data
        /// </summary>
        /// <param name="dataContainer"></param>
        /// <returns></returns>
        public virtual IDataContainer PostProcess(IDataContainer dataContainer)
        {
            return dataContainer;
        }

        public abstract IData Process(IData data);

        protected void Stage(IData data)
        {
            lock (_stagedDataLock)
            {
                StagedData.Add(data);
            }
        }

        protected void Stage(params IData[] data)
        {
            foreach (var d in data)
            {
                Stage(d);
            }
        }

        protected void Push()
        {
            // Do not publish if staged data is empty
            lock (_stagedDataLock)
            {
                if (!StagedData.Any())
                    return;
            }

            var container = new DataContainer(++_frameId, DateTime.Now);

            lock (_stagedDataLock)
            {
                foreach (var data in StagedData)
                    container.Add(data);
                StagedData.Clear();
            }

            Publish(container);
        }

        #endregion

        protected long NextId()
        {
            return _id++;
        }

        protected void Log(string format, params object[] args)
        {
            DispatcherHelper.RunAsync(() =>
            {
                if (Logs.Count > 100)
                    Logs.RemoveAt(100);

                var message = string.Format(format, args);

                Logs.Insert(0, string.Format("[{0}] {1}", DateTime.Now, message));
            });
        }

        public static IEnumerable<Type> GetKnownTypes()
        {
            return ProcessorTypesProvider.GetProcessorTypes<BaseProcessor>();
        }
    }
}
