namespace AirSharp.Net {
    using System;

    public class ConnectionReceivedEventArgs : EventArgs {

        public ConnectionReceivedEventArgs(ISession session) {
            Session = session;
        }

        public ISession Session { get; set; }
    }
}
