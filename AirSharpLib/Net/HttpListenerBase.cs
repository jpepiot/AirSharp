namespace AirSharp.Net {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Text.RegularExpressions;
    using AirSharp.Net.Exceptions;

    public abstract class HttpListenerBase : TcpListenerBase {

        private readonly Dictionary<string, IHttpRequestHandler> _processContextHandlers = new Dictionary<string, IHttpRequestHandler>();
        private readonly string _serverName;

        protected HttpListenerBase(string serverName) {
            _serverName = serverName;
        }

        public void AddRequestHandler(string uri, IHttpRequestHandler handler) {
            _processContextHandlers[uri] = handler;
        }

        protected override void ProcessSession(ISession session) {
            try {
                HttpProcessor processor = new HttpProcessor(session);
                using (HttpContext context = processor.CreateHttpContext()) {
                    Debug.WriteLine(context.Request.RequestUri);
                    context.Response.Server = _serverName;
                    IHttpRequestHandler handler = GetHandler(context.Request.RequestUri);
                    if (handler != null) {
                        try {
                            handler.Handle(context);
                        }
                        catch (Exception) {
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            context.Response.Content = new StringContent("<html><body><h1>Internal Server Error</h1></body></html>", MediaType.TEXT_HTML);
                        }
                    }
                    else {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Content = new StringContent("<html><body><h1>Not Found</h1></body></html>", MediaType.TEXT_HTML);
                    }
                }
            }
            catch (HttpException ex) {
                using (HttpContext context = new HttpContext(session)) {
                    context.Response = new HttpResponse();
                    context.Response.Server = _serverName;
                    context.Response.StatusCode = ex.StatusCode;
                    context.Response.StatusDescription = ex.Message;
                    context.Response.Content = new StringContent(string.Format("<html><body><h1>{0}</h1></body></html>", ex.Message), MediaType.TEXT_HTML);
                }
            }
            catch (Exception ex) {
                using (HttpContext context = new HttpContext(session)) {
                    context.Response = new HttpResponse();
                    context.Response.Server = _serverName;
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Content = new StringContent("<html><body><h1>Internal Server Error</h1></body></html>", MediaType.TEXT_HTML);
                }
            }
        }

        protected abstract override ISocketListener CreateSocketListener();

        private IHttpRequestHandler GetHandler(Uri uri) {
            if (uri == null) {
                return null;
            }

            foreach (var key in _processContextHandlers.Keys) {
                string localPath = uri.IsAbsoluteUri ? uri.LocalPath : uri.ToString();
                if (Regex.Match(localPath, key).Success) {
                    return _processContextHandlers[key];
                }
            }

            return null;
        }
    }
}
