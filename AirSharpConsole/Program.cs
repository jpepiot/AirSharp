namespace AirSharp {
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Network.Bonjour;

    /// <summary>
    /// Simple console client to control an apple TV
    /// </summary>
    class Program {

        private static readonly int[] _rates = new[] { -64, -16, -2, 0, 1, 2, 16, 64 };
        private static StateType _currentState = StateType.Unknown;
        private static TimeSpan _position;
        private static int _progress;
        private static string _failureReason, _appleTvIp, _localIp;
        private static int? _rate;

        static void Main(string[] args) {
            try {
                var commandLineArgs = CommandLineArguments.Parse(args);
                if (commandLineArgs.OptionExists("h")) {
                    PrintUsage();
                    return;
                }

                Console.Clear();
                Console.WriteLine("Searching AppleTV ...");

                BonjourServiceResolver resolver = new BonjourServiceResolver();
                ManualResetEvent signal = new ManualResetEvent(false);

                resolver.ServiceFound += service => {
                    var address = service.Addresses[0].Addresses.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    if (address != null) {
                        Console.WriteLine("Apple TV Found : {0}", address);
                        _appleTvIp = address.ToString();
                        signal.Set();
                    }
                };

                resolver.Resolve("_airplay._tcp.local.");
                bool signaled = signal.WaitOne(TimeSpan.FromSeconds(30));
                if (!signaled) {
                    PrintError("Apple TV not found");
                    return;
                }

                var localAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                if (localAddress == null) {
                    PrintError("Local Ipv4 not found");
                    return;
                }

                _localIp = localAddress.ToString();

                Player player = new Player(_appleTvIp, _localIp);
                player.StateChanged += (sender, eventArgs) => {
                    _currentState = eventArgs.NewState;
                    SetRate(player);
                    PrintInfo();
                };

                player.Error += (sender, eventArgs) => {
                    _failureReason = eventArgs.FailureReason;
                    PrintInfo();
                };

                player.Progress += (sender, eventArgs) => {
                    _position = eventArgs.Position;
                    if (eventArgs.Position.TotalSeconds > 0) {
                        _progress = (int)(((eventArgs.Position.TotalSeconds / eventArgs.Duration.TotalSeconds) * 100));
                    }

                    PrintInfo();
                };

                player.Start();

                if (commandLineArgs.OptionExists("i")) {
                    player.Play(commandLineArgs.GetOptionValue("i")).ContinueWith(t => {
                        if (t.Exception != null) {
                            _failureReason = string.Format("{0} : {1}", t.Exception.InnerExceptions[0].GetType().Name, t.Exception.InnerExceptions[0].Message);
                            PrintInfo();
                        }
                    });
                }

                ConsoleKeyInfo cki;

                do {
                    while (!Console.KeyAvailable) {
                        Thread.Sleep(10);
                    }

                    cki = Console.ReadKey(true);

                    switch (cki.Key) {
                        case ConsoleKey.Spacebar:
                            if (_currentState == StateType.Paused) {
                                player.Resume();
                            } else {
                                player.Pause();
                                if (_currentState == StateType.Unknown) {
                                    _currentState = StateType.Paused;
                                }
                            }

                            break;
                        case ConsoleKey.LeftArrow:
                            int lr;
                            if (TryGetPreviousRate(_rate.GetValueOrDefault(0), out lr)) {
                                player.SetRate(lr).ContinueWith(t => {
                                    if (t.IsCompleted) {
                                        SetRate(player);
                                    }
                                });
                            }

                            break;
                        case ConsoleKey.RightArrow:
                            int rr;
                            if (TryGetNextRate(_rate.GetValueOrDefault(0), out rr)) {
                                player.SetRate(rr).ContinueWith(t => {
                                    if (t.IsCompleted) {
                                        SetRate(player);
                                    }
                                });
                            }

                            break;
                    }
                } while (cki.Key != ConsoleKey.X);

                player.Stop();
            } catch (Exception e) {
                Console.Error.WriteLine(e.Message);
            }
        }

        private static void PrintInfo() {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine("Press space to pause.");
            Console.WriteLine("Press Left or Right arrow key to move backward or forward");
            Console.WriteLine("Press X to exit...\r\n");
            Console.WriteLine("Apple TV address: {0}", _appleTvIp);
            Console.WriteLine("Local address: {0}", _localIp);
            Console.WriteLine("Rate: {0}", _rate == null ? "Unknown" : _rate.Value.ToString());
            Console.WriteLine("State: {0}\r\n", _currentState);
            if (string.IsNullOrEmpty(_failureReason)) {
                int c = _progress > 0 ? (_progress * 20) / 100 : 0;
                Console.WriteLine("Time: {0} |{1}{2}| {3}% complete", _position, new string('*', c), new string('=', 20 - c), _progress);
            } else {
                Console.WriteLine(_failureReason);
            }
        }

        private static void PrintError(string message) {
            Console.WriteLine(message);
            Console.WriteLine("Press any key to exit ...");
            Console.ReadLine();
        }

        private static void PrintUsage() {
            Console.WriteLine("AirSharpConsole.exe /i={file}");
        }

        private static void SetRate(Player player) {
            var playbackInfo = player.GetPlaybackInfo();
            if (playbackInfo != null) {
                Debug.WriteLine("Rate : " + playbackInfo.Rate);
                _rate = playbackInfo.Rate;
            }
        }

        private static bool TryGetNextRate(int currentRate, out int newRate) {
            newRate = currentRate;
            for (int i = 0; i < _rates.Length - 1; i++) {
                if (_rates[i] == currentRate) {
                    newRate = _rates[i + 1];
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetPreviousRate(int currentRate, out int newRate) {
            newRate = currentRate;
            for (int i = 1; i < _rates.Length; i++) {
                if (_rates[i] == currentRate) {
                    newRate = _rates[i - 1];
                    return true;
                }
            }

            return false;
        }
    }
}
