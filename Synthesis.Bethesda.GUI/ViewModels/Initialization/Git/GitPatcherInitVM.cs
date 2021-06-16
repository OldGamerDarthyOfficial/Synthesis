using DynamicData;
using DynamicData.Binding;
using LibGit2Sharp;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive; 
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Input;
using Serilog;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.GUI.Services;

namespace Synthesis.Bethesda.GUI
{
    public class GitPatcherInitVM : PatcherInitVM
    {
        private readonly IPatcherFactory _PatcherFactory;
        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public GitPatcherVM Patcher { get; }

        [Reactive]
        public string Search { get; set; } = string.Empty;

        public IObservableCollection<PatcherStoreListingVM> PatcherRepos { get; }

        [Reactive]
        public ErrorResponse Error { get; private set; } = ErrorResponse.Success;

        public enum TabType
        {
            Browse,
            Input,
        }

        [Reactive]
        public TabType SelectedTab { get; set; } = TabType.Browse;

        public ICommand OpenPopulationInfoCommand { get; }
        public ICommand ClearSearchCommand { get; }

        [Reactive]
        public PatcherStoreListingVM? SelectedPatcher { get; set; }

        [Reactive]
        public bool ShowAll { get; set; }

        private bool _wasAdded = false;

        public GitPatcherInitVM(
            PatcherInitializationVM init,
            ILogger logger,
            IPatcherFactory patcherFactory,
            INavigateTo navigateTo, 
            IProvideRepositoryCheckouts repositoryCheckouts,
            ICheckOrCloneRepo checkOrClone,
            IGuiPaths guiPaths)
            : base(init)
        {
            _PatcherFactory = patcherFactory;
            Patcher = patcherFactory.Get<GitPatcherVM>();

            _CanCompleteConfiguration = this.WhenAnyValue(x => x.Patcher.RepoClonesValid)
                .Select(x => ErrorResponse.Create(x))
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);

            PatcherRepos = Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectTask(async _ =>
                {
                    try
                    {
                        var localRepoPath = checkOrClone.Check(
                            GetResponse<string>.Succeed("https://github.com/Mutagen-Modding/Synthesis.Registry"),
                            guiPaths.RegistryFolder,
                            logger.Error,
                            CancellationToken.None);
                        if (localRepoPath.Failed)
                        {
                            Error = localRepoPath;
                            return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                        }
                        using var repoCheckout = repositoryCheckouts.Get(localRepoPath.Value.Local);
                        var repo = repoCheckout.Repository;
                        repo.Fetch();

                        var master = repo.MainBranch;
                        if (master == null)
                        {
                            Error = ErrorResponse.Fail("Could not find master branch");
                            logger.Error(Error.Reason);
                            return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                        }
                        if (!repo.TryGetBranch($"{master.RemoteName}/{master.FriendlyName}", out var originBranch))
                        {
                            Error = ErrorResponse.Fail("Could not find remote master branch");
                            logger.Error(Error.Reason);
                            return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                        }
                        repo.ResetHard(originBranch.Tip);

                        var listingPath = Path.Combine(repo.WorkingDirectory, Constants.AutomaticListingFileName);
                        if (!File.Exists(listingPath))
                        {
                            Error = ErrorResponse.Fail("Could not locate listing file");
                            logger.Error(Error.Reason);
                            return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                        }
                        var settings = new JsonSerializerOptions();
                        settings.Converters.Add(new JsonStringEnumConverter());
                        var customization = JsonSerializer.Deserialize<MutagenPatchersListing>(File.ReadAllText(listingPath), settings)!;
                        return customization.Repositories
                            .NotNull()
                            .SelectMany(repo =>
                            {
                                var repoVM = new RepositoryStoreListingVM(repo);
                                return repo.Patchers
                                    .Select(p =>
                                    {
                                        return new PatcherStoreListingVM(this, p, repoVM, navigateTo);
                                    });
                            })
                            .AsObservableChangeSet();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error downloading patcher listing");
                        Error = ErrorResponse.Fail(ex);
                    }
                    return Observable.Empty<IChangeSet<PatcherStoreListingVM>>();
                })
                .Switch()
                .Sort(Comparer<PatcherStoreListingVM>.Create((x, y) => x.Name.CompareTo(y.Name)))
                .Filter(this.WhenAnyValue(x => x.ShowAll)
                    .DistinctUntilChanged()
                    .Select(show => new Func<PatcherStoreListingVM, bool>(
                        (p) =>
                        {
                            if (p.Raw.Customization?.Visibility is VisibilityOptions.Visible) return true;
                            else if (p.Raw.Customization?.Visibility is VisibilityOptions.IncludeButHide) return show;
                            else if (p.Raw.Customization?.Visibility is VisibilityOptions.Exclude) return false; // just in case.
                            else return true;
                        })))
                .Filter(this.WhenAnyValue(x => x.Search)
                    .Debounce(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
                    .Select(x => x.Trim())
                    .DistinctUntilChanged()
                    .Select(search =>
                    {
                        if (string.IsNullOrWhiteSpace(search))
                        {
                            return new Func<PatcherStoreListingVM, bool>(_ => true);
                        }
                        return new Func<PatcherStoreListingVM, bool>(
                            (p) =>
                            {
                                if (p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
                                if (p.Raw.Customization?.OneLineDescription?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) return true;
                                return false;
                            });
                    }))
                .ToObservableCollection(this);

            OpenPopulationInfoCommand = ReactiveCommand.Create(() => navigateTo.Navigate(Constants.ListingRepositoryAddress));
            ClearSearchCommand = ReactiveCommand.Create(() => Search = string.Empty);
        }

        public override async IAsyncEnumerable<PatcherVM> Construct()
        {
            _wasAdded = true;
            yield return Patcher;
        }

        public override void Cancel()
        {
            base.Cancel();
            Patcher.Delete();
        }

        public void AddStorePatcher(PatcherStoreListingVM listing)
        {
            var patcher = _PatcherFactory.Get<GitPatcherVM>();
            patcher.RemoteRepoPath = listing.RepoPath;
            patcher.ProjectSubpath = listing.Raw.ProjectPath.Replace('/', '\\');
            Init.AddNewPatchers(patcher.AsEnumerable<PatcherVM>().ToList());
        }

        public override void Dispose()
        {
            base.Dispose();
            if (!_wasAdded)
            {
                Patcher.Dispose();
            }
        }
    }
}
