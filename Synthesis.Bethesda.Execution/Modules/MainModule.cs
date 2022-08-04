﻿using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.Builder.Transient;
using Synthesis.Bethesda.Execution.DotNet.Singleton;
using Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;
using Synthesis.Bethesda.Execution.FileAssociations;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings.Calculators;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.Versioning.Query;
using Noggog.WorkEngine;
using Synthesis.Bethesda.Execution.Startup;

namespace Synthesis.Bethesda.Execution.Modules;

public class MainModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(ISynthesisSubProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(IQueryNewestLibraryVersions),
                typeof(ISynthesisSubProcessRunner),
                typeof(IWorkingDirectorySubPaths),
                typeof(IPatcherRun),
                typeof(IIsApplicableErrorLine),
                typeof(IInstalledSdkFollower),
                typeof(BuildCoreCalculator),
                typeof(IConsiderPrereleasePreference),
                typeof(IPatcherNameSanitizer),
                typeof(ILinesToReflectionConfigsParser),
                typeof(INugetErrorSolution),
                typeof(ExportGitAddFile),
                typeof(IBuildOutputAccumulator),
                typeof(IProjectRunProcessStartInfoProvider))
            .NotInNamespacesOf(
                typeof(IInstalledSdkFollower),
                typeof(IBuildOutputAccumulator))
            .TypicalRegistrations()
            .AsSelf();
        
        builder.RegisterAssemblyTypes(typeof(AddWorkConsumer).Assembly)
            .InNamespacesOf(
                typeof(AddWorkConsumer))
            .SingleInstance()
            .AsImplementedInterfaces();
            
        builder.RegisterAssemblyTypes(typeof(IWorkDropoff).Assembly)
            .InNamespacesOf(
                typeof(IWorkDropoff))
            .SingleInstance()
            .AsImplementedInterfaces();
            
        builder.RegisterAssemblyTypes(typeof(ISynthesisSubProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(IBuildOutputAccumulator))
            .InstancePerDependency()
            .TypicalRegistrations();
            
        builder.RegisterAssemblyTypes(typeof(ISynthesisSubProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(IExecuteRun))
            .InstancePerMatchingLifetimeScope(LifetimeScopes.RunNickname)
            .TypicalRegistrations();
            
        builder.RegisterAssemblyTypes(typeof(ISynthesisSubProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(IInstalledSdkFollower))
            .SingleInstance()
            .AsMatchingInterface();
            
        builder.RegisterAssemblyTypes(typeof(ISynthesisSubProcessRunner).Assembly)
            .InNamespacesOf(
                typeof(ICheckOrCloneRepo))
            .SingleInstance()
            .AsMatchingInterface();
    }
}