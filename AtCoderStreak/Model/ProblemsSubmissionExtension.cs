using System;
using System.Collections.Generic;

namespace AtCoderStreak.Model
{
    public static class ProblemsSubmissionExtension
    {
        public static ProblemsSubmission? Latest(this IEnumerable<ProblemsSubmission> submits)
        {
            var maxTime = DateTime.MinValue;
            ProblemsSubmission? max = null;
            foreach (var s in submits)
            {
                if (maxTime < s.DateTime)
                {
                    max = s;
                    maxTime = s.DateTime;
                }
            }
            return max;
        }
    }
}
