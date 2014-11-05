using System.Windows;

namespace Huddle.Engine.Filter
{
    public interface ISmoothing
    {
        Point SmoothPoint(Point point);

        double SmoothAngle(double angle);

        double SmoothDepth(double depth);
    }
}
