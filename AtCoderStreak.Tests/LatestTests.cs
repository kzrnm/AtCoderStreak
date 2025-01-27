using AtCoderStreak.Model;
using AtCoderStreak.TestUtil;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak
{
    public class LatestTests
    {
        readonly MockProgram pb = new();

        [Fact]
        public async Task TestLatest_NoCookie()
        {
            var ret = await pb.RunCommand("latest");
            ret.ShouldBe(255);
        }

        [Fact]
        public async Task TestLatest_NoSubmit()
        {
            pb.SetupCookie();
            pb.StreakMock
                .Setup(s => s.GetACSubmissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            (await pb.LatestInternal("", TestContext.Current.CancellationToken)).ShouldBeNull();
            (await pb.RunCommand("latest")).ShouldBe(1);
        }

        [Fact]
        public async Task TestLatest_Success()
        {
            pb.SetupCookie();
            pb.StreakMock
                .Setup(s => s.GetACSubmissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new ProblemsSubmission
                    {
                        Id=13,
                        ExecutionTime=1000,
                        Length=11344,
                        Language="C# (Mono 4.6.2.0)",
                        UserId="naminodarie",
                        Point=100,
                        ContestId="contest01",
                        ProblemId="contest01_a",
                        Result="AC",
                        DateTime=new DateTime(2019,1,1,11,4,13,0),
                    },
                    new ProblemsSubmission
                    {
                        Id=101,
                        ExecutionTime=100,
                        Length=11344,
                        Language="C# (Mono 4.6.2.0)",
                        UserId="naminodarie",
                        Point=100,
                        ContestId="contest02",
                        ProblemId="contest02_a",
                        Result="AC",
                        DateTime=new DateTime(2020,1,1,15,4,13,0),
                    },
                ]);
            var ret = await pb.LatestInternal("", TestContext.Current.CancellationToken);
            ret!.DateTime.Kind.ShouldBe(DateTimeKind.Unspecified);
            ret.ShouldBe(new ProblemsSubmission
            {
                Id = 101,
                ExecutionTime = 100,
                Length = 11344,
                Language = "C# (Mono 4.6.2.0)",
                UserId = "naminodarie",
                Point = 100,
                ContestId = "contest02",
                ProblemId = "contest02_a",
                Result = "AC",
                DateTime = new DateTime(2020, 1, 1, 15, 4, 13, 0),
            });
            (await pb.RunCommand("latest")).ShouldBe(0);
        }
    }
}
