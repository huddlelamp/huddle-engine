namespace Huddle.Engine.Data
{
    public struct Ratio
    {
        public static Ratio Identity = new Ratio { X = 1.0, Y = 1.0 };

        public double X { get; set; }

        public double Y { get; set; }
    }
}
