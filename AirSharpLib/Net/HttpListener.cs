namespace AirSharp.Net {
    public class HttpListener : HttpListenerBase {

        public HttpListener(string serverName)
            : base(serverName) {
        }

        protected override ISocketListener CreateSocketListener() {
            return new SocketListener();
        }
    }
}
