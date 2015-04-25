namespace AirSharp.Net {
    internal class HttpProcessor {

        private readonly ISession _session;

        public HttpProcessor(ISession session) {
            _session = session;
        }

        public HttpContext CreateHttpContext() {
            HttpContext context = new HttpContext(_session);
            context.Request = HttpRequest.CreateHttpRequest(_session.Reader);
            context.Response = new HttpResponse();
            return context;
        }
    }
}
