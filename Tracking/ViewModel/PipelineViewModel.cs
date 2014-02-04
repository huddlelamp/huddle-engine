using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using GalaSoft.MvvmLight.Command;
using Tools.FlockingDevice.Tracking.Controls;
using Tools.FlockingDevice.Tracking.Extensions;
using Tools.FlockingDevice.Tracking.Model;
using Tools.FlockingDevice.Tracking.Processor;
using Tools.FlockingDevice.Tracking.Processor.OpenCv;
using Tools.FlockingDevice.Tracking.Properties;
using Application = System.Windows.Application;
using DataObject = System.Windows.Forms.DataObject;
using DragDropEffects = System.Windows.Forms.DragDropEffects;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    public class PipelineViewModel : ProcessorViewModelBase<BaseProcessor>
    {
        #region commands

        #region control commands

        public RelayCommand StartCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }
        public RelayCommand ResumeCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand LoadCommand { get; private set; }

        #endregion

        #region Drag & Drop commands

        public RelayCommand<MouseButtonEventArgs> DragInitiateCommand { get; private set; }

        #endregion

        #endregion

        #region properties

        #region ProcessorTypes

        /// <summary>
        /// The <see cref="ProcessorTypes" /> property's name.
        /// </summary>
        public const string ProcessorTypesPropertyName = "ProcessorTypes";

        private ObservableCollection<Type> _processorTypes = new ObservableCollection<Type>();

        /// <summary>
        /// Sets and gets the ProcessorTypes property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<Type> ProcessorTypes
        {
            get
            {
                return _processorTypes;
            }

            set
            {
                if (_processorTypes == value)
                {
                    return;
                }

                RaisePropertyChanging(ProcessorTypesPropertyName);
                _processorTypes = value;
                RaisePropertyChanged(ProcessorTypesPropertyName);
            }
        }

        #endregion

        #region Processors

        /// <summary>
        /// The <see cref="Processors" /> property's name.
        /// </summary>
        public const string ProcessorsPropertyName = "Processors";

        private ObservableCollection<ProcessorViewModel> _processors = new ObservableCollection<ProcessorViewModel>();

        /// <summary>
        /// Sets and gets the Processors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<ProcessorViewModel> Processors
        {
            get
            {
                return _processors;
            }

            set
            {
                if (_processors == value)
                {
                    return;
                }

                RaisePropertyChanging(ProcessorsPropertyName);
                _processors = value;
                RaisePropertyChanged(ProcessorsPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public PipelineViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                ProcessorTypes.Add(typeof(Basics));
                ProcessorTypes.Add(typeof(CannyEdges));
                ProcessorTypes.Add(typeof(FindContours));
                ProcessorTypes.Add(typeof(BlobTracker));
            }
            else
            {
                ProcessorTypes = new ObservableCollection<Type>(GetTypes<BaseProcessor>());
            }

            // exit hook to stop input source
            Application.Current.Exit += (s, e) =>
            {
                Stop();
                Save();
            };


            StartCommand = new RelayCommand(Start);
            StopCommand = new RelayCommand(Stop);
            PauseCommand = new RelayCommand(Pause);
            ResumeCommand = new RelayCommand(Resume);
            SaveCommand = new RelayCommand(Save);
            LoadCommand = new RelayCommand(Load);

            DragInitiateCommand = new RelayCommand<MouseButtonEventArgs>(OnDragInitiate);

            Load();
        }

        #endregion

        protected override void OnAdd(BaseProcessor processor)
        {
            Processors.Add(new ProcessorViewModel { Model = processor });
        }

        protected override void OnRemove(BaseProcessor processor)
        {
            Processors.RemoveAll(vm => Equals(processor, vm.Model));
        }

        public void Start()
        {
            foreach (var processor in Processors)
                processor.Start();
        }

        public void Stop()
        {
            foreach (var processor in Processors)
                processor.Stop();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        #region private methods

        private void Save()
        {
            var filename = Settings.Default.PipelineFilename;
            var tempFilename = String.Format("{0}.tmp", filename);

            try
            {
                var serializer = new XmlSerializer(typeof(Pipeline));
                using (var stream = new FileStream(tempFilename, FileMode.Create))
                {
                    var xmlTextWriter = XmlWriter.Create(stream, new XmlWriterSettings { NewLineChars = Environment.NewLine, Indent = true });
                    serializer.Serialize(xmlTextWriter, new Pipeline
                    {
                        Processors = new List<BaseProcessor>(Processors.Select(vm => vm.Model))
                    });
                }

                var bakFilename = String.Format("{0}.bak", Settings.Default.PipelineFilename);
                File.Replace(tempFilename, filename, bakFilename);
            }
            catch (Exception e)
            {
                ExceptionMessageBox.ShowException(e, String.Format(@"Could not save pipeline.{0}Exception Message: {1}", Environment.NewLine, e.Message));
            }
        }

        private void Load()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Pipeline));
                using (var stream = new FileStream(Settings.Default.PipelineFilename, FileMode.Open))
                {
                    var pipeline = serializer.Deserialize(stream) as Pipeline;

                    if (pipeline == null)
                        throw new Exception(@"Could not load pipeline");

                    foreach (var processor in pipeline.Processors)
                    {
                        Processors.Add(BuildRecursiveViewModel(processor));
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not load pipeline. {0}.", e.Message), @"Pipeline Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static ProcessorViewModel BuildRecursiveViewModel(BaseProcessor processor)
        {
            var processorViewModel = new ProcessorViewModel { Model = processor };

            foreach (var child in processor.Children)
            {
                var childViewModel = BuildRecursiveViewModel(child);
                childViewModel.ParentProcessor = processorViewModel;
                processorViewModel.ChildProcessors.Add(childViewModel);
            }

            return processorViewModel;
        }

        private void OnDragInitiate(MouseButtonEventArgs e)
        {
            var element = e.Source as FrameworkElement;

            if (element == null) return;

            var type = (Type)element.DataContext;

            if (type == null) return;

            // Initialize the drag & drop operation
            var format = typeof(BaseProcessor).IsAssignableFrom(type)
                ? typeof(BaseProcessor).Name
                : null;

            var dragData = new System.Windows.DataObject(format, type);

            DragDrop.DoDragDrop(element, dragData, System.Windows.DragDropEffects.Copy);
        }

        private static IEnumerable<Type> GetTypes<T>()
        {
            return from t in Assembly.GetExecutingAssembly().GetTypes()
                   where typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface
                   select t;
        }

        #endregion
    }
}
