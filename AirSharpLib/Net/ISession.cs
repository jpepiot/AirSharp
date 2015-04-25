namespace AirSharp.Net {
    using System;
    using System.IO;
    using System.Net;

    public interface ISession {

        bool IsConnected { get; }

        EndPoint LocalEndPoint { get; }

        EndPoint RemoteEndPoint { get; }

        DateTime LastActivity { get; set; }

        StreamReader Reader { get; }

        StreamWriter Writer { get; }

        void Close();
    }
}
