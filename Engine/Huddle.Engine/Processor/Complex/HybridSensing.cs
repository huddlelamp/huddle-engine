using System;
using System.Collections.Generic;
using System.Linq;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.Complex.PolygonIntersection;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("Hybrid Sensing", "HybridSensing")]
    public class HybridSensing : BaseProcessor
    {
        #region private members

        private Dictionary<long, long> _depthToIds = new Dictionary<long, long>();
        private Dictionary<long, long> _colorToIds = new Dictionary<long, long>();

        #endregion

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            var depthToIds = new Dictionary<long, long>();
            var colorToIds = new Dictionary<long, long>();

            /* TODO add to IData an originates from another IData because this will be more independent from Source processor.
             * E.g., BlobData originates from Senz3D.Color or Senz3D.Depth or Senz3D.Confidence or just Color, Depth, Confidence */
            var depthBlobs = dataContainer.ToArray().OfType<BlobData>().Where(b => Equals(b.Key, "DepthBlob")).ToArray();
            var colorBlobs = dataContainer.ToArray().OfType<BlobData>().Where(b => Equals(b.Key, "ColorBlob")).ToArray();

            foreach (var depthBlob in depthBlobs)
            {
                long newId;
                if (_depthToIds.ContainsKey(depthBlob.Id))
                {
                    newId = _depthToIds[depthBlob.Id];
                }
                else
                {
                    newId = NextId();
                }
                depthToIds[depthBlob.Id] = newId;

                depthBlob.Id = newId;

                Stage(depthBlob);
            }

            var pushableColorBlobs = colorBlobs.ToList();

            foreach (var depthBlob in depthBlobs)
            {
                foreach (var colorBlob in colorBlobs)
                {
                    //if (colorBlob.Area.IntersectsWith(depthBlob.Area))
                    //    pushableColorBlobs.Remove(colorBlob);

                    PolygonCollisionUtils.PolygonCollisionResult r = PolygonCollisionUtils.PolygonCollision(depthBlob.Polygon, colorBlob.Polygon, Vector.Empty);

                    if (r.WillIntersect)
                    {
                        if (IsInTolerance(depthBlob, colorBlob, 0.01))
                        {
                            depthBlob.X = colorBlob.X;
                            depthBlob.Y = colorBlob.Y;
                            depthBlob.Angle = colorBlob.Angle;
                        }
                        pushableColorBlobs.Remove(colorBlob);
                    }
                }
            }

            foreach (var colorBlob in pushableColorBlobs)
            {
                long newId;
                if (_colorToIds.ContainsKey(colorBlob.Id))
                {
                    newId = _colorToIds[colorBlob.Id];
                }
                else
                {
                    newId = NextId();
                }
                colorToIds[colorBlob.Id] = newId;

                colorBlob.Id = newId;

                Stage(colorBlob);
            }

            _depthToIds = depthToIds;
            _colorToIds = colorToIds;

            Push();

            return null;
        }

        public override IData Process(IData data)
        {
            return null;
        }

        private bool IsInTolerance(BlobData blob1, BlobData blob2, double tolerance)
        {
            var dx = Math.Abs(blob1.X - blob2.X);
            var dy = Math.Abs(blob1.Y - blob2.Y);
            //var dAngle = Math.Abs(blob1.Angle - blob2.Angle);
            return dx < tolerance && dy < tolerance;
        }
    }
}
