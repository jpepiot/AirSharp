namespace AirSharp.Net {
    using System.IO;

    public class StreamContent : HttpContent {

        private readonly Stream _stream;

        public StreamContent(Stream stream, int contentLength, string contentType) {
            _stream = stream;
            ContentType = contentType;
            ContentLength = contentLength;
        }

        public override Stream ReadAsStream() {
            return _stream;
        }

        protected override byte[] GetContentByteArray() {
            using (var memoryStream = new MemoryStream()) {
                _stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
