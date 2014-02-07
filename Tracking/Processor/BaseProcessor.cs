using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Data;
using System.Xml.Serialization;
using Tools.FlockingDevice.Tracking.Model;
using Tools.FlockingDevice.Tracking.Processor.BarCodes;
using Tools.FlockingDevice.Tracking.Processor.OpenCv;
using Tools.FlockingDevice.Tracking.Processor.Sensors;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [KnownType(typeof(Senz3D))]
    [KnownType(typeof(Basics))]
    [KnownType(typeof(BlobTracker))]
    [KnownType(typeof(CannyEdges))]
    [KnownType(typeof(ErodeDilate))]
    [KnownType(typeof(FindContours))]
    [KnownType(typeof(QRCodeDecoder))]
    [KnownType(typeof(VideoRecordAndPlay))]
    [KnownType(typeof(DataTypeFilter))]
    public abstract class BaseProcessor : ObservableObject, IProcessor, ILocator
    {
        #region private fields

        private Thread _processingThread;

        // A bounded collection. It can hold no more than 100 items at once.
        private BlockingCollection<IDataContainer> _dataQueue = new BlockingCollection<IDataContainer>(100);

        private bool _processing;

        #endregion

        #region properties

        #region X

        /// <summary>
        /// The <see cref="X" /> property's name.
        /// </summary>
        public const string XPropertyName = "X";

        private double _x = 0.0;

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

        private double _y = 0.0;

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

        private double _angle = 0.0;

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

        private bool _isRenderContent = true;

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

        #region AllData

        protected List<IData> AllData { get; private set; }

        #endregion

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
            _dataQueue = new BlockingCollection<IDataContainer>(100);

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

                    dataContainer = PreProcess(dataContainer);

                    if (dataContainer != null)
                        dataContainer = ProcessInternal(dataContainer);

                    if (dataContainer != null)
                        dataContainer = PostProcess(dataContainer);

                    if (dataContainer != null)
                        Publish(dataContainer);
                }
            })
            {
                Name = string.Format(@"{0}{1}", GetType().Name, NextThreadNumber())
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

            for (var i = 0; i < Targets.Count - 1; i++)
            {
                var container = dataContainer;
                if (dataContainer.Count > 1)
                {
                    container = dataContainer.Copy();
                }

                Targets[i].Process(container);
            }
            Targets[Targets.Count - 1].Process(dataContainer);
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

            // Add data container to processing queue.
            if (!_dataQueue.IsCompleted)
                try
                {
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

            foreach (var data in allData)
            {
                var processedData = Process(data);

                if (processedData == null)
                    continue;

                dataContainer.Add(processedData);
            }

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

        #endregion

        protected void Log(string format, params object[] args)
        {
            //DispatcherHelper.RunAsync(() =>
            //{
            //    if (Messages.Count > 100)
            //        Messages.RemoveAt(100);

            //    Messages.Insert(0, string.Format(format, args));
            //});
        }
    }
}
