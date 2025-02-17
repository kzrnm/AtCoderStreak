using AtCoderStreak.Model;
using AtCoderStreak.Model.Entities;
using AtCoderStreak.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UtfUnknown;


await AtCoderStreak.Program.GetDefault().RunCommand(args);

namespace AtCoderStreak
{
    public class Program
    {
        public static Program GetDefault()
        {
            var services = new ServiceCollection();
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

            var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            services.AddSingleton<IDataService>(new DataService(Path.Combine(appDir, "data.db")));
            services.AddSingleton<IStreakService, StreakService>();

            services.AddLogging(logging =>
            {
                logging
                .AddFilter((s, t, lv) => t?.Contains("HttpClient") != true)
#if DEBUG
                .SetMinimumLevel(LogLevel.Debug)
#else
                .SetMinimumLevel(LogLevel.Information)
#endif
                .AddSimpleConsole();
            });

            services.AddTransient<Program>();
            return services.BuildServiceProvider().GetService<Program>()!;
        }

        private IDataService DataService { get; }
        private IStreakService StreakService { get; }
        private ILogger Logger { get; }
        private RootCommand RootCommand { get; }
        public Program(
            IDataService dataService,
            IStreakService streakService,
            ILoggerFactory loggerFactory
            ) : this(dataService, streakService, loggerFactory.CreateLogger<Program>()) { }
        public Program(
            IDataService dataService,
            IStreakService streakService,
            ILogger logger
            )
        {
            DataService = dataService;
            StreakService = streakService;
            Logger = logger;
            RootCommand = [
                BuildLoginCommand(),
                BuildAddCommand(),
                BuildRestoreCommand(),
                BuildLatestCommand(),
                BuildSubmitFileCommand(),
                BuildSubmitCommand(),
            ];
        }

        public async Task<int> RunCommand(params string[] args)
            => await RootCommand.InvokeAsync(args);

        private string? LoadCookie(string? argCookie)
        {
            if (!string.IsNullOrEmpty(argCookie))
            {
                if (File.Exists(argCookie))
                    argCookie = File.ReadAllText(argCookie).Trim();

                if (!argCookie.Contains("%00"))
                    argCookie = HttpUtility.UrlEncode(argCookie);
                return argCookie;
            }
            else
            {
                return DataService.GetSession();
            }
        }

        Command BuildLoginCommand()
        {
            async Task<int> Login(string? user, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(user))
                {
                    Console.Write("input username: ");
                    if (string.IsNullOrWhiteSpace(user = Console.ReadLine()))
                    {
                        Logger.LogError("Error: name is empty");
                        return 99;
                    }
                }

                Console.Write("input password: ");
                var password = ConsoleUtil.ReadPassword();
                if (string.IsNullOrWhiteSpace(password))
                {
                    Logger.LogError("Error: password is empty");
                    return 99;
                }

                return await LoginInternal(user, password, cancellationToken);
            }


            var userOption = new Option<string?>(
                aliases: ["--user", "-u"],
                description: "Specify AtCoder user name.");
            var command = new Command("login", "Save atcoder cookie")
        {
            userOption,
        };

            command.SetHandler(async (InvocationContext ctx) =>
            {
                var user = ctx.ParseResult.GetValueForOption(userOption);
                ctx.ExitCode = await Login(user, ctx.GetCancellationToken());
            });

            return command;
        }

        internal async Task<int> LoginInternal(string user, string password, CancellationToken cancellationToken)
        {
            var cookie = await StreakService.LoginAsync(user, password, cancellationToken);
            if (cookie == null)
            {
                Logger.LogError("Error: login failed");
                return 1;
            }
            DataService.SaveSession(cookie);
            Logger.LogInformation("login success");
            return 0;
        }

        Command BuildAddCommand()
        {
            var fileOption = new Option<FileInfo>(
                aliases: ["--file", "-f"],
                description: "source file path.")
            {
                IsRequired = true,
            };
            var langOption = new Option<string>(
                aliases: ["--lang", "-l"],
                description: "language ID")
            {
                IsRequired = true,
            };
            var urlOption = new Option<string>(
                aliases: ["--url", "-u"],
                description: "target task url")
            {
                IsRequired = true,
            };

            var priorityOption = new Option<int>(
                aliases: ["--priority", "-p"],
                getDefaultValue: () => 0);

            var command = new Command("add", "Add source code")
        {
            fileOption,
            langOption,
            urlOption,
            priorityOption,
        };

            command.SetHandler(async (InvocationContext ctx) =>
            {
                var url = ctx.ParseResult.GetValueForOption(urlOption)!;
                var file = ctx.ParseResult.GetValueForOption(fileOption)!;
                var lang = ctx.ParseResult.GetValueForOption(langOption)!;
                var priority = ctx.ParseResult.GetValueForOption(priorityOption);

                ctx.ExitCode = AddInternal(url, lang, file, priority);
            });
            return command;
        }
        public int AddInternal(string url, string lang, FileInfo file, int priority)
        {
            if (!file.Exists)
            {
                Logger.LogError("Error:file not found");
                return 1;
            }
            foreach (var s in DataService.GetSourcesByUrl(url))
            {
                Logger.LogInformation("[Warning]exist: {s}", s.ToString());
            }

            var encoding = CharsetDetector.DetectFromFile(file.FullName)?.Detected?.Encoding ?? Encoding.UTF8;
            using var fs = file.OpenRead();
            fs.Position = 0;
            using var reader = new StreamReader(fs, encoding);

            DataService.SaveSource(new Source
            {
                TaskUrl = url,
                LanguageId = lang,
                Priority = priority,
                SourceCode = reader.ReadToEnd(),
            });
            Logger.LogInformation("Finish: {url}, {file}, lang:{lang}, priority:{priority}", url, file, lang, priority);
            return 0;
        }

        Command BuildRestoreCommand()
        {
            async Task<int> Restore(FileInfo file, int id, string? url)
            {
                SavedSource? source;
                try
                {
                    source = RestoreInternal(id, url);
                }
                catch (ArgumentException e)
                {
                    Logger.LogError("Fail to Restore:{Message}", e.Message);
                    return 128;
                }
                if (source != null)
                {
                    Logger.LogInformation("Restore: {source}", source.ToString());

                    using var fs = file.OpenWrite();
                    using var sw = new StreamWriter(fs, new UTF8Encoding(false));
                    await sw.WriteAsync(source.SourceCode);
                    return 0;
                }
                else
                {
                    Logger.LogError($"Error: not found source");
                    return 1;
                }
            }

            var idArgument = new Argument<int>("id", getDefaultValue: () => -1, description: "Source id");
            var fileOption = new Option<FileInfo>(aliases: ["--file", "-f"], description: "source file path.")
            {
                IsRequired = true,
            };
            var urlOption = new Option<string>(aliases: ["--url", "-u"], description: "target task url");

            var command = new Command("restore", "Restore source code")
        {
            idArgument,
            fileOption,
            urlOption,
        };

            command.SetHandler(async (InvocationContext ctx) =>
            {
                var file = ctx.ParseResult.GetValueForOption(fileOption)!;
                var url = ctx.ParseResult.GetValueForOption(urlOption);
                var id = ctx.ParseResult.GetValueForArgument(idArgument);

                ctx.ExitCode = await Restore(file, id, url);
            });
            return command;
        }

        public SavedSource? RestoreInternal(int id = -1, string? url = null)
        {
            if (string.IsNullOrWhiteSpace(url) == (id < 0))
                throw new ArgumentException($"Error: must use either {nameof(url)} or {nameof(id)}");

            if (!string.IsNullOrWhiteSpace(url))
                return DataService.GetSourcesByUrl(url).FirstOrDefault();
            else if (id >= 0)
                return DataService.GetSourceById(id);

            throw new InvalidOperationException("never");
        }

        Command BuildLatestCommand()
        {
            async Task<int> Latest(string? cookie, CancellationToken cancellationToken)
            {
                cookie = LoadCookie(cookie);
                if (cookie == null)
                {
                    Logger.LogError("Error: no session");
                    return 255;
                }

                if (await LatestInternal(cookie, cancellationToken) is { } max)
                {
                    Logger.LogInformation("Latest Submit:{max}", max.ToString());
                    return 0;
                }
                else
                {
                    Logger.LogError("Error: no AC submit");
                    return 1;
                }
            }

            var cookieOption = new Option<string>(
                aliases: ["--cookie", "-c"],
                description: "cookie header string or textfile.");

            var command = new Command("latest", "Get latest submit")
        {
            cookieOption,
        };

            command.SetHandler(async (InvocationContext ctx) =>
            {
                var cookie = ctx.ParseResult.GetValueForOption(cookieOption);
                ctx.ExitCode = await Latest(cookie, ctx.GetCancellationToken());
            });
            return command;
        }

        public async Task<ProblemsSubmission?> LatestInternal(string cookie, CancellationToken cancellationToken = default)
        {
            var submits = await StreakService.GetACSubmissionsAsync(cookie, cancellationToken);
            return submits.Latest();
        }

        Command BuildSubmitFileCommand()
        {
            async Task<int> SubmitFile(FileInfo file, string url, string lang, string? cookie, CancellationToken cancellationToken)
            {
                if (!file.Exists)
                {
                    Logger.LogError("Error: file not found");
                    return 1;
                }

                var encoding = CharsetDetector.DetectFromFile(file.FullName)?.Detected?.Encoding ?? Encoding.UTF8;
                using var fs = file.OpenRead();
                fs.Position = 0;
                using var reader = new StreamReader(fs, encoding);

                return await SubmitFileInternal(reader.ReadToEnd(), url, lang, cookie, cancellationToken);
            }

            var fileOption = new Option<FileInfo>(
                aliases: ["--file", "-f"],
                description: "source file path.")
            {
                IsRequired = true,
            };
            var langOption = new Option<string>(
                aliases: ["--lang", "-l"],
                description: "language ID")
            {
                IsRequired = true,
            };
            var urlOption = new Option<string>(
                aliases: ["--url", "-u"],
                description: "target task url")
            {
                IsRequired = true,
            };


            var cookieOption = new Option<string>(
                aliases: ["--cookie", "-c"],
                description: "cookie header string or textfile.");

            var command = new Command("submitfile", "Submit source from file")
        {
            fileOption,
            langOption,
            urlOption,
            cookieOption,
        };

            command.SetHandler(async (InvocationContext ctx) =>
            {
                var file = ctx.ParseResult.GetValueForOption(fileOption)!;
                var lang = ctx.ParseResult.GetValueForOption(langOption)!;
                var url = ctx.ParseResult.GetValueForOption(urlOption)!;
                var cookie = ctx.ParseResult.GetValueForOption(cookieOption);
                ctx.ExitCode = await SubmitFile(file, url, lang, cookie, ctx.GetCancellationToken());
            });
            return command;
        }


        public async Task<int> SubmitFileInternal(string sourceCode, string url, string lang, string? cookie, CancellationToken cancellationToken = default)
        {
            cookie = LoadCookie(cookie);
            if (cookie == null)
            {
                Logger.LogError("Error: no session");
                return 255;
            }
            try
            {
                var source = new SavedSource(0, url, lang, 0, sourceCode);
                var submitRes = await StreakService.SubmitSource(source, cookie, false, cancellationToken);
                return 0;
            }
            catch (HttpRequestException e)
            {
                Logger.LogError(e, "Error: submit error");
                return 2;
            }
        }


        Command BuildSubmitCommand()
        {
            var orderOption = new Option<SourceOrder>(
                aliases: ["--order", "-o"],
                getDefaultValue: () => SourceOrder.None,
                description: "source order path.");

            var forceOption = new Option<bool>(
                aliases: ["--force", "-f"],
                description: "Submit force");

            var parallelOption = new Option<int>(
                aliases: ["--parallel", "-p"],
                getDefaultValue: () => 0,
                description: "Parallel count. if 0, streak mode");

            var cookieOption = new Option<string>(
                aliases: ["--cookie", "-c"],
                description: "cookie header string or textfile.");

            var command = new Command("submit", "Submit source")
            {
                orderOption,
                forceOption,
                parallelOption,
                cookieOption,
            };

            command.SetHandler(async (InvocationContext ctx) =>
            {
                var order = ctx.ParseResult.GetValueForOption(orderOption);
                var force = ctx.ParseResult.GetValueForOption(forceOption);
                var parallel = ctx.ParseResult.GetValueForOption(parallelOption);
                var cookie = ctx.ParseResult.GetValueForOption(cookieOption);
                ctx.ExitCode = await SubmitInternal(order, force, parallel, cookie, ctx.GetCancellationToken());
            });
            return command;
        }

        public async Task<int> SubmitInternal(SourceOrder order, bool force, int paralell, string? cookie, CancellationToken cancellationToken = default)
        {
            cookie = LoadCookie(cookie);
            if (cookie == null)
            {
                Logger.LogError("Error: no session");
                return 255;
            }

            if (paralell > 0)
                return await SubmitParallel(order, paralell, cookie, cancellationToken);

            try
            {
                if (await SubmitSingleInternal(order, force, cookie, cancellationToken) is { } latest)
                {
                    Logger.LogInformation("Submit: {latest}", latest.ToString());
                    return 0;
                }
                else
                {
                    Logger.LogError("Error: not found new source");
                    return 1;
                }
            }
            catch (HttpRequestException e)
            {
                Logger.LogError(e, "Error: submit error");
                return 2;
            }
        }

        internal async Task<int> SubmitParallel(SourceOrder order, int paralell, string cookie, CancellationToken cancellationToken)
        {
            var res = await SubmitInternalParallel(order, cookie, paralell, cancellationToken);
            Logger.LogInformation("Count: {Length}", res.Length);
            foreach (var (source, submitSuccess) in res)
            {
                if (submitSuccess)
                {
                    Logger.LogInformation("Submit: {Url}", source.TaskUrl);
                }
                else
                {
                    Logger.LogError("Failed to submit: {Url}", source.TaskUrl);
                }
            }
            return 0;
        }


        internal static bool IsToday(DateTime dateTime)
            => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), TimeSpan.FromHours(9)).Date >= DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(9)).Date;

        internal async Task<(string contest, string problem, DateTime time)?>
            SubmitSingleInternal(SourceOrder order, bool force, string cookie, CancellationToken cancellationToken)
        {
            ProblemsSubmission[] submits = [];
            if (!force)
            {
                submits = await StreakService.GetACSubmissionsAsync(cookie, cancellationToken);
                var latest = submits.Latest();
                if (latest != null && IsToday(latest.DateTime))
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
                        submitRes = await StreakService.SubmitSource(source, cookie, true, cancellationToken);
                }
                usedIds.Add(source.Id);
                if (submitRes.HasValue && submitRes.Value.time >= DateTime.SpecifyKind(DateTime.UtcNow.AddHours(9).Date, DateTimeKind.Unspecified))
                    break;
            }

            DataService.DeleteSources(usedIds);
            return submitRes;
        }

        internal async Task<(SavedSource source, bool submitSuccess)[]>
            SubmitInternalParallel(SourceOrder order, string cookie, int paralellNum, CancellationToken cancellationToken)
        {
            var tasks = new List<(SavedSource source, Task<(string contest, string problem, DateTime time)?> submitTask)>();
            foreach (var source in DataService.GetSources(order))
            {
                if (source.CanParse())
                {
                    if (--paralellNum < 0)
                        break;
                    tasks.Add((source, StreakService.SubmitSource(source, cookie, false, cancellationToken)));
                }
            }

            var usedIds = new List<int>();
            var res = new List<(SavedSource source, bool submitSuccess)>();
            foreach (var (source, task) in tasks)
            {
                try
                {
                    await task;
                    usedIds.Add(source.Id);
                    res.Add((source, true));
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "failed");
                    res.Add((source, false));
                }
            }

            BackupInternal();
            DeleteInternal(usedIds);
            return [.. res];
        }

        public IEnumerable<SavedSource> GetSources() => DataService.GetSources(SourceOrder.None);
        public void DeleteInternal(IEnumerable<int> ids) => DataService.DeleteSources(ids);
        public void BackupInternal() => DataService.Backup();
    }
}