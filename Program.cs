using Microsoft.TeamFoundation.Build.WebApi;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Text;

namespace review_evaluation_tool
{
    class Program
    {
        public static int Main(string[] args)
        {
            var exitCode = MainAsync(args).GetAwaiter().GetResult();
            if (Debugger.IsAttached) Console.ReadKey();
            return exitCode;
        }

        internal static async Task<int> MainAsync(string[] args)
        {
            var opt = new OptionProvider(args);

            var tp = opt.GetParam("SYSTEM_TEAMPROJECT", "tp", "syngo.net");
            var buildId = int.Parse(opt.GetParam("BUILD_BUILDID", "buildid", "0"));
            var prId = int.Parse(opt.GetParam(null, "prid", "0"));

            var tpc = new ConnectionProvider().Connect();
            var buildClient = tpc.GetClient<BuildHttpClient>();
            var git = tpc.GetClient<GitHttpClient>();

            if (buildId > 0)
            {
                var buildData = await buildClient.GetBuildAsync(project: tp, buildId);
                var buildParams = JsonSerializer.Deserialize<Dictionary<string, string>>(buildData.Parameters);
                Console_Dump($"parameters of build {buildData.BuildNumber}", buildParams);
                prId = int.Parse(buildParams["system.pullRequest.pullRequestId"]);
            }
            // at this point we should have a PR id
            if (prId == 0) throw new Exception("No PR could be identified.");

            Console.WriteLine($"loading PR {prId}");
            var pr = await git.GetPullRequestByIdAsync(project: tp, prId);
            //Console_Dump($"PR {prId}", pr);

            var isSafety = await new SafetynessEvaluator(git).Check(pr);
            Console.WriteLine($"safety relevant: {isSafety}");
            if (!isSafety) return 0;

            var rc = new ReviewerCounter(git, 2);
            if (await rc.Check(pr))
            {
                Console.WriteLine("Minimum number of reviewers is ensured.");
                return 0;
            }
            Console.WriteLine($"ERROR: {rc.GetErrorMessage()}");

            // the minimum number of reviewers cannot be set for a particular PR,
            // because that depends on the branch policy.
            // But it would be possible to add required reviewers (even a group)
            // to the given PR.

            // todo: let this exe run by a build triggered by branch policy
            // - what changes trigger the build to run again?
            // - can we achieve that, the build runs after each vote?

            // - the error message can be handed over in a file or over the pipe (stderr or stdout)
            // - another option is to emit the error as a vso command
            // - further it is possible to add a new comment thread to the PR with this message

            await git.CreateThreadAsync(new GitPullRequestCommentThread
            {
                Comments = new[]
                {
                    new Comment
                    {
                        Content = rc.GetErrorMessage()
                    }
                },
                Status = CommentThreadStatus.Active
            }, pr.Repository.Id, prId);

            return 1; // this will let the build task fail
        }

        internal static void Console_Dump(string title, object data)
        {
            const int LENGTH = 180;
            Console.WriteLine($"--- {title} {new StringBuilder().Insert(0, "-", LENGTH - 5 - title.Length)}");
            Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine(new StringBuilder().Insert(0, "-", LENGTH));
        }

    }


}