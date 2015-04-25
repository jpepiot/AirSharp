namespace AirSharp {

    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using AirSharp.Net;
    using HttpListener = AirSharp.Net.HttpListener;

    public abstract class AbstractMediaHandler : HttpListener, IMediaHandler {

        private readonly string _serverIp;
        private readonly int _serverPort;

        protected AbstractMediaHandler(string serverIp, int serverPort)
            : base("AirSharp") {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public virtual void Initialize() {
            IPAddress address = IPAddress.Parse(_serverIp);
            EndPoint = new IPEndPoint(address, _serverPort);
        }

        public abstract void Handle(HttpContext context);

        public abstract Uri GetContentLocation();
    }
}
