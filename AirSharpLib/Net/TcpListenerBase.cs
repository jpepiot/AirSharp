namespace AirSharp.Net {

    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    public abstract class TcpListenerBase {

        private readonly List<ISession> _sessions = new List<ISession>();
        private bool _started;
        private ISocketListener _socketListener;

        private DateTime _startedTime;

        public bool Started {
            get {
                return _started;
            }
        }

        public DateTime StartedTime {
            get {
                return _startedTime;
            }
        }

        public EndPoint EndPoint { get; set; }

        public void Start() {
            DoStart();
            _startedTime = DateTime.UtcNow;
            _socketListener = CreateSocketListener();
            _socketListener.ConnectionReceived += OnConnectionReceived;
            _socketListener.Bind(EndPoint);
            _started = true;
        }

        public void Stop() {
            DoStop();
            if (_sessions != null) {
                foreach (ISession session in _sessions) {
                    session.Close();
                }

                _sessions.Clear();
            }

            if (_socketListener != null) {
                _socketListener.Close();
            }
        }

        protected virtual void DoStart() {
        }

        protected virtual void DoStop() {
        }

        protected abstract ISocketListener CreateSocketListener();

        private void OnConnectionReceived(object sender, ConnectionReceivedEventArgs args) {
            try {
                lock (_sessions) {
                    _sessions.Add(args.Session);
                }

                Task.Factory.StartNew(ProcessConnection, args.Session);
            }
            catch (ObjectDisposedException) { }
            catch (Exception) {
                try {
                    args.Session.Close();
                }
                catch (Exception) {
                }
            }
        }

        private void ProcessConnection(object sessionInfo) {
            ISession session = (ISession)sessionInfo;
            try {
                ProcessSession(session);
            }
            finally {
                if (session != null) {
                    session.Close();
                }

                lock (_sessions) {
                    _sessions.Remove(session);
                }
            }
        }

        protected abstract void ProcessSession(ISession session);
    }
}
