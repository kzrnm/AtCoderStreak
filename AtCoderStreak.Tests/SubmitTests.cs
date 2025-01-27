using AtCoderStreak.Model;
using AtCoderStreak.Service;
using AtCoderStreak.TestUtil;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak
{
    public class SubmitTests
    {
        readonly MockProgram pb = new();

        public static TheoryData<bool, DateTime> TestIsToday_Data => new()
            {
                {
                    true,
                    DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local).Date.AddDays(1)
                },
                {
                    false,
                    DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified).AddDays(-1)
                },
                {
                    false,
                    DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified).Date.AddMinutes(-1)
                },
            };

        [Theory()]
        [MemberData(nameof(TestIsToday_Data))]
        public void TestIsToday(bool expect, DateTime date)
        {
            Program.IsToday(date).ShouldBe(expect);
        }

        [Fact]
        public async Task TestSubmit_Latest_Exist()
        {
            var todays = new ProblemsSubmission
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
                DateTime = DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(9)).Date.AddDays(1).AddSeconds(-1),
            };
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
                        DateTime=new DateTime(1000,1,1,11,4,13,0),
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
                    todays,
                ]);
            var ret = await pb.SubmitSingleInternal(SourceOrder.None, false, "", TestContext.Current.CancellationToken);
            ret.ShouldBe((todays.ContestId, todays.ProblemId, todays.DateTime));
        }


        [Fact]
        public async Task TestSubmit_Old_EmptyDB()
        {
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
                        DateTime=new DateTime(1000,1,1,11,4,13,0),
                    }
                ]);
            var ret = await pb.SubmitSingleInternal(SourceOrder.None, false, "dumcookie", TestContext.Current.CancellationToken);
            pb.StreakMock.Verify(s => s.GetACSubmissionsAsync("dumcookie", It.IsAny<CancellationToken>()), Times.Once());

            ret.ShouldBeNull();
        }

        [Fact]
        public async Task TestSubmit_Force_EmptyDB()
        {
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
                        DateTime=new DateTime(1000,1,1,11,4,13,0),
                    },
                ]);
            var ret = await pb.SubmitSingleInternal(SourceOrder.None, true, "dumcookie", TestContext.Current.CancellationToken);
            pb.StreakMock.Verify(s => s.GetACSubmissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());

            ret.ShouldBeNull();
        }

        [Fact]
        public async Task TestSubmit_Verify_Order()
        {
            await pb.SubmitSingleInternal(SourceOrder.None, true, "dumcookie", TestContext.Current.CancellationToken);
            pb.DataMock.Verify(d => d.GetSources(SourceOrder.None), Times.Once());
            pb.DataMock.Verify(d => d.GetSources(SourceOrder.Reverse), Times.Never());

            await pb.SubmitSingleInternal(SourceOrder.Reverse, true, "dumcookie", TestContext.Current.CancellationToken);
            pb.DataMock.Verify(d => d.GetSources(SourceOrder.None), Times.Once());
            pb.DataMock.Verify(d => d.GetSources(SourceOrder.Reverse), Times.Once());
        }

        [Fact]
        public async Task TestSubmit_All_Submitted()
        {
            pb.StreakMock
                .Setup(s => s.GetACSubmissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            pb.StreakMock
                .Setup(s => s.GetACSubmissionsAsync("dumcookie", It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new ProblemsSubmission
                    {
                        Id=1,
                        ContestId="ex2",
                        ProblemId="ex2_2",
                        Result="AC",
                        Language="1",
                        DateTime=new DateTime(100,2,3),
                    },
                    new ProblemsSubmission
                    {
                        Id=1,
                        ContestId="ex1",
                        ProblemId="ex1_2",
                        Result="AC",
                        Language="1",
                        DateTime=new DateTime(100,2,3),
                    },
                    new ProblemsSubmission
                    {
                        Id=1,
                        ContestId="ex3",
                        ProblemId="ex3_2",
                        Result="AC",
                        Language="1",
                        DateTime=new DateTime(100,2,3),
                    },
                ]);

            pb.DataMock
                .Setup(d => d.GetSources(It.IsAny<SourceOrder>()))
                .Returns([
                    new SavedSource(1,"http://example.com/contests/ex1/tasks/ex1_2", "1010", 0, @"echo 1"),
                    new SavedSource(2,"http://example.com/contests/ex2/tasks/ex2_2", "2020", 0, @"echo 2"),
                    new SavedSource(3,"http://example.com/contests/ex3/tasks/ex3_2", "3030", 2, @"echo 3"),
                ]);
            var ret = await pb.SubmitSingleInternal(SourceOrder.None, false, "dumcookie", TestContext.Current.CancellationToken);
            ret.ShouldBe(null);


            pb.StreakMock
                .Verify(s => s.GetACSubmissionsAsync("dumcookie", It.IsAny<CancellationToken>()), Times.Once());
            pb.StreakMock
                .Verify(s => s.SubmitSource(It.IsAny<SavedSource>(), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Never());

            var expectedDeleted = new[] { 1, 2, 3 };
            pb.DataMock
                .Verify(d => d.DeleteSources(It.Is<IEnumerable<int>>(input => input.SequenceEqual(expectedDeleted))), Times.Once());
        }

        [Fact]
        public async Task TestSubmit_Success()
        {
            pb.StreakMock
                .Setup(s => s.GetACSubmissionsAsync("dumcookie", It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new ProblemsSubmission
                    {
                        Id=1,
                        ContestId="ex2",
                        ProblemId="ex2_2",
                        Result="AC",
                        Language="1",
                        DateTime=new DateTime(100,2,3),
                    },
                    new ProblemsSubmission
                    {
                        Id=32,
                        ContestId="ex4",
                        ProblemId="ex4_2",
                        Result="AC",
                        Language="1",
                        DateTime=new DateTime(23,2,3),
                    },
                ]);
            pb.DataMock
                .Setup(d => d.GetSources(It.IsAny<SourceOrder>()))
                .Returns([
                    new SavedSource(1,"http://example.com/contests/ex1/tasks/ex1_2", "1010", 0, @"echo 1"),
                    new SavedSource(2,"http://example.com/contests/ex2/tasks/ex2_2", "2020", 0, @"echo 2"),
                    new SavedSource(3,"http://example.com/contests/ex3/tasks/ex3_2", "3030", 0, @"echo 3"),
                    new SavedSource(4,"http://example.com/contests/ex4/tasks/ex4_2", "4040", 0, @"echo 4"),
                ]);
            pb.StreakMock
                .Setup(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Id == 3), It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(("ex3", "ex3_2", new DateTime(2020, 3, 2, 4, 5, 0)));

            var ret = await pb.SubmitSingleInternal(SourceOrder.None, false, "dumcookie", TestContext.Current.CancellationToken);
            ret.ShouldBe(("ex3", "ex3_2", new DateTime(2020, 3, 2, 4, 5, 0)));

            var expectedDelete = new[] { 1, 2, 3, 4 };
            pb.DataMock
                .Verify(d => d.DeleteSources(It.Is<IEnumerable<int>>(input => input.SequenceEqual(expectedDelete))), Times.Once());


            pb.StreakMock
                .Verify(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Id == 1), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once());
            pb.StreakMock
                .Verify(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Id == 2), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Never());
            pb.StreakMock
                .Verify(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Id == 3), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once());
            pb.StreakMock
                .Verify(s => s.SubmitSource(It.Is<SavedSource>(ss => ss.Id == 4), It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Never());

        }
    }
}
