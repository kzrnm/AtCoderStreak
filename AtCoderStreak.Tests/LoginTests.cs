using AtCoderStreak.TestUtil;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak
{
    public class LoginTests
    {
        readonly MockProgram pb = new();
        [Fact]
        public async Task TestLogin_Failed()
        {
            var ret = await pb.RunCommand("login", "dummypass");
            pb.DataMock.Verify(d => d.SaveSession(It.IsAny<string>()), Times.Never());
            ret.ShouldBe(1);
        }

        [Fact]
        public async Task TestLogin_Success()
        {
            const string cookie = "REVEL_SESSION=012346798%00%00csrf_token%3AcrfafafafaD%00";
            pb.StreakMock
                .Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cookie);

            var ret = await pb.LoginInternal("dummyuser", "dummypass", TestContext.Current.CancellationToken);
            pb.DataMock.Verify(d => d.SaveSession(cookie), Times.Once());
            ret.ShouldBe(0);
        }
    }
}
