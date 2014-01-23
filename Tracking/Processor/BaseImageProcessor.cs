using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Threading;
using Tools.FlockingDevice.Tracking.Data;

namespace Tools.FlockingDevice.Tracking.Processor
{
    public abstract class BaseImageProcessor<TColor, TDepth> : BaseProcessor, IProcessor
        where TColor : struct, IColor
        where TDepth : new()
    {
        #region static fields

        public static MCvFont EmguFont = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 0.3, 0.3);

        #endregion

        #region FiendlyName

        public virtual string FriendlyName
        {
            get
            {
                return GetType().Name;
            }
        }

        #endregion

        #region IsRenderImage

        /// <summary>
        /// The <see cref="IsRenderImages" /> property's name.
        /// </summary>
        public const string IsRenderImagesPropertyName = "IsRenderImages";

        private bool _isRenderImages = true;

        /// <summary>
        /// Sets and gets the IsRenderImages property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public bool IsRenderImages
        {
            get
            {
                return _isRenderImages;
            }

            set
            {
                if (_isRenderImages == value)
                {
                    return;
                }

                RaisePropertyChanging(IsRenderImagesPropertyName);
                _isRenderImages = value;
                RaisePropertyChanged(IsRenderImagesPropertyName);
            }
        }

        #endregion

        #region PreProcessImage

        /// <summary>
        /// The <see cref="PreProcessImage" /> property's name.
        /// </summary>
        public const string PreProcessImagePropertyName = "PreProcessImage";

        private BitmapSource _preProcessImage;

        /// <summary>
        /// Sets and gets the PreProcessImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore]
        public BitmapSource PreProcessImage
        {
            get
            {
                return _preProcessImage;
            }

            set
            {
                if (_preProcessImage == value)
                {
                    return;
                }

                RaisePropertyChanging(PreProcessImagePropertyName);
                _preProcessImage = value;
                RaisePropertyChanged(PreProcessImagePropertyName);
            }
        }

        #endregion

        #region PostProcessImage

        /// <summary>
        /// The <see cref="PostProcessImage" /> property's name.
        /// </summary>
        public const string PostProcessImagePropertyName = "PostProcessImage";

        private BitmapSource _postProcessImage;

        /// <summary>
        /// Sets and gets the PostProcessImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore]
        public BitmapSource PostProcessImage
        {
            get
            {
                return _postProcessImage;
            }

            set
            {
                if (_postProcessImage == value)
                {
                    return;
                }

                RaisePropertyChanging(PostProcessImagePropertyName);
                _postProcessImage = value;
                RaisePropertyChanged(PostProcessImagePropertyName);
            }
        }

        #endregion

        #region Messages

        /// <summary>
        /// The <see cref="Messages" /> property's name.
        /// </summary>
        public const string MessagesPropertyName = "Messages";

        private ObservableCollection<string> _messages = new ObservableCollection<string>();

        /// <summary>
        /// Sets and gets the Messages property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore]
        public ObservableCollection<string> Messages
        {
            get
            {
                return _messages;
            }

            set
            {
                if (_messages == value)
                {
                    return;
                }

                RaisePropertyChanging(MessagesPropertyName);
                _messages = value;
                RaisePropertyChanged(MessagesPropertyName);
            }
        }

        #endregion

        public IData[] Process(IData[] data)
        {
            foreach (var d in data)
            {
                var imageData = d as ImageData<TColor, TDepth>;

                if (imageData == null)
                    continue;

                Process(imageData);
            }

            return data;
        }

        protected IData Process(ImageData<TColor, TDepth> data)
        {
            var image = data.Image;

            if (IsRenderImages)
            {
                // the copy is required in order to not influence processing that happens later
                var preProcessImage = PreProcess(image.Copy());

                // draw debug information on image -> TODO might worth be worth it to bind that information to the data template directly
                DrawDebug(preProcessImage);

                DispatcherHelper.RunAsync(() =>
                {
                    if (preProcessImage == null) return;

                    PreProcessImage = preProcessImage.ToBitmapSource();
                    preProcessImage.Dispose();
                });
            }

            try
            {
                data.Image = ProcessAndView(image);
            }
            catch (Exception e)
            {
                DispatcherHelper.RunAsync(() =>
                {
                    if (!Messages.Any(m => Equals(e.Message, m)))
                        Log(e.Message);
                });
                return data;
            }

            if (IsRenderImages)
            {
                var postProcessImage = data.Image.Copy();
                DispatcherHelper.RunAsync(() =>
                {
                    if (postProcessImage == null) return;

                    PostProcessImage = postProcessImage.ToBitmapSource();
                    postProcessImage.Dispose();
                });
            }
            return data;
        }

        public virtual Image<TColor, TDepth> PreProcess(Image<TColor, TDepth> image)
        {
            return image;
        }

        public abstract Image<TColor, TDepth> ProcessAndView(Image<TColor, TDepth> image);

        protected virtual void DrawDebug(Image<TColor, TDepth> image)
        {
            // empty
        }

        protected void Log(string format, params object[] args)
        {
            DispatcherHelper.RunAsync(() =>
            {
                if (Messages.Count > 100)
                    Messages.RemoveAt(100);

                Messages.Insert(0, string.Format(format, args));
            });
        }
    }
}
