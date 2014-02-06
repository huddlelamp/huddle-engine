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
using Tools.FlockingDevice.Tracking.Model;
using Tools.FlockingDevice.Tracking.Processor;
using Tools.FlockingDevice.Tracking.Processor.OpenCv;
using Tools.FlockingDevice.Tracking.Properties;
using Tools.FlockingDevice.Tracking.Util;
using Application = System.Windows.Application;
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

        #region Mode

        /// <summary>
        /// The <see cref="Mode" /> property's name.
        /// </summary>
        public const string ModePropertyName = "Mode";

        private PipelineMode _mode = PipelineMode.Stopped;

        /// <summary>
        /// Sets and gets the Mode property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public PipelineMode Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                if (_mode == value)
                {
                    return;
                }

                RaisePropertyChanging(ModePropertyName);
                _mode = value;
                RaisePropertyChanged(ModePropertyName);
            }
        }

        #endregion

        #region ProcessorTypes

        /// <summary>
        /// The <see cref="ProcessorTypes" /> property's name.
        /// </summary>
        public const string ProcessorTypesPropertyName = "ProcessorTypes";

        private ObservableCollection<ViewTemplateAttribute> _processorTypes = new ObservableCollection<ViewTemplateAttribute>();

        /// <summary>
        /// Sets and gets the ProcessorTypes property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<ViewTemplateAttribute> ProcessorTypes
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

        #endregion

        #region ctor

        public PipelineViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.
                //ProcessorTypes.Add(typeof(Basics));
                //ProcessorTypes.Add(typeof(CannyEdges));
                //ProcessorTypes.Add(typeof(FindContours));
                //ProcessorTypes.Add(typeof(BlobTracker));
            }
            else
            {
                var types = GetAttributesFromType<ViewTemplateAttribute, BaseProcessor>().ToArray();

                ProcessorTypes = new ObservableCollection<ViewTemplateAttribute>(types);
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

        public override void Start()
        {
            foreach (var processor in ChildProcessors)
                processor.Start();

            Mode = PipelineMode.Started;
        }

        public override void Stop()
        {
            foreach (var processor in ChildProcessors)
                processor.Stop();

            Mode = PipelineMode.Stopped;
        }

        public void Pause()
        {
            Mode = PipelineMode.Paused;

            throw new NotImplementedException();
        }

        public void Resume()
        {
            Start();

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
                        Processors = new List<BaseProcessor>(ChildProcessors.Select(vm => vm.Model))
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
                        var processorViewModel = BuildRecursiveViewModel(processor);
                        processorViewModel.ParentProcessor = this;
                        ChildProcessors.Add(processorViewModel);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Could not load pipeline. {0}.", e.Message), @"Pipeline Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static ProcessorViewModelBase<BaseProcessor> BuildRecursiveViewModel(BaseProcessor processor)
        {
            var processorViewModel = new ProcessorViewModelBase<BaseProcessor> { Model = processor };

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

            var viewTemplate = (ViewTemplateAttribute)element.DataContext;

            if (viewTemplate == null) return;

            // Initialize the drag & drop operation
            var format = typeof(BaseProcessor).IsAssignableFrom(viewTemplate.Type)
                ? typeof(BaseProcessor).Name
                : null;

            var dragData = new System.Windows.DataObject(format, viewTemplate.Type);

            DragDrop.DoDragDrop(element, dragData, System.Windows.DragDropEffects.Copy);
        }

        private static IEnumerable<TA> GetAttributesFromType<TA, T>()
            where TA : ViewTemplateAttribute
        {
            var types = from t in Assembly.GetExecutingAssembly().GetTypes()
                        where typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface
                        select t;

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<TA>();

                if (attr != null)
                {
                    attr.Type = type;
                    yield return attr;
                }
            }
        }

        #endregion
    }
}
