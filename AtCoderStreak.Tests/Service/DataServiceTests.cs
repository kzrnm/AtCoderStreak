using AtCoderStreak.Model;
using AtCoderStreak.Model.Entities;
using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace AtCoderStreak.Service
{
    public class DataServiceTests
    {
        const string SourceCode = @"class P
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(string.Join(' ', args));
    }
}";
        static string MakeSource(int i) => SourceCode.Replace("' '", $"\"{i}\"");

        readonly IDataService service = DataService.Memory();
        readonly SavedSource[] saved = new SavedSource[100];

        public DataServiceTests()
        {
            for (int i = 1; i <= 100; i++)
            {
                var ss = new SavedSource(i, $"http://example.com/{i / 2}", "1000", i % 5 - 2, MakeSource(i));
                saved[i - 1] = ss;
                service.SaveSource(new Source
                {
                    Id = 0,
                    TaskUrl = ss.TaskUrl,
                    LanguageId = ss.LanguageId,
                    Priority = i % 5 - 2,
                    SourceCode = ss.SourceCode,
                });
            }
        }

        [Fact]
        public void TestSession()
        {
            const string cookie = "REVEL_SESSION=012346798%00%00csrf_token%3AcrfafafafaD%00";
            IDataService service = DataService.Memory();
            service.GetSession().Should().Be(null);
            service.SaveSession(cookie);
            service.GetSession().Should().Be(cookie);
        }

        [Fact]
        public void TestSource()
        {
            service.GetSources(SourceOrder.None).Should()
                .Equal(saved.OrderByDescending(s => s.Priority).ThenBy(s => s.Id));
            service.GetSources(SourceOrder.Reverse).Should()
                .Equal(saved.OrderByDescending(s => s.Priority).ThenByDescending(s => s.Id));

            service.DeleteSources(new[] { 1, 2 });
            service.GetSources(SourceOrder.None).Should()
                .Equal(saved.Skip(2).OrderByDescending(s => s.Priority).ThenBy(s => s.Id));




            service.Invoking(s => s.SaveSource(new Source
            {
                Id = 0,
                TaskUrl = "http://example.com",
                LanguageId = "4000",
                Priority = 0,
                CompressedSourceCode = new byte[1024 * 1024]
            }))
                .Should()
                .Throw<ArgumentException>()
                .WithMessage("source code is too long (Parameter 'source')");
            service.Invoking(s => s.SaveSource(new Source
            {
                Id = 0,
                TaskUrl = "http://example.com",
                LanguageId = "4000",
                Priority = 0,
                CompressedSourceCode = new byte[1024 * 1024 - 1]
            }))
                .Should()
                .NotThrow<ArgumentException>();
        }

        [Fact]
        public void TestSourcesByUrl()
        {
            service.GetSourcesByUrl("http://example.com/2").Should()
                .Equal(new[] { saved[3], saved[4] });
        }

        [Fact]
        public void TestSourceById()
        {
            service.GetSourceById(1).Should().Be(saved[0]);
            service.GetSourceById(2).Should().Be(saved[1]);
            service.GetSourceById(101).Should().BeNull();
        }
    }
}
