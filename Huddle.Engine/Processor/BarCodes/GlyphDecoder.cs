using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using AForge;
using AForge.Vision.GlyphRecognition;
using Emgu.CV;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Extensions;
using Huddle.Engine.Processor.BarCodes.ZXingHelper;
using Huddle.Engine.Properties;
using Huddle.Engine.Util;
using Point = System.Drawing.Point;

namespace Huddle.Engine.Processor.BarCodes
{
    [ViewTemplate("Glyph Decoder", "GlyphDecoder", "/Huddle.Engine;component/Resources/qrcode.png")]
    public class GlyphDecoder : RgbProcessor
    {
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

        public override Image<Rgb, byte> ProcessAndView(Image<Rgb, byte> image)
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
                    if (quad != null && quad.Count == 4)
                    {
                        image.DrawPolyline(
                            new Point[]
                            {
                                new Point(quad[0].X, quad[0].Y),
                                new Point(quad[1].X, quad[1].Y),
                                new Point(quad[2].X, quad[2].Y),
                                new Point(quad[3].X, quad[3].Y)
                            },
                            true, Rgbs.Red, 1);
                    }

                    // write numbers on edge points (of all found glyphs)
                    int i = 0;
                    foreach (var point in quad)
                    {
                        image.Draw(i++.ToString(), ref EmguFont, new Point(point.X, point.Y), Rgbs.TrueRed);
                    }

                    // if glyphs are recognized then store and draw name 
                    var recQuad = glyph.RecognizedQuadrilateral;
                    var recGlyph = glyph.RecognizedGlyph;
                    if (recQuad != null && recGlyph != null)
                    {
                        _recognizedGlyphs.Add(recGlyph.Name, recGlyph);
                        _recognizedQuads.Add(recGlyph.Name, recQuad);

                        String label = recGlyph.Name;
                        Point labelPos = new Point(recQuad[2].X, recQuad[2].Y);
                        image.Draw(label, ref EmguFontBig, labelPos, Rgbs.Green);

                    }
                }


                // update all entries in glyph table using recognized glyphs

                for (int i = 0; i < _glyphTable.Length; i++)
                {
                    var name = i.ToString();

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
                            Point p1 = new Point(quad[0].X, quad[0].Y);
                            Point p2 = new Point(
                                (int)(quad[0].X + Math.Cos(orientation) * 100.0),
                                (int)(quad[0].Y + Math.Sin(orientation) * 100.0)
                                );
                            image.Draw(new LineSegment2D(p1, p2), Rgbs.Yellow, 2);

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

                            float centerX = minX + (maxX - minX)/2.0f;
                            float centerY = minY + (maxY - minY)/2.0f;
                            image.Draw(new Cross2DF(new PointF(centerX,centerY),6,6),Rgbs.Red,1);
                            
                            // Stage data for later push
                                Stage(new LocationData(string.Format("Glyph{0}", name))
                                {
                                    Id = name,
                                    X = centerX / image.Width,
                                    Y = centerY / image.Height,
                                    Angle = degOrientation
                                });
                            

                            //// Push staged data
                            Push();

                            // bring to human readable form                            
                            radOrientation = Math.Round(radOrientation, 2);
                            degOrientation = Math.Round(degOrientation, 2);

                            String label = radOrientation + " " + degOrientation;
                            Point labelPos = new Point(quad[0].X, quad[0].Y);
                            image.Draw(label, ref EmguFontBig, labelPos, Rgbs.Yellow);
                        }
                    }
                    else
                    {
                        // if glyph disappeared remove entry from table
                        _recognizedGlyphs[name] = null;
                    }
                }

            }
            return image.Clone();

            //var outputImage = image.Copy();

            //// get (hopefully) all visible QR codes
            //ZXing.Result[] results;
            //try
            //{
            //    results = _barcodeReader.DecodeMultiple(image);
            //}
            //catch (Exception)
            //{
            //    // sometimes there are some less important exceptions about the version of the QR codes
            //    return outputImage;
            //}

            //var numQRs = 0;
            //if (results != null)
            //{
            //    numQRs = results.Length;
            //    Log("Found {0} QR tags", numQRs);
            //}
            //else
            //{
            //    Log("Failed");
            //    //base.DrawDebug(image);
            //    return outputImage;
            //}

            //// Process found QR codes

            //if (!results.Any())
            //    return outputImage;

            //for (var i = 0; i < numQRs; i++)
            //{
            //    // Get content of tag from results[i].Text
            //    var qrText = results[i].Text;

            //    // Get corner points of tag from results[i].ResultPoints
            //    var qrPoints = results[i].ResultPoints;

            //    var minX = qrPoints.Min(p => p.X);
            //    var minY = qrPoints.Min(p => p.Y);
            //    var maxX = qrPoints.Max(p => p.X);
            //    var maxY = qrPoints.Max(p => p.Y);

            //    var colorEnumerator = _colors.GetEnumerator();

            //    foreach (var point in qrPoints)
            //    {
            //        if (!colorEnumerator.MoveNext())
            //        {
            //            colorEnumerator.Reset();
            //            colorEnumerator.MoveNext();
            //        }



            //        outputImage.Draw(new CircleF(new PointF(point.X, point.Y), 5), (Rgb)colorEnumerator.Current, 3);
            //    }

            //    if (qrPoints.Length >= 2)
            //        outputImage.Draw(new LineSegment2DF(new PointF(qrPoints[0].X, qrPoints[0].Y), new PointF(qrPoints[1].X, qrPoints[1].Y)), Rgbs.Red, 5);

            //    var dx = qrPoints[1].X - qrPoints[0].X;
            //    var dy = qrPoints[1].Y - qrPoints[0].Y;

            //    //// Get orientation of tag
            //    var qrOrientation = Math.Atan2(dy, dx) / Math.PI * 180 + 90;

            //    Log("Text={0} | Orientation={1}°", qrText, qrOrientation);

            //    var centerX = (minX + (maxX - minX) / 2);
            //    var centerY = (minY + (maxY - minY) / 2);

            //    // center point
            //    outputImage.Draw(new CircleF(new PointF(centerX, centerY), 5), Rgbs.TangerineTango, 3);

            //    // Stage data for later push
            //    Stage(new LocationData(string.Format("QrCode{0}", results[i].Text))
            //    {
            //        Id = results[i].Text,
            //        X = centerX / image.Width,
            //        Y = centerY / image.Height,
            //        Angle = qrOrientation
            //    });
            //}

            //// Push staged data
            //Push();

            //base.DrawDebug(image);
            //return outputImage;
        }
    }
}
