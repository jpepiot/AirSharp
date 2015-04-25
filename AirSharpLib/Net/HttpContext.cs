namespace AirSharp.Net {
    using System;
    using System.Net;

    public class HttpContext : IDisposable {

        private readonly ISession _session;
        private bool _disposed;

        internal HttpContext(ISession session) {
            _session = session;
        }

        public HttpRequest Request { get; set; }

        public HttpResponse Response { get; set; }

        public void Dispose() {
            if (!_disposed) {
                Flush();
                _disposed = true;
            }
        }

        private void Flush() {
            if (Response == null) {
                Response = new HttpResponse();
                Response.StatusCode = (int)HttpStatusCode.NoContent;
            }

           Response.WriteTo(_session.Writer);
        }
    }
}
