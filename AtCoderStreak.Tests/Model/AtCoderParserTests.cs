using AtCoderStreak.TestUtil;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AtCoderStreak.Model
{
    public class AtCoderParserTests
    {
        private readonly AtCoderParser parser = new AtCoderParser();

        [Fact]
        public void TestParseCookie_Failed()
        {
            const string cookie = "language=ja; timeDelta=-1953";
            Action act = () => parser.ParseCookie(cookie);
            act.Should()
               .Throw<ArgumentException>()
               .WithMessage("invalid cookie (Parameter 'cookie')");
        }

        [Fact]
        public void TestParseCookie_Invalid()
        {
            const string cookie = "language=ja; REVEL_SESSION=xxxxxxx; REVEL_FLASH=; timeDelta=-1953";

            Action act = () => parser.ParseCookie(cookie);
            act.Should()
               .Throw<ArgumentException>()
               .WithMessage("invalid cookie (Parameter 'cookie')");
        }


        [Fact]
        public void TestParseCookie()
        {
            const string cookie = "language=ja; REVEL_SESSION=xxxxxxxxxxxx-%00a%3Afalse%00%00UserName%3Ausernam%00%00csrf_token%3Azzzzzzxxxxxxxxxyyyyyyyyy%3D%00%00_TS%3A1604584674%00%00SessionKey%3Axxxxxxxxxxxxxxxxx%00%00Rating%3A1528%00%00UserScreenName%3Ausernam%00%00w%3Afalse%00; _kick_id=2012-04-25+04%3A51; REVEL_FLASH=; timeDelta=-1953";
            parser.ParseCookie(cookie)
                .Should()
                .Be(("zzzzzzxxxxxxxxxyyyyyyyyy=", "usernam"));
        }

        public static TheoryData SourceTestFilterREVEL_SESSION
            = new TheoryData<string?, IEnumerable<string>>
            {
                {
                    null,
                    new[]{ "foo=bar", "ban=bon" }
                },
                {
                    "REVEL_SESSION=abcdzyxwvu",
                    new[]{ "foo=bar", "ban=bon", "REVEL_SESSION=abcdzyxwvu" }
                },
            };
        [Theory]
        [MemberData(nameof(SourceTestFilterREVEL_SESSION))]
        public void TestFilterREVEL_SESSION(string? expected, IEnumerable<string> input)
        {
            parser.FilterREVEL_SESSION(input).Should().Be(expected);
        }

        public static TheoryData SourceTestParseLoginCSRFToken
            = new TheoryData<string?, string>
            {
                {
                    null,
                    @"<html><body></body></html>"
                },
                {
                    "abcdefg=",
                    @"<html><body><input name=""csrf_token"" value=""abcdefg=""></body></html>"
                },
            };
        [Theory]
        [MemberData(nameof(SourceTestParseLoginCSRFToken))]
        public async Task TestParseLoginCSRFToken(string? expected, string input)
        {
            using var ms = Util.StringToStream(input);
            var ret = await parser.ParseLoginCSRFToken(ms);
            ret.Should().Be(expected);
        }


        public static TheoryData SourceTestParseOldestSubmissionTime
            = new TheoryData<DateTime?, string>
            {
                {
                    null,
                    @"
<html><body>
</body></html>"
                },
                {
                    new DateTime(2018,6,1,19,49,31,DateTimeKind.Local),
                    @"
<html><body>
<div class=""panel-submission""><div><span><span class=""fixtime-second"">2020-04-11 09:19:51+0900</span></span></div></div>
<div class=""panel-submission""><div><span><span class=""fixtime-second"">2019-11-11 05:54:32+0900</span></span></div></div>
<div class=""panel-submission""><div><span><span class=""fixtime-second"">2019-02-21 13:32:48+0900</span></span></div></div>
<div class=""panel-submission""><div><span><span class=""fixtime-second"">2019-01-01 22:36:05+0900</span></span></div></div>
<div class=""panel-submission""><div><span><span class=""fixtime-second"">2019-01-01 11:46:14+0900</span></span></div></div>
<div class=""panel-submission""><div><span><span class=""fixtime-second"">2018-06-01 19:49:31+0900</span></span></div></div>
</body></html>"
                },
            };
        [Theory]
        [MemberData(nameof(SourceTestParseOldestSubmissionTime))]
        public async Task TestParseOldestSubmissionTime(DateTime? expected, string input)
        {
            using var ms = Util.StringToStream(input);
            var ret = await parser.ParseOldestSubmissionTime(ms);
            ret.Should().Be(expected);
        }

        public static TheoryData SourceTestParseLatestSubmissionId
            = new TheoryData<string?, string>
            {
                {
                    null,
                    @"
<html><body>
<table>
<tbody>
</tbody>
</table>
</body></html>"
                },
                {
                    "1084569",
                    @"
<html><body>
<table>
<tbody>
<tr><td class=""submission-score"" data-id=""1084569"">100</td></tr>
<tr><td class=""submission-score"" data-id=""957156"">100</td></tr>
<tr><td class=""submission-score"" data-id=""233036"">100</td></tr>
<tr><td class=""submission-score"" data-id=""222351"">100</td></tr>
<tr><td class=""submission-score"" data-id=""103566"">100</td></tr>
</tbody>
</table>
</body></html>"
                },
            };
        [Theory]
        [MemberData(nameof(SourceTestParseLatestSubmissionId))]
        public async Task TestParseLatestSubmissionId(string? expected, string input)
        {
            using var ms = Util.StringToStream(input);
            var ret = await parser.ParseFirstSubmissionId(ms);
            ret.Should().Be(expected);
        }

        public static TheoryData SourceTestDeserializeSubmissionDetail
            = new TheoryData<SubmissionStatus, string>
            {
                {
                    new SubmissionStatus{
                        Html="<span class='label label-success' aria-hidden='true' data-toggle='tooltip' data-placement='top' title=\"正解\">AC</span>",
                        Interval=null,
                        IsSuccess=true,
                    },
                    @"{""Html"":""\u003cspan class='label label-success' aria-hidden='true' data-toggle='tooltip' data-placement='top' title=\""正解\""\u003eAC\u003c/span\u003e""}"
                },
                {
                    new SubmissionStatus{
                        Html="<span class='label label-warning' aria-hidden='true' data-toggle='tooltip' data-placement='top' title=\"実行時間制限超過\">TLE</span>",
                        Interval=null,
                        IsSuccess=false,
                    },
                    @"{""Html"":""\u003cspan class='label label-warning' aria-hidden='true' data-toggle='tooltip' data-placement='top' title=\""実行時間制限超過\""\u003eTLE\u003c/span\u003e""}"
                },
                {
                    new SubmissionStatus{
                        Html="<span class='label label-default' aria-hidden='true' data-toggle='tooltip' data-placement='top' title=\"ジャッジ待ち\">WJ</span>",
                        Interval=1000,
                        IsSuccess=false,
                    },
                    @"{""Html"":""\u003cspan class='label label-default' aria-hidden='true' data-toggle='tooltip' data-placement='top' title=\""ジャッジ待ち\""\u003eWJ\u003c/span\u003e"",""Interval"":1000}"
                },
            };
        [Theory]
        [MemberData(nameof(SourceTestDeserializeSubmissionDetail))]
        public async Task TestDeserializeSubmissionDetail(SubmissionStatus expected, string input)
        {
            using var ms = Util.StringToStream(input);
            var ret = await parser.DeserializeSubmissionDetail(ms);
            ret.Should().Be(expected);
        }


        [Fact]
        public async Task TestDeserializeSubmitsJson()
        {
            const string json = @"[{""id"":7141771,""epoch_second"":1566748118,""problem_id"":""jsc2019_qual_a"",""contest_id"":""jsc2019-qual"",""user_id"":""naminodarie"",""language"":""C# (Mono 4.6.2.0)"",""point"":200.0,""length"":3059,""result"":""AC"",""execution_time"":25},{""id"":10324362,""epoch_second"":1582531152,""problem_id"":""abc023_d"",""contest_id"":""abc023"",""user_id"":""naminodarie"",""language"":""C# (Mono 4.6.2.0)"",""point"":100.0,""length"":9964,""result"":""AC"",""execution_time"":253},{""id"":10322896,""epoch_second"":1582526027,""problem_id"":""abc023_c"",""contest_id"":""abc023"",""user_id"":""naminodarie"",""language"":""C# (Mono 4.6.2.0)"",""point"":30.0,""length"":11344,""result"":""TLE"",""execution_time"":2108},{""id"":8016190,""epoch_second"":1571472722,""problem_id"":""arc065_b"",""contest_id"":""arc065"",""user_id"":""naminodarie"",""language"":""C# (Mono 4.6.2.0)"",""point"":0.0,""length"":5667,""result"":""CE"",""execution_time"":null}]";

            using var ms = Util.StringToStream(json);
            var submits = await parser.DeserializeProblemsSubmitAsync(ms);
            submits
                .Should()
                .Equal(new ProblemsSubmission[4] {
                new ProblemsSubmission
                {
                    Id=7141771,
                    ContestId="jsc2019-qual",
                    ProblemId="jsc2019_qual_a",
                    DateTime=new DateTime(2019,8,26,0,48,38),
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
                    DateTime=new DateTime(2020,2,24,16,59,12),
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
                    DateTime=new DateTime(2020,2,24,15,33,47),
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
                    DateTime=new DateTime(2019,10,19,17,12,2),
                    UserId="naminodarie",
                    Length=5667,
                    Language="C# (Mono 4.6.2.0)",
                    Point=0,
                    Result="CE",
                    ExecutionTime=null,
                },
                });
            submits.Select(s => s.DateTime.Kind)
                .Should()
                .AllBeEquivalentTo(DateTimeKind.Unspecified);
        }
    }
}
