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
using Tools.FlockingDevice.Tracking.Model;
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

        #region Pipeline

        /// <summary>
        /// The <see cref="Pipeline" /> property's name.
        /// </summary>
        public const string PipelinePropertyName = "Pipeline";

        private Pipeline _pipeline;

        /// <summary>
        /// Sets and gets the Pipeline property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public Pipeline Pipeline
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

        #endregion

        #region ctor

        public InputSourceViewModel()
        {
            // exit hook to stop input source
            Application.Current.Exit += (s, e) => Stop();

            PropertyChanged += (_s, _e) =>
            {
                if (Equals(PipelinePropertyName, _e.PropertyName))
                {
                    Pipeline.PropertyChanging += (s, e) =>
                    {
                        if (Pipeline.InputSource == null) return;

                        switch (e.PropertyName)
                        {
                            case Pipeline.InputSourcePropertyName:
                                Pipeline.InputSource.Stop();
                                Pipeline.InputSource.ImageReady -= OnImageReady;
                                break;
                        }
                    };

                    Pipeline.PropertyChanged += (s, e) =>
                    {
                        if (Pipeline.InputSource == null) return;

                        switch (e.PropertyName)
                        {
                            case Pipeline.InputSourcePropertyName:
                                Pipeline.InputSource.ImageReady += OnImageReady;
                                break;
                        }
                    };
                }
            };

            RemoveProcessorCommand = new RelayCommand<RgbProcessor>(processor =>
            {
                if (Pipeline == null) return;

                Pipeline.ColorImageProcessors.Remove(processor);
                Pipeline.DepthImageProcessors.Remove(processor);
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
            OnDropTarget(Pipeline.ColorImageProcessors, e);
        }

        private void OnDropTargetDepth(DragEventArgs e)
        {
            OnDropTarget(Pipeline.DepthImageProcessors, e);
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
                processor = Activator.CreateInstance(processorType, Pipeline.InputSource) as RgbProcessor;
            }

            toTarget.Add(processor); // add to new collection
        }

        #endregion

        #region public methods

        public void Start()
        {
            if (Pipeline != null && Pipeline.InputSource != null)
                Pipeline.InputSource.Start();
        }

        public void Stop()
        {
            if (Pipeline != null && Pipeline.InputSource != null)
                Pipeline.InputSource.Stop();
        }

        public void Pause()
        {
            if (Pipeline != null && Pipeline.InputSource != null)
                Pipeline.InputSource.Pause();
        }

        public void Resume()
        {
            if (Pipeline != null && Pipeline.InputSource != null)
                Pipeline.InputSource.Resume();
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

            if (Pipeline.ColorImageProcessors.Any())
                foreach (var colorProcessor in Pipeline.ColorImageProcessors.ToArray())
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

            if (Pipeline.DepthImageProcessors.Any())
                foreach (var depthProcessor in Pipeline.DepthImageProcessors.ToArray())
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
