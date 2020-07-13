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
        public SavedSource(
            int id,
            string url,
            string lang,
            string source)
        {
            this.Id = id;
            this.TaskUrl = url;
            this.LanguageId = lang;
            this.SourceCode = source;
        }

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
            using var sourceStream = reader.GetStream(3);
            using var gzStream = new GZipStream(sourceStream, CompressionMode.Decompress);
            Span<byte> buffer = new byte[1 << 20];
            var size = gzStream.Read(buffer);
            buffer = buffer[0..size];
            var ss = new SavedSource(id, url, lang, Encoding.UTF8.GetString(buffer));
            return ss;
        }
    }

}
