namespace AirSharp.Net {
    using System.IO;
    using System.Text;

    public abstract class HttpContent {

        internal static readonly Encoding DefaultStringEncoding;

        static HttpContent() {
            DefaultStringEncoding = new UTF8Encoding(false);
        }

        public int ContentLength { get; protected set; }

        public string ContentType { get; protected set; }

        public byte[] ReadAsByteArray() {
            return GetContentByteArray();
        }

        public abstract Stream ReadAsStream();

        public string ReadAsString() {
            byte[] bytes = GetContentByteArray();
            return bytes != null ? Encoding.UTF8.GetString(bytes, 0, bytes.Length) : null;
        }

        protected abstract byte[] GetContentByteArray();
    }
}
