using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using AForge.Imaging;
using DepthSenseWrapper;

class SoftKinetic
{
   public SoftKinetic()
    {
        Wrapper DSW = new Wrapper();
       if (DSW.init())
       {
           DSW.start();
       }

       //Thread cThread = new Thread(new ThreadStart( DSW.start ));
       //cThread.Start();

       //class1.stop();
       //cThread.Abort();
    }

    public void render()
    {
        
    }
}