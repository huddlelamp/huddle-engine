using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Navigation;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("Find Display", "FindDisplay")]
    public class FindDisplay : BaseProcessor
    {
        #region const


        #endregion

        #region member fields

        private Image<Rgb, byte> _lastFrame;
        private Image<Rgb, byte> _processedFrame;
        private bool _lastFrameProcessed = false;

        #endregion

        #region properties

        #region FloodFillDifference

        /// <summary>
        /// The <see cref="FloodFillDifference" /> property's name.
        /// </summary>
        public const string FloodFillDifferencePropertyName = "FloodFillDifference";

        private float _floodFillDifference = 25.0f;

        /// <summary>
        /// Sets and gets the FloodFillDifference property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float FloodFillDifference
        {
            get
            {
                return _floodFillDifference;
            }

            set
            {
                if (_floodFillDifference == value)
                {
                    return;
                }

                RaisePropertyChanging(FloodFillDifferencePropertyName);
                _floodFillDifference = value;
                RaisePropertyChanged(FloodFillDifferencePropertyName);
            }
        }

        #endregion

        #endregion

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            if (_lastFrame != null && !_lastFrameProcessed)
            {
                var imageCopy = _lastFrame.Copy();
                foreach (var blob in dataContainer.OfType<BlobData>().ToArray())
                {
                    var area = blob.Area;

                    var width = _lastFrame.Width;
                    var height = _lastFrame.Height;

                    var x = (int) (blob.X*width);
                    var y = (int) (blob.Y*height);
                    var offsetX = (int)(area.X * width);
                    var offsetY = (int)(area.Y * height);

                    var roi = new Rectangle(
                        offsetX,
                        offsetY,
                        (int)(area.Width * width),
                        (int)(area.Height * height)
                        );

                    //_lastFrame.ROI = roi;

                    var seedPoint = new Point(x, y);

                    if (IsRenderContent)
                    {
                        imageCopy.Draw(new Cross2DF(new PointF(x, y), 5, 5), Rgbs.Red, 3);
                    }

                    //MCvConnectedComp comp;
                    //DoFloodFill(ref imageCopy, seedPoint, out comp);
                }
                 
                _processedFrame = imageCopy;

                Stage(new RgbImageData(this, "FindDisplay", imageCopy));
                Push();
            }

            if (_lastFrame != null && _lastFrameProcessed)
            {
                Stage(new RgbImageData(this, "FindDisplay", _processedFrame));
                Push();
            }

            return base.PreProcess(dataContainer);
        }

        public override IData Process(IData data)
        {
            var imageData = data as RgbImageData;

            if (imageData == null) return null;

            _lastFrame = imageData.Image.Copy();

            return null;
        }

        #region private methods

        private void DoFloodFill(ref Image<Rgb, byte> image, Point seedPoint, out MCvConnectedComp comp)
        {
            var grayImage = image.Convert<Gray, byte>();

            var mask = new Image<Gray, byte>(grayImage.Width + 2, grayImage.Height + 2);
            CvInvoke.cvFloodFill(grayImage, seedPoint, new MCvScalar(255), new MCvScalar(FloodFillDifference), new MCvScalar(FloodFillDifference), out comp, CONNECTIVITY.FOUR_CONNECTED, FLOODFILL_FLAG.DEFAULT, mask);
        }

        #endregion
    }
}
