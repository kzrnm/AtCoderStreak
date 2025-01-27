using System;
using Xunit;
using Xunit.Sdk;

namespace AtCoderStreak.Model
{
    public class SavedSourceTests
    {
        public static TheoryData<string, string, string, SerializableSavedSource> SourceTestSubmitInfo => new() {
            {
                "abc169", "abc169_a", "https://atcoder.jp/contests/abc169",
                new SerializableSavedSource(1, "https://atcoder.jp/contests/abc169/tasks/abc169_a", "4009", 0, @"{print $1 * $2}")
            },
            {
                null, null, null,
                new SerializableSavedSource(1, "https://atcoder.jp/contests/abc169/", "4009", 0, @"{print $1 * $2}")
            },
        };
        [Theory]
        [MemberData(nameof(SourceTestSubmitInfo))]
        public void TestSubmitInfo(string expectedContest, string expectedProblem, string expectedSubmitUrl, SerializableSavedSource ssource)
        {
            var source = ssource.ToSavedSource();
            if (expectedContest != null)
            {
                source.CanParse().ShouldBeTrue();
                source.SubmitInfo().ShouldBe((expectedContest, expectedProblem, expectedSubmitUrl));
            }
            else
            {
                source.CanParse().ShouldBeFalse();
                Should.Throw<InvalidOperationException>(() => source.SubmitInfo())
                    .Message.ShouldBe("failed to parse TaskUrl");
            }
        }
    }
}
