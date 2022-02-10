﻿using System.IO;
using System.IO.Abstractions;
using LibGit2Sharp;
using Noggog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.Settings;

public class BackupSettings : IStartupTask
{
    private readonly IFileSystem _fileSystem;
    private readonly IInitRepository _initRepository;
    private readonly IProvideRepositoryCheckouts _repositoryCheckouts;

    private DirectoryPath RepoDirectory => Directory.GetCurrentDirectory();
    private FilePath GitIgnorePath => Path.Combine(RepoDirectory, ".gitignore");
    private FilePath PipelineSettings => Path.Combine(RepoDirectory, "PipelineSettings.json");
    private FilePath GuiSettings => Path.Combine(RepoDirectory, "GuiSettings.json");
    
    public BackupSettings(
        IFileSystem fileSystem,
        IInitRepository initRepository,
        IProvideRepositoryCheckouts repositoryCheckouts)
    {
        _fileSystem = fileSystem;
        _initRepository = initRepository;
        _repositoryCheckouts = repositoryCheckouts;
    }
    
    public void Start()
    {
        _initRepository.Init(RepoDirectory);
        CreateGitIgnore();
        using var repo = _repositoryCheckouts.Get(RepoDirectory);
        StageIfExists(GitIgnorePath, repo.Repository);
        StageIfExists(PipelineSettings, repo.Repository);
        StageIfExists(GuiSettings, repo.Repository);
        try
        {
            repo.Repository.Commit("Settings changed");
        }
        catch (EmptyCommitException)
        {
        }
    }

    private void CreateGitIgnore()
    {
        using var file = new StreamWriter(
            _fileSystem.File.Open(GitIgnorePath, FileMode.Create, FileAccess.Write));
        file.WriteLine("*");
        file.WriteLine("!.gitignore");
        file.WriteLine("!PipelineSettings.json");
        file.WriteLine("!GuiSettings.json");
    }

    private void StageIfExists(FilePath path, IGitRepository repo)
    {
        if (!_fileSystem.File.Exists(path)) return;
        repo.Stage(path);
    }
}