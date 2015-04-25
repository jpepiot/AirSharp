namespace AirSharp.Net {
    using System;
    using System.Net;

    public interface ISocketListener {
        void Bind(EndPoint endPoint);

        void Close();

        event EventHandler<ConnectionReceivedEventArgs> ConnectionReceived;
    }
}
