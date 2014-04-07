using System;
using System.Collections.Generic;
using System.Drawing;

namespace Huddle.Engine.Processor.Complex.PolygonIntersection {

	public class Polygon
    {
        #region member fields

        private readonly List<Vector> _points = new List<Vector>();
		private readonly List<Vector> _edges = new List<Vector>();

        #endregion

        #region properties

        #region LongestEdge

	    public double LongestEdge { get; set; }

        #endregion

        #endregion

        #region ctor

        public Polygon()
	    {
	        
	    }

        public Polygon(IEnumerable<Point> points, int width, int height)
        {
            foreach (var point in points)
            {
                var x = point.X/(double) width;
                var y = point.Y/(double) height;
                Points.Add(new Vector(x, y));
            }

            BuildEdges();
        }

        #endregion

        public void BuildEdges() {
			Vector p1;
			Vector p2;
			_edges.Clear();
			for (int i = 0; i < _points.Count; i++) {
				p1 = _points[i];
				if (i + 1 >= _points.Count) {
					p2 = _points[0];
				} else {
					p2 = _points[i + 1];
				}

			    var edge = p2 - p1;

			    LongestEdge = Math.Max(LongestEdge, edge.Magnitude);

				_edges.Add(edge);
			}
		}

		public List<Vector> Edges {
			get { return _edges; }
		}

		public List<Vector> Points {
			get { return _points; }
		}

		public Vector Center {
			get {
				double totalX = 0;
                double totalY = 0;
				for (int i = 0; i < _points.Count; i++) {
					totalX += _points[i].X;
					totalY += _points[i].Y;
				}

				return new Vector(totalX / _points.Count, totalY / _points.Count);
			}
		}

		public void Offset(Vector v) {
			Offset(v.X, v.Y);
		}

        public void Offset(double x, double y)
        {
			for (int i = 0; i < _points.Count; i++) {
				Vector p = _points[i];
				_points[i] = new Vector(p.X + x, p.Y + y);
			}
		}

		public override string ToString() {
			string result = "";

			for (int i = 0; i < _points.Count; i++) {
				if (result != "") result += " ";
				result += "{" + _points[i].ToString(true) + "}";
			}

			return result;
		}

	}

}

