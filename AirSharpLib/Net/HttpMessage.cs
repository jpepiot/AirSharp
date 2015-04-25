namespace AirSharp.Net {
    public class HttpMessage {
        internal HttpMessage() {
            Headers = new HeaderCollection();
        }

        public HeaderCollection Headers { get; set; }

        public string Version { get; set; }

        public string Charset { get; set; }

        public HttpContent Content { get; set; }
    }
}
