using MonoTorrent.Client;
using MonoTorrent.Client.Tracker;
using MonoTorrent.Common;
using SampleClient.WPF.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleClient.WPF.Models
{
    class TorrentItem : INotifyPropertyChanged
    {
        public string Name => manager.Torrent == null ? "MetaDataMode" : manager.Torrent.Name;
        public TorrentState State => manager.State;
        public string Filename => Path.GetFileName(manager.Torrent.TorrentPath);
        public string Progress => string.Format("{0:F2}%", manager.Progress);
        public int Seeds => manager.Peers.Seeds;
        public int Leechs => manager.Peers.Leechs;
        public int Available => manager.Peers.Available;
        public string DownSpeed => manager.Monitor.DownloadSpeed.HumanReadableSpeed();
        public string UpSpeed => manager.Monitor.UploadSpeed.HumanReadableSpeed();
        public string TotalDown => manager.Monitor.DataBytesDownloaded.HumanReadableSize();
        public string TotalUp => manager.Monitor.DataBytesUploaded.HumanReadableSize();
        public int? CurrentRequestCount => manager.PieceManager?.CurrentRequestCount();
        public ObservableCollection<PeerItem> Peers => new ObservableCollection<PeerItem>(manager.GetPeers().Select(p => new PeerItem(p)));
        private IEnumerable<string> FileStatistics => manager.Torrent != null ? manager.Torrent.Files.Select(file => string.Format("{1:0.00}% - {0}", file.Path, file.BitField.PercentComplete)) : new string[0];



        //public string WarningMessage => string.Format("Warning Message:    {0}", Tracker == null ? "<no tracker>" : Tracker.WarningMessage);
        //public string ErrorMessage => string.Format("Failure Message:    {0}", Tracker == null ? "<no tracker>" : Tracker.FailureMessage);
        private TorrentManager manager;
        private Tracker Tracker => manager.TrackerManager.CurrentTracker;

        public event PropertyChangedEventHandler PropertyChanged;

        public TorrentItem(TorrentManager manager)
        {
            this.manager = manager;
            Task.Run(() =>
            {
                while (true)
                {
                    Refresh();
                    Thread.Sleep(500);
                }
            });
        }

        public void Refresh()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Seeds)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Leechs)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Available)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DownSpeed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpSpeed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalDown)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalUp)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentRequestCount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Peers)));
        }

    }
}
