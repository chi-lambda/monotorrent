//
// ConnectionMonitor.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using MonoTorrent.Common;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonoTorrent.Client
{
    /// <summary>
    /// This class is used to track upload/download speed and bytes uploaded/downloaded for each connection
    /// </summary>
    public class ConnectionMonitor : INotifyPropertyChanged
    {
        #region Member Variables

        private object locker = new object();

        #endregion Member Variables


        #region Public Properties

              public SpeedMonitor DataDown { get; }
        public SpeedMonitor DataUp { get; }
        public SpeedMonitor ProtocolDown { get; }
        public SpeedMonitor ProtocolUp { get; }

        public event PropertyChangedEventHandler PropertyChanged;

  public long DataBytesDownloaded
        {
            get { return DataDown.Total; }
        }

        public long DataBytesUploaded
        {
            get { return DataUp.Total; }
        }

        public int DownloadSpeed
        {
            get { return DataDown.Rate + ProtocolDown.Rate; }
        }

        public long ProtocolBytesDownloaded
        {
            get { return ProtocolDown.Total; }
        }

        public long ProtocolBytesUploaded
        {
            get { return ProtocolUp.Total; }
        }

        public int UploadSpeed
        {
            get { return DataUp.Rate + ProtocolUp.Rate; }
        }

        #endregion Public Properties


        #region Constructors

        internal ConnectionMonitor()
            : this(12)
        {

        }

        internal ConnectionMonitor(int averagingPeriod)
        {
            DataDown = new SpeedMonitor(averagingPeriod);
            DataDown.PropertyChanged += (object sender, PropertyChangedEventArgs e) => NotifyPropertyChanged(nameof(DownloadSpeed));
            DataUp = new SpeedMonitor(averagingPeriod);
            DataDown.PropertyChanged += (object sender, PropertyChangedEventArgs e) => NotifyPropertyChanged(nameof(UploadSpeed));
            ProtocolDown = new SpeedMonitor(averagingPeriod);
            DataDown.PropertyChanged += (object sender, PropertyChangedEventArgs e) => NotifyPropertyChanged(nameof(DownloadSpeed));
            ProtocolUp = new SpeedMonitor(averagingPeriod);
            DataDown.PropertyChanged += (object sender, PropertyChangedEventArgs e) => NotifyPropertyChanged(nameof(UploadSpeed));
        }

        #endregion


        #region Methods

        internal void BytesSent(int bytesUploaded, TransferType type)
        {
            lock (locker)
            {
                if (type == TransferType.Data)
                {
                    DataUp.AddDelta(bytesUploaded);
                }
                else
                {
                    ProtocolUp.AddDelta(bytesUploaded);
                }
            }
        }

        internal void BytesReceived(int bytesDownloaded, TransferType type)
        {
            lock (locker)
            {
                if (type == TransferType.Data)
                {
                    DataDown.AddDelta(bytesDownloaded);
                }
                else
                {
                    ProtocolDown.AddDelta(bytesDownloaded);
                }
            }
        }

        internal void Reset()
        {
            DataDown.Reset();
            DataUp.Reset();
            ProtocolDown.Reset();
            ProtocolUp.Reset();
        }

        internal void Tick()
        {
            DataDown.Tick();
            DataUp.Tick();
            ProtocolDown.Tick();
            ProtocolUp.Tick();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
