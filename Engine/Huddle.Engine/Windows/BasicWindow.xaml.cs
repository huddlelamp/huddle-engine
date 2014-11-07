using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Controls.ContextMenu;

namespace Huddle.Engine.Windows
{
    /// <summary>
    /// Interaction logic for BasicWindow.xaml
    /// </summary>
    public partial class BasicWindow
    {
        #region private fields

        private readonly NotifyIcon _notifyIcon;

        #endregion

        public BasicWindow()
        {
            InitializeComponent();

            #region System Tray Initialization

            var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Huddle.Engine;component/Resources/HuddleLamp.ico")).Stream;
            var icon = new Icon(iconStream);
            _notifyIcon = new NotifyIcon { Icon = icon };

            _notifyIcon.MouseClick += NotifyIconMouseClick;
            _notifyIcon.MouseDoubleClick += OnActivateSystemTrayIcon;

            #endregion

            Loaded += BasicWindowLoaded;
        }

        void BasicWindowLoaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            // Start Huddle Engine in system tray
            WindowState = WindowState.Normal;
#else
            // Start Huddle Engine in system tray
            WindowState = WindowState.Minimized;
#endif
        }

        #region System Tray

        void NotifyIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var menu = (ContextMenu)FindResource("NotifierContextMenu");
                menu.IsOpen = true;
            }
        }

        private void OnActivateSystemTrayIcon(object sender, MouseEventArgs e)
        {
            WindowState = WindowState.Normal;
        }

        private void BasicWindow_OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowToSystemTray();
            }
            else if (WindowState == WindowState.Normal)
            {
                WindowToNormal();
            }
        }

        private void WindowToSystemTray()
        {
            ShowInTaskbar = false;
            _notifyIcon.BalloonTipTitle = "Huddle Engine";
            _notifyIcon.BalloonTipText = "Currently running/stopped";
            _notifyIcon.ShowBalloonTip(400);
            _notifyIcon.Visible = true;
        }

        private void WindowToNormal()
        {
            _notifyIcon.Visible = false;
            ShowInTaskbar = true;
        }

        private void MenuExit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        #endregion
    }
}
