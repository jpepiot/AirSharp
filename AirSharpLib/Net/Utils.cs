namespace AirSharp.Net {
    using System;
    using System.Net;

    public static class Utils {
        public static HttpContent CreateErrorContent(HttpStatusCode statusCode, string message) {
            return new StringContent(string.Format("<html><body><h1>{0}</h1></body></html>", message), MediaType.TEXT_HTML);
        }

        public static string MediaTypeToString(MediaType mediaType) {
            switch (mediaType) {
                case MediaType.TEXT_HTML:
                    return "text/html";
                case MediaType.TEXT_XML:
                    return "text/xml";
                case MediaType.IMAGE_PNG:
                    return "image/png";
                case MediaType.IMAGE_JPEG:
                    return "image/jpeg";
                case MediaType.IMAGE_GIF:
                    return "image/gif";
                case MediaType.APPLICATION_JSON:
                    return "application/json";
                case MediaType.APPLICATION_OCTET_STREAM:
                    return "application/octet-stream";
                case MediaType.MULTIPART_FORM_DATA:
                    return "multipart/form-data";
                case MediaType.TEXT_PLAIN:
                    return "text/plain";
                case MediaType.APPLICATION_ATOM_XML:
                    return "application/atom+xml";
                case MediaType.APPLICATION_FORM_URLENCODED:
                    return "application/x-www-form-urlencoded";
            }

            return null;
        }
    }

    public static class UriHelper {
        public static Uri CreateUri(Uri baseUrl, string url) {

            if (string.IsNullOrEmpty(url)) {
                return null;
            }

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) {
                if (url.StartsWith("/")) {
                    url = url.Substring(1);
                }

                return new Uri(string.Format("{0}{1}", baseUrl, url));
            }

            return new Uri(url);
        }
    }

    public static class HttpHelper {
        public static string GetStatusDescription(int statusCode) {
            switch (statusCode) {
                case 200:
                    return "OK";
            }

            return null;
        }
    }
}
