using MonoTorrent.Client;
using SampleClient.WPF.Extensions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SampleClient.WPF.Models
{
    class PeerItem : INotifyPropertyChanged
    {
        private readonly PeerId peerId;

        public Uri ConnectionUri => peerId.Peer.ConnectionUri;
        public string DownloadSpeed => peerId.Monitor.DownloadSpeed.HumanReadableSpeed();
        public string UploadSpeed => peerId.Monitor.UploadSpeed.HumanReadableSpeed();
        public int AmRequestingPiecesCount => peerId.AmRequestingPiecesCount;
        public int IsRequestingPiecesCount => peerId.IsRequestingPiecesCount;

        public PeerItem(PeerId peerId)
        {
            this.peerId = peerId;
            peerId.Monitor.PropertyChanged += (object sender, PropertyChangedEventArgs e) => NotifyPropertyChanged(e.PropertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Refresh()
        {
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownloadSpeed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UploadSpeed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AmRequestingPiecesCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRequestingPiecesCount)));
        }
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
