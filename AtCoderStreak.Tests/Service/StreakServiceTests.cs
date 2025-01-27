using AtCoderStreak.Model;
using System;
using Xunit;

namespace AtCoderStreak.Service
{
    public class StreakServiceTests
    {
        [Fact]
        public void TestFilterAC()
        {
            StreakService.FilterAC([
                new ProblemsSubmission
                {
                    Id=7141771,
                    ContestId="jsc2019-qual",
                    ProblemId="jsc2019_qual_a",
                    DateTime=new DateTime(2019,08,26,00,48,38),
                    UserId="naminodarie",
                    Length=3059,
                    Language="C# (Mono 4.6.2.0)",
                    Point=200,
                    Result="AC",
                    ExecutionTime=25,
                },
                new ProblemsSubmission
                {
                    Id=7141772,
                    ContestId="jsc2019-qual",
                    ProblemId="jsc2019_qual_a",
                    DateTime=new DateTime(2019,08,27,04,35,18),
                    UserId="naminodarie",
                    Length=3059,
                    Language="C# (Mono 4.6.2.0)",
                    Point=200,
                    Result="AC",
                    ExecutionTime=25,
                },
                new ProblemsSubmission
                {
                    Id=10324362,
                    ContestId="abc023",
                    ProblemId="abc023_d",
                    DateTime=new DateTime(2020,02,24,16,59,12),
                    UserId="naminodarie",
                    Length=9964,
                    Language="C# (Mono 4.6.2.0)",
                    Point=100,
                    Result="AC",
                    ExecutionTime=253,
                },
                new ProblemsSubmission
                {
                    Id=10324361,
                    ContestId="abc023",
                    ProblemId="abc023_d",
                    DateTime=new DateTime(2020,02,24,16,58,22),
                    UserId="naminodarie",
                    Length=9964,
                    Language="C# (Mono 4.6.2.0)",
                    Point=100,
                    Result="WA",
                    ExecutionTime=253,
                },
                new ProblemsSubmission
                {
                    Id=10324360,
                    ContestId="abc023",
                    ProblemId="abc023_d",
                    DateTime=new DateTime(2020,02,24,17,13,22),
                    UserId="naminodarie",
                    Length=9964,
                    Language="C# (Mono 4.6.2.0)",
                    Point=100,
                    Result="AC",
                    ExecutionTime=253,
                },
                new ProblemsSubmission
                {
                    Id=10322896,
                    ContestId="abc023",
                    ProblemId="abc023_c",
                    DateTime=new DateTime(2020,02,24,15,33,47),
                    UserId="naminodarie",
                    Length=11344,
                    Language="C# (Mono 4.6.2.0)",
                    Point=30,
                    Result="TLE",
                    ExecutionTime=2108,
                },
                new ProblemsSubmission
                {
                    Id=8016190,
                    ContestId="arc065",
                    ProblemId="arc065_b",
                    DateTime=new DateTime(2019,10,19,17,12,02),
                    UserId="naminodarie",
                    Length=5667,
                    Language="C# (Mono 4.6.2.0)",
                    Point=0,
                    Result="CE",
                    ExecutionTime=null,
                },
                ]).ShouldBe([
                    new ProblemsSubmission
                    {
                        Id=7141771,
                        ContestId="jsc2019-qual",
                        ProblemId="jsc2019_qual_a",
                        DateTime=new DateTime(2019,08,26,00,48,38),
                        UserId="naminodarie",
                        Length=3059,
                        Language="C# (Mono 4.6.2.0)",
                        Point=200,
                        Result="AC",
                        ExecutionTime=25,
                    },
                    new ProblemsSubmission
                    {
                        Id=10324362,
                        ContestId="abc023",
                        ProblemId="abc023_d",
                        DateTime=new DateTime(2020,02,24,16,59,12),
                        UserId="naminodarie",
                        Length=9964,
                        Language="C# (Mono 4.6.2.0)",
                        Point=100,
                        Result="AC",
                        ExecutionTime=253,
                    },
                ]);
        }
    }
}
