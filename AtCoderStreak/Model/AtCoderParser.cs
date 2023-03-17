using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AtCoderStreak.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

#pragma warning disable CA1822

namespace AtCoderStreak.Model
{
    public class AtCoderParser
    {
        public (string csrfToken, string userName) ParseCookie(string cookie)
        {
            var rs = FilterREVEL_SESSION(cookie.Split(';')) ?? throw new ArgumentException("invalid cookie", nameof(cookie));
            var prs = HttpUtility.UrlDecode(rs["REVEL_SESSION=".Length..]);
            var ra = prs.Split('\0', StringSplitOptions.RemoveEmptyEntries);

            try
            {
                var csrfToken = ra.Where(s => s.StartsWith("csrf_token:"))
                    .Select(s => s["csrf_token:".Length..])
                    .First();
                var userName = ra.Where(s => s.StartsWith("UserScreenName:"))
                    .Select(s => s["UserScreenName:".Length..])
                    .First();
                return (csrfToken, userName);
            }
            catch (Exception e)
            {
                throw new ArgumentException("invalid cookie", nameof(cookie), e);
            }
        }

        public string? FilterREVEL_SESSION(IEnumerable<string> cookies)
            => cookies.Select(s => s.Trim()).FirstOrDefault(s => s.StartsWith("REVEL_SESSION="));

        public async Task<string?> ParseLoginCSRFToken(Stream stream, CancellationToken cancellationToken = default)
        {
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(stream, cancellationToken);
            return document.GetElementsByName("csrf_token").OfType<IHtmlInputElement>().FirstOrDefault()?.Value;
        }
        public async Task<DateTimeOffset?> ParseOldestSubmissionTime(Stream stream, CancellationToken cancellationToken = default)
        {
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(stream, cancellationToken);
            return document
                .GetElementsByClassName("panel-submission")
                .SelectMany(el => el.GetElementsByClassName("fixtime-second"))
                .Select(el => (DateTimeOffset?)DateTimeOffset.Parse(el.TextContent))
                .DefaultIfEmpty()
                .Min();
        }
        public async Task<string?> ParseFirstSubmissionId(Stream stream, CancellationToken cancellationToken = default)
        {
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(stream, cancellationToken);
            return document
                .GetElementsByClassName("submission-score")
                .Select(el => el.Attributes["data-id"]?.Value)
                .FirstOrDefault(s => s != null);
        }

        public async Task<SubmissionStatus> DeserializeSubmissionDetail(Stream stream, CancellationToken cancellationToken = default)
        {
            var opt = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            };
            var status = await JsonSerializer.DeserializeAsync<SubmissionStatus>(stream, opt, cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException();
            if (status.Html is not null)
            {
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(status.Html, cancellationToken);
                status.IsSuccess = document.GetElementsByClassName("label-success").Any();
            }
            return status;
        }


        public ValueTask<ProblemsSubmission[]?> DeserializeProblemsSubmitAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var opt = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            };
            opt.Converters.Add(new DateTimeLongSerializer());
            return JsonSerializer.DeserializeAsync<ProblemsSubmission[]>(stream, opt, cancellationToken);
        }
    }
}
