using System;
using System.Diagnostics;
using System.Linq;
using GalaSoft.MvvmLight;

namespace Huddle.Engine.Processor
{
    public class Benchmark : ObservableObject
    {
        #region private members

        private readonly Stopwatch _stopwatch;

        private long[] _slidingAverage = new long[20];
        private int _slidingAveragePointer = 0;

        #endregion

        #region properties

        #region AccumulateMeasurements

        /// <summary>
        /// The <see cref="AccumulateMeasurements" /> property's name.
        /// </summary>
        public const string AccumulateMeasurementsPropertyName = "AccumulateMeasurements";

        private long _accumulateMeasurements = 0;

        /// <summary>
        /// Sets and gets the AccumulateMeasurements property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long AccumulateMeasurements
        {
            get
            {
                return _accumulateMeasurements;
            }

            set
            {
                if (_accumulateMeasurements == value)
                {
                    return;
                }

                RaisePropertyChanging(AccumulateMeasurementsPropertyName);
                _accumulateMeasurements = value;
                RaisePropertyChanged(AccumulateMeasurementsPropertyName);
            }
        }

        #endregion

        #region MeasurementCount

        /// <summary>
        /// The <see cref="MeasurementCount" /> property's name.
        /// </summary>
        public const string MeasurementCountPropertyName = "MeasurementCount";

        private long _measurementCount = 0;

        /// <summary>
        /// Sets and gets the MeasurementCount property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long MeasurementCount
        {
            get
            {
                return _measurementCount;
            }

            set
            {
                if (_measurementCount == value)
                {
                    return;
                }

                RaisePropertyChanging(MeasurementCountPropertyName);
                _measurementCount = value;
                RaisePropertyChanged(MeasurementCountPropertyName);
            }
        }

        #endregion

        #region Average

        /// <summary>
        /// The <see cref="Average" /> property's name.
        /// </summary>
        public const string AveragePropertyName = "Average";

        private long _average = 0;

        /// <summary>
        /// Sets and gets the Average property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long Average
        {
            get
            {
                return _average;
            }

            set
            {
                if (_average == value)
                {
                    return;
                }

                RaisePropertyChanging(AveragePropertyName);
                _average = value;
                RaisePropertyChanged(AveragePropertyName);
            }
        }

        #endregion

        #region LastMeasurement

        /// <summary>
        /// The <see cref="LastMeasurement" /> property's name.
        /// </summary>
        public const string LastMeasurementPropertyName = "LastMeasurement";

        private long _lastMeasurement = 0;

        /// <summary>
        /// Sets and gets the LastMeasurement property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public long LastMeasurement
        {
            get
            {
                return _lastMeasurement;
            }

            set
            {
                if (_lastMeasurement == value)
                {
                    return;
                }

                RaisePropertyChanging(LastMeasurementPropertyName);
                _lastMeasurement = value;
                RaisePropertyChanged(LastMeasurementPropertyName);
            }
        }

        #endregion

        #endregion

        #region ctor

        public Benchmark()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        public void StartMeasurement()
        {
            _stopwatch.Restart();
        }

        public void StopMeasurement()
        {
            var measurement = _stopwatch.ElapsedMilliseconds;

            //Console.WriteLine("Measurement {0}", measurement);

            _slidingAverage[_slidingAveragePointer++] = measurement;
            _slidingAveragePointer %= 20;

            LastMeasurement = measurement;
            //AccumulateMeasurements += measurement;

            Average = (long)_slidingAverage.Average(); // AccumulateMeasurements / ++MeasurementCount;
        }

        #endregion
    }
}
