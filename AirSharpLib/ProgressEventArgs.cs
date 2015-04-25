namespace AirSharp {
    using System;

    public class ProgressEventArgs : EventArgs {
        public TimeSpan Duration { get; set; }

        public TimeSpan Position { get; set; }
    }
}
