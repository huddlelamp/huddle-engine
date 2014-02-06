using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Tools.FlockingDevice.Tracking.Model;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    public class ProcessorViewModelBase<T> : ViewModelBase
        where T : BaseProcessor
    {
        #region commands

        #region Drag & Drop commands

        public RelayCommand<DragEventArgs> DragOverCommand { get; private set; }

        public RelayCommand<DragEventArgs> DragEnterCommand { get; private set; }

        public RelayCommand<DragEventArgs> DragLeaveCommand { get; private set; }

        public RelayCommand<MouseButtonEventArgs> DragSourceInitiateCommand { get; private set; }

        public RelayCommand<DragEventArgs> DropSourceCommand { get; private set; }

        #endregion

        public RelayCommand RemoveCommand { get; private set; }

        #endregion

        #region properties

        #region Model

        /// <summary>
        /// The <see cref="Model" /> property's name.
        /// </summary>
        public const string ModelPropertyName = "Model";

        private T _model;

        /// <summary>
        /// Sets and gets the Model property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public T Model
        {
            get
            {
                return _model;
            }

            set
            {
                if (Equals(_model, value))
                {
                    return;
                }

                RaisePropertyChanging(ModelPropertyName);
                _model = value;
                RaisePropertyChanged(ModelPropertyName);
            }
        }

        #endregion

        #region ParentProcessor

        /// <summary>
        /// The <see cref="ParentProcessor" /> property's name.
        /// </summary>
        public const string ParentProcessorPropertyName = "ParentProcessor";

        private ProcessorViewModelBase<T> _parentProcessor;

        /// <summary>
        /// Sets and gets the ParentProcessor property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ProcessorViewModelBase<T> ParentProcessor
        {
            get
            {
                return _parentProcessor;
            }

            set
            {
                if (_parentProcessor == value)
                {
                    return;
                }

                RaisePropertyChanging(ParentProcessorPropertyName);
                _parentProcessor = value;
                RaisePropertyChanged(ParentProcessorPropertyName);
            }
        }

        #endregion

        #region Children

        /// <summary>
        /// The <see cref="Children" /> property's name.
        /// </summary>
        public const string ChildProcessorsPropertyName = "Children";

        private ObservableCollection<ProcessorViewModelBase<T>> _children = new ObservableCollection<ProcessorViewModelBase<T>>();

        /// <summary>
        /// Sets and gets the Children property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<ProcessorViewModelBase<T>> Children
        {
            get
            {
                return _children;
            }

            set
            {
                if (_children == value)
                {
                    return;
                }

                RaisePropertyChanging(ChildProcessorsPropertyName);
                _children = value;
                RaisePropertyChanged(ChildProcessorsPropertyName);
            }
        }

        #endregion

        #region IsDragOver

        /// <summary>
        /// The <see cref="IsDragOver" /> property's name.
        /// </summary>
        public const string IsDragOverPropertyName = "IsDragOver";

        private bool _isDragOver = false;

        /// <summary>
        /// Sets and gets the IsDragOver property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsDragOver
        {
            get
            {
                return _isDragOver;
            }

            set
            {
                if (_isDragOver == value)
                {
                    return;
                }

                RaisePropertyChanging(IsDragOverPropertyName);
                _isDragOver = value;
                RaisePropertyChanged(IsDragOverPropertyName);
            }
        }

        #endregion

        #region Logs

        /// <summary>
        /// The <see cref="ProcessorViewModel.Logs" /> property's name.
        /// </summary>
        public const string LogsPropertyName = "Logs";

        private ObservableCollection<string> _logs = new ObservableCollection<string>();

        /// <summary>
        /// Sets and gets the Logs property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
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

        #endregion

        #region ctor

        internal ProcessorViewModelBase()
        {
            #region Drag & Drop

            DragSourceInitiateCommand = new RelayCommand<MouseButtonEventArgs>(OnDragSourceInitiate);

            DragOverCommand = new RelayCommand<DragEventArgs>(
                e =>
                {
                    if (!e.Data.GetFormats().Any(f => Equals(typeof(T).Name, f)))
                    {
                        e.Effects = DragDropEffects.None;
                    }
                });

            DragEnterCommand = new RelayCommand<DragEventArgs>(e =>
            {
                IsDragOver = true;
                e.Handled = true;
            });
            DragLeaveCommand = new RelayCommand<DragEventArgs>(e =>
            {
                IsDragOver = false;
                e.Handled = true;
            });

            DropSourceCommand = new RelayCommand<DragEventArgs>(e =>
            {
                if (!e.Data.GetFormats().Any(f => Equals(typeof(ProcessorViewModelBase<T>).Name, f))) return;
                var sourceProcessor = e.Data.GetData(typeof(ProcessorViewModelBase<T>).Name) as ProcessorViewModelBase<T>;

                if (sourceProcessor == null)
                    return;

                OnAdd(sourceProcessor);

                IsDragOver = false;

                e.Handled = true;
            });

            #endregion

            RemoveCommand = new RelayCommand(OnRemove);
        }

        #endregion

        private void OnDragSourceInitiate(MouseButtonEventArgs e)
        {
            var element = e.Source as FrameworkElement;

            if (element == null) return;

            var processorViewModel = element.DataContext as ProcessorViewModelBase<T>;

            if (processorViewModel == null) return;

            var dragData = new DataObject(typeof(ProcessorViewModelBase<T>).Name, processorViewModel);

            DragDrop.DoDragDrop(element, dragData, DragDropEffects.Copy);
        }

        protected virtual void OnAdd(ProcessorViewModelBase<T> processorViewModel)
        {
            processorViewModel.ParentProcessor = this;
            processorViewModel.Model.Children.Add(Model);
        }

        protected virtual void OnRemove()
        {
            Stop();

            // Relocate child processors
            foreach (var childProcessor in Children)
            {
                childProcessor.Stop();

                childProcessor.ParentProcessor = ParentProcessor;
                ParentProcessor.Children.Add(childProcessor);

                if (ParentProcessor.Model != null)
                    ParentProcessor.Model.Children.Add(childProcessor.Model);
            }

            if (ParentProcessor.Model != null)
                ParentProcessor.Model.Children.Remove(Model);

            ParentProcessor.Children.Remove(this);

            ParentProcessor.RaisePropertyChanged(ChildProcessorsPropertyName);
        }

        public virtual void Start()
        {
            if (Model != null)
                Model.Start();
        }

        public virtual void Stop()
        {
            if (Model != null)
                Model.Stop();
        }
    }
}
