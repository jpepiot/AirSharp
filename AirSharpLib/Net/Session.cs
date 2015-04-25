namespace AirSharp.Net {
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;

    public class Session : ISession {

        private readonly Socket _socket;
        private readonly EndPoint _localEndPoint;
        private readonly EndPoint _remoteEndPoint;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        public Session(Socket socket) {
            _socket = socket;
            Stream stream = new NetworkStream(socket);
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream);
            _localEndPoint = socket.LocalEndPoint;
            _remoteEndPoint = _socket.RemoteEndPoint;
            this.LastActivity = DateTime.UtcNow;
        }

        public bool IsConnected {
            get {
                if (_socket != null) {
                    if (!_socket.Connected)
                        return false;
                    try {
                        return !_socket.Poll(1, SelectMode.SelectRead) || _socket.Available != 0;
                    }
                    catch {
                        return false;
                    }
                }

                return false;
            }
        }

        public EndPoint LocalEndPoint {
            get {
                return _localEndPoint;
            }
        }

        public EndPoint RemoteEndPoint {
            get {
                return _remoteEndPoint;
            }
        }

        public DateTime LastActivity { get; set; }

        public StreamReader Reader {
            get {
                return _reader;
            }
        }

        public StreamWriter Writer {
            get {
                return _writer;
            }
        }

        public void Close() {
            if (_reader != null) {
                _reader.Dispose();
            }

            if (_writer != null) {
                try {
                    _writer.Dispose();
                }
                catch (Exception ignore) { }
            }

            if (_socket != null) {
                try {
                    _socket.Close();
                }
                catch (Exception ignore) { }
            }
        }
    }
}
