using System;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Huddle.Engine.Processor.OpenCv.Filter
{
    public class SyntheticData
    {
        #region properties

        public Matrix<float> State;
        public Matrix<float> TransitionMatrix;
        public Matrix<float> MeasurementMatrix;
        public Matrix<float> ProcessNoise;
        public Matrix<float> MeasurementNoise;
        public Matrix<float> ErrorCovariancePost;

        #endregion

        #region cto

        public SyntheticData(float strengthMatrix, double processNoise, double measurementNoise)
        {
            var newStrength = strengthMatrix;
            var newProcessNoise = processNoise;
            var newMeasurementNoise = measurementNoise;

            if (strengthMatrix > 1.0f || strengthMatrix < 0.0f)
                newStrength = 0.6f;

            if (processNoise > 1.0e-1 || processNoise < 1.0e-4)
                newProcessNoise = 1.0e-2;

            if (measurementNoise > 1.0e-1 || measurementNoise < 1.0e-4)
                newMeasurementNoise = 1.0e-1;


            State = new Matrix<float>(4, 1);
            State[0, 0] = 0f;                   // x-pos
            State[1, 0] = 0f;                   // y-pos
            State[2, 0] = 0f;                   // x-velocity
            State[3, 0] = 0f;                   // y-velocity
            TransitionMatrix = new Matrix<float>(new[,]
                    {
                        {newStrength, 0, 1, 0},
                        {0, newStrength, 0, 1},
                        {0, 0, 1, 0},
                        {0, 0, 0, 1}
                    });
            MeasurementMatrix = new Matrix<float>(new float[,]
                    {
                        { 1, 0, 0, 0 },
                        { 0, 1, 0, 0 }
                    });
            MeasurementMatrix.SetIdentity();
            ProcessNoise = new Matrix<float>(4, 4);                             //Linked to the size of the transition matrix
            ProcessNoise.SetIdentity(new MCvScalar(newProcessNoise));           //The smaller the value the more resistance to noise 
            MeasurementNoise = new Matrix<float>(2, 2);                         //Fixed accordiong to input data 
            MeasurementNoise.SetIdentity(new MCvScalar(newMeasurementNoise));   //larger the value more resitance to noise and the less responsive to velocity
            ErrorCovariancePost = new Matrix<float>(4, 4);                      //Linked to the size of the transition matrix
            ErrorCovariancePost.SetIdentity();
        }

        #endregion

        #region public methods

        public Matrix<float> GetMeasurement()
        {
            var measurementNoise = new Matrix<float>(2, 1);
            measurementNoise.SetRandNormal(new MCvScalar(), new MCvScalar(Math.Sqrt(measurementNoise[0, 0])));
            return MeasurementMatrix * State + measurementNoise;
        }

        public void GoToNextState()
        {
            var processNoise = new Matrix<float>(4, 1);
            processNoise.SetRandNormal(new MCvScalar(), new MCvScalar(processNoise[0, 0]));
            State = TransitionMatrix * State + processNoise;
        }

        #endregion
    }
}
