//
// TransferTest.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2008 Alan McGovern
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
using System.Net;
using System.Text;
using System.Threading;
using NUnit.Framework;
using MonoTorrent.Client.Messages.Standard;
using MonoTorrent.Common;
using MonoTorrent.Client.Messages.FastPeer;
using MonoTorrent.Client.Messages;
using MonoTorrent.Client.Encryption;
using System.Threading.Tasks;

namespace MonoTorrent.Client
{
    [TestFixture]
    public class TransferTest
    {
        IEncryption decryptor = PlainTextEncryption.Instance;
        IEncryption encryptor = PlainTextEncryption.Instance;

        private ConnectionPair pair;
        private TestRig rig;

        [SetUp]
        public void Setup()
        {
            pair = new ConnectionPair(55432);
            rig = TestRig.CreateMultiFile();
            rig.Manager.HashChecked = true;
            rig.Manager.Start();
        }

        [TearDown]
        public void Teardown()
        {
            rig.Manager.Stop();
            pair.Dispose();
            rig.Dispose();
        }

        [Test]
        public void IncomingRC4Full_SupportsPlain()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.PlainText;
            rig.AddConnection(pair.Outgoing);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Incoming, EncryptionTypes.RC4Full));
        }

        [Test]
        public void IncomingRC4Full_SupportsRC4Header()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Header;
            rig.AddConnection(pair.Outgoing);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Incoming, EncryptionTypes.RC4Full));
        }

        [Test]
        public async Task IncomingRC4Full_SupportsRC4Full ()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Full;
            rig.AddConnection(pair.Outgoing);
            await InitiateTransfer(pair.Incoming, EncryptionTypes.RC4Full);
        }

        [Test]
        public void IncomingRC4Header_SupportsPlainText()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.PlainText;
            rig.AddConnection(pair.Outgoing);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Incoming, EncryptionTypes.RC4Header));
        }

        [Test]
        public async Task IncomingRC4Header_SupportsRC4Header()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Header;
            rig.AddConnection(pair.Outgoing);
            await InitiateTransfer(pair.Incoming, EncryptionTypes.RC4Header);
        }

        [Test]
        public void IncomingRC4Header_SupportsRC4Full()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Full;
            rig.AddConnection(pair.Outgoing);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Incoming, EncryptionTypes.RC4Header));
        }

        [Test]
        public async Task IncomingPlainText_SupportsPlainText()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.PlainText;
            rig.AddConnection(pair.Outgoing);
            await InitiateTransfer(pair.Incoming, EncryptionTypes.PlainText);
        }

        [Test]
        public void IncomingPlainText_SupportsRC4Header()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Header;
            rig.AddConnection(pair.Outgoing);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Incoming, EncryptionTypes.PlainText));
        }

        [Test]
        public void IncomingPlainText_SupportsRC4Full()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Full;
            rig.AddConnection(pair.Outgoing);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Incoming, EncryptionTypes.PlainText));
        }

        [Test]
        public void OutgoingRC4Full_SupportsPlain()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.PlainText;
            rig.AddConnection(pair.Incoming);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Outgoing, EncryptionTypes.RC4Full));
        }

        [Test]
        public void OutgoingRC4Full_SupportsRC4Header()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Header;
            rig.AddConnection(pair.Incoming);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Outgoing, EncryptionTypes.RC4Full));
        }

        [Test]
        public async Task OutgoingRC4Full_SupportsRC4Full ()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Full;
            rig.AddConnection(pair.Incoming);
            await InitiateTransfer(pair.Outgoing, EncryptionTypes.RC4Full);
        }

        [Test]
        public void OutgoingRC4Header_SupportsPlainText()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.PlainText;
            rig.AddConnection(pair.Incoming);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Outgoing, EncryptionTypes.RC4Header));
        }

        [Test]
        public async Task OutgoingRC4Header_SupportsRC4Header()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Header;
            rig.AddConnection(pair.Incoming);
            await InitiateTransfer(pair.Outgoing, EncryptionTypes.RC4Header);
        }

        [Test]
        public void OutgoingRC4Header_SupportsRC4Full()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Full;
            rig.AddConnection(pair.Incoming);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Outgoing, EncryptionTypes.RC4Header));
        }

        [Test]
        public async Task OutgoingPlainText_SupportsPlainText()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.PlainText;
            rig.AddConnection(pair.Incoming);
            await InitiateTransfer(pair.Outgoing, EncryptionTypes.PlainText);
        }

        [Test]
        public void OutgoingPlainText_SupportsRC4Header()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Header;
            rig.AddConnection(pair.Incoming);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Outgoing, EncryptionTypes.PlainText));
        }

        [Test]
        public void OutgoingPlainText_SupportsRC4Full()
        {
            rig.Engine.Settings.AllowedEncryption = EncryptionTypes.RC4Full;
            rig.AddConnection(pair.Incoming);
            Assert.ThrowsAsync<EncryptionException> (() => InitiateTransfer(pair.Outgoing, EncryptionTypes.PlainText));
        }

        [Test]
        public async Task MassiveMessage()
        {
            rig.AddConnection(pair.Incoming);
            await InitiateTransfer(pair.Outgoing, EncryptionTypes.All);
            pair.Outgoing.EndSend(pair.Outgoing.BeginSend(new byte[] { 255 >> 1, 255, 255, 250 }, 0, 4, null, null));
            IAsyncResult result = pair.Outgoing.BeginReceive(new byte[1000], 0, 1000, null, null);
            if (!result.AsyncWaitHandle.WaitOne(1000, true))
                Assert.Fail("Connection never closed");

            int r = pair.Outgoing.EndReceive(result);
            if (r != 0)
                Assert.Fail("Connection should've been closed");
        }

        [Test]
        public async Task NegativeData()
        {
            rig.AddConnection(pair.Incoming);
            await InitiateTransfer(pair.Outgoing, EncryptionTypes.All);
            pair.Outgoing.EndSend(pair.Outgoing.BeginSend(new byte[] { 255, 255, 255, 250 }, 0, 4, null, null));
            IAsyncResult result = pair.Outgoing.BeginReceive(new byte[1000], 0, 1000, null, null);
            if (!result.AsyncWaitHandle.WaitOne(1000, true))
                Assert.Fail("Connection never closed");

            int r = pair.Outgoing.EndReceive(result);
            if (r != 0)
                Assert.Fail("Connection should've been closed");
        }

        public async Task InitiateTransfer(CustomConnection connection, EncryptionTypes allowedEncryption)
        {
            PeerId id = new PeerId(new Peer("", connection.Uri), rig.Manager);
            id.Peer.Encryption = allowedEncryption;
            id.Connection = connection;

            var data = await EncryptorFactory.CheckEncryptionAsync(id, 68, new InfoHash[] { id.TorrentManager.InfoHash });
            decryptor = id.Decryptor;
            encryptor = id.Encryptor;
            TestHandshake(data, connection);
        }

        public void TestHandshake(byte[] buffer, CustomConnection connection)
        {
            // 1) Send local handshake
            SendMessage(new HandshakeMessage(rig.Manager.Torrent.InfoHash, new string('g', 20), VersionInfo.ProtocolStringV100, true, false), connection);

            // 2) Receive remote handshake
            if (buffer == null || buffer.Length == 0)
            {
                buffer = new byte[68];
                Receive (connection, buffer, 0, 68);
                decryptor.Decrypt(buffer);
            }

            HandshakeMessage handshake = new HandshakeMessage();
            handshake.Decode(buffer, 0, buffer.Length);
            Assert.AreEqual(rig.Engine.PeerId, handshake.PeerId, "#2");
            Assert.AreEqual(VersionInfo.ProtocolStringV100, handshake.ProtocolString, "#3");
            Assert.AreEqual(ClientEngine.SupportsFastPeer, handshake.SupportsFastPeer, "#4");
            Assert.AreEqual(ClientEngine.SupportsExtended, handshake.SupportsExtendedMessaging, "#5");

            // 2) Send local bitfield
            SendMessage(new BitfieldMessage(rig.Manager.Bitfield), connection);

            // 3) Receive remote bitfield - have none
            PeerMessage message = ReceiveMessage(connection);
			Assert.IsTrue (message is HaveNoneMessage || message is BitfieldMessage, "HaveNone");
			
            // 4) Send a few allowed fast
            SendMessage(new AllowedFastMessage(1), connection);
            SendMessage(new AllowedFastMessage(2), connection);
            SendMessage(new AllowedFastMessage(3), connection);
            SendMessage(new AllowedFastMessage(0), connection);

            // 5) Receive a few allowed fast
            ReceiveMessage(connection);
            ReceiveMessage(connection);
            ReceiveMessage(connection);
            ReceiveMessage(connection);
            ReceiveMessage(connection);
            ReceiveMessage(connection);
            ReceiveMessage(connection);
            ReceiveMessage(connection);
            ReceiveMessage(connection);
            ReceiveMessage(connection);
        }

        public static void Send(CustomConnection connection, byte[] buffer, int offset, int count)
        {
            while (count > 0) {
                var r = connection.BeginSend (buffer, offset, count, null, null);
                if (!r.AsyncWaitHandle.WaitOne (TimeSpan.FromSeconds (4)))
                    throw new Exception ("Could not send required data");
                int transferred = connection.EndSend (r);
                if (transferred == 0)
                    throw new Exception ("The socket was gracefully killed");
                offset += transferred;
                count -= transferred;
            }
        }

        void SendMessage(PeerMessage message, CustomConnection connection)
        {
            byte[] b = message.Encode();
            encryptor.Encrypt(b);
            Send (connection, b, 0, b.Length);
        }

        public static void Receive(CustomConnection connection, byte[] buffer, int offset, int count)
        {
            while (count > 0) {
                var r = connection.BeginReceive (buffer, offset, count, null, null);
                if (!r.AsyncWaitHandle.WaitOne (TimeSpan.FromSeconds (4)))
                    throw new Exception ("Could not receive required data");
                int transferred = connection.EndReceive (r);
                if (transferred == 0)
                    throw new Exception ("The socket was gracefully killed");
                offset += transferred;
                count -= transferred;
            }
        }

        PeerMessage ReceiveMessage(CustomConnection connection)
        {
            return ReceiveMessage(connection, decryptor, rig.Manager);
        }

        public static PeerMessage ReceiveMessage(CustomConnection connection, IEncryption decryptor, TorrentManager manager)
        {
            byte[] buffer = new byte[4];
            Receive (connection, buffer, 0, buffer.Length);
            decryptor.Decrypt(buffer);

            int count = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(buffer, 0));
            byte[] message = new byte[count + 4];
            Buffer.BlockCopy(buffer, 0, message, 0, 4);

            Receive (connection, message, 4, count);
            decryptor.Decrypt(message, 4, count);

            return PeerMessage.DecodeMessage(message, 0, message.Length, manager);
        }
    }
}
