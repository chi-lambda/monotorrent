using System;
using System.Collections.Generic;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Client.Tracker
{
    public abstract class TrackerResponseEventArgs : EventArgs
    {
        private bool successful;
        TrackerConnectionID id;
        private Tracker tracker;

        internal TrackerConnectionID Id
        {
            get { return id; }
        }

        public object State
        {
            get { return id; }
        }

        /// <summary>
        /// True if the request completed successfully
        /// </summary>
        public bool Successful
        {
            get { return successful; }
            set { successful = value; }
        }

        /// <summary>
        /// The tracker which the request was sent to
        /// </summary>
        public Tracker Tracker
        {
            get { return tracker; }
            protected set { tracker = value; }
        }

        protected TrackerResponseEventArgs(Tracker tracker, TrackerConnectionID state, bool successful)
        {
            this.tracker = tracker ?? throw new ArgumentNullException("tracker");
            this.id = (TrackerConnectionID)state;
            this.successful = successful;
        }
    }
}
