using System;

namespace Huddle.Engine.Filter.Impl
{
    /// <summary>
    /// This is a variation of the 1€ Filter at http://www.lifl.fr/~casiez/1euro/OneEuroFilter.cs
    /// More information about at http://www.lifl.fr/~casiez/1euro/
    /// </summary>
    public class OneEuroFilter
    {
        public OneEuroFilter(double minCutoff, double beta)
        {
            _firstTime = true;
            _minCutoff = minCutoff;
            _beta = beta;

            _xFilt = new LowpassFilter();
            _dxFilt = new LowpassFilter();
            _dcutoff = 1;
        }

        private bool _firstTime;
        private double _minCutoff;
        private double _beta;
        private readonly LowpassFilter _xFilt;
        private readonly LowpassFilter _dxFilt;
        private readonly double _dcutoff;

        public double MinCutoff
        {
            get { return _minCutoff; }
            set { _minCutoff = value; }
        }

        public double Beta
        {
            get { return _beta; }
            set { _beta = value; }
        }

        public double Filter(double x, double rate)
        {
            double dx = _firstTime ? 0 : (x - _xFilt.Last()) * rate;
            if (_firstTime)
            {
                _firstTime = false;
            }

            var edx = _dxFilt.Filter(dx, Alpha(rate, _dcutoff));
            var cutoff = _minCutoff + _beta * Math.Abs(edx);

            return _xFilt.Filter(x, Alpha(rate, cutoff));
        }

        protected double Alpha(double rate, double cutoff)
        {
            var tau = 1.0 / (2 * Math.PI * cutoff);
            var te = 1.0 / rate;
            return 1.0 / (1.0 + tau / te);
        }
    }

    /// <summary>
    /// Lowpass filter class for OneEuroFilter.
    /// </summary>
    public class LowpassFilter
    {
        private bool _firstTime;
        private double _hatXPrev;

        public LowpassFilter()
        {
            _firstTime = true;
        }

        public double Last()
        {
            return _hatXPrev;
        }

        public double Filter(double x, double alpha)
        {
            double hatX;
            if (_firstTime)
            {
                _firstTime = false;
                hatX = x;
            }
            else
                hatX = alpha * x + (1 - alpha) * _hatXPrev;

            _hatXPrev = hatX;

            return hatX;
        }
    }
}
