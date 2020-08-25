using AtCoderStreak.Model;
using AtCoderStreak.TestUtil;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak
{
    public class LatestTests
    {
        readonly ProgramBuilder pb = new ProgramBuilder();

        [Fact]
        public async void TestLatest_NoCookie()
        {
            var p = pb.Build();
            var ret = await p.Latest();
            ret.Should().Be(255);
        }

        [Fact]
        public async void TestLatest_NoSubmit()
        {
            pb.SetupCookie();
            pb.StreakMock
                .Setup(s => s.GetACSubmissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<ProblemsSubmission>());

            var p = pb.Build();
            (await p.LatestInternal("")).Should().BeNull();
            (await p.Latest()).Should().Be(1);
        }

        [Fact]
        public async void TestLatest_Success()
        {
            pb.SetupCookie();
            pb.StreakMock
                .Setup(s => s.GetACSubmissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {
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
                });
            var p = pb.Build();
            var ret = await p.LatestInternal("");
            ret!.DateTime.Kind.Should().Be(DateTimeKind.Unspecified);
            ret.Should()
                .Be(new ProblemsSubmission
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
            (await p.Latest()).Should().Be(0);
        }
    }
}
