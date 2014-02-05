using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using Tools.FlockingDevice.Tracking.Extensions;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    public class ProcessorViewModel : ProcessorViewModelBase<BaseProcessor>
    {
        #region commands

        public RelayCommand<BaseProcessor> RemoveProcessorCommand { get; private set; }

        #endregion

        #region properties

        #region Model

        /// <summary>
        /// The <see cref="Model" /> property's name.
        /// </summary>
        public const string ModelPropertyName = "Model";

        private BaseProcessor _model;

        /// <summary>
        /// Sets and gets the Model property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public BaseProcessor Model
        {
            get
            {
                return _model;
            }

            set
            {
                if (_model == value)
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

        private ProcessorViewModel _parentProcessor;

        /// <summary>
        /// Sets and gets the ParentProcessor property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ProcessorViewModel ParentProcessor
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

        private ObservableCollection<ProcessorViewModel> _childProcessors = new ObservableCollection<ProcessorViewModel>();

        /// <summary>
        /// Sets and gets the ChildProcessors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<ProcessorViewModel> ChildProcessors
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

        public ProcessorViewModel()
        {
            //PropertyChanged += (s, e) =>
            //{
            //    switch (e.PropertyName)
            //    {
            //        case ModelPropertyName:
            //            if (Model != null)
            //                Model.Children.CollectionChanged += (s2, e2) =>
            //                {
            //                    switch (e2.Action)
            //                    {
            //                        case NotifyCollectionChangedAction.Add:
            //                            foreach (var processor in from object item in e2.NewItems select item as BaseProcessor)
            //                            {
            //                                if (processor == null) return;

            //                                ChildProcessors.Add(new ProcessorViewModel { Model = processor });
            //                            }
            //                            break;
            //                        case NotifyCollectionChangedAction.Remove:
            //                            foreach (var item in e2.NewItems)
            //                            {
            //                                var processor = item as BaseProcessor;
            //                                if (processor == null) return;

            //                                ChildProcessors.RemoveAll(vm => Equals(vm.Model, processor));
            //                            }
            //                            break;
            //                    }
            //                };
            //            break;
            //    }
            //};

            RemoveProcessorCommand = new RelayCommand<BaseProcessor>(processor =>
            {
                if (Model == null || Model.Children == null) return;

                Model.Children.Remove(processor);
            });
        }

        protected override void OnAdd(BaseProcessor processor)
        {
            Model.Children.Add(processor);
            ChildProcessors.Add(new ProcessorViewModel { Model = processor });
        }

        protected override void OnRemove(BaseProcessor processor)
        {
            if (ParentProcessor == null) return;
            
            ParentProcessor.Model.Children.Remove(processor);
            ParentProcessor.ChildProcessors.Remove(this);
        }

        #endregion

        public void Start()
        {
            if (Model != null)
                Model.Start();
        }

        public void Stop()
        {
            if (Model != null)
                Model.Stop();
        }
    }
}
