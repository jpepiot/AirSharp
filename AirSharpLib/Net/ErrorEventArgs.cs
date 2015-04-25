namespace AirSharp.Net {
    using System;

    public class ErrorEventArg : EventArgs {

        public ErrorEventArg(Exception e) {
            Exception = e;
        }

        public Exception Exception { get; set; }
    }
}
