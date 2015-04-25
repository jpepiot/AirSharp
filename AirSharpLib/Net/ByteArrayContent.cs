namespace AirSharp.Net {
    using System.IO;

    public class ByteArrayContent : HttpContent {

        private readonly byte[] _content;

        public ByteArrayContent(byte[] content)
            : this(content, MediaType.APPLICATION_OCTET_STREAM) {
        }

        public ByteArrayContent(byte[] content, MediaType mediaType)
            : this(content, Utils.MediaTypeToString(mediaType)) {
        }

        public ByteArrayContent(byte[] content, string mediaType) {
            ContentType = mediaType;
            _content = content;
            ContentLength = _content.Length;
        }

        public override Stream ReadAsStream() {
           return new MemoryStream(GetContentByteArray());
        }

        protected override byte[] GetContentByteArray() {
            return _content;
        }
    }
}
