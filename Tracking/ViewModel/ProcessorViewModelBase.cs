using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Tools.FlockingDevice.Tracking.Model;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    public class ProcessorViewModelBase<T> : ViewModelBase//, IDisposable
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

        #region IgnoreCollectionChanges

        /// <summary>
        /// The <see cref="IgnoreCollectionChanges" /> property's name.
        /// </summary>
        public const string IgnoreCollectionChangesPropertyName = "IgnoreCollectionChanges";

        private bool _ignoreCollectionChanges = false;

        /// <summary>
        /// Sets and gets the IgnoreCollectionChanges property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IgnoreCollectionChanges
        {
            get
            {
                return _ignoreCollectionChanges;
            }

            set
            {
                if (_ignoreCollectionChanges == value)
                {
                    return;
                }

                RaisePropertyChanging(IgnoreCollectionChangesPropertyName);
                _ignoreCollectionChanges = value;
                RaisePropertyChanged(IgnoreCollectionChangesPropertyName);
            }
        }

        #endregion

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

                ConnectToSource(sourceProcessor);

                IsDragOver = false;

                e.Handled = true;
            });

            #endregion

            RemoveCommand = new RelayCommand(OnRemove);

            #region Register for ViewModel Changes

            //PropertyChanging += (s, e) =>
            //{
            //    switch (e.PropertyName)
            //    {
            //        case SourcesPropertyName:
            //            Sources.CollectionChanged -= SourcesOnCollectionChanged;
            //            break;
            //        case TargetsPropertyName:
            //            Targets.CollectionChanged -= TargetsOnCollectionChanged;
            //            break;
            //    }
            //};

            //PropertyChanged += (s, e) =>
            //{
            //    switch (e.PropertyName)
            //    {
            //        case SourcesPropertyName:
            //            Sources.CollectionChanged += SourcesOnCollectionChanged;
            //            break;
            //        case TargetsPropertyName:
            //            Targets.CollectionChanged += TargetsOnCollectionChanged;
            //            break;
            //    }
            //};

            //if (Sources != null)
            //    Sources.CollectionChanged += SourcesOnCollectionChanged;

            //if (Targets != null)
            //    Targets.CollectionChanged += TargetsOnCollectionChanged;

            #endregion
        }

        #endregion

        #region Model Changes

        private void SourcesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Used because of DataContractSerializer
            if (IgnoreCollectionChanges) return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.OfType<ProcessorViewModelBase<T>>())
                        Model.Sources.Add(item.Model);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.NewItems.OfType<ProcessorViewModelBase<T>>())
                        Model.Sources.Remove(item.Model);
                    break;
            }
        }

        private void TargetsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Used because of DataContractSerializer
            if (IgnoreCollectionChanges) return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.OfType<ProcessorViewModelBase<T>>())
                        Model.Targets.Add(item.Model);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.NewItems.OfType<ProcessorViewModelBase<T>>())
                        Model.Targets.Remove(item.Model);
                    break;
            }
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

        protected virtual void ConnectToSource(ProcessorViewModelBase<T> source)
        {
            source.Model.Targets.Add(Model);
            Model.Sources.Add(source.Model);
        }

        protected virtual void OnRemove()
        {
            // Stop processing (ViewModel and Model)
            Stop();

            // Relocate incoming and outgoing connections
            RelocateSources();
            RelocateTargets();

            // Update UI
            //Sources.RaisePropertyChanged(TargetsPropertyName);

            //Dispose();
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

        #region Relocate Sources/Targets

        private void RelocateSources()
        {
            //// Relocate child processors
            //foreach (var source in Sources)
            //{
            //    // Connect parent processor to target
            //    foreach (var target in Targets)
            //    {
            //        source.Targets.Add(target);
            //    }
            //}
        }

        private void RelocateTargets()
        {
            //// Relocate child processors
            //foreach (var target in Targets)
            //{
            //    // Connect parent processor to target
            //    foreach (var source in Sources)
            //    {
            //        target.Sources.Add(source);
            //    }
            //}
        }

        #endregion

        //public void Dispose()
        //{
        //    Sources.Clear();
        //    Targets.Clear();
        //}
    }
}
