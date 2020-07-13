using AtCoderStreak.Model;
using AtCoderStreak.Service;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace AtCoderStreak
{
    public class Program : ConsoleAppBase
    {
        static string AppDir { get; } = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {

                    services.AddHttpClient("allowRedirect")
                    .ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        return new HttpClientHandler()
                        {
                            AllowAutoRedirect = true,
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        };
                    });
                    services.AddHttpClient("disallowRedirect")
                    .ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        return new HttpClientHandler()
                        {
                            AllowAutoRedirect = false,
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        };
                    });
                    services.AddSingleton<IDataService>(
                        new DataService(Path.Combine(AppDir, "data.sqlite")));
                    services.AddSingleton<IStreakService, StreakService>();
                })
                .ConfigureLogging(logging =>
                {
#if DEBUG
                    logging.SetMinimumLevel(LogLevel.Debug);
#else
                    logging.SetMinimumLevel(LogLevel.Information).ReplaceToSimpleConsole();
#endif
                })
                .RunConsoleAppFrameworkAsync<Program>(args);
        }


        private IDataService DataService { get; }
        private IStreakService StreakService { get; }
        public Program(
            IDataService dataService,
            IStreakService streakService
            )
        {
            this.DataService = dataService;
            this.StreakService = streakService;
        }



        private string? LoadCookie(string? argCookie)
        {
            if (!string.IsNullOrEmpty(argCookie))
            {
                if (File.Exists(argCookie))
                    argCookie = File.ReadAllText(argCookie);

                if (!argCookie.Contains("%00"))
                    argCookie = HttpUtility.UrlEncode(argCookie);
                return argCookie;
            }
            else
            {
                return DataService.GetSession();
            }
        }


        [Command("login", "save atcoder cookie")]
        public async Task<int> Login(
            [Option("u", "username")] string? user = null)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                Console.Write("input username: ");
                if (string.IsNullOrWhiteSpace(user = Console.ReadLine()))
                {
                    Context.Logger.LogError("Error: name is empty");
                    return 99;
                }
            }

            Console.Write("input password: ");
            var password = ConsoleUtil.ReadPassword();
            if (string.IsNullOrWhiteSpace(password))
            {
                Context.Logger.LogError("Error: password is empty");
                return 99;
            }

            return await LoginInternal(user, password);
        }

        internal async Task<int> LoginInternal(string user, string password)
        {
            var cookie = await StreakService.LoginAsync(user, password, Context.CancellationToken);
            if (cookie == null)
            {
                Context.Logger.LogError("Error: login failed");
                return 1;
            }
            DataService.SaveSession(cookie);
            Context.Logger.LogInformation("login success");
            return 0;
        }

        [Command("add", "add source code")]
        public int Add(
            [Option("u", "target task url")] string url,
            [Option("l", "language ID")] string lang,
            [Option("f", "source file path")] string file
            )
        {
            if (!File.Exists(file))
            {
                Context.Logger.LogError("Error:file not found");
                return 1;
            }
            DataService.SaveSource(url, lang, File.ReadAllBytes(file));
            return 0;
        }

        [Command("latest", "get latest submit")]
        public async Task<int> Latest(
            [Option("c", "cookie header string or textfile")] string? cookie = null)
        {
            cookie = LoadCookie(cookie);
            if (cookie == null)
            {
                Context.Logger.LogError("Error: no session");
                return 255;
            }

            if (await LatestInternal(cookie) is { } max)
            {
                Context.Logger.LogInformation(max.ToString());
                return 0;
            }
            else
            {
                Context.Logger.LogError("Error: no AC submit");
                return 1;
            }
        }
        internal async Task<ProblemsSubmission?> LatestInternal(string cookie)
        {
            var submits = await StreakService.GetACSubmissionsAsync(cookie, Context.CancellationToken);
            return submits.Latest();
        }

        [Command("submit", "submit source")]
        public async Task<int> Submit(
            [Option("o", "db order")] SourceOrder order = SourceOrder.None,
            [Option("f", "submit force")] bool force = false,
            [Option("c", "cookie header string or textfile")] string? cookie = null)
        {
            cookie = LoadCookie(cookie);
            if (cookie == null)
            {
                Context.Logger.LogError("Error: no session");
                return 255;
            }

            try
            {
                if (await SubmitInternal(order, force, cookie) is { } latest)
                {
                    Context.Logger.LogInformation(latest.ToString());
                    return 0;
                }
                else
                {
                    Context.Logger.LogError("Error: not found new source");
                    return 1;
                }
            }
            catch (HttpRequestException e)
            {
                Context.Logger.LogError(e, "Error: submit error");
                return 2;
            }
        }
        internal async Task<(string contest, string problem, DateTime time)?> SubmitInternal(SourceOrder order, bool force, string cookie)
        {
            ProblemsSubmission[] submits = Array.Empty<ProblemsSubmission>();
            if (!force)
            {
                submits = await StreakService.GetACSubmissionsAsync(cookie, Context.CancellationToken);
                var latest = submits.Latest();
                if (latest != null && latest.DateTime >= DateTime.SpecifyKind(DateTime.UtcNow.AddHours(9).Date, DateTimeKind.Unspecified))
                    return (latest.ContestId!, latest.ProblemId!, latest.DateTime);
            }

            var accepted = new HashSet<(string contest, string problem)>(submits.Select(s => (s.ContestId!, s.ProblemId!)));
            (string contest, string problem, DateTime time)? submitRes = null;
            var usedIds = new List<int>();
            foreach (var source in DataService.GetSources(order))
            {

                if (source.CanParse())
                {
                    var (contest, problem, _) = source.SubmitInfo();
                    if (!accepted.Contains((contest, problem)))
                        submitRes = await StreakService.SubmitSource(source, cookie, Context.CancellationToken);
                }
                usedIds.Add(source.Id);
                if (submitRes.HasValue && submitRes.Value.time >= DateTime.SpecifyKind(DateTime.UtcNow.AddHours(9).Date, DateTimeKind.Unspecified))
                    break;
            }

            DataService.DeleteSources(usedIds);
            return submitRes;
        }
    }
}
