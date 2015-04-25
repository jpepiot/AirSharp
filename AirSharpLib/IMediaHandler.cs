namespace AirSharp {
    using System;

    using AirSharp.Net;

    public interface IMediaHandler : IHttpRequestHandler {
        void Initialize();

        Uri GetContentLocation();

        void Start();

        void Stop();
    }
}
