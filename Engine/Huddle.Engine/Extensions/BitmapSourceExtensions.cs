using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace Huddle.Engine.Extensions
{
    public static class BitmapSourceExtensions
    {
        public static Bitmap BitmapFromSource(this BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }
    }
}
