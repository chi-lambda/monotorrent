using MonoTorrent.BEncoding;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Client.Tracker;
using MonoTorrent.Common;
using MonoTorrent.Dht;
using MonoTorrent.Dht.Listeners;
using Open.Nat;
using Prism.Commands;
using Prism.Mvvm;
using SampleClient.WPF.Extensions;
using SampleClient.WPF.Models;
using SampleClient.WPF.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SampleClient.WPF
{
    class ViewModel : BindableBase
    {

        #region Dependencies

        private NetworkUtil networkUtil = new NetworkUtil();

        #endregion

        /// <summary>
        /// This is the directory we are currently in
        /// </summary>
        private readonly string basePath = Environment.CurrentDirectory;
        /// <summary>
        /// This is the directory we will save .torrents to
        /// </summary>
        private readonly string torrentsPath;
        /// <summary>
        /// This is the directory we will save downloads to
        /// </summary>
        private readonly string downloadsPath;
        private readonly string fastResumeFile;
        private readonly string dhtNodeFile;
        /// <summary>
        /// This is where we will store the torrentmanagers
        /// </summary>
        private readonly List<TorrentManager> torrentManagers = new List<TorrentManager>();
        private readonly TraceListener listener;
        /// <summary>
        /// The engine used for downloading
        /// </summary>
        private ClientEngine engine;
        private int port;
        private IEnumerable<NatDevice> natDevices = new NatDevice[0];

        private string totalDownloadRate;
        private string totalUploadRate;
        private string diskReadRate;
        private string diskWriteRate;
        private string totalRead;
        private string totalWritten;
        private string openConnections;
        private TorrentItem _selectedTorrent;

        public ObservableCollection<string> DebugMessages { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> TorrentFiles { get; } = new ObservableCollection<string>();
        public ObservableCollection<TorrentItem> Torrents { get; } = new ObservableCollection<TorrentItem>();
        public TorrentItem SelectedTorrent
        {
            get => _selectedTorrent; set
            {
                _selectedTorrent = value;
                RaisePropertyChanged(nameof(SelectedTorrentPeers));
            }
        }
        public IEnumerable<PeerItem> SelectedTorrentPeers => SelectedTorrent?.Peers;

        public string TotalDownloadRate { get => totalDownloadRate; set { SetProperty(ref totalDownloadRate, value); } }
        public string TotalUploadRate { get => totalUploadRate; set { SetProperty(ref totalUploadRate, value); } }
        public string DiskReadRate { get => diskReadRate; set { SetProperty(ref diskReadRate, value); } }
        public string DiskWriteRate { get => diskWriteRate; set { SetProperty(ref diskWriteRate, value); } }
        public string TotalRead { get => totalRead; set { SetProperty(ref totalRead, value); } }
        public string TotalWritten { get => totalWritten; set { SetProperty(ref totalWritten, value); } }
        public string OpenConnections { get => openConnections; set { SetProperty(ref openConnections, value); } }

        public ICommand ShutdownCommand { get; }


        public ViewModel(TraceListener listener)
        {
            torrentsPath = Path.Combine(basePath, "Torrents");
            //downloadsPath = Path.Combine(basePath, "Downloads");
            downloadsPath = @"R:\\Downloads";
            fastResumeFile = Path.Combine(torrentsPath, "fastresume.data");
            dhtNodeFile = Path.Combine(basePath, "DhtNodes");
            this.listener = listener;
            Trace.Listeners.Add(listener);
            ShutdownCommand = new DelegateCommand(async () =>
            {
                var evt = new ManualResetEvent(false);
                await Shutdown(evt);
                evt.WaitOne();
            });

            StartEngine();
        }


        public void AddDebugMessage(string message)
        {
            DebugMessages.Add(message);
            RaisePropertyChanged(nameof(DebugMessages));
        }

        private async void StartEngine()
        {
            port = 5001;
            natDevices = await networkUtil.OpenPort(port);
            Torrent torrent = null;

            // Create the settings which the engine will use
            // downloadsPath - this is the path where we will save all the files to
            // port - this is the port we listen for connections on
            EngineSettings engineSettings = new EngineSettings(downloadsPath, port)
            {
                PreferEncryption = false,
                AllowedEncryption = EncryptionTypes.All
            };

            //engineSettings.GlobalMaxUploadSpeed = 30 * 1024;
            //engineSettings.GlobalMaxDownloadSpeed = 100 * 1024;
            //engineSettings.MaxReadRate = 1 * 1024 * 1024;


            // Create the default settings which a torrent will have.
            // 4 Upload slots - a good ratio is one slot per 5kB of upload speed
            // 50 open connections - should never really need to be changed
            // Unlimited download speed - valid range from 0 -> int.Max
            // Unlimited upload speed - valid range from 0 -> int.Max
            TorrentSettings torrentDefaults = new TorrentSettings(4, 150, 0, 0);

            // Create an instance of the engine.
            engine = new ClientEngine(engineSettings);
            engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, port));
            byte[] nodes = File.Exists(dhtNodeFile) ? File.ReadAllBytes(dhtNodeFile) : null;

            var dhtListner = new DhtListener(new IPEndPoint(IPAddress.Any, port));
            var dht = new DhtEngine(dhtListner);
            engine.RegisterDht(dht);
            dhtListner.Start();
            engine.DhtEngine.Start(nodes);

            // If the SavePath does not exist, we want to create it.
            if (!Directory.Exists(engine.Settings.SavePath))
            {
                Directory.CreateDirectory(engine.Settings.SavePath);
            }

            // If the torrentsPath does not exist, we want to create it
            if (!Directory.Exists(torrentsPath))
            {
                Directory.CreateDirectory(torrentsPath);
            }

            BEncodedDictionary fastResume;
            try
            {
                fastResume = BEncodedValue.Decode<BEncodedDictionary>(File.ReadAllBytes(fastResumeFile));
            }
            catch
            {
                fastResume = new BEncodedDictionary();
            }

            // For each file in the torrents path that is a .torrent file, load it into the engine.
            foreach (string file in Directory.GetFiles(torrentsPath))
            {
                if (file.EndsWith(".torrent"))
                {
                    try
                    {
                        // Load the .torrent from the file into a Torrent instance
                        // You can use this to do preprocessing should you need to
                        torrent = Torrent.Load(file);
                        Console.WriteLine(torrent.InfoHash.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.Write("Couldn't decode {0}: ", file);
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    // When any preprocessing has been completed, you create a TorrentManager
                    // which you then register with the engine.
                    TorrentManager manager = new TorrentManager(torrent, downloadsPath, torrentDefaults);
                    if (fastResume.ContainsKey(torrent.InfoHash.ToHex()))
                    {
                        manager.LoadFastResume(new FastResume((BEncodedDictionary)fastResume[torrent.InfoHash.ToHex()]));
                    }

                    await engine.Register(manager);

                    // Store the torrent manager in our list so we can access it later
                    torrentManagers.Add(manager);
                    Torrents.Add(new TorrentItem(manager));
                    manager.PeersFound += new EventHandler<PeersAddedEventArgs>(manager_PeersFound);
                    TorrentFiles.Add(Path.GetFileName(file));
                }
            }
            RaisePropertyChanged(nameof(Torrents));
            RaisePropertyChanged(nameof(TorrentFiles));

            // For each torrent manager we loaded and stored in our list, hook into the events
            // in the torrent manager and start the engine.
            foreach (TorrentManager manager in torrentManagers)
            {
                // Every time a piece is hashed, this is fired.
                manager.PieceHashed += delegate (object o, PieceHashedEventArgs e)
                {
                    lock (listener)
                    {
                        listener.WriteLine(string.Format("Piece Hashed: {0} - {1}", e.PieceIndex, e.HashPassed ? "Pass" : "Fail"));
                    }
                };

                // Every time the state changes (Stopped -> Seeding -> Downloading -> Hashing) this is fired
                manager.TorrentStateChanged += delegate (object o, TorrentStateChangedEventArgs e)
                {
                    lock (listener)
                    {
                        listener.WriteLine("OldState: " + e.OldState.ToString() + " NewState: " + e.NewState.ToString());
                    }
                };

                // Every time the tracker's state changes, this is fired
                foreach (var tier in manager.TrackerManager)
                {
                    foreach (var t in tier.GetTrackers())
                    {
                        t.AnnounceComplete += (object sender, AnnounceResponseEventArgs e) =>
                        {
                            listener.WriteLine(string.Format("{0}: {1}", e.Successful, e.Tracker.ToString()));
                        };
                    }
                }
                // Start the torrentmanager. The file will then hash (if required) and begin downloading/seeding
                manager.Start();
            }

            await Task.Run(() =>
            {
                // While the torrents are still running, print out some stats to the screen.
                // Details for all the loaded torrent managers are shown.
                bool running = true;
                while (running)
                {
                    running = torrentManagers.Exists(m => m.State != TorrentState.Stopped);

                    TotalDownloadRate = engine.TotalDownloadSpeed.HumanReadableSpeed();
                    TotalUploadRate = engine.TotalUploadSpeed.HumanReadableSpeed();
                    DiskReadRate = engine.DiskManager.ReadRate.HumanReadableSpeed();
                    DiskWriteRate = engine.DiskManager.WriteRate.HumanReadableSpeed();
                    TotalRead = engine.DiskManager.TotalRead.HumanReadableSize();
                    TotalWritten = engine.DiskManager.TotalWritten.HumanReadableSize();
                    OpenConnections = string.Format("{0}", engine.ConnectionManager.OpenConnections);

                    RaisePropertyChanged(nameof(SelectedTorrentPeers));

                    Thread.Sleep(500);
                }
            });
        }

        public async Task Shutdown(ManualResetEvent waitHandle)
        {
            Trace.Listeners.Remove(listener);
            var removedMappings = new ManualResetEvent(false);
            var shutdownEngine = new ManualResetEvent(false);
            foreach (var device in natDevices.Where(dev => !networkUtil.LocalAddress(dev).Equals(IPAddress.Parse("192.168.1.154"))))
            {
                try
                {
                    await networkUtil.ClosePort(device, port);
                }
                catch (Exception ex)
                {
                }
            }
            removedMappings.Set();

            await Task.Run(() =>
            {
                if (engine == null) { return; }
                BEncodedDictionary fastResume = new BEncodedDictionary();
                foreach (var torrentManager in torrentManagers)
                {
                    torrentManager.Stop(); ;
                    while (torrentManager.State != TorrentState.Stopped)
                    {
                        Console.WriteLine("{0} is {1}", torrentManager.Torrent.Name, torrentManager.State);
                        Thread.Sleep(250);
                    }

                    fastResume.Add(torrentManager.Torrent.InfoHash.ToHex(), torrentManager.SaveFastResume().Encode());
                }

#if !DISABLE_DHT
                File.WriteAllBytes(dhtNodeFile, engine.DhtEngine.SaveNodes());
#endif
                File.WriteAllBytes(fastResumeFile, fastResume.Encode());
                engine.Dispose();

                Thread.Sleep(2000);
                shutdownEngine.Set();
            });
            removedMappings.WaitOne();
            shutdownEngine.WaitOne();
            waitHandle.Set();
        }

        private void manager_PeersFound(object sender, PeersAddedEventArgs e)
        {
            lock (listener)
            {
                listener.WriteLine(string.Format("Found {0} new peers and {1} existing peers", e.NewPeers, e.ExistingPeers));
            }
        }

    }
}
