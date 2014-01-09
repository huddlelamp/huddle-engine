using System;
using System.Drawing;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.External.Extensions;
using Emgu.CV.Structure;
using GalaSoft.MvvmLight.Threading;
using Tools.FlockingDevice.Tracking.Properties;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor
{
    [ViewTemplate("DepthProcessor")]
    public class DepthProcessor : RgbProcessor
    {
        #region properties

        #region MinReproducedDepth

        /// <summary>
        /// The <see cref="MinReproducedDepth" /> property's name.
        /// </summary>
        public const string MinReproducedDepthPropertyName = "MinReproducedDepth";

        private double _minReproducedDepth = Settings.Default.MinReproducedDepth;

        /// <summary>
        /// Sets and gets the MinReproducedDepth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public double MinReproducedDepth
        {
            get
            {
                return _minReproducedDepth;
            }

            set
            {
                if (_minReproducedDepth == value)
                {
                    return;
                }

                RaisePropertyChanging(MinReproducedDepthPropertyName);
                _minReproducedDepth = value;
                RaisePropertyChanged(MinReproducedDepthPropertyName);
            }
        }

        #endregion

        #region MaxReproducedDepth

        /// <summary>
        /// The <see cref="MaxReproducedDepth" /> property's name.
        /// </summary>
        public const string MaxReproducedDepthPropertyName = "MaxReproducedDepth";

        private double _maxReproducedDepth = Settings.Default.MaxReproducedDepth;

        /// <summary>
        /// Sets and gets the MaxReproducedDepth property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlAttribute]
        public double MaxReproducedDepth
        {
            get
            {
                return _maxReproducedDepth;
            }

            set
            {
                if (_maxReproducedDepth == value)
                {
                    return;
                }

                RaisePropertyChanging(MaxReproducedDepthPropertyName);
                _maxReproducedDepth = value;
                RaisePropertyChanged(MaxReproducedDepthPropertyName);
            }
        }

        #endregion

        #endregion

        public DepthProcessor()
        {
            MinReproducedDepth = 0;
        }

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var outputImage = new Image<Hsv, double>(image.Width, image.Height);

            // draw gradient legend
            for (var cx = 0; cx < outputImage.Width; cx++)
            {
                var c = new Hsv(120.0 - (cx / (double)outputImage.Width * 120.0), 255.0, 255.0);
                outputImage[0, cx] = c;
                outputImage[1, cx] = c;
                outputImage[2, cx] = c;
            }

            // draw depth values as HSV
            for (var y = 3; y < outputImage.Height; y++)
            {
                for (var x = 0; x < outputImage.Width; x++)
                {
                    // TODO fix me!!
                    var cin = 1000; //image[y, x].Intensity;
            
                    if (cin >= MinReproducedDepth && cin <= MaxReproducedDepth)
                    {
                        var h = 120.0 - ((cin - MinReproducedDepth) / (MaxReproducedDepth - MinReproducedDepth) * 120.0);
                        outputImage[y, x] = new Hsv(h, 255.0, 255.0);
                    }
                    else
                        outputImage[y, x] = new Hsv(0.0, 0.0, 0.0);
                }

            }
            
            var message = outputImage.Width + " x " + outputImage.Height + " [rd: " + MinReproducedDepth + " ," + MaxReproducedDepth + "]";
            outputImage.Draw(message, ref EmguFont, new Point(5, 15), new Hsv(0, 0, 0));

            return outputImage.Convert<Rgb, byte>();
        }
    }
}
