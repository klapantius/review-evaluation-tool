using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;

namespace review_evaluation_tool
{
    internal interface IConnectionProvider
    {
        VssConnection Connect();
    }

    class ConnectionProvider : IConnectionProvider
    {
        public static string DefaultTfsCollectionUrl => "https://apollo.healthcare.siemens.com/tfs/ikm.tpc.projects";

        private Uri tpcUri;

        public ConnectionProvider(string tpcUrl = null)
        {
            var url = string.IsNullOrWhiteSpace(tpcUrl) ? DefaultTfsCollectionUrl : tpcUrl;
            tpcUri = new Uri(url);
        }
        public VssConnection Connect()
        {
            VssClientHttpRequestSettings settings = VssClientHttpRequestSettings.Default.Clone();
            settings.SendTimeout = TimeSpan.FromMinutes(5);

            var systemAccessToken = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");

            if (!string.IsNullOrEmpty(systemAccessToken))
            {
                Console.WriteLine($"Connect REST TPC '{tpcUri.AbsoluteUri}' with PAT");

                var credentials = new VssBasicCredential("", systemAccessToken);
                var connection = new VssConnection(tpcUri, credentials, settings);
                Console.WriteLine($"Logged in as {connection.AuthorizedIdentity.DisplayName}");
                return connection;
            }

            // fallback to old implementation for Xaml or command line usage (without build context)
            Console.WriteLine($"Connect REST TPC '{tpcUri.AbsoluteUri}' with Default");
            return new VssConnection(tpcUri, new VssCredentials(), settings);
        }
    }
}