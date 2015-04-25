namespace AirSharp.Net {
    using System;
    using System.Text;

    public class StringContent : ByteArrayContent {

        private const string DefaultMediaType = "text/plain";

        public StringContent(string content, MediaType mediaType)
            : this(content, Utils.MediaTypeToString(mediaType), null) {
        }

        public StringContent(string content, MediaType mediaType, Encoding encoding)
            : this(content, Utils.MediaTypeToString(mediaType), encoding) {
        }

        public StringContent(string content, string mediaType, Encoding encoding)
            : base(GetContentByteArray(content, encoding)) {
            ContentType = !string.IsNullOrEmpty(mediaType) ? mediaType : DefaultMediaType;
        }

        private static byte[] GetContentByteArray(string content, Encoding encoding) {
            if (content == null) {
                throw new ArgumentNullException("content");
            }

            if (encoding == null) {
                encoding = DefaultStringEncoding;
            }

            return encoding.GetBytes(content);
        }
    }
}
