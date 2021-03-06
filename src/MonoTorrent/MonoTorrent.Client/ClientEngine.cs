//
// ClientEngine.cs
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



using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using MonoTorrent.Client.Tracker;
using MonoTorrent.Client.PieceWriters;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MonoTorrent.Client
{
    /// <summary>
    /// The Engine that contains the TorrentManagers
    /// </summary>
    public class ClientEngine : IDisposable
    {
        internal static MainLoop MainLoop = new MainLoop("Client Engine Loop");
        #region Global Constants

        public static readonly bool SupportsInitialSeed = true;
        public static readonly bool SupportsLocalPeerDiscovery = true;
        public static readonly bool SupportsWebSeed = true;
        public static readonly bool SupportsExtended = true;
        public static readonly bool SupportsFastPeer = true;
        public static readonly bool SupportsEncryption = true;
        public static readonly bool SupportsEndgameMode = true;
#if !DISABLE_DHT
        public static readonly bool SupportsDht = true;
#else
        public static readonly bool SupportsDht = false;
#endif
        internal const int TickLength = 500;    // A logic tick will be performed every TickLength miliseconds
       
        #endregion


        #region Events

        public event EventHandler<StatsUpdateEventArgs> StatsUpdate;
        public event EventHandler<CriticalExceptionEventArgs> CriticalException;

        #endregion


        #region Member Variables

        internal static readonly BufferManager BufferManager = new BufferManager();
        private ListenManager listenManager;         // Listens for incoming connections and passes them off to the correct TorrentManager
        private LocalPeerManager localPeerManager;
        private LocalPeerListener localPeerListener;
        private int tickCount;
        private List<TorrentManager> torrents;
        private ReadOnlyCollection<TorrentManager> torrentsReadonly;
        private RateLimiterGroup uploadLimiter;
        private RateLimiterGroup downloadLimiter;

        #endregion


        #region Properties

        public ConnectionManager ConnectionManager { get; }

#if !DISABLE_DHT
        public IDhtEngine DhtEngine { get; private set; }
#endif
        public DiskManager DiskManager { get; }

        public bool Disposed { get; private set; }

        public PeerListener Listener { get; }

        public bool LocalPeerSearchEnabled
        {
            get { return localPeerListener.Status != ListenerStatus.NotListening; }
            set
            {
                if (value && !LocalPeerSearchEnabled)
                    localPeerListener.Start();
                else if (!value && LocalPeerSearchEnabled)
                    localPeerListener.Stop();
            }
        }

        public bool IsRunning { get; private set; }

        public string PeerId { get; }

        public EngineSettings Settings { get; }

        public IList<TorrentManager> Torrents
        {
            get { return torrentsReadonly; }
        }

        public int TotalDownloadSpeed
        {
            get
            {
                int total = 0;
                for (int i = 0; i < torrents.Count; i++)
                    total += torrents[i].Monitor.DownloadSpeed;
                return total;
            }
        }

        public int TotalUploadSpeed
        {
            get
            {
                int total = 0;
                for (int i = 0; i < torrents.Count; i++)
                    total += torrents[i].Monitor.UploadSpeed;
                return total;
            }
        }

        #endregion


        #region Constructors

        public ClientEngine(EngineSettings settings)
            : this (settings, new DiskWriter())
        {

        }

        public ClientEngine(EngineSettings settings, PieceWriter writer)
            : this(settings, new SocketListener(new IPEndPoint(IPAddress.Any, 0)), writer)

        {

        }

        public ClientEngine(EngineSettings settings, PeerListener listener)
            : this (settings, listener, new DiskWriter())
        {

        }

        public ClientEngine(EngineSettings settings, PeerListener listener, PieceWriter writer)
        {
            Check.Settings(settings);
            Check.Listener(listener);
            Check.Writer(writer);

            this.Listener = listener;
            this.Settings = settings;

            this.ConnectionManager = new ConnectionManager(this);
            RegisterDht (new NullDhtEngine());
            this.DiskManager = new DiskManager(this, writer);
            this.listenManager = new ListenManager(this);
            MainLoop.QueueTimeout(TimeSpan.FromMilliseconds(TickLength), delegate {
                if (IsRunning && !Disposed)
                    LogicTick();
                return !Disposed;
            });
            this.torrents = new List<TorrentManager>();
            this.torrentsReadonly = new ReadOnlyCollection<TorrentManager> (torrents);
            CreateRateLimiters();
            this.PeerId = GeneratePeerId();

            localPeerListener = new LocalPeerListener(this);
            localPeerManager = new LocalPeerManager();
            LocalPeerSearchEnabled = SupportsLocalPeerDiscovery;
            listenManager.Register(listener);
            // This means we created the listener in the constructor
            if (listener.Endpoint.Port == 0)
                listener.ChangeEndpoint(new IPEndPoint(IPAddress.Any, settings.ListenPort));
        }

        void CreateRateLimiters()
        {
            RateLimiter downloader = new RateLimiter();
            downloadLimiter = new RateLimiterGroup();
            downloadLimiter.Add(new DiskWriterLimiter(DiskManager));
            downloadLimiter.Add(downloader);

            RateLimiter uploader = new RateLimiter();
            uploadLimiter = new RateLimiterGroup();
            uploadLimiter.Add(uploader);

            MainLoop.QueueTimeout(TimeSpan.FromSeconds(1), delegate {
                downloader.UpdateChunks(Settings.GlobalMaxDownloadSpeed, TotalDownloadSpeed);
                uploader.UpdateChunks(Settings.GlobalMaxUploadSpeed, TotalUploadSpeed);
                return !Disposed;
            });
        }

        #endregion


        #region Methods

        public void ChangeListenEndpoint(IPEndPoint endpoint)
        {
            Check.Endpoint(endpoint);

            Settings.ListenPort = endpoint.Port;
            Listener.ChangeEndpoint(endpoint);
        }

        private void CheckDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        public bool Contains(InfoHash infoHash)
        {
            CheckDisposed();
            if (infoHash == null)
                return false;

            return torrents.Exists(delegate(TorrentManager m) { return m.InfoHash.Equals(infoHash); });
        }

        public bool Contains(Torrent torrent)
        {
            CheckDisposed();
            if (torrent == null)
                return false;

            return Contains (torrent.InfoHash);
        }

        public bool Contains(TorrentManager manager)
        {
            CheckDisposed();
            if (manager == null)
                return false;
            
            return Contains(manager.Torrent);
        }

        public void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            MainLoop.QueueWait((Action)delegate {
                this.DhtEngine.Dispose();
                this.DiskManager.Dispose();
                this.listenManager.Dispose();
                this.localPeerListener.Stop();
                this.localPeerManager.Dispose();
            });
        }

        public async Task PauseAll()
        {
            CheckDisposed();
            await MainLoop;
            foreach (TorrentManager manager in torrents)
                manager.Pause();
        }

        public async Task Register(TorrentManager manager)
        {
            CheckDisposed();
            Check.Manager(manager);

            await MainLoop;
            if (manager.Engine != null)
                throw new TorrentException("This manager has already been registered");

            if (Contains(manager.Torrent))
                throw new TorrentException("A manager for this torrent has already been registered");
            this.torrents.Add(manager);
            manager.Engine = this;
            manager.DownloadLimiter.Add(downloadLimiter);
            manager.UploadLimiter.Add(uploadLimiter);
            if (DhtEngine != null && manager.Torrent != null && manager.Torrent.Nodes != null && DhtEngine.State != DhtState.Ready)
            {
                try
                {
                    DhtEngine.Add(manager.Torrent.Nodes);
                }
                catch
                {
                    // FIXME: Should log this somewhere, though it's not critical
                }
            }
        }

        public void RegisterDht(IDhtEngine engine)
        {
            MainLoop.QueueWait(delegate
            {
                if (DhtEngine != null)
                {
                    DhtEngine.StateChanged -= DhtEngineStateChanged;
                    DhtEngine.Stop();
                    DhtEngine.Dispose();
                }
                DhtEngine = engine ?? new NullDhtEngine();
            });

            DhtEngine.StateChanged += DhtEngineStateChanged;
        }

        void DhtEngineStateChanged (object o, EventArgs e)
        {
            if (DhtEngine.State != DhtState.Ready)
                return;

            MainLoop.Queue (delegate {
                foreach (TorrentManager manager in torrents) {
                    if (!manager.CanUseDht)
                        continue;

                    DhtEngine.AnnounceAsync (manager.InfoHash, Listener.Endpoint.Port);
                    DhtEngine.GetPeersAsync (manager.InfoHash);
                }
            });
        }

        public async Task StartAll()
        {
            CheckDisposed();

            await MainLoop;
            for (int i = 0; i < torrents.Count; i++)
                torrents[i].Start();
        }

        public async Task StopAll()
        {
            CheckDisposed();

            await MainLoop;
            for (int i = 0; i < torrents.Count; i++)
                torrents[i].Stop();
        }

        public async Task Unregister(TorrentManager manager)
        {
            CheckDisposed();
            Check.Manager(manager);

            await MainLoop;
            if (manager.Engine != this)
                throw new TorrentException("The manager has not been registered with this engine");

            if (manager.State != TorrentState.Stopped)
                throw new TorrentException("The manager must be stopped before it can be unregistered");

            this.torrents.Remove(manager);

            manager.Engine = null;
            manager.DownloadLimiter.Remove(downloadLimiter);
            manager.UploadLimiter.Remove(uploadLimiter);
        }

        #endregion


        #region Private/Internal methods

        internal void Broadcast(TorrentManager manager)
        {
            if (LocalPeerSearchEnabled)
                localPeerManager.Broadcast(manager);
        }

        private void LogicTick()
        {
            tickCount++;

            ConnectionManager.TryConnect ();
            for (int i = 0; i < this.torrents.Count; i++)
                this.torrents[i].Mode.Tick(tickCount);

            RaiseStatsUpdate(new StatsUpdateEventArgs());
        }

        internal void RaiseCriticalException(CriticalExceptionEventArgs e)
        {
            Toolbox.RaiseAsyncEvent<CriticalExceptionEventArgs>(CriticalException, this, e); 
        }


        internal void RaiseStatsUpdate(StatsUpdateEventArgs args)
        {
            Toolbox.RaiseAsyncEvent<StatsUpdateEventArgs>(StatsUpdate, this, args);
        }

        internal void Start()
        {
            CheckDisposed();
            IsRunning = true;
            if (Listener.Status == ListenerStatus.NotListening)
                Listener.Start();
        }


        internal void Stop()
        {
            CheckDisposed();
            // If all the torrents are stopped, stop ticking
            IsRunning = torrents.Exists(delegate(TorrentManager m) { return m.State != TorrentState.Stopped; });
            if (!IsRunning)
                Listener.Stop();
        }


        static int count = 0;
        static string GeneratePeerId()
        {
            StringBuilder sb = new StringBuilder(20);
            sb.Append(Common.VersionInfo.ClientVersion);

            var random = new Random(count++);
            while (sb.Length < 20)
                sb.Append(random.Next(0, 9));

            return sb.ToString();
        }

        #endregion
    }
}