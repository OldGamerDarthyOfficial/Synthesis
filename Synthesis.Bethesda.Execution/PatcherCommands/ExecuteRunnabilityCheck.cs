﻿using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Noggog.WorkEngine;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.PatcherCommands;

public interface IExecuteRunnabilityCheck
{
    Task<ErrorResponse> Check(
        string path,
        bool directExe,
        string loadOrderPath,
        FilePath? buildMetaPath,
        CancellationToken cancel);
}

public class ExecuteRunnabilityCheck : IExecuteRunnabilityCheck
{
    private readonly IShortCircuitSettingsProvider _shortCircuitSettingsProvider;
    private readonly IWriteShortCircuitMeta _writeShortCircuitMeta;
    public const int MaxLines = 100;
        
    public IWorkDropoff Dropoff { get; }
    public IGameReleaseContext GameReleaseContext { get; }
    public ISynthesisSubProcessRunner ProcessRunner { get; }
    public IRunProcessStartInfoProvider RunProcessStartInfoProvider { get; }
    public IDataDirectoryProvider DataDirectoryProvider { get; }
    public IBuildMetaFileReader MetaFileReader { get; }
    public IPatcherExtraDataPathProvider ExtraDataPathProvider { get; }

    public ExecuteRunnabilityCheck(
        IGameReleaseContext gameReleaseContext,
        IWorkDropoff workDropoff,
        ISynthesisSubProcessRunner processRunner,
        IDataDirectoryProvider dataDirectoryProvider,
        IBuildMetaFileReader metaFileReader,
        IShortCircuitSettingsProvider shortCircuitSettingsProvider,
        IWriteShortCircuitMeta writeShortCircuitMeta,
        IPatcherExtraDataPathProvider patcherExtraDataPathProvider,
        IRunProcessStartInfoProvider runProcessStartInfoProvider)
    {
        MetaFileReader = metaFileReader;
        _shortCircuitSettingsProvider = shortCircuitSettingsProvider;
        _writeShortCircuitMeta = writeShortCircuitMeta;
        ExtraDataPathProvider = patcherExtraDataPathProvider;
        Dropoff = workDropoff;
        DataDirectoryProvider = dataDirectoryProvider;
        GameReleaseContext = gameReleaseContext;
        ProcessRunner = processRunner;
        RunProcessStartInfoProvider = runProcessStartInfoProvider;
    }

    public async Task<ErrorResponse> Check(
        string path,
        bool directExe,
        string loadOrderPath,
        FilePath? buildMetaPath,
        CancellationToken cancel)
    {
        var meta = buildMetaPath != null ? MetaFileReader.Read(buildMetaPath.Value) : default;

        if (_shortCircuitSettingsProvider.Shortcircuit && meta is { DoesNotHaveRunnability: true }) return ErrorResponse.Success;

        var results = new List<string>();
        void AddResult(string s)
        {
            if (results.Count < 100)
            {
                results.Add(s);
            }
        }

        var checkState = new CheckRunnability()
        {
            DataFolderPath = DataDirectoryProvider.Path,
            GameRelease = GameReleaseContext.Release,
            LoadOrderFilePath = loadOrderPath,
            ExtraDataFolder = ExtraDataPathProvider.Path,
        };

        var result = (Codes)await Dropoff.EnqueueAndWait(() =>
        {
            return ProcessRunner.RunWithCallback(
                RunProcessStartInfoProvider.GetStart(path, directExe, checkState),
                AddResult,
                cancel: cancel);
        }).ConfigureAwait(false);

        if (result == Codes.NotRunnable)
        {
            return ErrorResponse.Fail(string.Join(Environment.NewLine, results));
        }

        if (result == Codes.NotNeeded 
            && meta != null
            && buildMetaPath != null)
        {
            meta.DoesNotHaveRunnability = true;
            _writeShortCircuitMeta.WriteMeta(buildMetaPath, meta);
        }

        // Other error codes are likely the target app just not handling runnability checks, so return as runnable unless
        // explicitly told otherwise with the above error code
        return ErrorResponse.Success;
    }
}