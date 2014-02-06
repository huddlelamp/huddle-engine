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

        public RelayCommand<DragEventArgs> DropSourceCommand { get; private set; }

        public RelayCommand<DragEventArgs> DropTargetCommand { get; private set; }

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

        #region ChildProcessors

        /// <summary>
        /// The <see cref="ChildProcessors" /> property's name.
        /// </summary>
        public const string ChildProcessorsPropertyName = "ChildProcessors";

        private ObservableCollection<ProcessorViewModelBase<T>> _childProcessors = new ObservableCollection<ProcessorViewModelBase<T>>();

        /// <summary>
        /// Sets and gets the ChildProcessors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<ProcessorViewModelBase<T>> ChildProcessors
        {
            get
            {
                return _childProcessors;
            }

            set
            {
                if (_childProcessors == value)
                {
                    return;
                }

                RaisePropertyChanging(ChildProcessorsPropertyName);
                _childProcessors = value;
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

            DropTargetCommand = new RelayCommand<DragEventArgs>(e =>
            {
                if (!e.Data.GetFormats().Any(f => Equals(typeof(T).Name, f))) return;
                var type = e.Data.GetData(typeof(T).Name) as Type;

                if (type == null)
                    return;

                T t;
                try
                {
                    t = (T)Activator.CreateInstance(type);
                }
                catch (Exception)
                {
                    t = default(T);
                }
                OnAdd(t);

                IsDragOver = false;
            });

            #endregion

            RemoveCommand = new RelayCommand(OnRemove);
        }

        #endregion

        protected virtual void OnAdd(T processor)
        {
            if (Model != null)
                Model.Children.Add(processor);

            var childProcessor = new ProcessorViewModelBase<T>
            {
                Model = processor,
                ParentProcessor = this
            };
            ChildProcessors.Add(childProcessor);

            RaisePropertyChanged(ChildProcessorsPropertyName);

            var parent = this;
            while (!(parent is PipelineViewModel))
            {
                parent = parent.ParentProcessor;
            }

            if (parent != null)
            {
                var pipeline = parent as PipelineViewModel;

                if (pipeline != null)
                    switch (pipeline.Mode)
                    {
                        case PipelineMode.Started:
                            childProcessor.Start();
                            break;
                        //case PipelineMode.Paused:
                        //    childProcessor.Pause();
                        //    break;
                    }
            }
        }

        protected virtual void OnRemove()
        {
            Stop();

            // Relocate child processors
            foreach (var childProcessor in ChildProcessors)
            {
                childProcessor.Stop();

                childProcessor.ParentProcessor = ParentProcessor;
                ParentProcessor.ChildProcessors.Add(childProcessor);

                if (ParentProcessor.Model != null)
                    ParentProcessor.Model.Children.Add(childProcessor.Model);
            }

            if (ParentProcessor.Model != null)
                ParentProcessor.Model.Children.Remove(Model);

            ParentProcessor.ChildProcessors.Remove(this);

            ParentProcessor.RaisePropertyChanged(ChildProcessorsPropertyName);

            foreach (var childProcessor in ParentProcessor.ChildProcessors)
                childProcessor.Start();
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
