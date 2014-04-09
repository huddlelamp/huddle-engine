using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Vision.GlyphRecognition;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Domain;
using Huddle.Engine.Extensions;
using Huddle.Engine.Util;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("Find Display", "FindDisplay")]
    public class FindDisplay : BaseProcessor
    {
        #region const

        #endregion

        #region static fields

        public static MCvFont EmguFont = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 0.3, 0.3);
        public static MCvFont EmguFontBig = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 1.0, 1.0);

        #endregion

        #region member fields

        private Image<Rgb, byte> _lastRgbImage;

        private readonly GlyphRecognizer _glyphRecognizer;
        private GlyphMetadata[] _glyphTable;

        private readonly Dictionary<String, Glyph> _recognizedGlyphs = new Dictionary<string, Glyph>();
        private readonly Dictionary<String, Point[]> _recognizedQuads = new Dictionary<String, Point[]>();

        #endregion

        #region properties

        #region InputImageBitmapSource

        /// <summary>
        /// The <see cref="InputImageBitmapSource" /> property's name.
        /// </summary>
        public const string InputImageBitmapSourcePropertyName = "InputImageBitmapSource";

        private BitmapSource _inputImageBitmapSource;

        /// <summary>
        /// Sets and gets the InputImageBitmapSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource InputImageBitmapSource
        {
            get
            {
                return _inputImageBitmapSource;
            }

            set
            {
                if (_inputImageBitmapSource == value)
                {
                    return;
                }

                RaisePropertyChanging(InputImageBitmapSourcePropertyName);
                _inputImageBitmapSource = value;
                RaisePropertyChanged(InputImageBitmapSourcePropertyName);
            }
        }

        #endregion

        #region DebugImageBitmapSource

        /// <summary>
        /// The <see cref="DebugImageBitmapSource" /> property's name.
        /// </summary>
        public const string DebugImageBitmapSourcePropertyName = "DebugImageBitmapSource";

        private BitmapSource _debugImageBitmapSource;

        /// <summary>
        /// Sets and gets the DebugImageBitmapSource property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        [IgnoreDataMember]
        public BitmapSource DebugImageBitmapSource
        {
            get
            {
                return _debugImageBitmapSource;
            }

            set
            {
                if (_debugImageBitmapSource == value)
                {
                    return;
                }

                RaisePropertyChanging(DebugImageBitmapSourcePropertyName);
                _debugImageBitmapSource = value;
                RaisePropertyChanged(DebugImageBitmapSourcePropertyName);
            }
        }

        #endregion

        #region IsUseRoiToFindDisplay

        /// <summary>
        /// The <see cref="IsUseRoiToFindDisplay" /> property's name.
        /// </summary>
        public const string IsUseRoiToFindDisplayPropertyName = "IsUseRoiToFindDisplay";

        private bool _isUseRoiToFindDisplay = false;

        /// <summary>
        /// Sets and gets the IsUseRoiToFindDisplay property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public bool IsUseRoiToFindDisplay
        {
            get
            {
                return _isUseRoiToFindDisplay;
            }

            set
            {
                if (_isUseRoiToFindDisplay == value)
                {
                    return;
                }

                RaisePropertyChanging(IsUseRoiToFindDisplayPropertyName);
                _isUseRoiToFindDisplay = value;
                RaisePropertyChanged(IsUseRoiToFindDisplayPropertyName);
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

        #region MinFramesProperty

        /// <summary>
        /// The <see cref="MinFramesProperty" /> property's name.
        /// </summary>
        public const string MinFramesPropertyName = "MinFramesProperty";

        private int _minFramesProperty = 1;

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

        public FindDisplay()
        {
            // create glyph database for 5x5 glyphs
            var glyphDatabase = new GlyphDatabase(5);

            try
            {
                using (var stream = new StreamReader("tagdefinition.txt"))
                {
                    String line;
                    do
                    {
                        line = stream.ReadLine();
                        if (line != null)
                        {
                            var tokens = line.Split(' ');
                            var tagId = tokens[0];
                            var code = tokens[1];

                            var matrix = new byte[5, 5];

                            var i = 0;
                            for (var y = 0; y < 5; y++)
                            {
                                for (var x = 0; x < 5; x++)
                                {
                                    matrix[y, x] = byte.Parse(code.Substring(i++, 1));
                                }
                            }
                            glyphDatabase.Add(new Glyph(tagId, matrix));
                        }
                    } while (line != null);
                }

                _glyphRecognizer = new GlyphRecognizer(glyphDatabase)
                {
                    MaxNumberOfGlyphsToSearch = 1
                };
                _glyphTable = new GlyphMetadata[glyphDatabase.Count];

                PropertyChanged += (s, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case MinFramesPropertyName:
                            _glyphTable = new GlyphMetadata[glyphDatabase.Count];
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
            var rgbImages = dataContainer.OfType<RgbImageData>().ToArray();
            if (rgbImages.Any())
            {
                if (_lastRgbImage != null) _lastRgbImage.Dispose();
                _lastRgbImage = rgbImages.First().Image.Copy();

                var lastRgbImageCopy = _lastRgbImage.Copy();
                Task.Factory.StartNew(() =>
                {
                    var bitmapSource = lastRgbImageCopy.ToBitmapSource(true);
                    lastRgbImageCopy.Dispose();
                    return bitmapSource;
                }).ContinueWith(t => InputImageBitmapSource = t.Result);

                if (!IsUseRoiToFindDisplay)
                {

                }
            }

            // Do not process if last Rgb image frame is not set
            if (_lastRgbImage == null) return null;

            var devices = dataContainer.OfType<Device>().ToArray();
            var unknownDevices = devices.Where(d => !d.IsIdentified).ToArray();
            if (!devices.Any())
                return base.PreProcess(dataContainer);

            var debugImage = _lastRgbImage.Copy();

            if (unknownDevices.Any())
            {
                var binaryThreshold = BinaryThreshold;

                var colorImage = _lastRgbImage.Copy();
                var grayscaleImage = _lastRgbImage.Copy().Convert<Gray, byte>();

                var width = _lastRgbImage.Width;
                var height = _lastRgbImage.Height;

                foreach (var device in unknownDevices)
                {
                    var deviceRoi = CalculateRoiFromNormalizedBounds(device.Area, colorImage);
                    deviceRoi = deviceRoi.GetInflatedBy(RoiExpandFactor, colorImage.ROI);

                    var imageRoi = colorImage.ROI;
                    colorImage.ROI = deviceRoi;
                    List<Point[]> quadrilaterals;
                    var markers = GetMarkers(ref colorImage, deviceRoi, width, height, ref debugImage, out quadrilaterals);
                    colorImage.ROI = imageRoi;

                    var grayscaleImageRoi = grayscaleImage.ROI;
                    grayscaleImage.ROI = deviceRoi;

                    int i = 0;
                    foreach (var marker in markers)
                    {
                        grayscaleImage.FillConvexPoly(quadrilaterals[i], Grays.White);

                        // Overlap marker
                        //if (IsRenderContent)
                        //{
                        //    var debugImageRoi = debugImage.ROI;
                        //    debugImage.ROI = deviceRoi;
                        //    debugImage.FillConvexPoly(quadrilaterals[i], Rgbs.ChiliPepper);
                        //    debugImage.ROI = debugImageRoi;
                        //}

                        var display = FindDisplayInImage(ref grayscaleImage, deviceRoi, width, height, marker, ref debugImage);

                        if (display != null)
                            Stage(display);

                        i++;
                    }

                    grayscaleImage.ROI = grayscaleImageRoi;
                }

                grayscaleImage.Dispose();

                Push();
            }

            // draw debug output
            var debugImageCopy = debugImage.Copy();
            Task.Factory.StartNew(() =>
            {
                var bitmapSource = debugImageCopy.ToBitmapSource(true);
                debugImageCopy.Dispose();
                return bitmapSource;
            }).ContinueWith(t => DebugImageBitmapSource = t.Result);

            return null;
        }

        public override IData Process(IData data)
        {
            return null;
        }

        #region private methods

        private IEnumerable<Marker> GetMarkers(ref Image<Rgb, byte> image, Rectangle roi, int width, int height, ref Image<Rgb, byte> debugImage, out List<Point[]> quadrilaterals)
        {
            if (_glyphRecognizer == null)
            {
                quadrilaterals = new List<Point[]>();
                return null;
            }

            var imageWidth = image.Width;
            var imageHeight = image.Height;
            var imageRoi = image.ROI;

            var markers = new List<Marker>();

            var minFramesProperty = MinFramesProperty;

            // Draw new Roi
            if (IsRenderContent)
            {
                debugImage.Draw(roi, Rgbs.Red, 2);
            }

            _recognizedGlyphs.Clear();
            _recognizedQuads.Clear();

            var glyphs = _glyphRecognizer.FindGlyphs(image.Bitmap);

            foreach (var glyph in glyphs)
            {
                // highlight quadrilateral (of all found glyphs)
                var tmpQuad = glyph.Quadrilateral;

                if (tmpQuad == null || tmpQuad.Count != 4) continue;

                var quad = new[]
                {
                    new Point(tmpQuad[0].X, tmpQuad[0].Y),
                    new Point(tmpQuad[1].X, tmpQuad[1].Y),
                    new Point(tmpQuad[2].X, tmpQuad[2].Y),
                    new Point(tmpQuad[3].X, tmpQuad[3].Y)
                };

                if (IsRenderContent)
                {
                    var debugImageRoi = debugImage.ROI;
                    debugImage.ROI = roi;

                    debugImage.DrawPolyline(quad, true, Rgbs.Yellow, 1);

                    debugImage.ROI = debugImageRoi;
                }

                // if glyphs are recognized then store and draw name 
                var recQuad = glyph.RecognizedQuadrilateral;
                var recGlyph = glyph.RecognizedGlyph;
                if (recQuad != null && recGlyph != null)
                {
                    _recognizedGlyphs.Add(recGlyph.Name, recGlyph);
                    _recognizedQuads.Add(recGlyph.Name, quad);

                    if (IsRenderContent)
                    {
                        var debugImageRoi = debugImage.ROI;
                        debugImage.ROI = roi;

                        var labelPos = new Point(recQuad[2].X, recQuad[2].Y);
                        debugImage.Draw(recGlyph.Name, ref EmguFontBig, labelPos, Rgbs.Green);

                        debugImage.ROI = debugImageRoi;
                    }
                }
            }

            // update all entries in glyph table using recognized glyphs

            quadrilaterals = new List<Point[]>();
            for (int i = 0; i < _glyphTable.Length; i++)
            {
                var name = i.ToString(CultureInfo.InvariantCulture);

                Glyph glyph = null;
                Point[] quad = null;
                if (_recognizedGlyphs.ContainsKey(name)) glyph = _recognizedGlyphs[name];
                if (_recognizedQuads.ContainsKey(name)) quad = _recognizedQuads[name];

                // if glyph for this entry was recognized, update entry
                if (glyph != null && quad != null)
                {
                    quadrilaterals.Add(quad);

                    // if there is no entry yet, create it
                    if (_glyphTable[i] == null)
                        _glyphTable[i] = new GlyphMetadata();

                    var gmd = _glyphTable[i];

                    gmd.aliveFrames++;

                    Point upVector1 = quad[0].Sub(quad[3]);
                    Point upVector2 = quad[1].Sub(quad[2]);
                    upVector1 = (upVector1.Add(upVector2)).Div(2);

                    double orientation = Math.Atan2(upVector1.Y, upVector2.X);

                    // always keep only the last X frames in list
                    if (gmd.prevX.Count == minFramesProperty)
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
                        && gmd.aliveFrames >= minFramesProperty)
                    {
                        var x = orientation + Math.PI / 2;
                        var degOrientation = (x > 0.0 ? x : (2.0 * Math.PI + x)) * 360 / (2.0 * Math.PI);

                        // find bounding rectangle
                        float minX = image.Width;
                        float minY = image.Height;
                        float maxX = 0;
                        float maxY = 0;

                        foreach (Point p in quad)
                        {
                            minX = Math.Min(minX, p.X);
                            minY = Math.Min(minY, p.Y);
                            maxX = Math.Max(maxX, p.X);
                            maxY = Math.Max(maxY, p.Y);
                        }

                        var centerX = roi.X + minX + (maxX - minX) / 2.0f;
                        var centerY = roi.Y + minY + (maxY - minY) / 2.0f;

                        markers.Add(new Marker(this, "Display")
                        {
                            Id = name,
                            X = centerX / width,
                            Y = centerY / height,
                            RelativeX = centerX / imageWidth,
                            RelativeY = centerY / imageHeight,
                            Angle = degOrientation
                        });

                        // Render center and orientation of marker
                        if (IsRenderContent)
                        {
                            var markerCenter = new PointF(centerX, centerY);
                            var p2 = new Point(
                                (int)(markerCenter.X + Math.Cos(orientation) * 100.0),
                                (int)(markerCenter.Y + Math.Sin(orientation) * 100.0)
                                );
                            var p3 = new Point(
                                (int)(markerCenter.X + Math.Cos(orientation + Math.PI / 16) * 75.0),
                                (int)(markerCenter.Y + Math.Sin(orientation + Math.PI / 16) * 75.0)
                                );

                            debugImage.Draw(new Cross2DF(markerCenter, 6, 6), Rgbs.Green, 2);
                            debugImage.Draw(new LineSegment2DF(markerCenter, p2), Rgbs.Green, 2);
                            debugImage.Draw(string.Format("{0} deg", Math.Round(degOrientation, 1)), ref EmguFont, p3, Rgbs.Green);
                        }
                    }
                    else
                    {
                        // if glyph disappeared remove entry from table
                        _recognizedGlyphs[name] = null;
                    }
                }
            }

            image.ROI = imageRoi;

            return markers;
        }

        private static Rectangle CalculateRoiFromNormalizedBounds(Rect inputRect, IImage inputImage, int marginX = 0, int marginY = 0)
        {
            var width = inputImage.Width();
            var height = inputImage.Height();

            var offsetX = (int)(inputRect.X * width);
            var offsetY = (int)(inputRect.Y * height);

            var roiX = Math.Max(0, offsetX - marginX);
            var roiY = Math.Max(0, offsetY - marginY);
            var roiWidth = (int)Math.Min(width - roiX, inputRect.Width * width + 2 * marginX);
            var roiHeight = (int)Math.Min(height - roiY, inputRect.Height * height + 2 * marginY);

            return new Rectangle(
                    roiX,
                    roiY,
                    roiWidth,
                    roiHeight
                    );
        }

        private Marker FindDisplayInImage(ref Image<Gray, byte> grayscaleImage, Rectangle roi, int width, int height, Marker marker, ref Image<Rgb, byte> debugImage)
        {
            var imageWidth = grayscaleImage.Width;
            var imageHeight = grayscaleImage.Height;

            var x = (int)(marker.RelativeX * imageWidth) - roi.X;
            var y = (int)(marker.RelativeY * imageHeight) - roi.Y;

            var grayscaleImageRoi = grayscaleImage.ROI;
            grayscaleImage.ROI = roi;

            var enclosingRectangle = FindEnclosingRectangle(ref grayscaleImage, new Point(x, y), ref debugImage, roi);

            grayscaleImage.ROI = grayscaleImageRoi;

            if (enclosingRectangle == null) return null;

            return new Marker(this, "Display")
            {
                Id = marker.Id,
                X = marker.X,
                Y = marker.Y,
                Angle = marker.Angle,
                RgbImageToDisplayRatio = new Ratio
                {
                    X = width / enclosingRectangle.LongEdge.Length,
                    Y = height / enclosingRectangle.ShortEdge.Length
                }
            };
        }

        private EnclosingRectangle FindEnclosingRectangle(ref Image<Gray, byte> grayscaleImage, Point center, ref Image<Rgb, byte> debugImage, Rectangle roi)
        {
            var binaryThreshold = BinaryThreshold;
            var binaryThresholdImage = grayscaleImage.ThresholdBinary(new Gray(binaryThreshold), Grays.White);

            #region Debug Binary Image

            // Binary Threshold Image
            //var maskCopy = binaryThresholdImage.Copy();
            //Task.Factory.StartNew(() =>
            //{
            //    var bitmapSource = maskCopy.ToBitmapSource(true);
            //    maskCopy.Dispose();
            //    return bitmapSource;
            //}).ContinueWith(t => BinaryThresholdImageBitmapSource = t.Result);

            #endregion

            var floodFillImage = binaryThresholdImage.Copy();

            var imageWidth = floodFillImage.Width;
            var imageHeight = floodFillImage.Height;

            MCvConnectedComp comp;

            // mask needs to be 2 pixels wider and 2 pixels taller
            var mask = new Image<Gray, byte>(imageWidth + 2, imageHeight + 2);
            CvInvoke.cvFloodFill(floodFillImage, center, new MCvScalar(255), new MCvScalar(FloodFillDifference), new MCvScalar(FloodFillDifference), out comp, CONNECTIVITY.FOUR_CONNECTED, FLOODFILL_FLAG.DEFAULT, mask);

            #region Debug Flood Fill Image

            //// Flood fill image
            //var maskCopy = floodFillImage.Copy();
            //Task.Factory.StartNew(() =>
            //{
            //    var bitmapSource = maskCopy.ToBitmapSource(true);
            //    maskCopy.Dispose();
            //    return bitmapSource;
            //}).ContinueWith(t => BinaryThresholdImageBitmapSource = t.Result);

            #endregion

            // shrink mask back to original grayscale image size
            mask.ROI = new Rectangle(1, 1, imageWidth, imageHeight);
            var contourBinaryImage = mask.Mul(255);

            #region Debug Output Binary Image

            //var maskCopy = contourBinaryImage.Copy();
            //Task.Factory.StartNew(() =>
            //{
            //    var bitmapSource = maskCopy.ToBitmapSource(true);
            //    maskCopy.Dispose();
            //    return bitmapSource;
            //}).ContinueWith(t => BinaryThresholdImageBitmapSource = t.Result);

            #endregion

            mask.Dispose();
            floodFillImage.Dispose();

            EnclosingRectangle enclosingRectangle = null;
            using (var storage = new MemStorage())
            {
                for (
                    var contours = contourBinaryImage.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                        RETR_TYPE.CV_RETR_EXTERNAL, storage);
                    contours != null;
                    contours = contours.HNext)
                {
                    var contour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);

                    var contourBounds = contour.BoundingRectangle;
                    if (contourBounds.Width + 5 >= roi.Width || contourBounds.Height + 5 >= roi.Height)
                        continue;

                    if (!EmguExtensions.IsRectangle(contour, 3.0)) continue;

                    var edges = GetRightAngleEdges(contour);

                    if (IsRenderContent)
                    {
                        var debugImageRoi = debugImage.ROI;
                        debugImage.ROI = roi;
                        debugImage.Draw(contour.GetConvexHull(ORIENTATION.CV_CLOCKWISE), Rgbs.Cyan, 3);
                        debugImage.Draw(contour.GetMinAreaRect(storage), Rgbs.Cyan, 2);

                        DrawEdge(ref debugImage, edges[0], Rgbs.Red);
                        DrawEdge(ref debugImage, edges[1], Rgbs.Green);

                        debugImage.ROI = debugImageRoi;
                    }

                    enclosingRectangle = new EnclosingRectangle
                    {
                        Contour = contour,
                        LongEdge = edges[0],
                        ShortEdge = edges[1]
                    };
                }
            }

            contourBinaryImage.Dispose();

            return enclosingRectangle;
        }

        private LineSegment2D[] GetRightAngleEdges(Contour<Point> contour)
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

        private void DrawEdge(ref Image<Rgb, byte> debugImage, LineSegment2D edge, Rgb color)
        {
            debugImage.Draw(edge, color, 3);

            var p1 = edge.P1;
            var p2 = edge.P2;

            var minX = Math.Min(p1.X, p2.X);
            var minY = Math.Min(p1.Y, p2.Y);
            var maxX = Math.Max(p1.X, p2.X);
            var maxY = Math.Max(p1.Y, p2.Y);

            var centerX = minX + (maxX - minX) / 2;
            var centerY = minY + (maxY - minY) / 2;

            debugImage.Draw(string.Format("{0:F1}", edge.Length), ref EmguFont, new Point(centerX, centerY), color);
        }

        #endregion
    }

    internal class GlyphMetadata
    {
        public Glyph Glyph { get; set; }
        public List<double> prevOrientations = new List<double> { -10000.0 };
        public List<double> prevX = new List<double> { -10000.0 };
        public List<double> prevY = new List<double> { -10000.0 };
        public int aliveFrames = 0;
    }

    internal class EnclosingRectangle
    {
        public Contour<Point> Contour { get; set; }

        public LineSegment2D LongEdge { get; set; }

        public LineSegment2D ShortEdge { get; set; }
    }
}
