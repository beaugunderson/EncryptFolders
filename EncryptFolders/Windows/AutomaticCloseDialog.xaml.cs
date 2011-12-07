using System;
using System.Threading;
using System.Windows;

namespace EncryptFolders.Windows
{
    /// <summary>
    /// Interaction logic for AutomaticCloseDialog.xaml
    /// </summary>
    public partial class AutomaticCloseDialog
    {
        private Timer _closeTimer;

        public AutomaticCloseDialog()
        {
            InitializeComponent();
        }

        private void DefaultClose()
        {
            DialogResult = true;

            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int i = 15;

                _closeTimer = new Timer(delegate
                {
                    i--;
   
                    if (i <= 0)
                    {
                        Dispatcher.BeginInvoke((Action)DefaultClose);
                    }
   
                    Dispatcher.BeginInvoke((Action)delegate
                    {
                        closeButton.Content = string.Format("Close ({0})", i);
                    });
                },
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1));
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            DefaultClose();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}