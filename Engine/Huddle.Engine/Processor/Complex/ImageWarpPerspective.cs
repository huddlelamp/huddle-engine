using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using Huddle.Engine.Data;
using Huddle.Engine.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Accord.Math;

namespace Huddle.Engine.Processor.Complex
{
    /// <summary>
    /// image warpPerspective
    /// this should be called after depth image is cropped
    /// established on 27/4/2015 by Yunlong
    /// </summary>
    [ViewTemplate("Image WarpPerspective", "ImageWarpPerspective")]
    public class ImageWarpPerspective : BaseProcessor
    {
        Matrix<double> cameraRotation = new Matrix<double>(3, 3);
        Matrix<double> homoGraphy = new Matrix<double>(3, 3);

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {
            foreach (RotationMatrixData cameraMatrix in dataContainer.OfType<RotationMatrixData>())
            {
                cameraRotation = cameraMatrix.RotationMatrix;
            }

            if (cameraRotation == null)
            {
                return dataContainer;
            }
            foreach (GrayFloatImage imageData in dataContainer.OfType<GrayFloatImage>())
            {
                FindHomoGraphy(cameraRotation, imageData.Image.Width, imageData.Image.Height);
                if (imageData.Key.Equals("depth"))
                {
                    Image<Gray, float> warpDepthImage = imageData.Image.WarpPerspective(homoGraphy, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, Emgu.CV.CvEnum.WARP.CV_WARP_DEFAULT, new Gray());
                    Stage(new GrayFloatImage(this, "depth", warpDepthImage));
                }

            }
            foreach (RgbImageData imageData in dataContainer.OfType<RgbImageData>())
            {
                FindHomoGraphy(cameraRotation, imageData.Image.Width, imageData.Image.Height);
                if (imageData.Key.Equals("color"))
                {
                    Image<Rgb, byte> warpColorImage = imageData.Image.WarpPerspective(homoGraphy, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, Emgu.CV.CvEnum.WARP.CV_WARP_DEFAULT, new Rgb(0, 0, 0));
                    Stage(new RgbImageData(this, "wrappedColor", warpColorImage));
                }
                else if (imageData.Key.Equals("confidence"))
                {
                    Image<Rgb, byte> warpConfidence = imageData.Image.WarpPerspective(homoGraphy, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, Emgu.CV.CvEnum.WARP.CV_WARP_DEFAULT, new Rgb(0, 0, 0));
                    Stage(new RgbImageData(this, "wrappedConfidence", warpConfidence));
                }
            }
            Push();

            return null;
        }

        public override IData Process(IData data)
        {
            return data;
        }

        private void FindHomoGraphy(Matrix<double> rotateMatrix, int width, int height)
        {
            //Camera inner parameter
            double fov = 74 * 3.14 / 180;
            double focalLength = width / (2 * Math.Tan(fov / 2));
            //KK = [focalLength  0            width/2;
            //          0       focalLength   height/2; 
            //         0            0             1 ]
            Matrix<double> KK = new Matrix<double>(3, 3);
            KK.SetValue(0);
            KK.Data[0, 0] = (double)focalLength; KK.Data[0, 2] = (double)width / 2;
            KK.Data[1, 1] = (double)focalLength; KK.Data[1, 2] = (double)height / 2;
            KK.Data[2, 2] = 1;
            Matrix<double> KKInv = new Matrix<double>(3, 3);
            //KKInv.SetValue(0);
            //KKInv.Data[0, 0] = 0.0024; KKInv.Data[0, 2] = -0.7536;
            //KKInv.Data[1, 1] = 0.0024; KKInv.Data[1, 2] = -0.5652;
            //KKInv.Data[2, 2] = 1;
            CvInvoke.cvInvert(KK.Ptr, KKInv.Ptr, SOLVE_METHOD.CV_LU);

            homoGraphy = KK * rotateMatrix;
            homoGraphy = homoGraphy * KKInv;
            homoGraphy = homoGraphy / homoGraphy.Data[2, 2];

            /*
            //By this way, the size of the warped image can be thounds, so keep all the pixel is not propriat  
            //calc the size and translation of warped iamge
            //    (0,0) ________ (w, 0)
            //         |        |
            //         |________|
            //    (0,h)          (w,h)
            Matrix<double> box = new Matrix<double>(3, 4);
            box.SetValue(1);
            box.Data[0, 0] = 0; box.Data[0, 1] = colorImage.Width; box.Data[0, 2] = colorImage.Width; box.Data[0, 3] = 0;
            box.Data[1, 0] = 0; box.Data[1, 1] = 0; box.Data[1, 2] = colorImage.Height; box.Data[1, 3] = colorImage.Height;
            Matrix<double> boxWarp = homoGraphy * box;
            boxWarp.Data[0, 0] = boxWarp.Data[0, 0] / boxWarp.Data[2, 0]; boxWarp.Data[0, 1] = boxWarp.Data[0, 1] / boxWarp.Data[2, 1]; boxWarp.Data[0, 2] = boxWarp.Data[0, 2] / boxWarp.Data[2, 2]; boxWarp.Data[0, 3] = boxWarp.Data[0, 3] / boxWarp.Data[2, 3];
            boxWarp.Data[1, 0] = boxWarp.Data[1, 0] / boxWarp.Data[2, 0]; boxWarp.Data[1, 1] = boxWarp.Data[1, 1] / boxWarp.Data[2, 1]; boxWarp.Data[1, 2] = boxWarp.Data[1, 2] / boxWarp.Data[2, 2]; boxWarp.Data[1, 3] = boxWarp.Data[1, 3] / boxWarp.Data[2, 3];
            double maxX = Math.Max(Math.Max(boxWarp.Data[0, 0], boxWarp.Data[0, 1]),Math.Max(boxWarp.Data[0, 2], boxWarp.Data[0, 3]));
            double minX = Math.Min(Math.Min(boxWarp.Data[0, 0], boxWarp.Data[0, 1]), Math.Min(boxWarp.Data[0, 2], boxWarp.Data[0, 3]));
            double maxY = Math.Max(Math.Max(boxWarp.Data[1, 0], boxWarp.Data[1, 1]), Math.Max(boxWarp.Data[1, 2], boxWarp.Data[1, 3]));
            double minY = Math.Min(Math.Min(boxWarp.Data[1, 0], boxWarp.Data[1, 1]), Math.Min(boxWarp.Data[1, 2], boxWarp.Data[1, 3]));
            widthWarp = maxX - minX;
            heightWarp = maxY - minY;
            */

            //Using the center of the image as shift standard
            Matrix<double> center = new Matrix<double>(3, 1);
            center.Data[0, 0] = width / 2;
            center.Data[1, 0] = height / 2;
            center.Data[2, 0] = 1;
            Matrix<double> centerWarp = homoGraphy * center;
            centerWarp.Data[0, 0] = centerWarp.Data[0, 0] / centerWarp.Data[2, 0];
            centerWarp.Data[1, 0] = centerWarp.Data[1, 0] / centerWarp.Data[2, 0];
            double minX = centerWarp.Data[0, 0] - center.Data[0, 0];
            double minY = centerWarp.Data[1, 0] - center.Data[1, 0];
            Matrix<double> preWarp = new Matrix<double>(3, 3);
            preWarp.SetValue(0);
            preWarp.Data[0, 0] = 1; preWarp.Data[0, 2] = -minX;
            preWarp.Data[1, 1] = 1; preWarp.Data[1, 2] = -minY;
            preWarp.Data[2, 2] = 1;
            homoGraphy = preWarp * homoGraphy;

        }

    }
}
