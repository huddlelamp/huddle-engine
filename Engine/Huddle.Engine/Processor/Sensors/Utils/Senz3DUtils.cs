using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.External.Extensions;
using Emgu.CV.Structure;
using Huddle.Engine.Data;

namespace Huddle.Engine.Processor.Sensors.Utils
{
    public static class Senz3DUtils
    {
        public static Bitmap GetRgb32Pixels(PXCMImage image)
        {
            var cwidth = Align16(image.info.width); /* aligned width */
            var cheight = (int)image.info.height;

            PXCMImage.ImageData cdata;
            byte[] cpixels;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_RGB32, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cpixels = cdata.ToByteArray(0, cwidth * cheight * 4);
                image.ReleaseAccess(ref cdata);
            }
            else
            {
                cpixels = new byte[cwidth * cheight * 4];
            }

            var width = (int)image.info.width;
            var height = (int)image.info.height;

            Bitmap bitmap;
            lock (image)
            {
                bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                Marshal.Copy(cpixels, 0, data.Scan0, width * height * 4);
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        public static IImage[] GetHighPrecisionDepthImage(PXCMImage depthImage, float minValue, float maxValue)
        {
            var inputWidth = Align16(depthImage.info.width);  /* aligned width */
            var inputHeight = (int)depthImage.info.height;

            var returnImages = new IImage[2];
            returnImages[0] = new Image<Gray, float>(inputWidth, inputHeight);
            returnImages[1] = new Image<Rgb, Byte>(inputWidth, inputHeight);

            PXCMImage.ImageData cdata;
            if (depthImage.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, out cdata) <
                pxcmStatus.PXCM_STATUS_NO_ERROR) return returnImages;

            var depthValues = cdata.ToShortArray(0, inputWidth * inputHeight);
            depthImage.ReleaseAccess(ref cdata);
            
            var depthReturnImage = ((Image<Gray, float>)returnImages[0]);
            var confidenceReturnImage = ((Image<Rgb, Byte>)returnImages[1]);
            var depthReturnImageData = depthReturnImage.Data;
            var confidenceReturnImageData = confidenceReturnImage.Data;

            Parallel.For(0, inputHeight, y =>
            {
                for (int x = 0; x < inputWidth; x++)
                {
                    float depth = depthValues[y * inputWidth + x];
                    if (depth != EmguExtensions.LowConfidence && depth != EmguExtensions.Saturation)
                    {
                        var test = (depth - minValue) / (maxValue - minValue);

                        if (test < 0)
                            test = 0.0f;
                        else if (test > 1.0)
                            test = 1.0f;

                        test *= 255.0f;

                        depthReturnImageData[y, x, 0] = test;
                    }
                    else
                    {
                        depthReturnImageData[y, x, 0] = depth;
                        confidenceReturnImageData[y, x, 0] = 255;
                    }
                }
            });
            return returnImages;
        }

        /// <summary>
        /// Get UV Map of a PXCMImage.
        /// </summary>
        /// <param name="image">PXCMImage</param>
        /// <returns>UVMap as EmguCV image.</returns>
        public static Image<Rgb, float> GetDepthUvMap(PXCMImage image)
        {
            var inputWidth = (int)image.info.width;
            var inputHeight = (int)image.info.height;

            var uvMap = new Image<Rgb, float>(inputWidth, inputHeight);

            PXCMImage.ImageData cdata;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.ColorFormat.COLOR_FORMAT_DEPTH, out cdata) <
                pxcmStatus.PXCM_STATUS_NO_ERROR) return uvMap;

            var uv = new float[inputHeight * inputWidth * 2];

            // read UV
            var pData = cdata.buffer.planes[2];
            Marshal.Copy(pData, uv, 0, inputWidth * inputHeight * 2);
            image.ReleaseAccess(ref cdata);

            Parallel.For(0, uv.Length / 2, i =>
            {
                var j = i * 2;
                //Console.WriteLine(j + ": " + uv[j] * 255.0 + ", " + uv[j + 1] * 255.0);
                uvMap[(j / 2) / inputWidth, (j / 2) % inputWidth] = new Rgb(uv[j] * 255.0, uv[j + 1] * 255.0, 0);
            });

            return uvMap;
        }
        public static Image<Rgb, byte> GetRgbOfDepthPixels(Image<Gray, float> depth, Image<Rgb, byte> rgb,
            Image<Rgb, float> uvmap)
        {
            var dummyRect = new Rectangle();
            return GetRgbOfDepthPixels(depth, rgb, uvmap, false, ref dummyRect);
        }

        public static Image<Rgb, byte> GetRgbOfDepthPixels(Image<Gray, float> depth, Image<Rgb, byte> rgb, Image<Rgb, float> uvmap,
            bool getRgbContour, ref Rectangle rgbInDepthRect)
        {
            var resImg = new Image<Rgb, byte>(depth.Width, depth.Height);

            // number of rgb pixels per depth pixel
            var regWidth = rgb.Width / depth.Width;
            var regHeight = rgb.Height / depth.Height;
            var rgbWidth = rgb.Width;
            var rgbHeight = rgb.Height;
            var xfactor = 1.0f / 255.0f * rgbWidth;
            var yfactor = 1.0f / 255.0f * rgbHeight;
            var uvmapData = uvmap.Data;
            var rgbData = rgb.Data;
            var resImgData = resImg.Data;

            Image<Gray, byte> contourImg = null;
            byte[, ,] contourImgData = null;
            if (getRgbContour)
            {
                // dummy image to extract contour of RGB image in depth image
                contourImg = new Image<Gray, byte>(depth.Width, depth.Height);
                contourImgData = contourImg.Data;
            }

            Parallel.For(0, depth.Height, y =>
            {
                for (int x = 0; x < depth.Width; x++)
                {
                    int xindex = (int)(uvmapData[y, x, 0] * xfactor + 0.5);
                    int yindex = (int)(uvmapData[y, x, 1] * yfactor + 0.5);

                    double rsum = 0, gsum = 0, bsum = 0;
                    int pixelcount = 0;
                    for (int rx = xindex - regWidth / 2; rx < xindex + regWidth / 2; rx++)
                    {
                        for (int ry = yindex - regHeight / 2; ry < yindex + regHeight / 2; ry++)
                        {
                            if (rx > 0 && ry > 0 && rx < rgbWidth && ry < rgbHeight)
                            {
                                rsum += rgbData[ry, rx, 0];
                                gsum += rgbData[ry, rx, 1];
                                bsum += rgbData[ry, rx, 2];
                                pixelcount++;
                            }
                        }
                    }
                    resImgData[y, x, 0] = (byte)(rsum / pixelcount);
                    resImgData[y, x, 1] = (byte)(gsum / pixelcount);
                    resImgData[y, x, 2] = (byte)(bsum / pixelcount);
                    if ((resImgData[y, x, 0] + resImgData[y, x, 1] + resImgData[y, x, 2]) > 0.01)
                    {
                        if (getRgbContour && contourImgData != null)
                        {
                            contourImgData[y, x, 0] = 255;
                        }
                    }
                }
            });

            if (getRgbContour)
            {
                using (var storage = new MemStorage())
                {
                    for (var contours = contourImg.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                        RETR_TYPE.CV_RETR_EXTERNAL, storage); contours != null; contours = contours.HNext)
                    {
                        var currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                        if (currentContour.Area > 160 * 120)
                        {
                            rgbInDepthRect = currentContour.BoundingRectangle;
                        }
                    }
                }
            }

            return resImg;
        }

        public static Image<Gray, float> GetDepthOfRGBPixels(Image<Gray, float> depth, Image<Rgb, byte> rgb, Image<Rgb, float> uvmap)
        {
            // create RGB-sized image
            var retdepth = new Image<Gray, float>(rgb.Width, rgb.Height, new Gray(EmguExtensions.LowConfidence));
            var retdepthWidth = retdepth.Width;
            var retdepthHeight = retdepth.Height;

            var uvmapWidth = uvmap.Width;
            var uvmapHeight = uvmap.Height;

            var depthData = depth.Data;
            var uvmapData = uvmap.Data;

            float xfactor = 1.0f / 255.0f * retdepthWidth;
            float yfactor = 1.0f / 255.0f * retdepthHeight;

            //for (int uvy = 0; uvy < uvmapHeight - 1; uvy++)
            Parallel.For(0, uvmapHeight - 1, uvy =>
            {

                //for (int uvx = 0; uvx < uvmapWidth - 1; uvx++)
                Parallel.For(0, uvmapWidth - 1, uvx =>
                {
                    // for each point in UVmap create two triangles that connect this point with the right/bottom neighbors                   

                    var pts1 = new Point[3];
                    var d1 = new float[]
                    {
                        depthData[uvy, uvx, 0],
                        depthData[uvy, uvx + 1, 0],
                        depthData[uvy + 1, uvx, 0]
                    };

                    double d1avg = 0;
                    int count = 0;
                    for (int i = 0; i < d1.Length; i++)
                    {
                        if (d1[i] != EmguExtensions.Saturation && d1[i] != EmguExtensions.LowConfidence)
                        {
                            d1avg += d1[i];
                            count++;
                        }
                    }
                    if (count > 0)
                        d1avg = d1avg / (float)count;
                    else
                        d1avg = EmguExtensions.LowConfidence;

                    var pts2 = new Point[3];
                    var d2 = new float[]
                    {
                        depthData[uvy, uvx + 1, 0],
                        depthData[uvy + 1, uvx + 1, 0],
                        depthData[uvy + 1, uvx, 0]
                    };

                    double d2avg = 0;
                    count = 0;
                    for (int i = 0; i < d2.Length; i++)
                    {
                        if (d2[i] != EmguExtensions.Saturation && d2[i] != EmguExtensions.LowConfidence)
                        {
                            d2avg += d2[i];
                            count++;
                        }
                    }
                    if (count > 0)
                        d2avg = d2avg / (float)count;
                    else
                        d2avg = EmguExtensions.LowConfidence;


                    bool outofbounds = false;

                    // get points for triangle 1 (top left)
                    pts1[0].X = (int)(uvmapData[uvy, uvx, 0] * xfactor + 0.5);
                    outofbounds |= pts1[0].X < 0 || pts1[0].X > retdepthWidth;

                    pts1[0].Y = (int)(uvmapData[uvy, uvx, 1] * yfactor + 0.5);
                    outofbounds |= pts1[0].Y < 0 || pts1[0].Y > retdepthHeight;

                    pts1[1].X = (int)(uvmapData[uvy, uvx + 1, 0] * xfactor + 0.5) - 1;
                    outofbounds |= pts1[1].X < 0 || pts1[1].X > retdepthWidth;

                    pts1[1].Y = (int)(uvmapData[uvy, uvx + 1, 1] * yfactor + 0.5) - 1;
                    outofbounds |= pts1[1].Y < 0 || pts1[1].Y > retdepthHeight;

                    pts1[2].X = (int)(uvmapData[uvy + 1, uvx, 0] * xfactor + 0.5);
                    outofbounds |= pts1[2].X < 0 || pts1[2].X > retdepthWidth;

                    pts1[2].Y = (int)(uvmapData[uvy + 1, uvx, 1] * yfactor + 0.5) - 1;
                    outofbounds |= pts1[2].Y < 0 || pts1[2].Y > retdepthHeight;

                    if (!outofbounds)
                        retdepth.FillConvexPoly(pts1, new Gray(d1avg));

                    // get points for triangle 2 (bottom right)
                    outofbounds = false;

                    pts2[0].X = pts1[1].X;
                    outofbounds |= pts2[0].X < 0 || pts2[0].X > retdepthWidth;

                    pts2[0].Y = pts1[1].Y;
                    outofbounds |= pts2[0].Y < 0 || pts2[0].Y > retdepthHeight;

                    pts2[1].X = (int)(uvmapData[uvy + 1, uvx + 1, 0] * xfactor + 0.5);
                    outofbounds |= pts2[1].X < 0 || pts2[1].X > retdepthWidth;

                    pts2[1].Y = (int)(uvmapData[uvy + 1, uvx + 1, 1] * yfactor + 0.5) - 1;
                    outofbounds |= pts2[1].Y < 0 || pts2[1].Y > retdepthHeight;

                    pts2[2].X = pts1[2].X;
                    outofbounds |= pts2[2].X < 0 || pts2[2].X > retdepthWidth;

                    pts2[2].Y = pts1[2].Y;
                    outofbounds |= pts2[2].Y < 0 || pts2[2].Y > retdepthHeight;

                    if (!outofbounds)
                        retdepth.FillConvexPoly(pts2, new Gray(d2avg));

                });
            });

            return retdepth;
        }

        private static int Align16(uint width)
        {
            return ((int)((width + 15) / 16)) * 16;
        }
    }
}
