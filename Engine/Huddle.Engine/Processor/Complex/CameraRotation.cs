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
    /// calculate the table plane and the camera rotation
    /// established on 27/4/2015 by Yunlong
    /// </summary>
    [ViewTemplate("Camera Rotation", "CameraRotation")]
    public class CameraRotation : BaseProcessor
    {
        Image<Gray,float> depthImage1 = null;
        Image<Gray, float> depthImage2 = null;
        Image<Gray, float> depthImage3 = null;
        Image<Gray, float> depthImage4 = null;
        Image<Gray, float> depthImage0 = null;
        double[] eula0 = { 0.0, 0.0, 0.0 };
        bool dataReady = false;
        int shakeNum = 0;
        bool init = false;
        int cnt = 0;
        Matrix<double> cameraRotation = new Matrix<double>(3, 3);
        Matrix<double> rotationMatrix;

        public override IDataContainer PreProcess(IDataContainer dataContainer)
        {         
            // EmguCv depth image
            foreach (GrayFloatImage imageData in dataContainer.OfType<GrayFloatImage>())
            {
                //initialize Matrix homoGraph
                if (!init)
                {
                    Init();
                    init = true;
                }
                cnt++;
                if (cnt == 5)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    rotationMatrix = GetCameraRotation(imageData.Image);
                    Console.WriteLine("time Rotation: " + sw.ElapsedMilliseconds);
                    cnt = 0;
                }
                Stage(new RotationMatrixData(this, "cameraRotation", rotationMatrix));
            }

            Push();
            return null;
        }

        public override IData Process(IData data)
        {
            return null;
        }

        private void Init(){
            cameraRotation.SetZero();
            cameraRotation.Data[0, 0] = 1; cameraRotation.Data[1, 1] = 1; cameraRotation.Data[2, 2] = 1;
        }

        private Matrix<double> GetCameraRotation(Image<Gray, float> depthImage)
        {
            //time filter
            if (!dataReady)
            {
                depthImage1 = depthImage2 = depthImage3 = depthImage4 = depthImage;
                dataReady = true;
            }
            depthImage0 = depthImage;
            depthImage = (depthImage0 + depthImage1 + depthImage2 + depthImage3 + depthImage4) / 5.0;
            depthImage4 = depthImage3; depthImage3 = depthImage2; depthImage2 = depthImage1; depthImage1 = depthImage0;

            //median filter
            depthImage = depthImage.SmoothMedian(3);

            //subImage  !! depthImage.Data [240,320,1] !!                      
            int padding = 8;
            int widthSubImage = (int)Math.Ceiling(depthImage.Width * (1.0 - (2.0 / padding)));
            int heightSubImage = (int)Math.Ceiling(depthImage.Height * (1.0 - (2.0 / padding)));
            int originalX = (int)Math.Ceiling(depthImage.Width * (1.0 / padding));
            int originalY = (int)Math.Ceiling(depthImage.Height * (1.0 / padding));
            Image<Gray, float> depthImageSub = new Image<Gray, float>(widthSubImage, heightSubImage);
            for (int j = 0; j < heightSubImage; j++)
            {
                for (int i = 0; i < widthSubImage; i++)
                {
                    depthImageSub.Data[j, i, 0] = depthImage.Data[originalY + j, originalX + i, 0];
                }
            }

            //sampling 
            //convent to world coordinates
            //move coordinate original to center
            int samplingSpace = 3;
            int widthSampling = widthSubImage / samplingSpace;
            int heightSampling = heightSubImage / samplingSpace;
            double focalLengthDepth = depthImage.Width / (2 * Math.Tan((double)(74 * 3.14 / 180) / 2));
            float[, ,] depthData = depthImageSub.Data;
            Point3[] depthDataSamples = new Point3[widthSampling * heightSampling];
            for (int j = 0; j < heightSampling; j++)
            {
                for (int i = 0; i < widthSampling; i++)
                {
                    float z = Math.Abs(depthData[samplingSpace * j, samplingSpace * i, 0]);
                    float x = (float)((originalX + i * samplingSpace - depthImage.Width / 2.0) * z / focalLengthDepth);
                    float y = (float)((originalY + j * samplingSpace - depthImage.Height / 2.0) * z / focalLengthDepth);
                    depthDataSamples[j * widthSampling + i] = new Point3(x, y, z);
                }
            }
            //Ransac                       
            Stopwatch sw = Stopwatch.StartNew();
            RansacFitPlane ransacFitPlane = new RansacFitPlane(depthDataSamples, 5, 20);
            Plane plane = ransacFitPlane.FitPlane();
            if (plane != null)
            {
                Console.WriteLine(plane.A / plane.Offset + " " + plane.B / plane.Offset + " " + plane.C / plane.Offset + " " + plane.Offset);

                //calc rotation matrix
                double[] eula = { Math.Atan((plane.B) / (plane.C)), -Math.Atan((plane.A) / (plane.C)), 0 };
                Console.WriteLine("Xaxis: " + Math.Atan((plane.B) / (plane.C)) * 180 / Math.PI + " Yaxis: " + Math.Atan((plane.A) / (plane.C)) * 180 / Math.PI);
                //avoid shaking
                if (Math.Abs(eula0[0] - eula[0]) > 2 * Math.PI / 180 || Math.Abs(eula0[1] - eula[1]) > 2 * Math.PI / 180)
                {
                    shakeNum++;
                    if (shakeNum == 5)
                    {
                        shakeNum = 0;
                        eula0[0] = eula[0]; eula0[1] = eula[1];
                    }
                }
                else
                {
                    shakeNum = 0;
                }
                cameraRotation = RansacFitPlane.EulaArray2Matrix(eula0);
                Console.WriteLine("time: " + sw.ElapsedMilliseconds);
                
            }
            return cameraRotation;
        }    
    }
}
