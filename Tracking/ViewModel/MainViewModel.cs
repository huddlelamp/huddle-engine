using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Serialization;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Tools.FlockingDevice.Tracking.Domain;
using Tools.FlockingDevice.Tracking.Extensions;
using Tools.FlockingDevice.Tracking.Model;
using Tools.FlockingDevice.Tracking.Processor;
using Tools.FlockingDevice.Tracking.Processor.OpenCv;
using Tools.FlockingDevice.Tracking.Sources;
using Tools.FlockingDevice.Tracking.Sources.Senz3D;
using Tablet = Tools.FlockingDevice.Tracking.Domain.Tablet;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region private fields

        private static readonly string InputSourceFormat = typeof(IInputSource).Name;

        public static MCvFont EmguFont = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 0.3, 0.3);

        #endregion

        #region commands
        public RelayCommand ClosingCommand { get; private set; }

        public RelayCommand StartDataSourceCommand { get; private set; }
        public RelayCommand StopDataSourceCommand { get; private set; }
        public RelayCommand PauseDataSourceCommand { get; private set; }
        public RelayCommand ResumeDataSourceCommand { get; private set; }

        public RelayCommand SavePipelineCommand { get; private set; }

        public RelayCommand<MouseButtonEventArgs> DragInitiateCommand { get; private set; }
        public RelayCommand<DragEventArgs> DragOverCommand { get; private set; }
        public RelayCommand<DragEventArgs> DropSourceCommand { get; private set; }
        public RelayCommand<DragEventArgs> DropTargetCommand { get; private set; }

        #endregion

        #region public properties

        #region InputSourceTypes

        /// <summary>
        /// The <see cref="InputSourceTypes" /> property's name.
        /// </summary>
        public const string InputSourceTypesPropertyName = "InputSourceTypes";

        private ObservableCollection<Type> _inputSourceTypes;

        /// <summary>
        /// Sets and gets the InputSourceTypes property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<Type> InputSourceTypes
        {
            get
            {
                return _inputSourceTypes;
            }

            set
            {
                if (_inputSourceTypes == value)
                {
                    return;
                }

                RaisePropertyChanging(InputSourceTypesPropertyName);
                _inputSourceTypes = value;
                RaisePropertyChanged(InputSourceTypesPropertyName);
            }
        }

        #endregion

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

        #region Pipeline

        /// <summary>
        /// The <see cref="Pipeline" /> property's name.
        /// </summary>
        public const string PipelinePropertyName = "Pipeline";

        private PipelineViewModel _pipeline = new PipelineViewModel();

        /// <summary>
        /// Sets and gets the Pipeline property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public PipelineViewModel Pipeline
        {
            get
            {
                return _pipeline;
            }

            set
            {
                if (_pipeline == value)
                {
                    return;
                }

                RaisePropertyChanging(PipelinePropertyName);
                _pipeline = value;
                RaisePropertyChanged(PipelinePropertyName);

                RaisePropertyChanged(IsInputSourceSetPropertyName);
            }
        }

        #endregion

        #region IsInputSourceSet

        /// <summary>
        /// The <see cref="IsInputSourceSet" /> property's name.
        /// </summary>
        public const string IsInputSourceSetPropertyName = "IsInputSourceSet";

        /// <summary>
        /// Sets and gets the IsInputSourceSet property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsInputSourceSet
        {
            get { return Pipeline != null; }
        }

        #endregion

        #region Devices

        /// <summary>
        /// The <see cref="Devices" /> property's name.
        /// </summary>
        public const string DevicesPropertyName = "Devices";

        private ObservableCollection<IDevice> _devices = new ObservableCollection<IDevice>();

        /// <summary>
        /// Sets and gets the Devices property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<IDevice> Devices
        {
            get
            {
                return _devices;
            }

            set
            {
                if (_devices == value)
                {
                    return;
                }

                RaisePropertyChanging(DevicesPropertyName);
                _devices = value;
                RaisePropertyChanged(DevicesPropertyName);
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                // Code runs in Blend --> create design time data.

                Devices.Add(new Smartphone { Id = 21, X = 68.3728, Y = 34.7203, Angle = 324.8937 });
                Devices.Add(new Smartphone { Id = 7, X = 86.4733, Y = 76.2743, Angle = 34.6327 });
                Devices.Add(new Tablet { Id = 11, X = 12.4536, Y = 33.7632, Angle = 12.8237 });

                InputSourceTypes.Add(typeof(Senz3DInputSource));

                ProcessorTypes.Add(typeof(Basics));
                ProcessorTypes.Add(typeof(CannyEdges));
                ProcessorTypes.Add(typeof(FindContours));
                ProcessorTypes.Add(typeof(BlobTracker));

                var pipeline = new Pipeline
                {
                    InputSource = new Senz3DInputSource(),
                    
                };
                pipeline.ColorImageProcessors.Add(new Basics());
                pipeline.DepthImageProcessors.Add(new Basics());
                pipeline.DepthImageProcessors.Add(new CannyEdges());

                Pipeline = new PipelineViewModel
                {
                    Model = pipeline
                };
            }
            else
            {
                // Code runs "for real"
            }

            ClosingCommand = new RelayCommand(OnSavePipeline);

            InputSourceTypes = new ObservableCollection<Type>(GetTypes<IInputSource>());
            ProcessorTypes = new ObservableCollection<Type>(GetTypes<RgbProcessor>());

            StartDataSourceCommand = new RelayCommand(() =>
            {
                if (Pipeline != null) try
                    {
                        Pipeline.Start();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
            });
            StopDataSourceCommand = new RelayCommand(() =>
            {
                if (Pipeline != null) Pipeline.Stop();
            });
            PauseDataSourceCommand = new RelayCommand(() =>
            {
                if (Pipeline != null) Pipeline.Pause();
            });
            ResumeDataSourceCommand = new RelayCommand(() =>
            {
                if (Pipeline != null) Pipeline.Resume();
            });

            SavePipelineCommand = new RelayCommand(OnSavePipeline);

            DragInitiateCommand = new RelayCommand<MouseButtonEventArgs>(OnDragInitiate);

            DragOverCommand = new RelayCommand<DragEventArgs>(
                e =>
                {
                    if (!e.Data.GetFormats().Any(f => Equals(typeof(RgbProcessor).Name, f)) &&
                        !e.Data.GetFormats().Any(f => Equals(InputSourceFormat, f)))
                    {
                        e.Effects = DragDropEffects.None;
                    }

                    //var currentMousePosition = e.GetPosition(_topLevelGrid);

                    //if (_topLevelGrid != null && _draggedAdorner != null)
                    //    _draggedAdorner.UpdateAdornerPosition(_topLevelGrid, currentMousePosition);
                });

            DropTargetCommand = new RelayCommand<DragEventArgs>(
                e =>
                {
                    if (!e.Data.GetDataPresent(InputSourceFormat)) return;

                    var inputSourceType = e.Data.GetData(InputSourceFormat) as Type;

                    if (inputSourceType == null) return;

                    var inputSourceModel = Activator.CreateInstance(inputSourceType) as InputSource;

                    Pipeline.Model = new Pipeline();
                    Pipeline.Model.InputSource = inputSourceModel;

                    //// pass in the root grid since its adorner layer was used to add ListBoxItems adorners to
                    //RemoveAdorner(_listBoxItem, _topLevelGrid);
                });

            // if dropping on the source list, remove the adorner
            DropSourceCommand = new RelayCommand<DragEventArgs>(e =>
            {
                //RemoveAdorner(_listBoxItem, _topLevelGrid)
            });

            LoadPipeline();
        }

        private void OnSavePipeline()
        {
            Pipeline.Save();
        }

        private void LoadPipeline()
        {
            Pipeline.Load();
        }

        private void OnDragInitiate(MouseButtonEventArgs e)
        {
            var element = e.Source as FrameworkElement;

            if (element == null) return;

            var type = (Type)element.DataContext;

            if (type == null) return;

            // Initialize the drag & drop operation
            var format = typeof(RgbProcessor).IsAssignableFrom(type)
                ? typeof(RgbProcessor).Name
                : InputSourceFormat;

            var dragData = new DataObject(format, type);

            DragDrop.DoDragDrop(element, dragData, DragDropEffects.Copy);
        }

        private static IEnumerable<Type> GetTypes<T>()
        {
            return from t in Assembly.GetExecutingAssembly().GetTypes()
                   where typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface
                   select t;
        }
    }
}