using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleClient.WPF.Diagnostics
{
    class WpfListener : TraceListener
    {
        private ViewModel model;
        private readonly MainWindow view;

        public WpfListener(MainWindow view)
        {
            this.view = view;
        }

        internal void SetModel(ViewModel model)
        {
            this.model = model;
        }

        public override void Write(string message)
        {
            try
            {
                view.Dispatcher.Invoke(() => model.AddDebugMessage(message));
            }
            catch { }
        }

        public override void WriteLine(string message)
        {
            Write(message);
        }
    }
}
