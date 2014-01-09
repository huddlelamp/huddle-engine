using Emgu.CV;

namespace Tools.FlockingDevice.Tracking.Processor
{
    public interface IProcessor<TColor, TDepth>
        where TColor : struct, IColor
        where TDepth : new()
    {
        #region public properties

        string FriendlyName { get; }

        #endregion

        Image<TColor, TDepth> Process(Image<TColor, TDepth> image);
    }
}
