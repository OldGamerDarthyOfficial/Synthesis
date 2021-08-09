﻿using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Json;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.Versioning.Query;

namespace Synthesis.Bethesda.Execution.Modules
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(ICheckOrCloneRepo).Assembly)
                .InNamespacesOf(
                    typeof(ICheckOrCloneRepo),
                    typeof(IQueryNewestLibraryVersions),
                    typeof(IProcessRunner),
                    typeof(IWorkingDirectorySubPaths),
                    typeof(IPatcherRun),
                    typeof(IInstalledSdkFollower),
                    typeof(IConsiderPrereleasePreference),
                    typeof(IPatcherNameSanitizer),
                    typeof(ILinesToReflectionConfigsParser),
                    typeof(IBuild))
                .NotInNamespacesOf(typeof(IInstalledSdkFollower))
                .Except<IBuildOutputAccumulator>()
                .AsMatchingInterface();
            
            builder.RegisterAssemblyTypes(typeof(ICheckOrCloneRepo).Assembly)
                .InNamespacesOf(
                    typeof(IInstalledSdkFollower))
                .SingleInstance()
                .AsMatchingInterface();

            builder.RegisterType<BuildOutputAccumulator>()
                .AsImplementedInterfaces()
                .InstancePerRequest();
        }
    }
}