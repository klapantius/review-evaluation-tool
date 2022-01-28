using Microsoft.TeamFoundation.Build.WebApi;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Text;

namespace review_evaluation_tool
{
    class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        internal static async Task MainAsync(string[] args)
        {
            var opt = new OptionProvider(args);

            var tp = opt.GetParam("BUILD_TEAMPROJECT", "tp", "syngo.net");
            var buildId = int.Parse(opt.GetParam("BUILD_BUILDID", "buildid", "0"));
            var prId = int.Parse(opt.GetParam(null, "prid", "0"));
            var repoName = string.Empty;

            var tpc = new ConnectionProvider().Connect();
            var buildClient = tpc.GetClient<BuildHttpClient>();
            var git = tpc.GetClient<GitHttpClient>();

            if (buildId > 0)
            {
                var buildData = await buildClient.GetBuildAsync(project: tp, buildId);
                var buildParams = JsonSerializer.Deserialize<Dictionary<string, string>>(buildData.Parameters);
                Console_Dump($"parameters of build {buildData.BuildNumber}", buildParams);

                prId = int.Parse(buildParams["system.pullRequest.pullRequestId"]);
                var repoUri = buildParams["system.pullRequest.sourceRepositoryUri"];
                repoName = repoUri.Split('/').Last();
            }
            else
            {
                if (prId == 0) throw new Exception("Neither a build id nor a PR id is available to start with.");
            }
            Console.WriteLine($"loading reviewer information for PR {prId}");
            var pr = await git.GetPullRequestByIdAsync(project: tp, prId);
            //Console_Dump($"PR {prId}", pr);
            var reviewers = pr.Reviewers;

            Console_Dump("reviewers", reviewers.Select(r => new { r.DisplayName, _ = r.IsRequired ? "required" : "optional", r.Vote }));


            // count required reviewers and their votes

            // count votes from optional reviewers
            //pr.Reviewers.First().Vote == 10

            if (Debugger.IsAttached) Console.ReadKey();
        }

        private static void Console_Dump(string title, object data)
        {
            const int LENGTH = 180;
            Console.WriteLine($"--- {title} {new StringBuilder().Insert(0, "-", LENGTH - 5 - title.Length)}");
            Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine(new StringBuilder().Insert(0, "-", LENGTH));
        }

    }


}