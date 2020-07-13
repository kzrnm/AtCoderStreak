using AtCoderStreak.TestUtil;
using FluentAssertions;
using Moq;
using System.Threading;
using Xunit;

namespace AtCoderStreak
{
    public class LoginTests
    {
        readonly ProgramBuilder pb = new ProgramBuilder();
        [Fact]
        public async void TestLogin_Failed()
        {
            var ret = await pb.Build().LoginInternal("dummyuser", "dummypass");
            pb.DataMock.Verify(d => d.SaveSession(It.IsAny<string>()), Times.Never());
            ret.Should().Be(1);
        }

        [Fact]
        public async void TestLogin_Success()
        {
            const string cookie = "REVEL_SESSION=012346798%00%00csrf_token%3AcrfafafafaD%00";
            pb.StreakMock
                .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cookie);

            var ret = await pb.Build().LoginInternal("dummyuser", "dummypass");
            pb.DataMock.Verify(d => d.SaveSession(cookie), Times.Once());
            ret.Should().Be(0);
        }
    }
}
