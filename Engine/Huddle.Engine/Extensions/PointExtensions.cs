using System;
using System.Windows;
using ThicknessConverter = Xceed.Wpf.DataGrid.Converters.ThicknessConverter;

namespace Huddle.Engine.Extensions
{
    public static class PointExtensions
    {
        public static Point Scale(this Point point, double scale)
        {
            point.X /= scale;
            point.Y /= scale;
            return point;
        }

        public static System.Drawing.Point Add(this System.Drawing.Point point, System.Drawing.Point otherPoint)
        {
            var x = point.X + otherPoint.X;
            var y = point.Y + otherPoint.Y;
            return new System.Drawing.Point(x, y);
        }

        public static System.Drawing.Point Sub(this System.Drawing.Point point, System.Drawing.Point otherPoint)
        {
            var x = point.X - otherPoint.X;
            var y = point.Y - otherPoint.Y;
            return new System.Drawing.Point(x, y);
        }

        public static System.Drawing.Point Div(this System.Drawing.Point point, int divisor)
        {
            var x = point.X / divisor;
            var y = point.Y / divisor;
            return new System.Drawing.Point(x, y);
        }

        public static double Length(this System.Drawing.Point self, System.Drawing.Point point)
        {
            return Math.Sqrt(Math.Pow(self.X - point.X, 2) + Math.Pow(self.Y - point.Y, 2));
        }
    }
}
