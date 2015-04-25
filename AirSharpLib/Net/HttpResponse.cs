namespace AirSharp.Net {
    using System;
    using System.IO;
    using System.Text;

    public class HttpResponse : HttpMessage {

        private int _statusCode;

        public int StatusCode {
            get {
                return _statusCode;
            }

            set {
                _statusCode = value;
                if (string.IsNullOrEmpty(StatusDescription)) {
                    StatusDescription = HttpHelper.GetStatusDescription(value);
                }
            }
        }

        public string StatusDescription { get; set; }

        public string Server {
            get {
                return Headers.GetHeader(HttpHeaderConstants.Server);
            }

            set {
                Headers[HttpHeaderConstants.Server] = value;
            }
        }

        public void WriteTo(StreamWriter writer) {
            writer.WriteLine("HTTP/1.1 {0} {1}", StatusCode, StatusDescription);
            foreach (var key in Headers.Keys) {
                writer.WriteLine("{0}: {1}", key, Headers[key]);
            }

            if (Content != null) {
                if (!string.IsNullOrEmpty(Charset)) {
                    writer.WriteLine("{0}: {1}; charset={2}", HttpHeaderConstants.ContentType, Content.ContentType, Charset);
                } else {
                    writer.WriteLine("{0}: {1}", HttpHeaderConstants.ContentType, Content.ContentType);
                }

                writer.WriteLine("{0}: {1}", HttpHeaderConstants.ContentLength, Content.ContentLength);
            } else {
                writer.WriteLine("{0}: 0", HttpHeaderConstants.ContentLength);
            }

            writer.WriteLine("{0}: Close", HttpHeaderConstants.Connection);
            writer.WriteLine();
            writer.Flush();

            if (Content != null) {
                int byteToSent = Content.ContentLength;
                using (Stream stream = Content.ReadAsStream()) {
                    byte[] bytes = new byte[1000000];
                    int read;
                    while ((read = stream.Read(bytes, 0, bytes.Length < byteToSent ? bytes.Length : byteToSent)) > 0) {
                        byteToSent -= read;
                        writer.BaseStream.Write(bytes, 0, read);
                        writer.Flush();
                        if (byteToSent <= 0) {
                            break;
                        }
                    }
                }
            }
        }

        public virtual void ReadFrom(StreamReader reader) {
            HttpResponse response = CreateHttpResponse(reader);
            StatusCode = response.StatusCode;
            StatusDescription = response.StatusDescription;
            Headers = response.Headers;
            Content = response.Content;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(StatusCode + " " + StatusDescription);
            if (Headers != null) {
                foreach (var key in Headers.Keys) {
                    sb.AppendLine(key + ": " + Headers[key]);
                }
            }

            return sb.ToString();
        }

        public static HttpResponse CreateHttpResponse(StreamReader reader) {
            if (reader == null) {
                throw new ArgumentNullException("reader");
            }

            HttpResponse response = new HttpResponse();
            string firstLine = reader.ReadLine();

            if (string.IsNullOrEmpty(firstLine)) {
                throw new InvalidDataException("Not a valid HTTP response");
            }

            string[] tokens = firstLine.Split(new[] { ' ' });
            if (tokens.Length < 3) {
                throw new InvalidDataException("Not a valid HTTP response");
            }

            if (!tokens[0].StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidDataException("Not a valid HTTP response");
            }

            response.Version = tokens[0].Substring(5);
            response.StatusCode = int.Parse(tokens[1]);
            response.StatusDescription = string.Join(" ", tokens, 2, tokens.Length - 2);

            // Parse HTTP Headers
            string line;
            while ((line = reader.ReadLine()) != null && line != string.Empty) {
                string[] parts = line.Split(new[] { ':' });
                string name = parts[0].Trim();
                string value = string.Empty;
                for (int i = 1; i < parts.Length; i++) {
                    value += parts[i];
                    if (i < parts.Length - 1) {
                        value += ":";
                    }
                }

                response.Headers[name] = value.Trim();
            }

            string contentLengthHeader = response.Headers.GetHeader(HttpHeaderConstants.ContentLength);
            if (!string.IsNullOrEmpty(contentLengthHeader)) {
                int contentLength;
                if (int.TryParse(contentLengthHeader, out contentLength)) {
                    if (contentLength > 0) {
                        char[] buffer = new char[contentLength];
                        int readBytes = 0;
                        while (readBytes < buffer.Length) {
                            int i = reader.Read(buffer, readBytes, buffer.Length - readBytes);
                            readBytes += i;
                        }

                        response.Content = new StringContent(new string(buffer), null, null);
                    }
                }
            }

            return response;
        }
    }
}
