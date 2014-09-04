using System;
using System.Linq;
using System.ServiceModel;
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

#if DEBUG
            StartupUri = new Uri("Windows/EditorWindow.xaml", UriKind.Relative);
#else
            StartupUri = new Uri("Windows/BasicWindow.xaml", UriKind.Relative);
#endif

            DispatcherUnhandledException += AppDispatcherUnhandledException;

            Startup += AppStartup;
        }

        void AppStartup(object sender, StartupEventArgs e)
        {
            Console.WriteLine("Cuda support available: {0}", GpuInvoke.HasCuda);
            //MessageBox.Show(string.Format( "Cuda support available: {0}", GpuInvoke.HasCuda), "Cuda Support");

            foreach (var arg in e.Args)
            {
                var option = arg.Split('=');

                if (option.Length != 2) continue;

                var key = option[0];
                var value = option[1];

                if (!key.StartsWith("--") || !key.StartsWith("-")) continue;

                if (key.StartsWith("--"))
                    key = key.Substring(2);

                if (key.StartsWith("-"))
                    key = key.Substring(1);

                switch (key)
                {
                    case "ui":
                        switch (value)
                        {
                            case "basic":
                                StartupUri = new Uri("Windows/BasicWindow.xaml", UriKind.Relative);
                                break;
                            case "editor":
                                StartupUri = new Uri("Windows/EditorWindow.xaml", UriKind.Relative);
                                break;
                        }
                        break;
                }
            }
        }

        static void AppDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            //MessageBox.Show(e.Exception.StackTrace.ToString(), "Dispatcher Error", MessageBoxButton.OK, MessageBoxImage.Error);

            Console.WriteLine("{0}{1}{2}", e.Exception.Message, Environment.NewLine, e.Exception.StackTrace);

            e.Handled = true;
        }
    }
}
