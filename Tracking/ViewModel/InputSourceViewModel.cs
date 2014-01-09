using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using Emgu.CV.External.Extensions;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Tools.FlockingDevice.Tracking.InputSource;
using Tools.FlockingDevice.Tracking.Processor;

namespace Tools.FlockingDevice.Tracking.ViewModel
{
    public class InputSourceViewModel : ViewModelBase
    {
        #region commands

        public RelayCommand<RgbProcessor> RemoveProcessorCommand { get; private set; }

        public RelayCommand<DragEventArgs> DragOverCommand { get; private set; }

        public RelayCommand<DragEventArgs> DropSourceCommand { get; private set; }

        public RelayCommand<DragEventArgs> DropTargetColorCommand { get; private set; }

        public RelayCommand<DragEventArgs> DropTargetDepthCommand { get; private set; }

        #endregion

        #region properties

        #region Model

        /// <summary>
        /// The <see cref="Model" /> property's name.
        /// </summary>
        public const string ModelPropertyName = "Model";

        private IInputSource _model;

        /// <summary>
        /// Sets and gets the Model property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public IInputSource Model
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

        #region ColorImage

        /// <summary>
        /// The <see cref="ColorImage" /> property's name.
        /// </summary>
        public const string ColorImagePropertyName = "ColorImage";

        private BitmapSource _colorImage;

        /// <summary>
        /// Sets and gets the ColorImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public BitmapSource ColorImage
        {
            get
            {
                return _colorImage;
            }

            set
            {
                if (_colorImage == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorImagePropertyName);
                _colorImage = value;
                RaisePropertyChanged(ColorImagePropertyName);
            }
        }

        #endregion

        #region DepthImage

        /// <summary>
        /// The <see cref="DepthImage" /> property's name.
        /// </summary>
        public const string DepthImagePropertyName = "DepthImage";

        private BitmapSource _depthImage;

        /// <summary>
        /// Sets and gets the DepthImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public BitmapSource DepthImage
        {
            get
            {
                return _depthImage;
            }

            set
            {
                if (_depthImage == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthImagePropertyName);
                _depthImage = value;
                RaisePropertyChanged(DepthImagePropertyName);
            }
        }

        #endregion

        #region ColorImageProcessors

        /// <summary>
        /// The <see cref="ColorImageProcessors" /> property's name.
        /// </summary>
        public const string ColorImageProcessorsPropertyName = "ColorImageProcessors";

        private ObservableCollection<RgbProcessor> _colorImageProcessors = new ObservableCollection<RgbProcessor>();

        /// <summary>
        /// Sets and gets the ColorImageProcessors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<RgbProcessor> ColorImageProcessors
        {
            get
            {
                return _colorImageProcessors;
            }

            set
            {
                if (_colorImageProcessors == value)
                {
                    return;
                }

                RaisePropertyChanging(ColorImageProcessorsPropertyName);
                _colorImageProcessors = value;
                RaisePropertyChanged(ColorImageProcessorsPropertyName);
            }
        }

        #endregion

        #region DepthImageProcessors

        /// <summary>
        /// The <see cref="DepthImageProcessors" /> property's name.
        /// </summary>
        public const string DepthImageProcessorsPropertyName = "DepthImageProcessors";

        private ObservableCollection<RgbProcessor> _depthImageProcessors = new ObservableCollection<RgbProcessor>();

        /// <summary>
        /// Sets and gets the DepthImageProcessors property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public ObservableCollection<RgbProcessor> DepthImageProcessors
        {
            get
            {
                return _depthImageProcessors;
            }

            set
            {
                if (_depthImageProcessors == value)
                {
                    return;
                }

                RaisePropertyChanging(DepthImageProcessorsPropertyName);
                _depthImageProcessors = value;
                RaisePropertyChanged(DepthImageProcessorsPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public InputSourceViewModel()
        {
            // exit hook to stop input source
            Application.Current.Exit += (s, e) => Stop();

            PropertyChanging += (s, e) =>
            {
                if (Model == null) return;

                switch (e.PropertyName)
                {
                    case ModelPropertyName:
                        Model.Stop();
                        Model.ImageReady -= OnImageReady;
                        break;
                }
            };

            PropertyChanged += (s, e) =>
            {
                if (Model == null) return;

                switch (e.PropertyName)
                {
                    case ModelPropertyName:
                        Model.ImageReady += OnImageReady;
                        break;
                }
            };

            RemoveProcessorCommand = new RelayCommand<RgbProcessor>(processor =>
            {
                ColorImageProcessors.Remove(processor);
                DepthImageProcessors.Remove(processor);
            });

            #region Drag & Drop

            DragOverCommand = new RelayCommand<DragEventArgs>(
                e =>
                {
                    if (!e.Data.GetFormats().Any(f => Equals(typeof(RgbProcessor).Name, f)) &&
                        !e.Data.GetFormats().Any(f => Equals(typeof(IInputSource).Name, f)))
                    {
                        e.Effects = DragDropEffects.None;
                    }

                    //var currentMousePosition = e.GetPosition(_topLevelGrid);

                    //if (_topLevelGrid != null && _draggedAdorner != null)
                    //    _draggedAdorner.UpdateAdornerPosition(_topLevelGrid, currentMousePosition);
                });

            DropTargetColorCommand = new RelayCommand<DragEventArgs>(OnDropTargetColor);
            DropTargetDepthCommand = new RelayCommand<DragEventArgs>(OnDropTargetDepth);

            // if dropping on the source list, remove the adorner
            DropSourceCommand = new RelayCommand<DragEventArgs>(e =>
            {
                //RemoveAdorner(_listBoxItem, _topLevelGrid)
            });

            #endregion
        }

        private void OnDropTargetColor(DragEventArgs e)
        {
            OnDropTarget(ColorImageProcessors, e);
        }

        private void OnDropTargetDepth(DragEventArgs e)
        {
            OnDropTarget(DepthImageProcessors, e);
        }

        private void OnDropTarget(ICollection<RgbProcessor> toTarget, DragEventArgs e)
        {
            if (!e.Data.GetFormats().Any(f => Equals(typeof(RgbProcessor).Name, f))) return;
            var processorType = e.Data.GetData(typeof(RgbProcessor).Name) as Type;

            if (processorType == null)
                return;

            RgbProcessor processor;
            try
            {
                processor = Activator.CreateInstance(processorType) as RgbProcessor;
            }
            catch (Exception)
            {
                processor = Activator.CreateInstance(processorType, Model) as RgbProcessor;
            }

            toTarget.Add(processor); // add to new collection
        }

        #endregion

        #region public methods

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

        public void Pause()
        {
            if (Model != null)
                Model.Pause();
        }

        public void Resume()
        {
            if (Model != null)
                Model.Resume();
        }

        #endregion

        #region Image Source

        private void OnImageReady(object sender, ImageEventArgs2 e)
        {
            //Console.WriteLine("OnImageReady");

            #region Color Image Handling

            //if (_colorImageTask != null)
            //    _colorImageTask.Wait();

            //_colorImageTask = Task.Factory.StartNew(() =>
            //{
            var colorImage = e.ColorImage;

            var colorImageCopy = colorImage.Copy();
            DispatcherHelper.RunAsync(() =>
            {
                ColorImage = colorImageCopy.ToBitmapSource();
                colorImageCopy.Dispose();
            });

            if (ColorImageProcessors.Any())
                foreach (var colorProcessor in ColorImageProcessors.ToArray())
                {
                    colorImage = colorProcessor.Process(colorImage);
                    //image.Dispose();
                }
            //});

            #endregion

            #region Depth Image Handling

            //if (_depthImageTask != null)
            //    _depthImageTask.Wait();

            //_depthImageTask = Task.Factory.StartNew(() =>
            //{
            var depthImage = e.DepthImage;

            var depthImageCopy = depthImage.Copy();
            DispatcherHelper.RunAsync(() =>
            {
                DepthImage = depthImageCopy.ToBitmapSource();
                depthImageCopy.Dispose();
            });

            if (DepthImageProcessors.Any())
                foreach (var depthProcessor in DepthImageProcessors.ToArray())
                {
                    depthImage = depthProcessor.Process(depthImage);
                    //image.Dispose();
                }
            //});

            #endregion

            Thread.Sleep(25);
        }

        #endregion
    }
}
