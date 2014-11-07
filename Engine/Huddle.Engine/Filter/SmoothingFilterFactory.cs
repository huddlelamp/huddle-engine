namespace Huddle.Engine.Filter
{
    public class SmoothingFilterFactory
    {
        public static ISmoothing CreateDefault()
        {
            return new OneEuroSmoothing();
            //return new KalmanSmoothing();
            //return new NoSmoothing();
        }
    }
}
