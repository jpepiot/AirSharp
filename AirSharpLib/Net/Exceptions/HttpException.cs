namespace AirSharp.Net.Exceptions {
    using System;

    public class HttpException : Exception {
        public HttpException(int statusCode, string description)
            : base(description) {
            _statusCode = statusCode;
        }

        private readonly int _statusCode;

        public int StatusCode {
            get {
                return _statusCode;
            }
        }
    }
}
