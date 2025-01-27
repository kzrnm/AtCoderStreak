using AtCoderStreak.Model;
using AtCoderStreak.TestUtil;
using Moq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak
{
    public class SubmitFileTests
    {
        readonly MockProgram pb = new();

        [Fact]
        public async Task TestSubmitFile_NoCookie()
        {
            using var file = new TemporaryFile();
            var ret = await pb.RunCommand("submitfile", "-f", file.Path, "-u", "example.com/contests/ex3/tasks/ex3_2", "-l", "4000");
            ret.ShouldBe(255);
        }

        [Fact]
        public async Task TestSubmitFile_NotExist()
        {
            pb.SetupCookie();
            var ret = await pb.RunCommand("submitfile", "-f", @"<:/:>", "-u", "example.com/contests/ex3/tasks/ex3_2", "-l", "4000");
            ret.ShouldBe(1);
        }

        [Fact]
        public async Task TestSubmitFile_Failure()
        {
            pb.SetupCookie();
            pb.StreakMock
                .Setup(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Equals(new SavedSource(0, "example.com/contests/ex3/tasks/ex3_2", "4000", 0, "1\n2"))), It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("exception for test"));

            var ret = await pb.SubmitFileInternal("1\n2", "example.com/contests/ex3/tasks/ex3_2", "4000", null, TestContext.Current.CancellationToken);
            ret.ShouldBe(2);

            pb.StreakMock
                .Verify(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Equals(new SavedSource(0, "example.com/contests/ex3/tasks/ex3_2", "4000", 0, "1\n2"))), It.IsAny<string>(), false, It.IsAny<CancellationToken>()));
        }
        [Fact]
        public async Task TestSubmitFile_Success()
        {
            pb.SetupCookie();
            pb.StreakMock
                .Setup(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Equals(new SavedSource(0, "example.com/contests/ex3/tasks/ex3_2", "4000", 0, "1\n2"))), It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(default((string, string, DateTime)?));

            var ret = await pb.SubmitFileInternal("1\n2", "example.com/contests/ex3/tasks/ex3_2", "4000", null, TestContext.Current.CancellationToken);
            ret.ShouldBe(0);

            pb.StreakMock
                .Verify(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Equals(new SavedSource(0, "example.com/contests/ex3/tasks/ex3_2", "4000", 0, "1\n2"))), It.IsAny<string>(), false, It.IsAny<CancellationToken>()));
        }
    }
}
