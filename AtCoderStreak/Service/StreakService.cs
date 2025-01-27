using AtCoderStreak.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AtCoderStreak.Service
{
    public interface IStreakService
    {
        Task<string?> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
        Task<ProblemsSubmission[]> GetSubmissionsAsync(string cookie, CancellationToken cancellationToken = default);
        Task<ProblemsSubmission[]> GetACSubmissionsAsync(string cookie, CancellationToken cancellationToken = default);

        Task<(string contest, string problem, DateTime time)?> SubmitSource(SavedSource source, string cookie, bool waitResult, CancellationToken cancellationToken = default);
    }
    public class StreakService(IHttpClientFactory clientFactory) : IStreakService
    {
        private readonly AtCoderParser parser = new();


        #region Login
        private const string LoginUrl = "https://atcoder.jp/login";
        public async Task<string?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var csrfToken = await GetLoginCSRFToken(cancellationToken);
            if (csrfToken == null) return null;

            var client = clientFactory.CreateClient("disallowRedirect");

            var res = await client.PostAsync(LoginUrl,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"username", username },
                    {"password", password },
                    {"csrf_token", csrfToken },
                }),
                cancellationToken);


            if (res.Headers?.Location?.OriginalString is string to && !to.StartsWith("/login"))
            {
                var cookieHeaders = res.Headers.FirstOrDefault(p => p.Key.Equals("set-cookie", StringComparison.InvariantCultureIgnoreCase));
                if (cookieHeaders.Key != null)
                {
                    return parser.FilterREVEL_SESSION(cookieHeaders.Value.SelectMany(c => c.Split(';')));
                }
            }
            return null;
        }

        private async Task<string?> GetLoginCSRFToken(CancellationToken cancellationToken = default)
        {
            var client = clientFactory.CreateClient("allowRedirect");
            var res = await client.GetAsync(LoginUrl, cancellationToken);
            return await parser.ParseLoginCSRFToken(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken);
        }
        #endregion

        #region Latest
        const string SubmitResultsUrl = "https://kenkoooo.com/atcoder/atcoder-api/results";
        public async Task<ProblemsSubmission[]> GetSubmissionsAsync(string cookie, CancellationToken cancellationToken = default)
        {
            (_, string userName) = parser.ParseCookie(cookie);
            var query = HttpUtility.ParseQueryString("");
            query.Add("user", userName);
            var uri = new UriBuilder(SubmitResultsUrl)
            {
                Query = query.ToString()
            }.Uri;
            var client = clientFactory.CreateClient("allowRedirect");
            var res = await client.GetAsync(uri, cancellationToken);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"failed: {uri}");
            using var jsonStream = await res.Content.ReadAsStreamAsync(cancellationToken);

            var submissions = await parser.DeserializeProblemsSubmitAsync(jsonStream, cancellationToken)
                ?? throw new InvalidDataException();
            return submissions;
        }

        public async Task<ProblemsSubmission[]> GetACSubmissionsAsync(string cookie, CancellationToken cancellationToken = default)
            => FilterAC(await GetSubmissionsAsync(cookie, cancellationToken));

        internal static ProblemsSubmission[] FilterAC(ProblemsSubmission[] submits)
        {
            var dic = new Dictionary<(string, string), ProblemsSubmission>();
            foreach (var submit in submits)
            {
                if (submit.Result != "AC") continue;
                (string, string) tup;
                if (submit.ContestId is { } cid && submit.ProblemId is { } pid)
                {
                    tup = (submit.ContestId, submit.ProblemId);
                }
                else continue;
                if (dic.TryGetValue(tup, out var ex))
                {
                    if (submit.DateTime < ex.DateTime)
                        dic[tup] = submit;
                }
                else dic[tup] = submit;
            }
            var pss = dic.Values.ToArray();
            Array.Sort(pss, (s1, s2) => s1.DateTime.CompareTo(s2.DateTime));
            return pss;
        }
        #endregion

        public async Task<(string contest, string problem, DateTime time)?>
            SubmitSource(SavedSource source, string cookie, bool waitResult, CancellationToken cancellationToken = default)
        {
            if (!source.TaskUrl.StartsWith("https://atcoder.jp")) return null;
            if (!source.CanParse()) return null;

            var (contest, problem, baseUrl) = source.SubmitInfo();
            (string csrfToken, _) = parser.ParseCookie(cookie);

            var client = clientFactory.CreateClient("allowRedirect");
            HttpRequestMessage req;
            HttpResponseMessage res;
            NameValueCollection query;
            req = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/submit");
            req.Headers.Add("Cookie", cookie);
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"data.TaskScreenName", problem },
                {"data.LanguageId", source.LanguageId},
                {"sourceCode", source.SourceCode},
                {"csrf_token", csrfToken},
            });

            res = await client.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                Console.Error.WriteLine("Request");
                Console.Error.WriteLine(req);
                Console.Error.WriteLine("Response");
                Console.Error.WriteLine(res);
                throw new HttpRequestException($"failed");
            }
            var resContent = await res.Content.ReadAsStringAsync(cancellationToken);
            if (resContent.Contains("href=\"/reset_password\""))
                throw new HttpRequestException($"Require login: {req}");

            // 最古のACを取得
            query = HttpUtility.ParseQueryString("");
            query.Add("orderBy", "created");
            query.Add("f.Task", problem);
            query.Add("f.Status", "AC");
            req = new HttpRequestMessage(HttpMethod.Get, new UriBuilder(baseUrl + "/submissions/me")
            {
                Query = query.ToString()
            }.Uri);
            req.Headers.Add("Cookie", cookie);

            res = await client.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"failed: {req}");
            resContent = await res.Content.ReadAsStringAsync(cancellationToken);
            if (resContent.Contains("href=\"/reset_password\""))
                throw new HttpRequestException($"Require login: {req}");
            var oldestTime = await parser.ParseOldestSubmissionTime(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken);
            if (oldestTime is { } time)
            {
                return (contest, problem, time.DateTime);
            }

            // 最新の提出を取得
            await Task.Delay(500, cancellationToken);
            query = HttpUtility.ParseQueryString("");
            query.Add("orderBy", "created");
            query.Add("f.Task", problem);
            query.Add("desc", "true");
            req = new HttpRequestMessage(HttpMethod.Get, new UriBuilder(baseUrl + "/submissions/me")
            {
                Query = query.ToString()
            }.Uri);
            req.Headers.Add("Cookie", cookie);
            res = await client.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"failed: {req}");
            var subId = await parser.ParseFirstSubmissionId(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken);
            if (subId == null) return null;

            if (!waitResult) return null;
            // 結果が出るまで待機
            var statusUrl = new Uri(baseUrl + $"/submissions/{subId}/status/json");
            var startTime = DateTime.Now;
            while (DateTime.Now - startTime < TimeSpan.FromSeconds(120))
            {
                req = new HttpRequestMessage(HttpMethod.Get, statusUrl);
                req.Headers.Add("Cookie", cookie);
                res = await client.SendAsync(req, cancellationToken);
                if (!res.IsSuccessStatusCode)
                    throw new HttpRequestException($"failed: {req}");
                resContent = await res.Content.ReadAsStringAsync(cancellationToken);
                if (resContent.Contains("href=\"/reset_password\""))
                    throw new HttpRequestException($"Require login: {req}");
                var status = await parser.DeserializeSubmissionDetail(await res.Content.ReadAsStreamAsync(cancellationToken), cancellationToken);
                if (!status.Interval.HasValue)
                {
                    if (status.IsSuccess)
                        return (contest, problem, DateTime.Now);
                    return null;
                }

                await Task.Delay(2000, cancellationToken);
            }

            return null;
        }
    }
}
