using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics;
using Accord;
using Emgu.CV;

namespace Huddle.Engine.Processor.Complex
{
    public class RansacFitPlane
    {
        public Point3[] points { get; set; }
        public double threshold { get; set; }
        public int maxEvaluations { get; set; }

        private int[] inliers;
        private double[] d2;
        private int maxSamplings = 5;
        private Plane bestPlane;

        public RansacFitPlane(Point3[] points, double threshold, int maxEvaluations)
        {
            this.points = points;
            this.threshold = threshold;
            this.maxEvaluations = maxEvaluations;
            this.d2 = new double[points.Length];
        }

        public Plane FitPlane()
        {
            // Initial argument checks
            if (points.Length < 3)
                throw new ArgumentException("At least three points are required to fit a plane");

            computeInliers();
            if (inliers.Length == 0)
                return null;

            // Compute the final plane
            Plane plane = fitting(points.Submatrix(inliers));
            return plane;

        }

        private void computeInliers()
        {
            int[] bestInliers = null;
            int maxInliers = 0;
            int size = this.points.Length;
            Plane plane = null;

            int count = 0;  // Total number of trials performed
            double N = maxEvaluations;   // Estimative of number of trials needed.

            // While the number of trials is less than our estimative,
            //   and we have not surpassed the maximum number of trials
            while (count < N)
            {

                int[] sample = null;
                int samplings = 0;

                // While the number of samples attempted is less
                //   than the maximum limit of attempts
                while (samplings < maxSamplings)
                {
                    // Select at random s data points to form a trial model.
                    sample = Accord.Statistics.Tools.RandomSample(size, 3);

                    if (!Point3.Collinear(points[sample[0]], points[sample[1]], points[sample[2]]))
                    {
                        Point3[] randPoints = { points[sample[0]], points[sample[1]], points[sample[2]] };
                        // Fit model using the random selection of points
                        plane = fitting(randPoints);
                        break;
                    }

                    samplings++; // Increase the samplings counter
                }

                if (plane == null)
                    throw new ConvergenceException("A model could not be inferred from the data points");

                // Now, evaluate the distances between total points and the model returning the
                //  indices of the points that are inliers (according to a distance threshold t).
                inliers = distance(plane, this.threshold);

                // Check if the model was the model which highest number of inliers:
                if (bestInliers == null || inliers.Length > maxInliers)
                {
                    // Yes, this model has the highest number of inliers.

                    maxInliers = inliers.Length;  // Set the new maximum,
                    bestPlane = plane;            // This is the best model found so far,
                    bestInliers = inliers;        // Store the indices of the current inliers.

                    // Update estimate of N, the number of trials to ensure we pick, 
                    //   with probability p, a data set with no outliers.
                    double pInlier = (double)inliers.Length / (double)size;
                    double pNoOutliers = 1.0 - System.Math.Pow(pInlier, 3);

                    N = System.Math.Ceiling(System.Math.Log(1.0 - 0.99) / System.Math.Log(pNoOutliers));
                }
                Console.WriteLine("trail " + count + " out of " + N);
                count++; // Increase the trial counter.
                if (count > maxEvaluations)
                {
                    int[] temp = { };
                    inliers = temp;
                    return;
                }
            }

            inliers = bestInliers;

        }

        private Plane fitting(Point3[] points)
        {
            // Set up constraint equations of the form  AB = 0,
            // where B is a column vector of the plane coefficients
            // in the form   b(1)*X + b(2)*Y +b(3)*Z + b(4) = 0.
            //
            // A = [XYZ' ones(npts,1)]; % Build constraint matrix
            if (points.Length < 3)
                return null;

            if (points.Length == 3)
                return Plane.FromPoints(points[0], points[1], points[2]);

            float[,] A = new float[points.Length, 4];
            for (int i = 0; i < points.Length; i++)
            {
                A[i, 0] = points[i].X;
                A[i, 1] = points[i].Y;
                A[i, 2] = points[i].Z;
                A[i, 3] = -1;
            }

            SingularValueDecompositionF svd = new SingularValueDecompositionF(A,
                computeLeftSingularVectors: false, computeRightSingularVectors: true,
                autoTranspose: true, inPlace: true);

            float[,] v = svd.RightSingularVectors;

            float a = v[0, 3];
            float b = v[1, 3];
            float c = v[2, 3];
            float d = v[3, 3];

            float norm = (float)Math.Sqrt(a * a + b * b + c * c);

            a /= norm;
            b /= norm;
            c /= norm;
            d /= norm;

            return new Plane(a, b, c, -d);
        }


        private int[] distance(Plane p, double t)
        {
            for (int i = 0; i < points.Length; i++)
                d2[i] = p.DistanceToPoint(points[i]);

            return Matrix.Find(d2, z => z < t);
        }

        public static Matrix<double> EulaArray2Matrix(double[] eula)
        {
            Matrix<double> t = new Matrix<double>(1, 3);
            t.Data[0, 0] = eula[0]; t.Data[0, 1] = eula[1]; t.Data[0, 2] = eula[2];
            double theta = t.Norm;
            t = t / theta;
            double x = t.Data[0, 0]; double y = t.Data[0, 1]; double z = t.Data[0, 2];

            double c = Math.Cos(theta); double s = Math.Sin(theta); double C = 1 - c;
            double xs = x * s; double ys = y * s; double zs = z * s;
            double xC = x * C; double yC = y * C; double zC = z * C;
            double xyC = x * yC; double yzC = y * zC; double zxC = z * xC;

            Matrix<double> T = new Matrix<double>(3, 3);

            //           T = [ x*xC+c   xyC-zs   zxC+ys  0
            //                   xyC+zs   y*yC+c   yzC-xs  0
            //                   zxC-ys   yzC+xs   z*zC+c  0
            //                   0         0       0     1];
            T.Data[0, 0] = x * xC + c; T.Data[0, 1] = xyC - zs; T.Data[0, 2] = zxC + ys;
            T.Data[1, 0] = xyC + zs; T.Data[1, 1] = y * yC + c; T.Data[1, 2] = yzC - xs;
            T.Data[2, 0] = zxC - ys; T.Data[2, 1] = yzC + xs; T.Data[2, 2] = z * zC + c;
            return T;
        }
    }
}
