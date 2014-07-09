using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Structure;

namespace Emgu.CV.External.Structure
{
    public class Bgrs
    {
        public static Bgr Blue = new Bgr(255.0, 0.0, 0.0);
        public static Bgr Green = new Bgr(0.0, 255.0, 0.0);
        public static Bgr Red = new Bgr(0.0, 0.0, 255.0);
        public static Bgr Cyan = new Bgr(255.0, 255.0, 0.0);
        public static Bgr Yellow = new Bgr(0.0, 255.0, 255.0);
        public static Bgr White = new Bgr(255.0, 255.0, 255.0);
        public static Bgr Black = new Bgr(0.0, 0.0, 0.0);
    }
}
