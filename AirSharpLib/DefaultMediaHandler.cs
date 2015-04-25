namespace AirSharp {

    using System;
    using System.IO;

    using System.Net;
    using System.Text.RegularExpressions;
    using System.Web;

    using AirSharp.Net;
    using HttpContext = AirSharp.Net.HttpContext;

    public class DefaultMediaHandler : AbstractMediaHandler {

        private const int MaxContentLength = 10000000;
        private readonly string _fileName;

        public DefaultMediaHandler(string serverIp, int serverPort, string fileName)
            : base(serverIp, serverPort) {
            _fileName = fileName;
        }

        public override Uri GetContentLocation() {
            return new Uri(string.Format("http://{0}/?f={1}&ct={2}", EndPoint, HttpUtility.UrlEncode(_fileName), HttpUtility.UrlEncode(GetContentTypeByExtension(Path.GetExtension(_fileName)))));
        }

        public override void Handle(HttpContext context) {
            if (context.Request.Headers.ContainsHeader("range")) {
                var match = Regex.Match(context.Request.RequestUri.ToString(), "/\\?f=(?<fileName>.*)&ct=(?<contentType>.*)");
                if (match.Success) {

                    string fileName = HttpUtility.UrlDecode(match.Groups["fileName"].Value);
                    string contentType = HttpUtility.UrlDecode(match.Groups["contentType"].Value);

                    if (string.IsNullOrEmpty(fileName)) {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        context.Response.StatusDescription = "Bad Request";
                        return;
                    }

                    string range = context.Request.Headers["range"];
                    int index;
                    long startRange = 0;
                    long endRange = 1;

                    if ((index = range.IndexOf("bytes=", StringComparison.InvariantCulture)) > -1) {
                        var parts = range.Substring(index + 6).Split('-');
                        if (parts.Length == 2) {
                            startRange = long.Parse(parts[0]);
                            endRange = long.Parse(parts[1]);
                        }
                    }

                    if (File.Exists(fileName)) {
                        FileInfo fileInfo = new FileInfo(fileName);
                        context.Response.Headers.AddHeader("Content-Type", contentType);
                        context.Response.Headers.AddHeader("Content-Range", "bytes " + startRange + '-' + endRange + '/' + fileInfo.Length);
                        context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                        context.Response.StatusDescription = "Partial Content";
                        FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        fileStream.Seek(startRange, 0);
                        int contentLength = (int)(endRange - startRange + 1);

                        // if the range requested is too large, send only 10.000.000 bytes
                        contentLength = contentLength > MaxContentLength ? MaxContentLength : contentLength;
                        context.Response.Content = new StreamContent(fileStream, contentLength, contentType);
                    }
                    else {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.StatusDescription = "Not Found";
                    }
                }
                else {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.StatusDescription = "Not Found";
                }
            }
            else {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.StatusDescription = "Bad Request";
            }
        }

        protected override void DoStart() {
            AddRequestHandler("/*", this);
        }

        private static string GetContentTypeByExtension(string ext) {
            switch (ext) {
                case ".mpg":
                    return "video/mpeg";
                case ".mov":
                    return "video/quicktime";
                case ".wmv":
                    return "video/x-ms-wmv";
                case ".m4v":
                    return "video/x-m4v";
                case ".avi":
                    return "video/x-msvideo";
                case ".dvr-ms":
                    return "video/x-msvideo";
                case ".mp4":
                    return "video/mp4";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
