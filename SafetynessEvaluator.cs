using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace review_evaluation_tool
{
    public class SafetynessEvaluator
    {
        private readonly GitHttpClient git;

        public SafetynessEvaluator(GitHttpClient git)
        {
            this.git = git;
        }

        public virtual async Task<bool> Check(GitPullRequest pr)
        {
            var tp = pr.Repository.ProjectReference.Name;
            var commits = await git.GetPullRequestCommitsAsync(pr.Repository.Id, pr.PullRequestId);
            var items = new ConcurrentBag<string>();
            Task.WaitAll(commits.Select(async c =>
            {
                // todo: implement paging for the case of many-many changes
                var chgCtr = await git.GetChangesAsync(
                    project: tp, repositoryId: pr.Repository.Id, commitId: c.CommitId);
                chgCtr.Changes.ToList().ForEach(i => items.Add(i.Item.Path));
            }).ToArray());
            //Program.Console_Dump("impacted items", items.OrderBy(i => i.ToLowerInvariant()));
            var safetyRule = new Regex(@"_safety\.", RegexOptions.IgnoreCase);
            return items.Any(i => safetyRule.IsMatch(i));
        }
    }
}
