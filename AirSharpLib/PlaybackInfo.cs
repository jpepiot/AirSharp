using System;
using System.Collections.Generic;

namespace AirSharp {
    public class PlaybackInfo {

        public string Uuid { get; set; }

        public TimeSpan? Duration { get; set; }

        public TimeSpan? Position { get; set; }

        public int? Rate { get; set; }

        public bool ReadyToPlay { get; set; }

        public bool PlaybackBufferFull { get; set; }

        public bool PlaybackBufferEmpty { get; set; }

        public static PlaybackInfo CreateFromDictionary(IDictionary<string, object> dictionary) {

            if (dictionary == null) {
                return null;
            }

            PlaybackInfo playbackInfo = new PlaybackInfo();
            if (dictionary.ContainsKey("uuid")) {
                playbackInfo.Uuid = dictionary["uuid"].ToString();
            }

            if (dictionary.ContainsKey("readyToPlay")) {
                playbackInfo.ReadyToPlay = bool.Parse(dictionary["readyToPlay"].ToString());
            }

            if (dictionary.ContainsKey("playbackBufferEmpty")) {
                playbackInfo.PlaybackBufferEmpty = bool.Parse(dictionary["playbackBufferEmpty"].ToString());
            }

            if (dictionary.ContainsKey("playbackBufferFull")) {
                playbackInfo.PlaybackBufferFull = bool.Parse(dictionary["playbackBufferFull"].ToString());
            }

            if (dictionary.ContainsKey("rate")) {
                playbackInfo.Rate = int.Parse(dictionary["rate"].ToString());
            }

            if (dictionary.ContainsKey("duration")) {
                playbackInfo.Duration = TimeSpan.FromSeconds(double.Parse(dictionary["duration"].ToString()));
            }

            if (dictionary.ContainsKey("position")) {
                playbackInfo.Position = TimeSpan.FromSeconds(double.Parse(dictionary["position"].ToString()));
            }

            return playbackInfo;
        }
    }
}
