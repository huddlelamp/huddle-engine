using System;
using System.Windows;
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
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.StackTrace.ToString(), "Dispatcher Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
