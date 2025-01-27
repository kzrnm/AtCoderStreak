using AtCoderStreak.Model;
using AtCoderStreak.Model.Entities;
using LiteDB;
using System;
using System.Linq;
using Xunit;

namespace AtCoderStreak.Service
{
    public class MemoryDataService : DataService
    {
        private readonly LiteDatabase db;

        public MemoryDataService() : base(":memory:")
        {
            db = new LocalLiteDatabase();
        }
        protected override LiteDatabase Connect()
        {
            return db;
        }

        class LocalLiteDatabase : LiteDatabase
        {
            public LocalLiteDatabase() : base(":memory:")
            {
            }

            protected override void Dispose(bool disposing)
            {
            }

            protected void DisposeForce()
            {
                base.Dispose(true);
            }
        }
    }

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

        public readonly IDataService service = new MemoryDataService();
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
            new MemoryDataService().ShouldSatisfyAllConditions<IDataService>([
                s => s.GetSession().ShouldBe(null),
                s => s.SaveSession(cookie),
                s => s.GetSession().ShouldBe(cookie),
            ]);
        }

        [Fact]
        public void TestSource()
        {
            service.GetSources(SourceOrder.None).ShouldBe(saved.OrderByDescending(s => s.Priority).ThenBy(s => s.Id));
            service.GetSources(SourceOrder.Reverse).ShouldBe(saved.OrderByDescending(s => s.Priority).ThenByDescending(s => s.Id));

            service.DeleteSources([1, 2]);
            service.GetSources(SourceOrder.None).ShouldBe(saved.Skip(2).OrderByDescending(s => s.Priority).ThenBy(s => s.Id));

            Should.Throw<ArgumentException>(() =>
            {
                service.SaveSource(new Source
                {
                    Id = 0,
                    TaskUrl = "http://example.com",
                    LanguageId = "4000",
                    Priority = 0,
                    CompressedSourceCode = new byte[1024 * 1024]
                });
            }).Message.ShouldBe("source code is too long (Parameter 'source')");

            Should.NotThrow(() =>
            {
                service.SaveSource(new Source
                {
                    Id = 0,
                    TaskUrl = "http://example.com",
                    LanguageId = "4000",
                    Priority = 0,
                    CompressedSourceCode = new byte[1024 * 1024 - 1]
                });
            });
        }

        [Fact]
        public void TestSourcesByUrl()
        {
            service.GetSourcesByUrl("http://example.com/2").ShouldBe([saved[3], saved[4]]);
        }

        [Fact]
        public void TestSourceById()
        {
            service.GetSourceById(1).ShouldBe(saved[0]);
            service.GetSourceById(2).ShouldBe(saved[1]);
            service.GetSourceById(101).ShouldBeNull();
        }
    }
}
