namespace AirSharp {
    using System;

    public class StateChangedEventArgs : EventArgs {
        public StateType NewState { get; set; }
    }
}
