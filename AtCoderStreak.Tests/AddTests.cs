using AtCoderStreak.TestUtil;
using FluentAssertions;
using Moq;
using System;
using System.IO;
using Xunit;

namespace AtCoderStreak
{
    public class AddTests
    {
        readonly ProgramBuilder pb = new ProgramBuilder();
        [Fact]
        public void TestAdd_Failed()
        {
            var file = Path.GetTempFileName();
            File.Delete(file);
            pb.Build().Add("http://example.com", "1001", file)
                .Should().Be(1);
            pb.DataMock.Verify(d => d.SaveSource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never());
        }

        [Fact]
        public void TestAdd_Success()
        {
            var file = Path.GetTempFileName();
            try
            {
                var source = new byte[2000];
                new Random().NextBytes(source);
                File.WriteAllBytes(file, source);

                pb.Build().Add("http://example.com", "1001", file)
                    .Should().Be(0);
                pb.DataMock.Verify(d => d.SaveSource("http://example.com", "1001", source));
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
