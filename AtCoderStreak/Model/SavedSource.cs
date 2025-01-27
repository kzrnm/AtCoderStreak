using System;

namespace AtCoderStreak.Model
{
    public record SavedSource(int Id, string TaskUrl, string LanguageId, int Priority, string SourceCode)
    {
        public override string ToString() => $"{Id}(priority:{Priority}): {TaskUrl}";

        private bool? parseUrlResult;
        private string contest = "";
        private string problem = "";
        private string baseUrl = "";
        public bool CanParse()
        {
            bool ParseUrl()
            {
                var sp1 = TaskUrl.Split("/tasks/", StringSplitOptions.RemoveEmptyEntries);
                if (sp1.Length != 2) return false;
                baseUrl = sp1[0];
                var sp2 = baseUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);

                contest = sp2[^1];
                problem = sp1[1];
                return true;
            }
            return parseUrlResult ??= ParseUrl();
        }
        public (string contest, string problem, string baseUrl) SubmitInfo()
        {
            if (CanParse()) return (contest, problem, baseUrl);
            throw new InvalidOperationException("failed to parse TaskUrl");
        }
    }
}
