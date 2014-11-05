using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.OpenCv;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("BlobRenderer", "BlobRenderer")]
    public class BlobRenderer : BaseProcessor
    {
        #region static fields

        public static MCvFont EmguFont = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 0.3, 0.3);
        public static MCvFont EmguFontBig = new MCvFont(FONT.CV_FONT_HERSHEY_SIMPLEX, 1.0, 1.0);

        #endregion

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            const int width = 1280;
            const int height = 720;
            var image = new Image<Rgb, byte>(width, height);

            foreach (var blob in dataContainer.OfType<BlobData>())
            {
                var polyline = new List<Point>();
                foreach (var point in blob.Polygon.Points)
                {
                    var x = point.X * width;
                    var y = point.Y * height;

                    polyline.Add(new Point((int)x, (int)y));
                }

                var color = Rgbs.White;
                if (typeof(RectangleTracker) == blob.Source.GetType())
                    color = Rgbs.Red;
                else if (typeof(RectangleTrackerColor) == blob.Source.GetType())
                    color = Rgbs.Yellow;

                var centerX = (int)(blob.X * width);
                var centerY = (int)(blob.Y * height);

                image.DrawPolyline(polyline.ToArray(), true, color, 5);
                image.Draw(string.Format("Id {0}", blob.Id), ref EmguFontBig, new Point(centerX, centerY), Rgbs.White);
            }

            Stage(new RgbImageData(this, "BlobRenderer", image.Copy()));
            Push();

            image.Dispose();

            return null;
        }

        public override IData Process(IData data)
        {
            return null;
        }
    }
}
