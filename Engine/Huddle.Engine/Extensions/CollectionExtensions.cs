using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Huddle.Engine.Extensions
{
    public static class CollectionExtensions
    {
        public static float Median(this IEnumerable<float> data)
        {
            return Median(data.ToArray());
        }

        public static float Median(this float[] data)
        {
            var orderedList = data
                .OrderBy(numbers => numbers)
                .ToList();

            int listSize = orderedList.Count;
            float result;

            if (listSize % 2 == 0) // even
            {
                var midIndex = listSize / 2;
                result = ((orderedList.ElementAt(midIndex - 1) +
                           orderedList.ElementAt(midIndex)) / 2);
            }
            else // odd
            {
                var element = (double)listSize / 2;
                element = Math.Round(element, MidpointRounding.AwayFromZero);

                result = orderedList.ElementAt((int)(element - 1));
            }

            return result;
        }
        public static double Median(this IEnumerable<double> data)
        {
            return Median(data.ToArray());
        }

        public static double Median(this double[] data)
        {
            var orderedList = data
                .OrderBy(numbers => numbers)
                .ToList();

            int listSize = orderedList.Count;
            double result;

            if (listSize % 2 == 0) // even
            {
                var midIndex = listSize / 2;
                result = ((orderedList.ElementAt(midIndex - 1) +
                           orderedList.ElementAt(midIndex)) / 2);
            }
            else // odd
            {
                var element = (double)listSize / 2;
                element = Math.Round(element, MidpointRounding.AwayFromZero);

                result = orderedList.ElementAt((int)(element - 1));
            }

            return result;
        }
    }
}
