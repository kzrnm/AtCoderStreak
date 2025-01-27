using AtCoderStreak.Model;
using AtCoderStreak.Model.Entities;
using AtCoderStreak.TestUtil;
using Moq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak
{
    public class AddTests
    {
        readonly MockProgram pb = new();
        [Fact]
        public async Task TestAdd_Failed()
        {
            var file = Path.GetTempFileName();
            File.Delete(file);
            var ret = await pb.RunCommand("add", "-u", "http://example.com", "-l", "1001", "-f", file);
            ret.ShouldBe(1);
            pb.DataMock.Verify(d => d.SaveSource(It.IsAny<Source>()), Times.Never());
        }

        [Fact]
        public async Task TestAdd_Success()
        {
            using var file = new TemporaryFile();
            const string source = "print 2";
            File.WriteAllText(file.Path, source, new UTF8Encoding(false));

            var p = new SavedSource(0, "http://example.com", "1001", 0, source);
            var ret = await pb.RunCommand("add", "-u", "http://example.com", "-l", "1001", "-f", file.Path);
            ret.ShouldBe(0);
            pb.DataMock.Verify(d => d.SaveSource(It.Is<Source>(
                s => s.ToImmutable() == new SavedSource(0, "http://example.com", "1001", 0, source))));
        }

        [Fact]
        public async Task TestAdd_Success_Priority()
        {
            var file = Path.GetTempFileName();
            try
            {
                const string source = "print 2";
                File.WriteAllText(file, source, new UTF8Encoding(false));

                var p = new SavedSource(0, "http://example.com", "1001", 0, source);
                var ret = await pb.RunCommand("add", "-u", "http://example.com", "-l", "1001", "-f", file, "-p", "123");
                ret.ShouldBe(0);
                pb.DataMock.Verify(d => d.SaveSource(It.Is<Source>(
                    s => s.ToImmutable() == new SavedSource(0, "http://example.com", "1001", 123, source))));
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public async Task TestAdd_Success_LongName()
        {
            var file = Path.GetTempFileName();
            try
            {
                const string source = "print 2";
                File.WriteAllText(file, source, new UTF8Encoding(false));

                var p = new SavedSource(0, "http://example.com", "1001", 0, source);
                var ret = await pb.RunCommand("add", "--url", "http://example.com", "--lang", "1001", "--file", file, "--priority", "123");
                ret.ShouldBe(0);
                pb.DataMock.Verify(d => d.SaveSource(It.Is<Source>(
                    s => s.ToImmutable() == new SavedSource(0, "http://example.com", "1001", 123, source))));
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
