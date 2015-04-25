namespace AirSharp {
    using System;

    public class ErrorEventArgs : EventArgs {
        public int Code { get; set; }

        public string Suggestion { get; set; }

        public string FailureReason { get; set; }

        public string Description { get; set; }
    }
}
