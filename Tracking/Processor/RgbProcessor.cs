using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.External.Structure;
using Emgu.CV.Structure;

namespace Tools.FlockingDevice.Tracking.Processor
{
    public abstract class RgbProcessor : BaseImageProcessor<Rgb, byte>
    {
        #region private fields

        private readonly Point _debugPoint = new Point(5, 15);

        private long _lastImageFrameTime = -1;

        #endregion

        #region Draw Debug

        protected override void DrawDebug(Image<Rgb, byte> image)
        {
            var currentImageFrameTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var diffImageFrameTime = currentImageFrameTime - _lastImageFrameTime;
            _lastImageFrameTime = currentImageFrameTime;

            var message = image.Width + " x " + image.Height + " -> " + diffImageFrameTime.ToString("#.#") + " ms " + (diffImageFrameTime / 1000).ToString("#.#") + " Hz";

            image.Draw(message, ref EmguFont, _debugPoint, Rgbs.Yellow);
        }

        #endregion
    }
}
