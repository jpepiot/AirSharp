namespace AirSharp.Net {
    public interface IHttpRequestHandler {
        void Handle(HttpContext context);
    }
}
