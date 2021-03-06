using System;
using System.Collections.Generic;
using System.Text;
using MonoTorrent.Common;
using MonoTorrent.Client.Encryption;
using System.Net.Sockets;
using MonoTorrent.Client.Messages.Standard;
using MonoTorrent.Client.Messages;
using System.Threading.Tasks;

namespace MonoTorrent.Client
{
    /// <summary>
    /// Instance methods of this class are threadsafe
    /// </summary>
    public class ListenManager : IDisposable
    {
        #region Member Variables

        private ClientEngine engine;
        private MonoTorrentCollection<PeerListener> listeners;

        #endregion Member Variables


        #region Properties

        public MonoTorrentCollection<PeerListener> Listeners
        {
            get { return listeners; }
        }

        internal ClientEngine Engine
        {
            get { return engine; }
            private set { engine = value; }
        }

        #endregion Properties


        #region Constructors

        internal ListenManager(ClientEngine engine)
        {
            Engine = engine;
            listeners = new MonoTorrentCollection<PeerListener>();
        }

        #endregion Constructors


        #region Public Methods

        public void Dispose()
        {
        }

        public void Register(PeerListener listener)
        {
            listener.ConnectionReceived += new EventHandler<NewConnectionEventArgs>(ConnectionReceived);
        }

        public void Unregister(PeerListener listener)
        {
            listener.ConnectionReceived -= new EventHandler<NewConnectionEventArgs>(ConnectionReceived);
        }

        #endregion Public Methods




        private async void ConnectionReceived(object sender, NewConnectionEventArgs e)
        {
            await ClientEngine.MainLoop;
            try
            {
                if (engine.ConnectionManager.ShouldBanPeer(e.Peer))
                {
                    e.Connection.Dispose();
                    return;
                }
                var id = new PeerId(e.Peer, e.TorrentManager);
                id.Connection = e.Connection;
                if (!e.Connection.IsIncoming) {
                    engine.ConnectionManager.ProcessFreshConnection(id);
                    return;
                }

                Logger.Log(id.Connection, "ListenManager - ConnectionReceived");

                var skeys = new List<InfoHash>();
                for (int i = 0; i < engine.Torrents.Count; i++)
                    skeys.Add(engine.Torrents[i].InfoHash);

                var initialData = await EncryptorFactory.CheckEncryptionAsync(id, HandshakeMessage.HandshakeLength, skeys.ToArray());
                if (initialData != null && initialData.Length != HandshakeMessage.HandshakeLength)
                {
                    e.Connection.Dispose();
                    return;
                }

                HandshakeMessage handshake;
                if (initialData == null)
                {
                    handshake = await PeerIO.ReceiveHandshakeAsync(id.Connection, id.Decryptor);
                }
                else
                {
                    handshake = new HandshakeMessage();
                    handshake.Decode(initialData, 0, initialData.Length);
                }
                if (!await HandleHandshake(id, handshake))
                    e.Connection.Dispose();
            }
            catch
            {
                e.Connection.Dispose();
            }
        }


        private async Task<bool> HandleHandshake(PeerId id, HandshakeMessage message)
        {
            TorrentManager man = null;
            if (message.ProtocolString != VersionInfo.ProtocolStringV100)
                return false;

            // If we're forcing encrypted connections and this is in plain-text, close it!
            if (id.Encryptor is PlainTextEncryption && !engine.Settings.AllowedEncryption.HasFlag(EncryptionTypes.PlainText))
                return false;

            for (int i = 0; i < engine.Torrents.Count; i++)
                if (message.infoHash == engine.Torrents[i].InfoHash)
                    man = engine.Torrents[i];

            // We're not hosting that torrent
            if (man == null)
                return false;

			if (man.State == TorrentState.Stopped)
                return false;

            if (!man.Mode.CanAcceptConnections)
                return false;

            id.Peer.PeerId = message.PeerId;
            id.TorrentManager = man;

            message.Handle(id);
            Logger.Log(id.Connection, "ListenManager - Handshake successful handled");

            id.ClientApp = new Software(message.PeerId);

            message = new HandshakeMessage(id.TorrentManager.InfoHash, engine.PeerId, VersionInfo.ProtocolStringV100);
            await PeerIO.SendMessageAsync (id.Connection, id.Encryptor, message, id.TorrentManager.UploadLimiter, id.Monitor, id.TorrentManager.Monitor);
            engine.ConnectionManager.IncomingConnectionAccepted (id);
            return true;
        }
    }
}
