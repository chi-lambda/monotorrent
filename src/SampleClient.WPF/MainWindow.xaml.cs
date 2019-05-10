using SampleClient.WPF.Diagnostics;
using System;
using System.Threading;
using System.Windows;

namespace SampleClient.WPF
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel model;
        private bool reallyClose = false;

        public MainWindow()
        {
            InitializeComponent();
            var listener = new WpfListener(this);
            model = new ViewModel(listener);
            listener.SetModel(model);
            DataContext = model;
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!reallyClose)
            {
                e.Cancel = true;
                var evt = new ManualResetEvent(false);
                    await model.Shutdown(evt);
                evt.WaitOne();
                reallyClose = true;
                Close();
            }
        }
    }
}
