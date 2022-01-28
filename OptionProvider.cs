using System;
using System.Collections.Generic;
using System.Linq;

namespace review_evaluation_tool
{
    interface IOptionProvider
    {
        string GetParam(string envVarName, string optionName, string defaultValue = null);
        bool IsSpecified(string envVarName, string optionName);
    }

    internal class OptionProvider : IOptionProvider
    {
        private readonly List<string> args;

        public OptionProvider(string[] args)
        {
            this.args = args.Select(a => a.ToLowerInvariant().Trim('-', '/')).ToList();
        }

        public string GetParam(string envVarName, string optionName, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(envVarName) && string.IsNullOrEmpty(optionName))
            {
                throw new Exception("neither an environment variable nor an option name specified");
            }
            if (!string.IsNullOrEmpty(envVarName))
            {
                var env = Environment.GetEnvironmentVariable(envVarName);
                if (!string.IsNullOrEmpty(env))
                {
                    return env;
                }
            }
            if (!string.IsNullOrWhiteSpace(optionName))
            {
                var idx = args.IndexOf(optionName.ToLowerInvariant());
                if (idx >= 0)
                {
                    if (idx < args.Count) return args[idx + 1];
                    throw new Exception($"Argument \"{optionName}\" has no value");
                }
            }
            if (!string.IsNullOrWhiteSpace(defaultValue))
            {
                return defaultValue;
            }
            throw new Exception($"Neither environment variable \"{envVarName}\" nor cmd line argument \"{optionName}\" is available.");
        }

        public bool IsSpecified(string envVarName, string optionName)
        {
            var env = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrWhiteSpace(env))
            {
                if (bool.TryParse(envVarName, out var result))
                {
                    return result;
                }
                throw new Exception($"Environment variable \"{envVarName}\" is not a boolean.");
            }
            var idx = args.IndexOf(optionName.ToLowerInvariant());
            if (idx >= 0)
            {
                return true;
            }
            return false;
        }
    }
}
