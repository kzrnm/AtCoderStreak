using System;
using System.Data.SQLite;
using System.IO.Compression;
using System.Text;

namespace AtCoderStreak.Model
{
    public class SavedSource
    {
        public int Id { get; }
        public string TaskUrl { get; }
        public string LanguageId { get; }
        public string SourceCode { get; }
        public int Priority { get; }
        public SavedSource(
            int id,
            string url,
            string lang,
            string source,
            int priority)
        {
            this.Id = id;
            this.TaskUrl = url;
            this.LanguageId = lang;
            this.SourceCode = source;
            this.Priority = priority;
        }

        public override string ToString() => $"{Id}(priority:{Priority}): {TaskUrl}";

        private bool? parseUrlResult;
        private string contest = "";
        private string problem = "";
        private string baseUrl = "";
        public bool CanParse()
        {
            bool ParseUrl()
            {
                var sp1 = TaskUrl.Split("/tasks/", StringSplitOptions.RemoveEmptyEntries);
                if (sp1.Length != 2) return false;
                baseUrl = sp1[0];
                var sp2 = baseUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);

                contest = sp2[^1];
                problem = sp1[1];
                return true;
            }
            return parseUrlResult ??= ParseUrl();
        }
        public (string contest, string problem, string baseUrl) SubmitInfo()
        {
            if (this.CanParse()) return (contest, problem, baseUrl);
            throw new InvalidOperationException("failed to parse TaskUrl");
        }
        public static SavedSource FromReader(SQLiteDataReader reader)
        {
            var id = reader.GetInt32(0);
            var url = reader.GetString(1);
            var lang = reader.GetString(2);
            var priority = reader.GetInt32(4);
            using var sourceStream = reader.GetStream(3);
            using var gzStream = new GZipStream(sourceStream, CompressionMode.Decompress);
            Span<byte> buffer = new byte[1 << 20];
            var size = gzStream.Read(buffer);
            buffer = buffer[0..size];
            var ss = new SavedSource(id, url, lang, Encoding.UTF8.GetString(buffer), priority);
            return ss;
        }

        public override bool Equals(object? obj)
        {
            return obj is SavedSource source &&
                   this.Id == source.Id &&
                   this.TaskUrl == source.TaskUrl &&
                   this.LanguageId == source.LanguageId &&
                   this.SourceCode == source.SourceCode &&
                   this.Priority == source.Priority;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(this.Id);
            hash.Add(this.TaskUrl);
            hash.Add(this.LanguageId);
            hash.Add(this.SourceCode);
            hash.Add(this.Priority);
            return hash.ToHashCode();
        }
    }

}
