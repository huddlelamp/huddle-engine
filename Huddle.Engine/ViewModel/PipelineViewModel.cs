using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using GalaSoft.MvvmLight.Command;
using Huddle.Engine.Controls;
using Huddle.Engine.InkCanvas;
using Huddle.Engine.Model;
using Huddle.Engine.Processor;
using Huddle.Engine.Properties;
using Huddle.Engine.Util;
using Huddle.Engine.View;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit.Zoombox;

namespace Huddle.Engine.ViewModel
{
    public class PipelineViewModel : ProcessorViewModelBase<Pipeline>
    {
        #region prviate fields

        private readonly DataContractSerializer _serializer = new DataContractSerializer(typeof(Pipeline), null,
                                                                0x7FFF /*maxItemsInObjectGraph*/,
                                                                false /*ignoreExtensionDataObject*/,
                                                                true /*preserveObjectReferences : this is where the magic happens */,
                                                                null /*dataContractSurrogate*/);

        #endregion

        #region commands

        #region control commands

        public RelayCommand StartCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        public RelayCommand PauseCommand { get; private set; }
        public RelayCommand ResumeCommand { get; private set; }
        public RelayCommand NewCommand { get; private set; }
        public RelayCommand OpenCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }

        public RelayCommand<ProcessorViewModelBase<BaseProcessor>> MoveZIndexUpCommand { get; private set; }

        public RelayCommand<SenderAwareEventArgs> StrokeCollectedCommand { get; private set; }

        #endregion

        #region Drag & Drop commands

        public RelayCommand<MouseButtonEventArgs> DragInitiateCommand { get; private set; }
        public RelayCommand<SenderAwareEventArgs> DropProcessorCommand { get; private set; }

        #endregion

        #region Zoombox commands

        public RelayCommand<SenderAwareEventArgs> ZoomCommand { get; private set; }

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

        #region Processors

        /// <summary>
        /// The <see cref="Processors" /> property's name.
        /// </summary>
        public const string ProcessorsPropertyName = "Processors";

        private ObservableCollection<ProcessorViewModelBase<BaseProcessor>> _processors = new ObservableCollection<ProcessorViewModelBase<BaseProcessor>>();

        /// <summary>
        /// Sets and gets the Processors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<ProcessorViewModelBase<BaseProcessor>> Processors
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

        #region Pipes

        /// <summary>
        /// The <see cref="Pipes" /> property's name.
        /// </summary>
        public const string PipesPropertyName = "Pipes";

        private ObservableCollection<PipeViewModel> _pipes = new ObservableCollection<PipeViewModel>();

        /// <summary>
        /// Sets and gets the Pipes property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<PipeViewModel> Pipes
        {
            get
            {
                return _pipes;
            }

            set
            {
                if (_pipes == value)
                {
                    return;
                }

                RaisePropertyChanging(PipesPropertyName);
                _pipes = value;
                RaisePropertyChanged(PipesPropertyName);
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
                var types = GetAttributesFromType<ViewTemplateAttribute, BaseProcessor>().ToList();
                types.Sort(new ProcessorComparer());

                ProcessorTypes = new ObservableCollection<ViewTemplateAttribute>(types);
            }

            // exit hook to stop input source
            Application.Current.Exit += (s, e) =>
            {
                Stop();
                Save(Settings.Default.DefaultPipelineFileName);
            };

            Model = new Pipeline();

            StartCommand = new RelayCommand(Start);
            StopCommand = new RelayCommand(Stop);
            PauseCommand = new RelayCommand(Pause);
            ResumeCommand = new RelayCommand(Resume);
            NewCommand = new RelayCommand(OnNew);
            OpenCommand = new RelayCommand(OnOpen);
            SaveCommand = new RelayCommand(OnSave);

            MoveZIndexUpCommand = new RelayCommand<ProcessorViewModelBase<BaseProcessor>>(p =>
            {
                foreach (var processor in Processors)
                    processor.Model.ZIndex = 0;

                p.Model.ZIndex = 1;
            });

            StrokeCollectedCommand = new RelayCommand<SenderAwareEventArgs>(OnStrokeCollected);

            DragInitiateCommand = new RelayCommand<MouseButtonEventArgs>(e =>
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
            });

            DropProcessorCommand = new RelayCommand<SenderAwareEventArgs>(e =>
            {
                var args = e.OriginalEventArgs as DragEventArgs;

                if (args == null) return;

                if (!args.Data.GetFormats().Any(f => Equals(typeof(BaseProcessor).Name, f))) return;
                var type = args.Data.GetData(typeof(BaseProcessor).Name) as Type;

                if (type == null)
                    return;

                var processor = (BaseProcessor)Activator.CreateInstance(type);

                // drop position
                var sender = e.Sender as IInputElement;
                if (sender != null)
                {
                    var position = args.GetPosition(sender);
                    processor.X = position.X;
                    processor.Y = position.Y;
                }

                AddNode(processor);

                //RaisePropertyChanged(ProcessorsPropertyName);

                IsDragOver = false;
            });

            ZoomCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                var sender = args.Sender as Zoombox;
                var e = args.OriginalEventArgs as MouseWheelEventArgs;

                if (sender == null || e == null) return;

                var zoom = e.Delta / 500.0;

                sender.Zoom(zoom, e.GetPosition(sender));
            });

            Load(Settings.Default.DefaultPipelineFileName);
        }

        #endregion

        public override void Start()
        {
            foreach (var processor in Processors)
                processor.Start();

            Mode = PipelineMode.Started;
        }

        public override void Stop()
        {
            foreach (var processor in Processors)
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

        private void OnStrokeCollected(SenderAwareEventArgs e)
        {
            var inkCanvas = e.Sender as AdvancedInkCanvas;
            var eventArgs = e.OriginalEventArgs as StrokeEventArgs;

            var pg = PathGeometry.CreateFromGeometry(eventArgs.Stroke.GetGeometry());

            pg.Transform = new ScaleTransform(Model.Scale, Model.Scale);

            //if (eventArgs.Device == Device.Stylus)
            //{
            //    //throw new Exception("Analyze text");
            //    Console.WriteLine("STROKE {0}", AnalyzeStroke(eventArgs.Stroke));
            //    RecognizeGesture(pg);
            //}
            //else if (eventArgs.Device == Device.StylusInverted)
            //{
            //    var nodesToDelete = HitTestHelper.GetElementsInGeometry<UserControl>(pg, inkCanvas)
            //        .Select(view => view.DataContext)
            //        .OfType<QueryCanvasNodeViewModel>().ToArray();

            //    foreach (var vm in nodesToDelete)
            //    {
            //        var node = vm.Model as QueryCanvasNode;
            //        Debug.Assert(node != null, "node != null");

            //        Model.RemoveNode(node);
            //    }
            //}

            var elementsInGeometry = HitTestHelper.GetElementsInGeometry<PipeView>(pg, inkCanvas);

            var linksToDelete = elementsInGeometry.Select(view => view.DataContext)
                .OfType<PipeViewModel>().ToArray();

            ////TODO: add method to model, which is capable of deleting more than one link safely
            foreach (var vm in linksToDelete)
            {
                var source = vm.Source as BaseProcessor;
                var target = vm.Target as BaseProcessor;

                if (source != null)
                    source.Targets.Remove(target);

                if (target != null)
                    target.Sources.Remove(source);

                Pipes.Remove(vm);
            }
        }

        private void OnNew()
        {
            var defaultPipeline = MessageBox.Show("Do you want create an new pipeline?", "Default Pipeline", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (defaultPipeline != MessageBoxResult.Yes) return;

            foreach (var processor in Processors)
                processor.Stop();

            Model.Stop();

            Processors.Clear();
            Pipes.Clear();
            Model = new Pipeline();
        }

        private void OnOpen()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "Huddle Engine Pipeline|*.pipeline.huddle",
                Multiselect = false
            };

            var result = openDialog.ShowDialog(Application.Current.MainWindow);

            if (!result.Value) return;

            var fileName = openDialog.FileName;

            AskForSaveAsDefaultPipeline(fileName);

            Load(fileName);
        }

        private void OnSave()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Huddle Engine Pipeline|*.pipeline.huddle"
            };

            var result = saveDialog.ShowDialog(Application.Current.MainWindow);

            if (!result.Value) return;

            var fileName = saveDialog.FileName;

            AskForSaveAsDefaultPipeline(fileName);

            Save(fileName);
        }

        private void Save(string fileName)
        {
            var tempFilename = String.Format("{0}.tmp", fileName);

            try
            {
                using (var stream = new FileStream(tempFilename, FileMode.Create))
                {
                    var xmlTextWriter = XmlWriter.Create(stream, new XmlWriterSettings { NewLineOnAttributes = true, Indent = true });
                    _serializer.WriteObject(stream, Model);
                    //serializer.WriteObject(stream, new Test() { A = "ASDF" });
                }

                var bakFilename = String.Format("{0}.bak", Settings.Default.DefaultPipelineFileName);

                if (File.Exists(fileName))
                    File.Replace(tempFilename, fileName, bakFilename);
                else
                    File.Move(tempFilename, fileName);
            }
            catch (Exception e)
            {
                ExceptionMessageBox.ShowException(e, String.Format(@"Could not save pipeline.{0}Exception Message: {1}", Environment.NewLine, e.Message));
            }
        }

        private void Load(string fileName)
        {
            Processors.Clear();
            Pipes.Clear();

            try
            {
                //var serializer = new XmlSerializer(typeof(Pipeline));
                using (var stream = new FileStream(fileName, FileMode.Open))
                {
                    Model = _serializer.ReadObject(stream) as Pipeline ?? new Pipeline();

                    foreach (var target in Model.Targets)
                    {
                        AddNode(target);
                    }

                    BuildPipeViewModels(Model);
                }
            }
            catch (Exception e)
            {
                ExceptionMessageBox.ShowException(e, String.Format("Could not load pipeline. {0}.", e.Message));
            }
        }

        private static void AskForSaveAsDefaultPipeline(string fileName)
        {
            var defaultPipeline = MessageBox.Show("Do you want to load this pipeline on next program start?", "Default Pipeline", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (defaultPipeline != MessageBoxResult.Yes) return;

            Settings.Default.DefaultPipelineFileName = fileName;
            Settings.Default.Save();
        }

        private void BuildPipeViewModels(Pipeline pipeline)
        {
            foreach (var source in pipeline.Targets)
            {
                foreach (var target in source.Targets)
                {
                    Pipes.Add(new PipeViewModel(source, target));
                }
            }
        }

        //private static ProcessorViewModelBase<BaseProcessor> BuildRecursiveViewModel(BaseProcessor processor)
        //{
        //    var processorViewModel = new ProcessorViewModelBase<BaseProcessor>
        //    {
        //        IgnoreCollectionChanges = true,
        //        Model = processor
        //    };

        //    foreach (var target in processor.Targets)
        //    {
        //        var childViewModel = BuildRecursiveViewModel(target);

        //        childViewModel.Sources.Add(processorViewModel);
        //        processorViewModel.Targets.Add(childViewModel);
        //    }

        //    foreach (var source in processor.Sources)
        //    {

        //    }

        //    processorViewModel.IgnoreCollectionChanges = false;

        //    return processorViewModel;
        //}

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

        private void AddNode(BaseProcessor node)
        {
            if (!Model.Targets.Contains(node))
                Model.Targets.Add(node);

            var nodeViewModel = new NodeViewModel
            {
                Model = node,
                Pipeline = this
            };
            Processors.Add(nodeViewModel);

            if (Equals(Mode, PipelineMode.Started))
                nodeViewModel.Start();
        }

        internal void AddPipe(BaseProcessor source, BaseProcessor target)
        {
            var sourceHasTarget = source.Targets.Any(t => Equals(t, target));
            var targetHasSource = target.Sources.Any(s => Equals(s, source));

            // Do not pipe only once
            if (sourceHasTarget && targetHasSource)
                return;

            source.Targets.Add(target);
            target.Sources.Add(source);

            Pipes.Add(new PipeViewModel(source, target));
        }

        #endregion

        private class ProcessorComparer : IComparer<ViewTemplateAttribute>
        {
            public int Compare(ViewTemplateAttribute x, ViewTemplateAttribute y)
            {
                return String.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }
        }}
}
