using Noggog.WPF;
using System;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Windows;
using Synthesis.Bethesda.Execution.Settings;
using System.Reactive.Linq;
using Noggog;

namespace Synthesis.Bethesda.GUI.Views
{
    public class GithubConfigViewBase : NoggogUserControl<GithubPatcherVM> { }

    /// <summary>
    /// Interaction logic for GithubConfigView.xaml
    /// </summary>
    public partial class GithubConfigView : GithubConfigViewBase
    {
        public GithubConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.BindStrict(this.ViewModel, vm => vm.RemoteRepoPath, view => view.RepositoryPath.Text)
                    .DisposeWith(disposable);

                var processing = Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel.RepoValidity),
                        this.WhenAnyValue(x => x.ViewModel.State),
                        (repo, state) => repo.Succeeded && !state.IsHaltingError && state.RunnableState.Failed);

                this.WhenAnyValue(x => x.ViewModel.RepoValidity)
                    .BindError(this.RepositoryPath)
                    .DisposeWith(disposable);

                processing
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.CloningRing.Visibility)
                    .DisposeWith(disposable);

                // Bind project picker
                this.BindStrict(this.ViewModel, vm => vm.ProjectSubpath, view => view.ProjectsPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.OneWayBindStrict(this.ViewModel, vm => vm.AvailableProjects, view => view.ProjectsPickerBox.ItemsSource)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.RepoClonesValid)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, view => view.ProjectsPickerBox.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.RepoClonesValid)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, view => view.ProjectTitle.Visibility)
                    .DisposeWith(disposable);

                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel.RepoClonesValid),
                        this.WhenAnyValue(x => x.ViewModel.SelectedProjectPath.ErrorState),
                        (driver, proj) => driver && proj.Succeeded)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, view => view.VersioningGrid.Visibility)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.PatcherVersioning, view => view.PatcherVersioningTab.SelectedIndex, (e) => (int)e, i => (PatcherVersioningEnum)i)
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.MutagenVersioning, view => view.MutagenVersioningTab.SelectedIndex, (e) => (int)e, i => (MutagenVersioningEnum)i)
                    .DisposeWith(disposable);

                // Bind tag picker
                this.BindStrict(this.ViewModel, vm => vm.TargetTag, view => view.TagPickerBox.SelectedItem)
                    .DisposeWith(disposable);
                this.OneWayBindStrict(this.ViewModel, vm => vm.AvailableTags, view => view.TagPickerBox.ItemsSource)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.TargetCommit, view => view.CommitShaBox.Text)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel.RunnableData),
                        this.WhenAnyValue(x => x.ViewModel.PatcherVersioning),
                        (data, patcher) => data == null && patcher == PatcherVersioningEnum.Commit)
                    .Subscribe(x => this.CommitShaBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.BindStrict(this.ViewModel, vm => vm.TargetBranchName, view => view.BranchNameBox.Text)
                    .DisposeWith(disposable);
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.ViewModel.RunnableData),
                        this.WhenAnyValue(x => x.ViewModel.PatcherVersioning),
                        (data, patcher) => data == null && patcher == PatcherVersioningEnum.Branch)
                    .Subscribe(x => this.BranchNameBox.SetValue(ControlsHelper.InErrorProperty, x))
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.RunnableData)
                    .Select(x => x == null ? string.Empty : x.CommitDate.ToString())
                    .BindToStrict(this, view => view.PatcherVersionDateText.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.RunnableData)
                    .Select(x => x != null ? Visibility.Visible : Visibility.Hidden)
                    .BindToStrict(this, view => view.CommitDateText.Visibility)
                    .DisposeWith(disposable);

                // Bind github open commands
                this.WhenAnyValue(x => x.ViewModel.OpenGithubPageCommand)
                    .BindToStrict(this, x => x.OpenGithubButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.OpenGithubPageToVersionCommand)
                    .BindToStrict(this, x => x.OpenGithubToVersionButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
