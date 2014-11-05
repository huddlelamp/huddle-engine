namespace Huddle.Engine.Data
{
    public struct Ratio
    {
        public static Ratio Empty = new Ratio { X = 0, Y = 0 };

        public double X { get; set; }

        public double Y { get; set; }
    }
}
