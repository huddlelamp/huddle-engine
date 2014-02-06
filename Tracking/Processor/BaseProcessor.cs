using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using GalaSoft.MvvmLight;
using Tools.FlockingDevice.Tracking.Data;
using System.Xml.Serialization;
using Tools.FlockingDevice.Tracking.Processor.BarCodes;
using Tools.FlockingDevice.Tracking.Processor.OpenCv;
using Tools.FlockingDevice.Tracking.Processor.Sensors;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [XmlInclude(typeof(Senz3D))]
    [XmlInclude(typeof(Basics))]
    [XmlInclude(typeof(BlobTracker))]
    [XmlInclude(typeof(CannyEdges))]
    [XmlInclude(typeof(ErodeDilate))]
    [XmlInclude(typeof(FindContours))]
    [XmlInclude(typeof(QRCodeDecoder))]
    [XmlInclude(typeof(VideoRecordAndPlay))]
    [XmlInclude(typeof(DataTypeFilter))]
    public abstract class BaseProcessor : ObservableObject, IProcessor
    {
        #region private fields

        private Thread _processingThread;

        // A bounded collection. It can hold no more than 100 items at once.
        private BlockingCollection<IDataContainer> _dataQueue = new BlockingCollection<IDataContainer>(100);

        private bool _processing;

        #endregion

        #region properties

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

        #region Children

        private ObservableCollection<BaseProcessor> _children = new ObservableCollection<BaseProcessor>();

        [XmlArray("Children")]
        [XmlArrayItem("Processor")]
        public ObservableCollection<BaseProcessor> Children
        {
            get { return _children; }
            set { _children = value; }
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

            // Start sub-processor
            foreach (var processor in Children)
            {
                processor.Start();
            }

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
                        continue;
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

            // Stop sub-processor
            foreach (var processor in Children)
            {
                processor.Stop();
            }
        }

        public void Publish(IDataContainer dataContainer)
        {
            if (!Children.Any())
            {
                dataContainer.Dispose();
                return;
            }

            for (var i = 0; i < Children.Count - 1; i++)
            {
                var container = dataContainer;
                if (dataContainer.Count > 1)
                {
                    container = dataContainer.Copy();
                }

                Children[i].Process(container);
            }
            Children[Children.Count - 1].Process(dataContainer);
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
                Publish(dataContainer);

            // Add data container to processing queue.
            if (!_dataQueue.IsCompleted)
                _dataQueue.Add(dataContainer);
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
