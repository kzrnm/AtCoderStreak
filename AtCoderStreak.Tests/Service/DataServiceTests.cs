using AtCoderStreak.Model;
using FluentAssertions;
using System;
using System.Linq;
using System.Text;
using Xunit;

namespace AtCoderStreak.Service
{
    public class DataServiceTests
    {
        [Fact]
        public void TestSession()
        {
            const string cookie = "REVEL_SESSION=012346798%00%00csrf_token%3AcrfafafafaD%00";
            IDataService service = new DataService(":memory:");
            service.GetSession().Should().Be(null);
            service.SaveSession(cookie);
            service.GetSession().Should().Be(cookie);
        }

        [Fact]
        public void TestSource()
        {
            const string source = @"class P
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(string.Join(' ', args));
    }
}";
            static string MakeSource(int i) => source.Replace("' '", $"\"{i}\"");
            IDataService service = new DataService(":memory:");
            var expected = new SavedSource[100];
            for (int i = 1; i <= 100; i++)
            {
                var ss = new SavedSource(i, $"http://example.com/{i}", "1000", MakeSource(i));
                expected[i - 1] = ss;
                service.SaveSource(ss.TaskUrl, ss.LanguageId, Encoding.UTF8.GetBytes(ss.SourceCode));
            }

            service.GetSources(SourceOrder.None).Should().BeEquivalentTo(expected);
            service.GetSources(SourceOrder.Reverse).Should().BeEquivalentTo(expected.Reverse());

            service.DeleteSources(new[] { 1, 2 });
            service.GetSources(SourceOrder.None).Should().BeEquivalentTo(expected.Skip(2));

            service
                .Invoking(s => s.SaveSource("http://example.com", "4000", new byte[(512 << 10) + 1]))
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("source code is too long (Parameter 'fileBytes')");
            service
                .Invoking(s => s.SaveSource("http://example.com", "4000", new byte[512 << 10]))
                .Should()
                .NotThrow<ArgumentException>();
        }
    }
}
