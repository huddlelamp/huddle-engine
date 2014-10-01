using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using AForge.Imaging;
using Class1Wrapper;
using Image = Class1Wrapper.ImageData;

class SoftKinetic
{
   public SoftKinetic()
    {
        Class1 class1 = new Class1();
        int b = class1.test();

        Class1.FrameFormat c = class1.FrameFormat_fromResolution(352, 288);
       var sb = new StringBuilder(128);
       String a = class1.FrameFormat_toString(c);

       Thread cThread = new Thread(new ThreadStart( class1.stream ));
       cThread.Start();

       //class1.registerFunc(&render);
        System.Console.WriteLine(String.Format("TEST:{0}", a));
        System.Console.WriteLine("Device:{0}", class1.getSerialNumber());
        System.Console.WriteLine("Device:{0}", class1.getNumberofNodes());

       int frame = 0;

       Class1Wrapper.ImageData img = null;// = new ImageData();

       int cnt = 0;
       while (img == null || img.size == 0)
       {
           cnt++;
           img = class1.getImage();
       }
       System.Console.WriteLine("{0}",img.size);

       //Emgu.CV.Matrix<2>() <img.width, img.height>()
       //cv::Mat buffer(cv::Size(width, height), CV_8UC1, (void*)data);

       String myString = img.getDataAsWstring();
       class1.stop();
       cThread.Abort();
    }

    public void render()
    {
        
    }
}