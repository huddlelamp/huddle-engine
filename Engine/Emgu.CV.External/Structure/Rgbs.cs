using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Structure;

namespace Emgu.CV.External.Structure
{
    public class Rgbs
    {
        public static Rgb Blue = new Rgb(0.0, 0.0, 255.0);
        public static Rgb Green = new Rgb(0.0, 255.0, 0.0);
        public static Rgb Red = new Rgb(255.0, 0.0, 0.0);
        public static Rgb Cyan = new Rgb(0.0, 255.0, 255.0);
        public static Rgb Yellow = new Rgb(255.0, 255.0, 0.0);
        public static Rgb White = new Rgb(255.0, 255.0, 255.0);
        public static Rgb Black = new Rgb(0.0, 0.0, 0.0);

        // http://thelandofcolor.com/pantone-color-of-the-year-2013-2000/
        #region Pantone Colors of the Year

        // 2014
        public static Rgb RadiantOrchid = new Rgb(200, 107, 168);

        // 2013
        public static Rgb Emerald = new Rgb(0, 152, 116);

        // 2012
        public static Rgb TangerineTango = new Rgb(221, 65, 36);

        // 2011
        public static Rgb HoneySuckle = new Rgb(214, 80, 118);

        // 2010
        public static Rgb Turquoise = new Rgb(68, 184, 172);

        // 2009
        public static Rgb Mimosa = new Rgb(239, 192, 80);

        // 2008
        public static Rgb BlueIris = new Rgb(91, 94, 166);

        // 2007
        public static Rgb ChiliPepper = new Rgb(155, 35, 53);

        // 2006
        public static Rgb SandDollar = new Rgb(223, 207, 190);

        // 2005
        public static Rgb BlueTorquoise = new Rgb(85, 180, 176);

        // 2004
        public static Rgb TigerLily = new Rgb(225, 93, 68);

        // 2003
        public static Rgb AquaSky = new Rgb(127, 205, 205);

        // 2002
        public static Rgb TrueRed = new Rgb(188, 36, 60);

        // 2001
        public static Rgb FuchsiaRose = new Rgb(195, 68, 122);

        // 2000
        public static Rgb CeruleanBlue = new Rgb(152, 180, 212);

        #endregion
    }
}
