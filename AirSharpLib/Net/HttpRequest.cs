namespace AirSharp.Net {

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using AirSharp.Net.Exceptions;

    public class HttpRequest : HttpMessage {

        private const int MaxHeaderBytes = 32 * 1024;

        public HttpRequest() {
        }

        public HttpRequest(string method, Uri requestUri) {
            Method = method;
            RequestUri = requestUri;
        }

        public Uri RequestUri { get; set; }

        public virtual string Method { get; set; }

        public string UserAgent {
            get {
                return Headers.GetHeader(HttpHeaderConstants.UserAgent);
            }

            set {
                Headers[HttpHeaderConstants.UserAgent] = value;
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Method + " " + RequestUri + " HTTP/" + Version);
            if (Headers != null) {
                foreach (var key in Headers.Keys) {
                    sb.AppendLine(key + ": " + Headers[key]);
                }
            }

            return sb.ToString();
        }

        public void WriteTo(StreamWriter writer) {
            writer.WriteLine("{0} {1} HTTP/1.1", Method, RequestUri);
            foreach (var key in Headers.Keys) {
                writer.WriteLine("{0}: {1}", key, Headers[key]);
            }

            if (Content != null) {
                if (!string.IsNullOrEmpty(Charset)) {
                    writer.WriteLine("{0}: {1}; charset={2}", HttpHeaderConstants.ContentType, Content.ContentType, Charset);
                }
                else {
                    writer.WriteLine("{0}: {1}", HttpHeaderConstants.ContentType, Content.ContentType);
                }

                writer.WriteLine("{0}: " + Content.ContentLength, HttpHeaderConstants.ContentLength);
            }
            else {
                writer.WriteLine("{0}: 0", HttpHeaderConstants.ContentLength);
            }

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

        public static HttpRequest CreateHttpRequest(StreamReader reader) {

            if (reader == null) {
                throw new ArgumentNullException("reader");
            }

            HttpRequest httpRequest = new HttpRequest();
            byte[] headerBytes = null;
            int endHeadersOffset = 0;
            List<ByteString> headerByteStrings = null;

            do {
                byte[] tmpHeaderBytes = new byte[MaxHeaderBytes];
                int numReceived = reader.BaseStream.Read(tmpHeaderBytes, 0, MaxHeaderBytes);
                if (numReceived < MaxHeaderBytes) {
                    byte[] tempBuf = new byte[numReceived];
                    if (numReceived > 0) {
                        Buffer.BlockCopy(tmpHeaderBytes, 0, tempBuf, 0, numReceived);
                    }

                    tmpHeaderBytes = tempBuf;

                    if (tmpHeaderBytes.Length == 0) {
                        throw new HttpException((int)HttpStatusCode.BadRequest, HttpHelper.GetStatusDescription((int)HttpStatusCode.BadRequest));
                    }

                    if (headerBytes != null) {
                        int len = tmpHeaderBytes.Length + headerBytes.Length;
                        if (len > MaxHeaderBytes) {
                            throw new HttpException((int)HttpStatusCode.BadRequest, HttpHelper.GetStatusDescription((int)HttpStatusCode.BadRequest));
                        }

                        byte[] bytes = new byte[len];
                        Buffer.BlockCopy(headerBytes, 0, bytes, 0, headerBytes.Length);
                        Buffer.BlockCopy(tmpHeaderBytes, 0, bytes, headerBytes.Length, tmpHeaderBytes.Length);
                        headerBytes = bytes;
                    }
                    else {
                        headerBytes = tmpHeaderBytes;
                    }

                    headerByteStrings = new List<ByteString>();
                    int startHeadersOffset = -1;
                    endHeadersOffset = -1;

                    ByteParser parser = new ByteParser(headerBytes);
                    while (true) {
                        ByteString line = parser.ReadLine();

                        if (line == null) {
                            break;
                        }

                        if (startHeadersOffset < 0) {
                            startHeadersOffset = parser.CurrentOffset;
                        }

                        if (line.IsEmpty) {
                            endHeadersOffset = parser.CurrentOffset;
                            break;
                        }

                        headerByteStrings.Add(line);
                    }
                }
            }
            while (endHeadersOffset < 0); // found \r\n\r\n

            if (headerByteStrings == null) {
                throw new HttpException((int)HttpStatusCode.BadRequest, HttpHelper.GetStatusDescription((int)HttpStatusCode.BadRequest));
            }

            // Parse Request Line        
            ByteString requestLine = headerByteStrings[0];
            ByteString[] elems = requestLine.Split(' ');
            if (elems == null || elems.Length < 2 || elems.Length > 3) {
                throw new HttpException((int)HttpStatusCode.BadRequest, HttpHelper.GetStatusDescription((int)HttpStatusCode.BadRequest));
            }

            httpRequest.Method = elems[0].GetString();
            ByteString urlBytes = elems[1];
            httpRequest.RequestUri = new Uri(urlBytes.GetString(), UriKind.RelativeOrAbsolute);

            string protocol = elems.Length == 3 ? elems[2].GetString() : "HTTP/1.0";
            if (!elems[2].GetString().StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase)) { // HTTP/1.1
                throw new HttpException((int)HttpStatusCode.BadRequest, HttpHelper.GetStatusDescription((int)HttpStatusCode.BadRequest));
            }

            httpRequest.Version = protocol.Substring(5);

            // Parse HTTP Headers
            for (int i = 1; i < headerByteStrings.Count; i++) {
                string headerLine = headerByteStrings[i].GetString();
                int c;
                if ((c = headerLine.IndexOf(':')) >= 0) {
                    string name = headerLine.Substring(0, c).Trim();
                    string value = headerLine.Substring(c + 1).Trim();
                    httpRequest.Headers[name] = value.Trim();
                }
            }

            if (httpRequest.Headers.ContainsHeader(HttpHeaderConstants.ContentLength)) {
                int contentLength;
                if (int.TryParse(httpRequest.Headers[HttpHeaderConstants.ContentLength], out contentLength)) {
                    if (headerBytes.Length > endHeadersOffset) {
                        int preloadedContentLength = headerBytes.Length - endHeadersOffset;
                        if (preloadedContentLength > contentLength) {
                            preloadedContentLength = contentLength;
                        }

                        if (preloadedContentLength > 0) {
                            byte[] preloadedContent = new byte[preloadedContentLength];
                            Buffer.BlockCopy(headerBytes, endHeadersOffset, preloadedContent, 0, preloadedContentLength);
                            if (httpRequest.Headers.ContainsHeader(HttpHeaderConstants.ContentType)) {
                                Encoding encoding = Encoding.UTF8;
                                string contentTypeHeader = httpRequest.Headers.GetHeader(HttpHeaderConstants.ContentType);
                                string mediaType = contentTypeHeader;
                                int pos = contentTypeHeader.IndexOf(";", StringComparison.OrdinalIgnoreCase);
                                if (pos > -1) {
                                    mediaType = contentTypeHeader.Substring(0, pos);
                                    int charSetPos = contentTypeHeader.IndexOf("charset=", pos, StringComparison.OrdinalIgnoreCase);
                                    if (charSetPos > -1) {
                                        string charset = contentTypeHeader.Substring(charSetPos + 8);
                                        if (charset.StartsWith("\"")) {
                                            charset = charset.Substring(1);
                                        }

                                        if (charset.EndsWith("\"")) {
                                            charset = charset.Substring(0, charset.Length - 1);
                                        }

                                        try {
                                            encoding = Encoding.GetEncoding(charset.ToLower());
                                        }
                                        catch (ArgumentException) {
                                            encoding = Encoding.UTF8;
                                        }
                                    }
                                }

                                switch (mediaType.ToLower()) {
                                    case "text/xml":
                                    case "text/html":
                                        httpRequest.Content = new StringContent(encoding.GetString(preloadedContent, 0, preloadedContent.Length), mediaType, encoding);
                                        break;
                                    default:
                                        httpRequest.Content = new ByteArrayContent(preloadedContent, mediaType);
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return httpRequest;
        }
    }
}
