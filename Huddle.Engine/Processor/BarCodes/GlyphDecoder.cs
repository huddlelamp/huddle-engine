using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using AForge;
using AForge.Vision.GlyphRecognition;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Emgu.CV.External.Extensions;
using Huddle.Engine.Data;
using Huddle.Engine.Domain;
using Huddle.Engine.Extensions;
using Huddle.Engine.Util;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.BarCodes
{
    [ViewTemplate("Glyph Decoder", "GlyphDecoder", "/Huddle.Engine;component/Resources/Glyph.png")]
    public class GlyphDecoder : BaseProcessor
    {
        #region static fields

        public static MCvFont EmguFont = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 0.3, 0.3);
        public static MCvFont EmguFontBig = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 1.0, 1.0);

        #endregion

        #region private fields

        // possible number of different glyphs in glyph set
        private const int GLYPHCOUNT = 20;
        private GlyphMetadata[] _glyphTable = new GlyphMetadata[GLYPHCOUNT];

        // number of simultaneous glyphs to search for 
        private const int GLYPHSTOSEARCHFOR = 5;
        private Dictionary<String, Glyph> _recognizedGlyphs = new Dictionary<string, Glyph>(GLYPHSTOSEARCHFOR);
        private Dictionary<String, List<IntPoint>> _recognizedQuads = new Dictionary<String, List<IntPoint>>(GLYPHSTOSEARCHFOR);

        // number of frames to wait for and to store in history 
        private const int GLYPHHISTORYFRAMES = 10;

        private GlyphRecognizer _glyphRecognizer = null;

        private Image<Rgb, Byte> _lastFrame;

        private DispatcherOperation _outputImageRendering;

        internal class GlyphMetadata
        {
            public Glyph glyph;
            public List<double> prevOrientations = new List<double> { -10000.0 };
            public List<double> prevX = new List<double> { -10000.0 };
            public List<double> prevY = new List<double> { -10000.0 };
            public int aliveFrames = 0;
        }


        #endregion

        #region public properties

        #region BinaryThresholdImageSource

        /// <summary>
        /// The <see cref="BinaryThresholdImageSource" /> property's name.
        /// </summary>
        public const string BinaryThresholdImageSourcePropertyName = "BinaryThresholdImageSource";

        private BitmapSource _binaryThresholdImageSource = null;

        /// <summary>
        /// Sets and gets the BinaryThresholdImageSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource BinaryThresholdImageSource
        {
            get
            {
                return _binaryThresholdImageSource;
            }

            set
            {
                if (_binaryThresholdImageSource == value)
                {
                    return;
                }

                RaisePropertyChanging(BinaryThresholdImageSourcePropertyName);
                _binaryThresholdImageSource = value;
                RaisePropertyChanged(BinaryThresholdImageSourcePropertyName);
            }
        }

        #endregion

        #region OutputImage

        /// <summary>
        /// The <see cref="OutputImage" /> property's name.
        /// </summary>
        public const string OutputImagePropertyName = "OutputImage";

        private BitmapSource _outputImage;

        /// <summary>
        /// Sets and gets the OutputImage property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource OutputImage
        {
            get
            {
                return _outputImage;
            }

            set
            {
                if (_outputImage == value)
                {
                    return;
                }

                RaisePropertyChanging(OutputImagePropertyName);
                _outputImage = value;
                RaisePropertyChanged(OutputImagePropertyName);
            }
        }

        #endregion

        #region MinFramesProperty

        /// <summary>
        /// The <see cref="MinFramesProperty" /> property's name.
        /// </summary>
        public const string MinFramesPropertyName = "MinFramesProperty";

        private int _minFramesProperty = 0;

        /// <summary>
        /// Sets and gets the MinFramesProperty property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public int MinFramesProperty
        {
            get
            {
                return _minFramesProperty;
            }

            set
            {
                if (_minFramesProperty == value)
                {
                    return;
                }

                RaisePropertyChanging(MinFramesPropertyName);
                _minFramesProperty = value;
                RaisePropertyChanged(MinFramesPropertyName);
            }
        }

        #endregion

        #region BinaryThreshold

        /// <summary>
        /// The <see cref="BinaryThreshold" /> property's name.
        /// </summary>
        public const string BinaryThresholdPropertyName = "BinaryThreshold";

        private byte _binaryThreshold = 220;

        /// <summary>
        /// Sets and gets the BinaryThreshold property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public byte BinaryThreshold
        {
            get
            {
                return _binaryThreshold;
            }

            set
            {
                if (_binaryThreshold == value)
                {
                    return;
                }

                RaisePropertyChanging(BinaryThresholdPropertyName);
                _binaryThreshold = value;
                RaisePropertyChanged(BinaryThresholdPropertyName);
            }
        }

        #endregion

        #region UseBlobs

        /// <summary>
        /// The <see cref="UseBlobs" /> property's name.
        /// </summary>
        public const string UseBlobsPropertyName = "UseBlobs";

        private bool _useBlobs = false;

        /// <summary>
        /// Sets and gets the UseBlobs property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool UseBlobs
        {
            get
            {
                return _useBlobs;
            }

            set
            {
                if (_useBlobs == value)
                {
                    return;
                }

                RaisePropertyChanging(UseBlobsPropertyName);
                _useBlobs = value;
                RaisePropertyChanged(UseBlobsPropertyName);
            }
        }

        #endregion

        #region RoiExpandFactor

        /// <summary>
        /// The <see cref="RoiExpandFactor" /> property's name.
        /// </summary>
        public const string RoiExpandFactorPropertyName = "RoiExpandFactor";

        private float _roiExpandFactor = 0.02f;

        /// <summary>
        /// Sets and gets the RoiExpandFactor property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public float RoiExpandFactor
        {
            get
            {
                return _roiExpandFactor;
            }

            set
            {
                if (_roiExpandFactor == value)
                {
                    return;
                }

                RaisePropertyChanging(RoiExpandFactorPropertyName);
                _roiExpandFactor = value;
                RaisePropertyChanged(RoiExpandFactorPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public GlyphDecoder()
        {
            // create glyph database for 5x5 glyphs
            GlyphDatabase glyphDatabase = new GlyphDatabase(5);

            try
            {
                using (StreamReader sr = new StreamReader("tagdefinition.txt"))
                {
                    String line;
                    do
                    {
                        line = sr.ReadLine();
                        if (line != null)
                        {
                            var tokens = line.Split(' ');
                            var tagID = tokens[0];
                            var code = tokens[1];

                            var matrix = new byte[5, 5];

                            int i = 0;
                            for (int y = 0; y < 5; y++)
                            {
                                for (int x = 0; x < 5; x++)
                                {
                                    matrix[y, x] = byte.Parse(code.Substring(i++, 1));
                                }
                            }
                            glyphDatabase.Add(new Glyph(tagID, matrix));
                        }
                    } while (line != null);
                }

                _glyphRecognizer = new GlyphRecognizer(glyphDatabase);
                _glyphRecognizer.MaxNumberOfGlyphsToSearch = GLYPHSTOSEARCHFOR;

                PropertyChanged += (s, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case MinFramesPropertyName:
                            _glyphTable = new GlyphMetadata[GLYPHCOUNT];
                            break;
                    }
                };

            }
            catch (Exception e)
            {
                Log(e + e.Message + e.StackTrace);
            }
        }

        #endregion

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            if (_lastFrame == null) return base.PreProcess(dataContainer);

            var devices = dataContainer.OfType<Device>().ToArray();
            var unknownDevices = devices.Where(d => !d.IsIdentified).ToArray();
            if (!devices.Any() || !unknownDevices.Any())
            {
                return base.PreProcess(dataContainer);
            }

            var outputImage = _lastFrame.Copy();

            var binaryThreshold = BinaryThreshold;

            if (unknownDevices.Any())
            //if (devices.Any())
            {
                var grayImage = _lastFrame.Copy().Convert<Gray, byte>();
                grayImage = grayImage.ThresholdBinary(new Gray(binaryThreshold), new Gray(255));

                var width = _lastFrame.Width;
                var height = _lastFrame.Height;

                foreach (var device in unknownDevices)
                //foreach (var device in devices)
                {
                    var area = device.Area;

                    var roiExpandFactor = RoiExpandFactor;

                    var indentX = width * roiExpandFactor;
                    var indentY = height * roiExpandFactor;

                    var offsetX = (int)(area.X * width);
                    var offsetY = (int)(area.Y * height);

                    var roiX = (int)Math.Max(0, offsetX - indentX);
                    var roiY = (int)Math.Max(0, offsetY - indentY);
                    var roiWidth = (int)Math.Min(width - roiX, area.Width * width + 2 * indentX);
                    var roiHeight = (int)Math.Min(height - roiY, area.Height * height + 2 * indentY);

                    var roi = new Rectangle(
                        roiX,
                        roiY,
                        roiWidth,
                        roiHeight
                        );

                    if (IsRenderContent)
                    {
                        outputImage.Draw(roi, Rgbs.Red, 2);
                    }

                    var imageWithROI = _lastFrame.Copy(roi);

                    //FindMarker(imageWithROI, offsetX, offsetY, width, height, ref outputImage);

                    grayImage.ROI = roi;

                    Contour<Point> largestContour = null;
                    using (var storage = new MemStorage())
                    {
                        for (var contours = grayImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_EXTERNAL); contours != null; contours = contours.HNext)
                        {
                            var contour = contours.ApproxPoly(contours.Perimeter * 0.05);

                            if (largestContour == null)
                            {
                                largestContour = contour;
                                continue;
                            }

                            if (contour.Area > largestContour.Area)
                            {
                                largestContour = contour;
                            }
                        }

                        if (largestContour != null)
                        {
                            var edges = DetermineLongestEdge(largestContour);

                            if (IsRenderContent)
                            {
                                var oldROI = outputImage.ROI;
                                outputImage.ROI = roi;
                                outputImage.Draw(largestContour.GetMinAreaRect(storage), Rgbs.Cyan, 2);

                                outputImage.Draw(edges[0], Rgbs.Red, 3);
                                outputImage.Draw(edges[1], Rgbs.Green, 3);

                                outputImage.ROI = oldROI;
                            }
                        }
                    }
                }

                outputImage.ROI = new Rectangle(0, 0, width, height);
                grayImage.ROI = new Rectangle(0, 0, width, height);
                var binaryThresholdImageCopy = grayImage.Copy();
                Task.Factory.StartNew(() =>
                {
                    var bitmap = binaryThresholdImageCopy.ToBitmapSource(true);
                    binaryThresholdImageCopy.Dispose();
                    return bitmap;
                }).ContinueWith(s => BinaryThresholdImageSource = s.Result);

                grayImage.Dispose();
            }

            if (IsRenderContent)
            {
                // Render known devices with green rectangle
                foreach (var device in devices.Where(d => d.IsIdentified))
                {
                    var area = device.Area;

                    var width = _lastFrame.Width;
                    var height = _lastFrame.Height;

                    var offsetX = (int)(area.X * width);
                    var offsetY = (int)(area.Y * height);

                    var roi = new Rectangle(
                            offsetX,
                            offsetY,
                            (int)(area.Width * width),
                            (int)(area.Height * height)
                            );

                    outputImage.Draw(roi, Rgbs.Green, 4);
                }

                var outputImageCopy = outputImage.Copy();
                Task.Factory.StartNew(() =>
                {
                    var bitmap = outputImageCopy.ToBitmapSource(true);
                    outputImageCopy.Dispose();
                    return bitmap;
                }).ContinueWith(s => OutputImage = s.Result);

                outputImage.Dispose();
            }

            // Push staged data
            Push();

            return base.PreProcess(dataContainer);
        }

        public override IData Process(IData data)
        {
            var imageData = data as RgbImageData;
            if (imageData != null)
            {
                if (_lastFrame != null) _lastFrame.Dispose();
                _lastFrame = imageData.Image.Copy();

                if (!UseBlobs)
                {
                    FindMarker(_lastFrame, 0, 0, _lastFrame.Width, _lastFrame.Height, ref _lastFrame);

                    if (IsRenderContent)
                    {
                        var outputImageCopy = _lastFrame.Copy();
                        Task.Factory.StartNew(() =>
                        {
                            var bitmap = outputImageCopy.ToBitmapSource(true);
                            outputImageCopy.Dispose();
                            return bitmap;
                        }).ContinueWith(s => OutputImage = s.Result);
                    }

                    // Push staged data
                    Push();
                }
            }
            return null;
        }

        private void FindMarker(Image<Rgb, byte> image, int offsetX, int offsetY, int width, int height, ref Image<Rgb, byte> outputImage)
        {
            if (_glyphRecognizer != null)
            {
                // go through all found glyphs, highlight them and put the recognized ones in a list                

                _recognizedGlyphs.Clear();
                _recognizedQuads.Clear();
                var glyphs = _glyphRecognizer.FindGlyphs(image.Bitmap);

                foreach (var glyph in glyphs)
                {
                    // highlight quadrilateral (of all found glyphs)
                    var quad = glyph.Quadrilateral;

                    if (IsRenderContent && quad != null && quad.Count == 4)
                    {
                        outputImage.DrawPolyline(
                            new[]
                            {
                                new Point(quad[0].X + offsetX, quad[0].Y + offsetY),
                                new Point(quad[1].X + offsetX, quad[1].Y + offsetY),
                                new Point(quad[2].X + offsetX, quad[2].Y + offsetY),
                                new Point(quad[3].X + offsetX, quad[3].Y + offsetY)
                            },
                            true, Rgbs.Red, 1);

                        // write numbers on edge points (of all found glyphs)
                        var i = 0;
                        foreach (var point in quad)
                        {
                            outputImage.Draw(i++.ToString(CultureInfo.InvariantCulture), ref EmguFont, new Point(point.X + offsetX, point.Y + offsetY), Rgbs.TrueRed);
                        }
                    }

                    // if glyphs are recognized then store and draw name 
                    var recQuad = glyph.RecognizedQuadrilateral;
                    var recGlyph = glyph.RecognizedGlyph;
                    if (recQuad != null && recGlyph != null)
                    {
                        _recognizedGlyphs.Add(recGlyph.Name, recGlyph);
                        _recognizedQuads.Add(recGlyph.Name, recQuad);

                        if (IsRenderContent)
                        {
                            String label = recGlyph.Name;
                            Point labelPos = new Point(recQuad[2].X + offsetX, recQuad[2].Y + offsetY);
                            outputImage.Draw(label, ref EmguFontBig, labelPos, Rgbs.Green);
                        }
                    }
                }


                // update all entries in glyph table using recognized glyphs

                for (int i = 0; i < _glyphTable.Length; i++)
                {
                    var name = i.ToString(CultureInfo.InvariantCulture);

                    Glyph glyph = null;
                    List<IntPoint> quad = null;
                    if (_recognizedGlyphs.ContainsKey(name)) glyph = _recognizedGlyphs[name];
                    if (_recognizedQuads.ContainsKey(name)) quad = _recognizedQuads[name];

                    // if glyph for this entry was recognized, update entry
                    if (glyph != null && quad != null)
                    {
                        // if there is no entry yet, create it
                        if (_glyphTable[i] == null)
                            _glyphTable[i] = new GlyphMetadata();

                        var gmd = _glyphTable[i];

                        gmd.aliveFrames++;

                        IntPoint upVector1 = quad[0] - quad[3];
                        IntPoint upVector2 = quad[1] - quad[2];
                        upVector1 = (upVector1 + upVector2) / 2;

                        double orientation = Math.Atan2(upVector1.Y, upVector2.X);

                        // always keep only the last X frames in list
                        if (gmd.prevX.Count == MinFramesProperty)
                        {
                            gmd.prevX.RemoveAt(0);
                            gmd.prevY.RemoveAt(0);
                            gmd.prevOrientations.RemoveAt(0);
                        }
                        gmd.prevX.Add(quad[0].X);
                        gmd.prevY.Add(quad[0].Y);
                        gmd.prevOrientations.Add(orientation);

                        // check if marker stops moving and rotating
                        if (Math.Abs(gmd.prevX.Max() - gmd.prevX.Min()) < 5
                            && Math.Abs(gmd.prevY.Max() - gmd.prevY.Min()) < 5
                            && gmd.aliveFrames >= MinFramesProperty)
                        {
                            double radOrientation = gmd.prevOrientations.Average() + Math.PI / 2;

                            if (IsRenderContent)
                            {
                                Point p1 = new Point(quad[0].X + offsetX, quad[0].Y + offsetY);
                                Point p2 = new Point(
                                    (int)(quad[0].X + offsetX + Math.Cos(orientation) * 100.0),
                                    (int)(quad[0].Y + offsetY + Math.Sin(orientation) * 100.0)
                                    );

                                outputImage.Draw(new LineSegment2D(p1, p2), Rgbs.Yellow, 2);
                            }

                            double degOrientation = orientation.RadiansToDegree() + 90;

                            // find bounding rectangle
                            float minX = image.Width;
                            float minY = image.Height;
                            float maxX = 0;
                            float maxY = 0;

                            foreach (IntPoint p in quad)
                            {
                                minX = Math.Min(minX, p.X);
                                minY = Math.Min(minY, p.Y);
                                maxX = Math.Max(maxX, p.X);
                                maxY = Math.Max(maxY, p.Y);
                            }

                            float centerX = offsetX + minX + (maxX - minX) / 2.0f;
                            float centerY = offsetY + minY + (maxY - minY) / 2.0f;

                            if (IsRenderContent)
                                outputImage.Draw(new Cross2DF(new PointF(centerX, centerY), 6, 6), Rgbs.Red, 1);

                            // Stage data for later push
                            Stage(new Marker(this, string.Format("Glyph{0}", name))
                            {
                                Id = name,
                                X = centerX / width,
                                Y = centerY / height,
                                Angle = degOrientation
                            });

                            if (IsRenderContent)
                            {
                                // bring to human readable form                            
                                radOrientation = Math.Round(radOrientation, 2);
                                degOrientation = Math.Round(degOrientation, 2);

                                String label = radOrientation + " " + degOrientation;
                                Point labelPos = new Point(quad[0].X + offsetX, quad[0].Y + offsetY);
                                outputImage.Draw(label, ref EmguFontBig, labelPos, Rgbs.Yellow);
                            }
                        }
                    }
                    else
                    {
                        // if glyph disappeared remove entry from table
                        _recognizedGlyphs[name] = null;
                    }
                }

            }
        }

        private LineSegment2D[] DetermineLongestEdge(Contour<Point> contour)
        {
            var pts = contour.ToArray();
            var edges = PointCollection.PolyLine(pts, true);

            var longestEdge = edges[0];
            var index = 0;
            for (var i = 1; i < edges.Length; i++)
            {
                var edge = edges[i];

                // Assumption is that the longest edge defines the width of the tracked device in the blob
                if (edge.Length > longestEdge.Length)
                {
                    index = i;
                    longestEdge = edges[i];
                }
            }

            var nextEdgeToLongestEdge = edges[(index + 1) % edges.Length];

            return new[] { longestEdge, nextEdgeToLongestEdge };
        }
    }
}
