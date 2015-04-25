namespace AirSharp.Net {
    using System;
    using System.Net;
    using System.Net.Sockets;

    public class SocketListener : ISocketListener {

        private Socket _serverSocket;

        public void Bind(EndPoint endPoint) {
            _serverSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(endPoint);
            _serverSocket.Listen((int)SocketOptionName.MaxConnections);
            _serverSocket.BeginAccept(OnAcceptClient, _serverSocket);
        }

        public event EventHandler<ConnectionReceivedEventArgs> ConnectionReceived;

        public event EventHandler<ErrorEventArg> Error;

        public void Close() {
            if (_serverSocket != null) {
                try {
                    _serverSocket.Shutdown(SocketShutdown.Both);
                } catch (Exception) {
                }

                try {
                    _serverSocket.Close();
                    _serverSocket = null;
                } catch (Exception) {
                }
            }
        }

        protected virtual ISession CreateSession(Socket clientSocket) {
            return new Session(clientSocket);
        }

        private void OnAcceptClient(IAsyncResult result) {
            Socket clientSocket = null;
            try {

                clientSocket = _serverSocket.EndAccept(result);
                if (clientSocket == null) {
                    return;
                }

                OnConnectionReceived(CreateSession(clientSocket));
            } catch (ObjectDisposedException) {

            } catch (Exception e) {
                try {
                    if (clientSocket != null) {
                        clientSocket.Close();
                    }

                    OnError(e);
                } catch (Exception ignore) {
                }
            }

            try {
                _serverSocket.BeginAccept(OnAcceptClient, null);
            } catch (Exception ignore) {
            }
        }

        private void OnConnectionReceived(ISession session) {
            if (ConnectionReceived != null) {
                ConnectionReceived(this, new ConnectionReceivedEventArgs(session));
            }
        }

        private void OnError(Exception e) {
            if (Error != null) {
                Error(this, new ErrorEventArg(e));
            }
        }
    }
}
