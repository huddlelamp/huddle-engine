using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Tools.FlockingDevice.Tracking.Processor.QRCodes;
using Tools.FlockingDevice.Tracking.Properties;
using Tools.FlockingDevice.Tracking.Util;

namespace Tools.FlockingDevice.Tracking.Processor.BarCodes
{
    [XmlType]
    [ViewTemplate("QRCodeDecoder")]
    public class QRCodeDecoder : RgbProcessor
    {
        #region private fields

        private IBarcodeReaderImage _barcodeReader;
        private readonly Rgb[] _colors = { Rgbs.Red, Rgbs.Green, Rgbs.Blue, Rgbs.Yellow };

        #endregion

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

        #region ctor

        public QRCodeDecoder()
        {
            _barcodeReader = new BarcodeReaderImage
            {
                AutoRotate = false,
                Options = { PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE } }
            };

            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case TryHarderPropertyName:
                        _barcodeReader.Options.TryHarder = TryHarder;
                        break;
                }
            };
        }

        #endregion

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
        {
            var outputImage = image.Copy();

            // get (hopefully) all visible QR codes
            ZXing.Result[] results;
            try
            {
                results = _barcodeReader.DecodeMultiple(image);
            }
            catch (Exception e)
            {
                // sometimes there are some less important exceptions about the version of the QR codes
                return outputImage;
            }

            var num_QRs = 0;
            if (results != null)
            {
                num_QRs = results.Length;
                Log("Found {0} QR tags", num_QRs);
            }
            else
            {
                Log("Failed");
                //base.DrawDebug(image);
                return outputImage;
            }

            // Process found QR codes

            for (int i = 0; i < num_QRs; i++)
            {
                // Get content of tag from results[i].Text
                var qrText = results[i].Text;

                // Get corner points of tag from results[i].ResultPoints
                var qrPoints = results[i].ResultPoints;

                var colorEnumerator = _colors.GetEnumerator();

                foreach (var point in qrPoints)
                {
                    if (!colorEnumerator.MoveNext())
                    {
                        colorEnumerator.Reset();
                        colorEnumerator.MoveNext();
                    }

                    outputImage.Draw(new CircleF(new PointF(point.X, point.Y), 5), (Rgb)colorEnumerator.Current, 3);
                }

                if (qrPoints.Length >= 2)
                    outputImage.Draw(new LineSegment2DF(new PointF(qrPoints[0].X, qrPoints[0].Y), new PointF(qrPoints[1].X, qrPoints[1].Y)), Rgbs.Red, 5);

                var dx = qrPoints[1].X - qrPoints[0].X;
                var dy = qrPoints[1].Y - qrPoints[0].Y;

                //// Get orientation of tag
                var qrOrientation = Math.Atan2(dy, dx) / Math.PI * 180 + 90;

                Log("Text={0} | Orientation={1}°", qrText, qrOrientation);
            }

            //base.DrawDebug(image);
            return outputImage;
        }
    }
}
