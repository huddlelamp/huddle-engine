using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Imaging;
using Huddle.Engine.Data;
using Huddle.Engine.Processor.Complex.PolygonIntersection;
using Huddle.Engine.Processor.OpenCv;
using Huddle.Engine.Util;

namespace Huddle.Engine.Processor.Complex
{
    [ViewTemplate("Blob Fusion", "BlobFusion")]
    public class BlobFusion : BaseProcessor
    {
        #region private members

        private Dictionary<long, long> _depthToIds = new Dictionary<long, long>();
        private Dictionary<long, long> _colorToIds = new Dictionary<long, long>();

        #endregion

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            var depthToIds = new Dictionary<long, long>();
            var colorToIds = new Dictionary<long, long>();

            var depthBlobs = dataContainer.ToArray().OfType<BlobData>().Where(b => b.Source.GetType() == typeof(FindContours)).ToArray();
            var colorBlobs = dataContainer.ToArray().OfType<BlobData>().Where(b => b.Source.GetType() == typeof(FindContours2)).ToArray();

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

                    PolygonCollisionResult r = PolygonCollision(depthBlob.Polygon, colorBlob.Polygon, Vector.Empty);

                    if (r.WillIntersect)
                    {
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

        #region Polygon Collision Detection

        // Structure that stores the results of the PolygonCollision function
        public struct PolygonCollisionResult
        {
            public bool WillIntersect; // Are the polygons going to intersect forward in time?
            public bool Intersect; // Are the polygons currently intersecting
            public Vector MinimumTranslationVector; // The translation to apply to polygon A to push the polygons appart.
        }

        // Check if polygon A is going to collide with polygon B for the given velocity
        public PolygonCollisionResult PolygonCollision(Polygon polygonA, Polygon polygonB, Vector velocity)
        {
            PolygonCollisionResult result = new PolygonCollisionResult();
            result.Intersect = true;
            result.WillIntersect = true;

            int edgeCountA = polygonA.Edges.Count;
            int edgeCountB = polygonB.Edges.Count;
            double minIntervalDistance = double.PositiveInfinity;
            Vector translationAxis = new Vector();
            Vector edge;

            // Loop through all the edges of both polygons
            for (int edgeIndex = 0; edgeIndex < edgeCountA + edgeCountB; edgeIndex++)
            {
                if (edgeIndex < edgeCountA)
                {
                    edge = polygonA.Edges[edgeIndex];
                }
                else
                {
                    edge = polygonB.Edges[edgeIndex - edgeCountA];
                }

                // ===== 1. Find if the polygons are currently intersecting =====

                // Find the axis perpendicular to the current edge
                Vector axis = new Vector(-edge.Y, edge.X);
                axis.Normalize();

                // Find the projection of the polygon on the current axis
                double minA = 0; double minB = 0; double maxA = 0; double maxB = 0;
                ProjectPolygon(axis, polygonA, ref minA, ref maxA);
                ProjectPolygon(axis, polygonB, ref minB, ref maxB);

                // Check if the polygon projections are currentlty intersecting
                if (IntervalDistance(minA, maxA, minB, maxB) > 0) result.Intersect = false;

                // ===== 2. Now find if the polygons *will* intersect =====

                // Project the velocity on the current axis
                double velocityProjection = axis.DotProduct(velocity);

                // Get the projection of polygon A during the movement
                if (velocityProjection < 0)
                {
                    minA += velocityProjection;
                }
                else
                {
                    maxA += velocityProjection;
                }

                // Do the same test as above for the new projection
                double intervalDistance = IntervalDistance(minA, maxA, minB, maxB);
                if (intervalDistance > 0) result.WillIntersect = false;

                // If the polygons are not intersecting and won't intersect, exit the loop
                if (!result.Intersect && !result.WillIntersect) break;

                // Check if the current interval distance is the minimum one. If so store
                // the interval distance and the current distance.
                // This will be used to calculate the minimum translation vector
                intervalDistance = Math.Abs(intervalDistance);
                if (intervalDistance < minIntervalDistance)
                {
                    minIntervalDistance = intervalDistance;
                    translationAxis = axis;

                    Vector d = polygonA.Center - polygonB.Center;
                    if (d.DotProduct(translationAxis) < 0) translationAxis = -translationAxis;
                }
            }

            // The minimum translation vector can be used to push the polygons appart.
            // First moves the polygons by their velocity
            // then move polygonA by MinimumTranslationVector.
            if (result.WillIntersect) result.MinimumTranslationVector = translationAxis * minIntervalDistance;

            return result;
        }

        // Calculate the distance between [minA, maxA] and [minB, maxB]
        // The distance will be negative if the intervals overlap
        public double IntervalDistance(double minA, double maxA, double minB, double maxB)
        {
            if (minA < minB)
            {
                return minB - maxA;
            }
            else
            {
                return minA - maxB;
            }
        }

        // Calculate the projection of a polygon on an axis and returns it as a [min, max] interval
        public void ProjectPolygon(Vector axis, Polygon polygon, ref double min, ref double max)
        {
            // To project a point on an axis use the dot product
            double d = axis.DotProduct(polygon.Points[0]);
            min = d;
            max = d;
            for (int i = 0; i < polygon.Points.Count; i++)
            {
                d = polygon.Points[i].DotProduct(axis);
                if (d < min)
                {
                    min = d;
                }
                else
                {
                    if (d > max)
                    {
                        max = d;
                    }
                }
            }
        }

        #endregion
    }
}
