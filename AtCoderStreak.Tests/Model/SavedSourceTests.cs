using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AtCoderStreak.Model
{
    public class SavedSourceTests
    {
        public static TheoryData SourceTestSubmitInfo
            = new TheoryData<(string, string, string)?, SavedSource> {
            {
                ("abc169", "abc169_a", "https://atcoder.jp/contests/abc169"),
                new SavedSource(1, "https://atcoder.jp/contests/abc169/tasks/abc169_a", "4009", @"{print $1 * $2}")
            },
            {
                null,
                new SavedSource(1, "https://atcoder.jp/contests/abc169/", "4009", @"{print $1 * $2}")
            },
        };
        [Theory]
        [MemberData(nameof(SourceTestSubmitInfo))]
        public void TestSubmitInfo(
            (string contest, string problem, string submitUrl)? expected, SavedSource source)
        {
            if (expected is { } ex)
            {
                source.CanParse().Should().BeTrue();
                source.SubmitInfo().Should().Be(ex);
            }
            else
            {
                source.CanParse().Should().BeFalse();
                source.Invoking(s => s.SubmitInfo())
                    .Should()
                    .Throw<InvalidOperationException>()
                    .WithMessage("failed to parse TaskUrl");
            }
        }
    }
}
