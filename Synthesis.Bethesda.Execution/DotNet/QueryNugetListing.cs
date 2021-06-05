﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution
{
    public record NugetListingQuery(string Package, string Requested, string Resolved, string Latest);
    
    public interface IQueryNugetListing
    {
        Task<IEnumerable<NugetListingQuery>> Query(
            string projectPath,
            bool outdated,
            bool includePrerelease,
            CancellationToken cancel);
    }

    public class QueryNugetListing : IQueryNugetListing
    {
        public async Task<IEnumerable<NugetListingQuery>> Query(string projectPath, bool outdated, bool includePrerelease, CancellationToken cancel)
        {
            // Run restore first
            {
                using var restore = ProcessWrapper.Create(
                    new ProcessStartInfo("dotnet", $"restore \"{projectPath}\""),
                    cancel: cancel);
                await restore.Run();
            }

            bool on = false;
            List<string> lines = new();
            List<string> errors = new();
            using var process = ProcessWrapper.Create(
                new ProcessStartInfo("dotnet", $"list \"{projectPath}\" package{(outdated ? " --outdated" : null)}{(includePrerelease ? " --include-prerelease" : null)}"),
                cancel: cancel);
            using var outSub = process.Output.Subscribe(s =>
            {
                if (s.Contains("Top-level Package"))
                {
                    @on = true;
                    return;
                }
                if (!@on) return;
                lines.Add(s);
            });
            using var errSub = process.Error.Subscribe(s => errors.Add(s));
            var result = await process.Run();
            if (errors.Count > 0)
            {
                throw new ArgumentException($"Error while retrieving nuget listings: \n{string.Join("\n", errors)}");
            }

            var ret = new List<NugetListingQuery>();
            foreach (var line in lines)
            {
                if (!DotNetCommands.TryParseLibraryLine(
                    line, 
                    out var package,
                    out var requested, 
                    out var resolved, 
                    out var latest))
                {
                    continue;
                }
                ret.Add(new(package, requested, resolved, latest));
            }
            return ret;
        }
    }
}