using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;

namespace Huddle.Engine.Processor
{
    public abstract class BaseImageProcessor<TColor, TDepth> : BaseProcessor
        where TColor : struct, IColor
        where TDepth : new()
    {
        #region static fields

        public static MCvFont EmguFont = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 0.3, 0.3);
        public static MCvFont EmguFontBig = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 1.0, 1.0);

        #endregion

        #region private fields

        private DispatcherOperation _preProcessRendering;
        private DispatcherOperation _postProcessRendering;

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
        [IgnoreDataMember]
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
        [IgnoreDataMember]
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

        public override IData Process(IData data)
        {
            var imageData = data as BaseImageData<TColor, TDepth>;

            return imageData != null ? Process(imageData) : data;
        }

        private IData Process(BaseImageData<TColor, TDepth> data)
        {
            var image = data.Image;

            if (IsRenderContent)
            {
                // draw debug information on image -> TODO might worth be worth it to bind that information to the data template directly
                var preProcessImage = image.Copy();

                Task.Factory.StartNew(() =>
                {
                    if (preProcessImage == null) return null;

                    BitmapSource bitmap;
                    if (preProcessImage is Image<Gray, float>)
                        bitmap = (preProcessImage as Image<Gray, float>).ToGradientBitmapSource(32001, 32002, true);
                    else
                        bitmap = preProcessImage.ToBitmapSource(true);

                    preProcessImage.Dispose();

                    return bitmap;
                }).ContinueWith(s => PreProcessImage = s.Result);
            }

            try
            {
                try
                {
                    var processedImage = ProcessAndView(image);

                    if (processedImage == null) return null;

                    data.Image = processedImage;
                }
                catch (Exception e)
                {
                    Log("Exception occured in ProcessAndView:{0}{1}{2}", e.Message, Environment.NewLine, e.StackTrace);
                }
            }
            catch (Exception)
            {
                //DispatcherHelper.RunAsync(() =>
                //{
                //    //if (!Messages.Any(m => Equals(e.Message, m)))
                //    //    Log(e.Message);
                //});
                return data;
            }

            if (IsRenderContent)
            {
                var postProcessImage = data.Image.Copy();

                Task.Factory.StartNew(() =>
                {
                    if (postProcessImage == null) return null;

                    BitmapSource bitmap;
                    if (postProcessImage is Image<Gray, float>)
                        bitmap = (postProcessImage as Image<Gray, float>).ToGradientBitmapSource(32001, 32002, true);
                    else
                        bitmap = postProcessImage.ToBitmapSource(true);

                    postProcessImage.Dispose();

                    return bitmap;
                }).ContinueWith(s => PostProcessImage = s.Result);
            }
            return data;
        }

        public virtual Image<TColor, TDepth> PreProcess(Image<TColor, TDepth> image)
        {
            return image;
        }

        public abstract Image<TColor, TDepth> ProcessAndView(Image<TColor, TDepth> image);
    }
}
