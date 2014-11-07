using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Huddle.Engine.Extensions;
using Huddle.Engine.Model;
using Huddle.Engine.Processor;
using Huddle.Engine.Util;
using Microsoft.Win32;

namespace Huddle.Engine.ViewModel
{
    public abstract class ProcessorViewModelBase<T> : ViewModelBase//, IDisposable
        where T : BaseProcessor
    {
        #region private fields

        private ILocator _dragLocator;

        #endregion

        #region commands

        #region Drag & Drop commands

        public RelayCommand<DragEventArgs> DragOverCommand { get; private set; }

        public RelayCommand<DragEventArgs> DragEnterCommand { get; private set; }

        public RelayCommand<DragEventArgs> DragLeaveCommand { get; private set; }

        public RelayCommand<SenderAwareEventArgs> DragSourceStartCommand { get; private set; }

        public RelayCommand<SenderAwareEventArgs> DragSourceMoveCommand { get; private set; }

        public RelayCommand<SenderAwareEventArgs> DragSourceEndCommand { get; private set; }

        public RelayCommand<DragEventArgs> DropSourceCommand { get; private set; }

        public RelayCommand TakeSnapshotCommand { get; private set; }

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

        #region Pipeline

        /// <summary>
        /// The <see cref="Pipeline" /> property's name.
        /// </summary>
        public const string PipelinePropertyName = "Pipeline";

        private PipelineViewModel _pipeline;

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

            #region DragSourceStartCommand

            DragSourceStartCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                var sender = args.Sender as IInputElement;
                var e = args.OriginalEventArgs as MouseEventArgs;

                if (e == null) return;

                //e.MouseDevice.Capture(sender);

                var element = e.Source as FrameworkElement;

                if (element == null) return;

                var processorViewModel = element.DataContext as ProcessorViewModelBase<T>;

                if (processorViewModel == null) return;

                var dragData = new DataObject(typeof(ProcessorViewModelBase<T>).Name, processorViewModel);

                var position = e.GetPosition(sender);

                DragDrop.DoDragDrop(element, dragData, DragDropEffects.Copy);

                //_dragLocator = new DragLocator
                //{
                //    X = position.X,
                //    Y = position.Y
                //};

                //Pipeline.Pipes.Add(new PipeViewModel(Model, _dragLocator));

                //e.Handled = true;
            });

            #endregion

            #region DragSourceMoveCommand

            DragSourceMoveCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                var e = args.OriginalEventArgs as MouseEventArgs;

                if (e == null) return;

                if (_dragLocator == null)
                    return;

                var sender = args.Sender as IInputElement;

                var position = e.GetPosition(sender);

                if (_dragLocator == null) return;

                _dragLocator.X = position.X;
                _dragLocator.Y = position.Y;
            });

            #endregion

            #region DragSourceEndCommand

            DragSourceEndCommand = new RelayCommand<SenderAwareEventArgs>(args =>
            {
                Console.WriteLine("echo");

                var sender = args.Sender as IInputElement;

                if (sender == null) return;

                Pipeline.Pipes.RemoveAll(p => Equals(p.Source, Model));
                _dragLocator = null;

                sender.ReleaseMouseCapture();
            });

            #endregion

            #region DragOverCommand

            DragOverCommand = new RelayCommand<DragEventArgs>(
                e =>
                {
                    if (!e.Data.GetFormats().Any(f => Equals(typeof(T).Name, f)))
                        e.Effects = DragDropEffects.None;

                    if (!e.Data.GetFormats().Any(f => Equals(typeof(ProcessorViewModelBase<T>).Name, f))) return;
                    var sourceProcessor = e.Data.GetData(typeof(ProcessorViewModelBase<T>).Name) as ProcessorViewModelBase<T>;

                    if (sourceProcessor != null && !Equals(Model, sourceProcessor.Model))
                        e.Effects = DragDropEffects.Copy;
                    else
                        e.Effects = DragDropEffects.None;
                });

            #endregion

            #region DragEnterCommand

            DragEnterCommand = new RelayCommand<DragEventArgs>(e =>
            {
                if (!e.Data.GetFormats().Any(f => Equals(typeof(ProcessorViewModelBase<T>).Name, f))) return;
                var sourceProcessor = e.Data.GetData(typeof(ProcessorViewModelBase<T>).Name) as ProcessorViewModelBase<T>;

                if (sourceProcessor == null || Equals(Model, sourceProcessor.Model))
                    return;

                IsDragOver = true;
            });

            #endregion

            #region DragLeaveCommand

            DragLeaveCommand = new RelayCommand<DragEventArgs>(e =>
            {
                IsDragOver = false;
            });

            #endregion

            #region DropSourceCommand

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

            #endregion

            RemoveCommand = new RelayCommand(OnRemove);

            TakeSnapshotCommand = new RelayCommand(OnTakeSnapshot);

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

        protected virtual void ConnectToSource(ProcessorViewModelBase<T> source)
        {
            Pipeline.AddPipe(source.Model, Model);
        }

        protected virtual void OnRemove()
        {
            var deleteProcessor = MessageBox.Show("Do you want to delete processor?", "Delete Processor", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (deleteProcessor != MessageBoxResult.Yes) return;

            // Stop processing (ViewModel and Model)
            Stop();

            // Relocate incoming and outgoing connections
            RelocateSources();
            RelocateTargets();

            Pipeline.Model.Targets.Remove(Model);
            Pipeline.Processors.RemoveAll(p => Equals(p.Model, Model));
            Pipeline.Pipes.RemoveAll(pvm => Equals(pvm.Source, Model) || Equals(pvm.Target, Model));

            // Remove this processor target from all sources
            foreach (var source in Model.Sources)
                source.Targets.Remove(Model);

            // Remove this processor source from all targets
            foreach (var target in Model.Targets)
                target.Sources.Remove(Model);

            Model.Sources.Clear();
            Model.Targets.Clear();

            //Dispose();
        }

        public virtual void Start()
        {
            Model.Start();

            // Set green border to indicate running state...
        }

        public virtual void Stop()
        {
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
        private void OnTakeSnapshot()
        {
            var snapshoter = Model as ISnapshoter;

            if (snapshoter != null)
            {
                var task = Task.Factory.StartNew<Bitmap[]>(snapshoter.TakeSnapshots);

                task.ContinueWith(t =>
                {
                    var snapshotBitmaps = t.Result;

                    if (snapshotBitmaps == null)
                    {
                        MessageBox.Show(string.Format("Snapshot function not implemented for {0}.", Model.GetType().Name));
                        return;
                    }

                    if (snapshotBitmaps.Length == 0)
                    {
                        MessageBox.Show("No images available.");
                        return;
                    }

                    var i = 1;
                    foreach (var bitmap in snapshotBitmaps)
                    {
                        var dialog = new SaveFileDialog
                        {
                            Title = string.Format("Snaptshot of Image {0}", i++),
                            Filter = "Snapshot Image|*.png"
                        };
                        var result = dialog.ShowDialog(Application.Current.MainWindow);

                        if (!result.Value) return;

                        var filename = dialog.FileName;

                        using (var newBitmap = new Bitmap(bitmap))
                        {
                            newBitmap.Save(filename, ImageFormat.Png);
                        }
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }
}
