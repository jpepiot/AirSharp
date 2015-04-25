namespace AirSharp {

    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using AirSharp.Net;
    using AirSharp.Net.Exceptions;

    public class Player {

        private const int AppleTvPort = 7000;
        private const int DefaultServerPort = 25895;

        private readonly string _appleTvAddress, _serverAddress;
        private readonly int _serverPort;
        private readonly string _sessionId;
        private readonly object _objLock = new object();

        private bool _initialized;
        private IMediaHandler _mediaHandler;
        private NetworkStream _eventsStream;
        private StreamWriter _commandWriter;
        private StreamReader _commandReader;
        private Socket _eventsSocket, _commandSocket;
        private Thread _eventsThread, _commandThread;
        private StateType _currentState;

        public Player(string appleTvAddress, string serverAddress)
            : this(appleTvAddress, serverAddress, DefaultServerPort) {
        }

        public Player(string appleTvAddress, string serverAddress, int serverPort) {
            _appleTvAddress = appleTvAddress;
            _serverAddress = serverAddress;
            _sessionId = Guid.NewGuid().ToString();
            _serverPort = serverPort;
        }

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public event EventHandler<ProgressEventArgs> Progress;

        public event EventHandler<ErrorEventArgs> Error;

        public void Start() {

            if (_initialized) {
                return;
            }

            _initialized = true;

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(_appleTvAddress), AppleTvPort);

            _commandSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _commandSocket.Connect(endPoint);

            NetworkStream commandStream = new NetworkStream(_commandSocket);
            _commandWriter = new StreamWriter(commandStream);
            _commandReader = new StreamReader(commandStream);
            _commandThread = new Thread(DoGetProgress);
            _commandThread.Start();

            _eventsSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _eventsSocket.Connect(endPoint);

            _eventsStream = new NetworkStream(_eventsSocket);
            _eventsThread = new Thread(DoListenForEvents);
            _eventsThread.Start();
        }

        public Task Play(string fileName, int position = 0, PlaybackSettings settings = null) {

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            Task.Factory.StartNew(() => {

                if (!File.Exists(fileName)) {
                    tcs.TrySetException(new FileNotFoundException(fileName));
                    tcs.TrySetResult(false);
                    return;
                }

                OnStateChanged(StateType.Initializing);

                try {
                    Start();

                    if (_mediaHandler != null) {
                        _mediaHandler.Stop();
                        _mediaHandler = null;
                    }

                    //TODO : implement Http Live streaming
                    _mediaHandler = new DefaultMediaHandler(_serverAddress, _serverPort, fileName);
                    _mediaHandler.Initialize();
                    _mediaHandler.Start();

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("Content-Location:{0}", _mediaHandler.GetContentLocation())
                        .AppendLine()
                        .AppendFormat("Start-Position:{0}", position)
                        .AppendLine()
                        .AppendLine();

                    HttpResponse response = SendRequest("POST", "/play", sb.ToString());
                    if (response.StatusCode == (int)HttpStatusCode.OK) {
                        tcs.TrySetResult(true);
                    } else {
                        tcs.TrySetException(new HttpException(response.StatusCode, response.StatusDescription));
                        tcs.TrySetResult(false);
                    }

                    tcs.TrySetResult(true);
                } catch (Exception e) {
                    tcs.TrySetException(e);
                    tcs.TrySetResult(false);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Seek to the specified position
        /// </summary>
        /// <param name="position">Position in seconds</param>
        public Task Seek(int position) {
            return SendRequestTask("POST", "/scrub?position=" + position);
        }

        /// <summary>
        /// Resume the video
        /// </summary>
        public Task Resume() {
            return SendRequestTask("POST", "/rate?value=1.000000");
        }

        /// <summary>
        /// Set the video rate
        /// </summary>
        public Task SetRate(int rate) {
            return SendRequestTask("POST", "/rate?value=" + rate);
        }

        /// <summary>
        /// Pause the video
        /// </summary>
        public Task Pause() {
            return SendRequestTask("POST", "/rate?value=0.000000");
        }

        /// <summary>
        /// Get current playback information
        /// </summary>
        public PlaybackInfo GetPlaybackInfo() {
            var response = SendRequest("GET", "/playback-info");
            if (response.StatusCode == (int)HttpStatusCode.OK) {
                string content = response.Content.ReadAsString();
                return PlaybackInfo.CreateFromDictionary(new PropertyList(content));
            }

            return null;
        }

        /// <summary>
        /// Stop the video
        /// </summary>
        public void Stop() {

            if (_mediaHandler != null) {
                _mediaHandler.Stop();
            }

            if (_eventsSocket != null) {
                _eventsSocket.Close();
            }

            if (_eventsStream != null) {
                _eventsStream.Dispose();
            }

            if (_commandSocket != null) {
                _commandSocket.Close();
            }

            if (_commandWriter != null) {
                _commandWriter.Dispose();
            }

            if (_commandReader != null) {
                _commandReader.Dispose();
            }

            _initialized = false;

            OnStateChanged(StateType.Stopped);
        }

        private void DoGetProgress() {
            while (_initialized) {
                try {
                    HttpResponse response = SendRequest("GET", "/scrub");
                    if (response.StatusCode == (int)HttpStatusCode.OK) {
                        string content = response.Content.ReadAsString();
                        double position = double.Parse(Regex.Match(content, "^position: (.*)\n", RegexOptions.Multiline).Groups[1].Value, CultureInfo.InvariantCulture);
                        double duration = double.Parse(Regex.Match(content, "^duration: (.*)\n", RegexOptions.Multiline).Groups[1].Value, CultureInfo.InvariantCulture);
                        OnProgress(new ProgressEventArgs {
                            Position = TimeSpan.FromSeconds(position),
                            Duration = TimeSpan.FromSeconds(duration)
                        });
                    }
                } catch (Exception e) {
                    OnError(new ErrorEventArgs {
                        Code = -1,
                        Description = e.Message,
                        FailureReason = e.Message
                    });
                }

                if (_initialized) {
                    Thread.Sleep(1000);
                }
            }
        }

        private void DoListenForEvents() {

            StreamWriter writer = new StreamWriter(_eventsStream);
            StreamReader reader = new StreamReader(_eventsStream);

            try {
                HttpRequest req = new HttpRequest("POST", new Uri("/reverse", UriKind.Relative));
                req.Headers.AddHeader(HttpHeaderConstants.Upgrade, "PTTH/1.0");
                req.Headers.AddHeader(HttpHeaderConstants.Connection, "Upgrade");
                req.WriteTo(writer);
                HttpResponse response = HttpResponse.CreateHttpResponse(reader);
                if (response.StatusCode != (int)HttpStatusCode.SwitchingProtocols) {
                    throw new HttpException(response.StatusCode, response.StatusDescription);
                }

                while (_initialized) {
                    while (_eventsStream.DataAvailable) {
                        HttpRequest request = HttpRequest.CreateHttpRequest(reader);
                        if (request.Method == "POST" && request.RequestUri.ToString() == "/event") {
                            string content = request.Content.ReadAsString();
                            PropertyList plist = new PropertyList(content);
                            if (plist.ContainsKey("state")) {
                                string currentState = plist["state"].ToString();
                                OnStateChanged(currentState);
                            }

                            if (plist.ContainsKey("error")) {
                                PropertyList errorPropertyList = plist["error"] as PropertyList;
                                if (errorPropertyList != null) {
                                    OnError(new ErrorEventArgs {
                                        Code = errorPropertyList.ContainsKey("code") ? (int)errorPropertyList["code"] : 0,
                                        Description = errorPropertyList.ContainsKey("NSLocalizedDescription") ? errorPropertyList["NSLocalizedDescription"].ToString() : null,
                                        FailureReason = errorPropertyList.ContainsKey("NSLocalizedFailureReason") ? errorPropertyList["NSLocalizedFailureReason"].ToString() : null,
                                        Suggestion = errorPropertyList.ContainsKey("NSLocalizedRecoverySuggestion") ? errorPropertyList["NSLocalizedRecoverySuggestion"].ToString() : null
                                    });
                                }
                            }

                            new HttpResponse { StatusCode = (int)HttpStatusCode.OK }.WriteTo(writer);
                        } else {
                            new HttpResponse { StatusCode = (int)HttpStatusCode.NotImplemented }.WriteTo(writer);
                        }
                    }

                    Thread.Sleep(100);
                }
            } catch (Exception e) {
                OnError(new ErrorEventArgs {
                    Code = -1,
                    Description = e.Message,
                    FailureReason = e.Message
                });
            } finally {
                reader.Dispose();
                writer.Dispose();
            }
        }

        private Task SendRequestTask(string method, string command, string content = null) {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(() => {
                HttpResponse response = SendRequest(method, command, content);
                if (response.StatusCode != (int)HttpStatusCode.OK) {
                    tcs.TrySetException(new HttpException(response.StatusCode, response.StatusDescription));
                }

                tcs.TrySetResult(true);
            });

            return tcs.Task;
        }

        private HttpResponse SendRequest(string method, string command, string content = null) {
            lock (_objLock) {
                HttpRequest request = new HttpRequest(method, new Uri(command, UriKind.Relative));
                request.Headers.AddHeader("X-Apple-Session-ID", _sessionId);
                request.Headers.AddHeader(HttpHeaderConstants.ContentType, "text/parameters");
                request.Headers.AddHeader(HttpHeaderConstants.Connection, "keep-alive");
                if (content != null) {
                    request.Content = new StringContent(content, "text/parameters", Encoding.UTF8);
                }

                request.WriteTo(_commandWriter);
                return HttpResponse.CreateHttpResponse(_commandReader);
            }
        }

        private void OnStateChanged(string newState) {
            StateType newStateType;
            if (Enum.TryParse(newState, true, out  newStateType)) {
                OnStateChanged(newStateType);
            }
        }

        private void OnStateChanged(StateType newStateType) {
            if (_currentState != newStateType) {
                if (StateChanged != null) {
                    StateChanged(this, new StateChangedEventArgs { NewState = newStateType });
                }

                _currentState = newStateType;
            }
        }

        private void OnProgress(ProgressEventArgs args) {
            if (Progress != null) {
                Progress(this, args);
            }
        }

        private void OnError(ErrorEventArgs args) {
            if (Error != null && _currentState != StateType.Stopped) {
                Error(this, args);
            }
        }
    }
}
