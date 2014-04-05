using System;
using System.Windows;
using Emgu.CV.GPU;
using GalaSoft.MvvmLight.Threading;

namespace Huddle.Engine
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            DispatcherHelper.Initialize();

            //if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData != null)
            //    foreach (var commandLineFile in AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData)
            //    {
            //        MessageBox.Show(string.Format("Command Line File: {0}", commandLineFile));
            //    }

            DispatcherUnhandledException += App_DispatcherUnhandledException;

            Startup += App_Startup;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            Console.WriteLine("Cuda support available: {0}", GpuInvoke.HasCuda);
            //MessageBox.Show(string.Format( "Cuda support available: {0}", GpuInvoke.HasCuda), "Cuda Support");
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //MessageBox.Show(e.Exception.StackTrace.ToString(), "Dispatcher Error", MessageBoxButton.OK, MessageBoxImage.Error);

            Console.WriteLine("{0}{1}{2}", e.Exception.Message, Environment.NewLine, e.Exception.StackTrace);

            e.Handled = true;
        }
    }
}
