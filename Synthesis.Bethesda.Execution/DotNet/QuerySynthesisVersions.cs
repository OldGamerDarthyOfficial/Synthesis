﻿using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public record SynthVersions(string? MutagenVersion, string? SynthesisVersion);
    
    public interface IQuerySynthesisVersions
    {
        Task<SynthVersions> Query(string projectPath, bool current, bool includePrerelease, CancellationToken cancel);
    }

    public class QuerySynthesisVersions : IQuerySynthesisVersions
    {
        private readonly IQueryNugetListing _QueryNuget;

        public QuerySynthesisVersions(IQueryNugetListing queryNuget)
        {
            _QueryNuget = queryNuget;
        }
        
        public async Task<SynthVersions> Query(
            string projectPath, bool current, bool includePrerelease, CancellationToken cancel)
        {
            string? mutagenVersion = null, synthesisVersion = null;
            var queries = await _QueryNuget.Query(projectPath, outdated: !current, includePrerelease: includePrerelease, cancel: cancel);
            foreach (var item in queries)
            {
                if (item.Package.StartsWith("Mutagen.Bethesda")
                    && !item.Package.EndsWith("Synthesis"))
                {
                    mutagenVersion = current ? item.Resolved : item.Latest;
                }
                if (item.Package.Equals("Mutagen.Bethesda.Synthesis"))
                {
                    synthesisVersion = current ? item.Resolved : item.Latest;
                }
            }
            return new(mutagenVersion, synthesisVersion);
        }
    }
}