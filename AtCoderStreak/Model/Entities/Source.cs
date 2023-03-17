using LiteDB;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AtCoderStreak.Model.Entities
{
#nullable disable
    public class Source
    {
        public int Id { get; set; }
        public string TaskUrl { get; set; }
        public string LanguageId { get; set; }
        public int Priority { get; set; }
        public byte[] CompressedSourceCode { get; set; }

        [BsonIgnore]
        public string SourceCode
        {
            set
            {
                using MemoryStream ms = new();
                using (GZipStream gzStream = new(ms, CompressionMode.Compress))
                    gzStream.Write(new(new UTF8Encoding(false).GetBytes(value)));

                CompressedSourceCode = ms.ToArray();
            }
        }

        public SavedSource ToImmutable()
        {
            ArgumentNullException.ThrowIfNull(TaskUrl);
            ArgumentNullException.ThrowIfNull(LanguageId);
            ArgumentNullException.ThrowIfNull(CompressedSourceCode);

            using var ms = new MemoryStream();
            using (var sourceStream = new MemoryStream(CompressedSourceCode))
            using (var gzStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                gzStream.CopyTo(ms);

            return new SavedSource(Id, TaskUrl, LanguageId, Priority, Encoding.UTF8.GetString(ms.ToArray()));
        }
    }
#nullable restore
}
