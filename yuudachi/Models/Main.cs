using System.Text.Json.Serialization;

namespace yuudachi.Models;

record Main([property: JsonPropertyName("np")] string Np,
            [property: JsonPropertyName("listeners")] int Listeners,
            [property: JsonPropertyName("bitrate")] int Bitrate,
            [property: JsonPropertyName("isafkstream")] bool Isafkstream,
            [property: JsonPropertyName("isstreamdesk")] bool Isstreamdesk,
            [property: JsonPropertyName("current")] int Current,
            [property: JsonPropertyName("start_time")] int StartTime,
            [property: JsonPropertyName("end_time")] int EndTime,
            [property: JsonPropertyName("lastset")] string Lastset,
            [property: JsonPropertyName("trackid")] int Trackid,
            [property: JsonPropertyName("thread")] string Thread,
            [property: JsonPropertyName("requesting")] bool Requesting,
            [property: JsonPropertyName("djname")] string Djname,
            [property: JsonPropertyName("dj")] Dj Dj,
            [property: JsonPropertyName("queue")] Queue[] Queue,
            [property: JsonPropertyName("lp")] Lp[] Lp);
