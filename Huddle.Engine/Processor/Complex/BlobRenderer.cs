using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
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
        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            const int width = 320;
            const int height = 240;
            var image = new Image<Rgb, byte>(width, height);

            foreach (var blob in dataContainer.OfType<BlobData>())
            {
                var x = (int)(blob.Area.X * width);
                var y = (int)(blob.Area.Y * height);
                var w = (int)(blob.Area.Width * width);
                var h = (int)(blob.Area.Height * height);

                if (typeof(FindContours) == blob.Source.GetType())
                    image.Draw(new Rectangle(x, y, w, h), Rgbs.Yellow, 2);
                else if (typeof(FindContours2) == blob.Source.GetType())
                    image.Draw(new Rectangle(x, y, w, h), Rgbs.TangerineTango, 2);
            }

            Stage(new RgbImageData(this, "BlobRenderer", image));
            Push();

            return null;
        }

        public override IData Process(IData data)
        {
            return null;
        }
    }
}
