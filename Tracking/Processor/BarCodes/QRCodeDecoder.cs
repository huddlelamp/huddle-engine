using System;
using System.Drawing;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Tools.FlockingDevice.Tracking.Properties;
using Tools.FlockingDevice.Tracking.Util;
using System.Threading;

namespace Tools.FlockingDevice.Tracking.Processor.QRCodes
{
    [XmlType]
    [ViewTemplate("QRCodeDecoder")]
    public class QRCodeDecoder : RgbProcessor
    {
        #region public properties

        #region FriendlyName

        public override string FriendlyName
        {
            get
            {
                return "QR Code Decoder";
            }
        }

        #endregion


        #region OutputText

        /// <summary>
        /// The <see cref="OutputText" /> property's name.
        /// </summary>
        public const string OutputTextPropertyName = "OutputText";

        private string _outputText = "";

        /// <summary>
        /// Sets and gets the OutputText property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>

        [XmlIgnore]
        public string OutputText
        {
            get
            {
                return _outputText;
            }

            set
            {
                if (_outputText == value)
                {
                    return;
                }

                RaisePropertyChanging(OutputTextPropertyName);
                _outputText = value;
                RaisePropertyChanged(OutputTextPropertyName);
            }
        }

        #endregion


        #region TryHarder

        /// <summary>
        /// The <see cref="TryHarder" /> property's name.
        /// </summary>
        public const string TryHarderPropertyName = "TryHarder";

        private bool _tryHarder = Settings.Default.TryHarder;

        /// <summary>
        /// Sets and gets the TryHarder property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [XmlIgnore]
        public bool TryHarder
        {
            get
            {
                return _tryHarder;
            }

            set
            {
                if (_tryHarder == value)
                {
                    return;
                }

                RaisePropertyChanging(TryHarderPropertyName);
                _tryHarder = value;
                RaisePropertyChanged(TryHarderPropertyName);
            }
        }

        #endregion

        #endregion

        private IBarcodeReaderImage reader = null;
        private Rgb[] pointColors = { new Rgb(Color.Red), new Rgb(Color.Green), new Rgb(Color.Blue) };

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            if (reader == null)
            {
                reader = new BarcodeReaderImage()
                {
                    PossibleFormats = new System.Collections.Generic.List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE },
                    AutoRotate = false
                };                
            }
            reader.Options.TryHarder = TryHarder;


            // get (hopefully) all visible QR codes
            ZXing.Result[] results;
            try
            {
                results = reader.DecodeMultiple(image);
            }
            catch (Exception e)
            {               
                // sometimes there are some less important exceptions about the version of the QR codes
                return image;
            }

            var num_QRs = 0;
            if (results != null)
            {
                num_QRs = results.Length;
                OutputText = "Found " + num_QRs + " QR tags.\n";
            }
            else
            {
                OutputText = "Failed.";
                base.DrawDebug(image);
                return image;
            }

            // Process found QR codes
            
            for (int i = 0; i < num_QRs;i++)
            {
                // Get content of tag from results[i].Text
                string qrText  = results[i].Text;
              
                // Get corner points of tag from results[i].ResultPoints
                var qrPoints = results[i].ResultPoints;

                for (int j = 0; j<qrPoints.Length; j++)
                {
                    image.Draw(
                        new CircleF(
                            new PointF(qrPoints[j].X,qrPoints[j].Y), 5
                            ),
                        pointColors[j], 
                        3);
                }
                image.Draw(
                    new LineSegment2DF(
                        new PointF(qrPoints[0].X, qrPoints[0].Y),
                        new PointF(qrPoints[1].X, qrPoints[1].Y)
                        ), 
                    new Rgb(Color.Red), 5
                    );

                var dx = qrPoints[1].X - qrPoints[0].X;
                var dy = qrPoints[1].Y - qrPoints[0].Y;

                // Get orientation of tag
                var qrOrientation = Math.Atan2(dy, dx)/Math.PI*180 + 90;

                OutputText += "\""+qrText+"\"\norient:("+qrOrientation+"°)\n\n";
            }
            
            //base.DrawDebug(image);
            return image;
        }
    }
}
