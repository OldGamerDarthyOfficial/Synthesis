using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public interface IPipelineSettings
    {
        List<ISynthesisProfile> Profiles { get; set; }
    }

    public class PipelineSettings : IPipelineSettings
    {
        public List<ISynthesisProfile> Profiles { get; set; } = new();
    }
}
