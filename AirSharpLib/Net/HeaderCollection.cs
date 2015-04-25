namespace AirSharp.Net {
    using System;
    using System.Collections.Generic;

    public class HeaderCollection {
        private readonly Dictionary<string, string> _headers;

        public HeaderCollection() {
            _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string GetHeader(string name) {
            return _headers.ContainsKey(name) ? _headers[name] : null;
        }

        public void AddHeader(string name, string value) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException("name");
            }

            _headers[name] = value;
        }

        public bool ContainsHeader(string name) {
            return _headers.ContainsKey(name);
        }

        public string this[string name] {
            get {
                return _headers.ContainsKey(name) ? _headers[name] : null;
            }

            set {
                if (string.IsNullOrEmpty(name)) {
                    throw new ArgumentNullException("name");
                }

                _headers[name] = value;
            }
        }

        public Dictionary<string, string>.KeyCollection Keys {
            get {
                return _headers.Keys;
            }
        }
    }
}
