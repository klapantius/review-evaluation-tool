using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Linq;
using System.Threading.Tasks;

namespace review_evaluation_tool
{
    public class ReviewerCounter
    {
        private readonly GitHttpClient git;
        private readonly int minCountsRequired;

        public ReviewerCounter(GitHttpClient git, int minCountsRequired)
        {
            this.git = git;
            this.minCountsRequired = minCountsRequired;
        }

        public int MissingReviewers { get; protected set; } = 0;
        public string GetErrorMessage()
        {
            if (MissingReviewers == 0) return "";
            return $"Only {minCountsRequired - MissingReviewers} votes are forseenable. Please add {MissingReviewers} required reviewers to fulfill the safetyness requirement for the number of reviewers.";
        }

        /// <summary>
        /// the minimum number of votes must be ensured (quality doesn't matter)
        /// </summary>
        /// <param name="pr">PullRequest object</param>
        /// <returns>true if everything fine, else false</returns>
        public virtual async Task<bool> Check(GitPullRequest pr)
        {
            var ensuredVotes = pr.Reviewers
                .Count(
                    r => r.IsRequired        // reviewer is either required (thus a vote is enforced)
                    || r.Vote != 0);         // or already has voted anything

            MissingReviewers = minCountsRequired - ensuredVotes;
            return MissingReviewers <= 0;
        }
    }
}
