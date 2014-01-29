using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.Structure;

namespace Tools.FlockingDevice.Tracking.Sources
{
    public class ImageEventArgs : EventArgs
    {
        #region properties

        public Dictionary<string, Image<Rgb, byte>> Images = new Dictionary<string, Image<Rgb, byte>>();

        public long ElapsedTime { get; private set; }

        public double Fps
        {
            get
            {
                return 1000.0 / ElapsedTime;
            }
        }

        #endregion

        #region ctor

        public ImageEventArgs(Dictionary<string, Image<Rgb, byte>> images, long elapsedTime)
        {
            Images = images;
            ElapsedTime = elapsedTime;
        }

        #endregion
    }
}
