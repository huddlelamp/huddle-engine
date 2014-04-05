using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.OpenCv;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("Blob Fusion", "BlobFusion")]
    public class BlobFusion : BaseProcessor
    {
        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            Log("Data to fuse {0}", dataContainer.Count);

            var depthBlobs = dataContainer.ToArray().OfType<BlobData>().Where(b => b.Source.GetType() == typeof (FindContours)).ToArray();
            var colorBlobs = dataContainer.ToArray().OfType<BlobData>().Where(b => b.Source.GetType() == typeof (FindContours2)).ToArray();

            foreach (var depthBlob in depthBlobs)
                Stage(depthBlob);

            var pushableColorBlobs = colorBlobs.ToList();

            foreach (var depthBlob in depthBlobs)
            {
                foreach (var colorBlob in colorBlobs)
                {
                    if (colorBlob.Area.IntersectsWith(depthBlob.Area))
                        pushableColorBlobs.Remove(colorBlob);
                }
            }

            foreach (var colorBlob in pushableColorBlobs)
                Stage(colorBlob);

            Push();

            return null;
        }

        public override IData Process(IData data)
        {
            return null;
        }
    }
}
