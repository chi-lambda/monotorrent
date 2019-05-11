//
// NullDhtEngine.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2009 Alan McGovern
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
using System.Threading.Tasks;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Client
{
    class NullDhtEngine : IDhtEngine
    {
        #pragma warning disable 0067
        public event EventHandler<PeersFoundEventArgs> PeersFound;
        public event EventHandler StateChanged;
        #pragma warning restore 0067

        public bool Disposed
        {
            get { return false; }
        }

        public DhtState State
        {
            get { return DhtState.NotReady; }
        }

        public void Add(BEncodedList nodes)
        {
            
        }

        public async Task AnnounceAsync(InfoHash infohash, int port)
        {
            await Task.CompletedTask;
        }

        public void Dispose()
        {

        }

        public async Task GetPeersAsync(InfoHash infohash)
        {
            await Task.CompletedTask;
        }

        public byte[] SaveNodes()
        {
            return new byte[0];
        }

        public void Start()
        {
            
        }

        public void Start(byte[] initialNodes)
        {
            
        }

        public void Stop()
        {
            
        }
    }
}
