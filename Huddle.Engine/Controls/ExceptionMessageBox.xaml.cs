using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Huddle.Engine.Controls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ExceptionMessageBox : Window
    {
        readonly string _userExceptionMessage;
        readonly List<string> _exceptionInformationList = new List<string>();

        public static bool? ShowException(Exception e, string userExceptionMessage)
        {
            var window = new ExceptionMessageBox(e, userExceptionMessage);
            return window.ShowDialog();
        }


        public ExceptionMessageBox(System.Exception e, string userExceptionMessage)
        {
            InitializeComponent();

            this._userExceptionMessage = userExceptionMessage;
            textBox1.Text = userExceptionMessage;

            TreeViewItem treeViewItem = new TreeViewItem();
            treeViewItem.Header = "Exception";
            treeViewItem.ExpandSubtree();
            buildTreeLayer(e, treeViewItem);
            treeView1.Items.Add(treeViewItem);
        }

        void buildTreeLayer(System.Exception e, TreeViewItem parent)
        {
            String exceptionInformation = "\n\r\n\r" + e.GetType().ToString() + "\n\r\n\r";
            parent.DisplayMemberPath = "Header";
            parent.Items.Add(new TreeViewStringSet() { Header = "Type", Content = e.GetType().ToString() });
            PropertyInfo[] memberList = e.GetType().GetProperties();
            foreach (PropertyInfo info in memberList)
            {
                var value = info.GetValue(e, null);
                if (value != null)
                {
                    if (info.Name == "InnerException")
                    {
                        TreeViewItem treeViewItem = new TreeViewItem();
                        treeViewItem.Header = info.Name;
                        buildTreeLayer(e.InnerException, treeViewItem);
                        parent.Items.Add(treeViewItem);
                    }
                    else
                    {
                        TreeViewStringSet treeViewStringSet = new TreeViewStringSet() { Header = info.Name, Content = value.ToString() };
                        parent.Items.Add(treeViewStringSet);
                        exceptionInformation += treeViewStringSet.Header + "\n\r\n\r" + treeViewStringSet.Content + "\n\r\n\r";
                    }
                }
            }
            _exceptionInformationList.Add(exceptionInformation);
        }


        private void treeView1_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue.GetType() == typeof(TreeViewItem)) textBox1.Text = "Exception";
            else textBox1.Text = e.NewValue.ToString();
        }

        private class TreeViewStringSet
        {
            public string Header { get; set; }
            public string Content { get; set; }

            public override string ToString()
            {
                return Content;
            }
        }

        private void buttonClipboard_Click(object sender, RoutedEventArgs e)
        {
            string clipboardMessage = _userExceptionMessage + "\n\r\n\r";
            foreach (string info in _exceptionInformationList) clipboardMessage += info;
            Clipboard.SetText(clipboardMessage);
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}