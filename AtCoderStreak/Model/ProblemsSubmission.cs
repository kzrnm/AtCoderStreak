using System;
using System.Text.Json.Serialization;

namespace AtCoderStreak.Model
{
    public class ProblemsSubmission : IEquatable<ProblemsSubmission?>
    {
        [JsonPropertyName("id")]
        public long Id { set; get; }

        [JsonPropertyName("epoch_second")]
        public DateTime DateTime { set; get; }


        [JsonPropertyName("problem_id")]
        public string? ProblemId { set; get; }
        [JsonPropertyName("contest_id")]
        public string? ContestId { set; get; }

        [JsonPropertyName("user_id")]
        public string? UserId { set; get; }

        [JsonPropertyName("language")]
        public string? Language { set; get; }

        [JsonPropertyName("point")]
        public double Point { set; get; }

        [JsonPropertyName("length")]
        public int Length { set; get; }

        [JsonPropertyName("result")]
        public string? Result { set; get; }

        [JsonPropertyName("execution_time")]
        public int? ExecutionTime { set; get; }

        public override bool Equals(object? obj) => this.Equals(obj as ProblemsSubmission);
        public bool Equals(ProblemsSubmission? other)
        {
            return other != null &&
                   this.Id == other.Id &&
                   this.DateTime.Equals(other.DateTime) &&
                   this.ProblemId == other.ProblemId &&
                   this.ContestId == other.ContestId &&
                   this.UserId == other.UserId &&
                   this.Language == other.Language &&
                   this.Point == other.Point &&
                   this.Length == other.Length &&
                   this.Result == other.Result &&
                   this.ExecutionTime == other.ExecutionTime;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(this.Id);
            hash.Add(this.DateTime);
            hash.Add(this.ProblemId);
            hash.Add(this.ContestId);
            hash.Add(this.UserId);
            hash.Add(this.Language);
            hash.Add(this.Point);
            hash.Add(this.Length);
            hash.Add(this.Result);
            hash.Add(this.ExecutionTime);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"{Id} {UserId} ({ContestId}/{ProblemId})-{Result} at {DateTime} ({Language}) Point:{Point} Length:{Length} ExecutionTime:{ExecutionTime}";
        }
    }
}
