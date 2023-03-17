using AtCoderStreak.Model;
using AtCoderStreak.Model.Entities;
using AtCoderStreak.TestUtil;
using FluentAssertions;
using Moq;
using System.IO;
using System.Text;
using Xunit;

namespace AtCoderStreak
{
    public class AddTests
    {
        readonly ProgramBuilder pb = new();
        [Fact]
        public void TestAdd_Failed()
        {
            var file = Path.GetTempFileName();
            File.Delete(file);
            pb.Build().Add("http://example.com", "1001", file)
                .Should().Be(1);
            pb.DataMock.Verify(d => d.SaveSource(It.IsAny<Source>()), Times.Never());
        }

        [Fact]
        public void TestAdd_Success()
        {
            var file = Path.GetTempFileName();
            try
            {
                const string source = "print 2";
                File.WriteAllText(file, source, new UTF8Encoding(false));

                var p = new SavedSource(0, "http://example.com", "1001", 0, source);

                pb.Build().Add("http://example.com", "1001", file).Should().Be(0);
                pb.DataMock.Verify(d => d.SaveSource(It.Is<Source>(
                    s => s.ToImmutable() == new SavedSource(0, "http://example.com", "1001", 0, source))));
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
