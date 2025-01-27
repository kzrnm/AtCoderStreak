using System;
using System.Text.Json.Serialization;

namespace AtCoderStreak.Model
{
    public class SubmissionStatus : IEquatable<SubmissionStatus>
    {
        [JsonPropertyName("Html")]
        public string? Html { set; get; }
        [JsonPropertyName("Interval")]
        public long? Interval { set; get; }
        [JsonIgnore]
        public bool IsSuccess { set; get; }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as SubmissionStatus);
        }

        public bool Equals(SubmissionStatus? other)
        {
            return other != null &&
                   this.Html == other.Html &&
                   this.IsSuccess == other.IsSuccess &&
                   this.Interval == other.Interval;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Html, this.Interval, this.IsSuccess);
        }
    }
}
