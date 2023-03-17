using System;
using System.IO;

namespace AtCoderStreak.TestUtil
{
    internal class TemporaryFile : IDisposable
    {
        public TemporaryFile()
        {
            File = new FileInfo(System.IO.Path.GetTempFileName());
        }
        public FileInfo File { get; }
        public string Path => File.FullName;

        public void Dispose()
        {
            File.Delete();
        }
    }
}
