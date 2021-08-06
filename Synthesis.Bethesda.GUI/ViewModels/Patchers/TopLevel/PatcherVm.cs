using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Autofac;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel
{
    public abstract class PatcherVm : ViewModel
    {
        public ILifetimeScope Scope { get; }
        public IPatcherNameVm NameVm { get; }
        private readonly IRemovePatcherFromProfile _Remove;

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        public int InternalID { get; }

        [Reactive]
        public bool IsOn { get; set; } = true;

        public ICommand DeleteCommand { get; }

        public ICommand GoToErrorCommand => NoggogCommand.CreateFromObject(
            objectSource: this.WhenAnyValue(x => x.State.RunnableState),
            canExecute: x => x.Failed,
            execute: x => DisplayedObject = ErrorVM,
            disposable: this.CompositeDisposable);

        public abstract ConfigurationState State { get; }

        [Reactive]
        public ViewModel DisplayedObject { get; set; }

        public ErrorVM ErrorVM { get; }

        public virtual bool IsNameEditable => true;

        public PatcherVm(
            ILifetimeScope scope,
            IPatcherNameVm nameVm,
            IRemovePatcherFromProfile remove,
            IProfileDisplayControllerVm selPatcher,
            IConfirmationPanelControllerVm confirmation,
            IPatcherIdProvider idProvider,
            PatcherSettings? settings)
        {
            Scope = scope;
            Scope.DisposeWith(this);
            NameVm = nameVm;
            _Remove = remove;
            DisplayedObject = this;
            InternalID = idProvider.InternalId;
            ErrorVM = new ErrorVM("Error", backAction: () =>
            {
                DisplayedObject = this;
            });

            _IsSelected = selPatcher.WhenAnyValue(x => x.SelectedObject)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));

            // Set to settings
            IsOn = settings?.On ?? false;

            DeleteCommand = ReactiveCommand.Create(() =>
            {
                confirmation.TargetConfirmation = new ConfirmationActionVm(
                    "Confirm",
                    $"Are you sure you want to delete {NameVm.Name}?",
                    Delete);
            });

            this.WhenAnyValue(x => x.IsSelected)
                .DistinctUntilChanged()
                .Where(x => x)
                .Subscribe(_ =>
                {
                    DisplayedObject = this;
                })
                .DisposeWith(this);

            this.WhenAnyValue(x => x.State.RunnableState)
                .Subscribe(state =>
                {
                    if (state.Failed)
                    {
                        ErrorVM.String = state.Reason;
                    }
                    else
                    {
                        ErrorVM.String = null;
                    }
                })
                .DisposeWith(this);
        }

        public abstract PatcherSettings Save();

        protected void CopyOverSave(PatcherSettings settings)
        {
            settings.On = IsOn;
            settings.Nickname = NameVm.Name;
        }

        public virtual void Delete()
        {
            _Remove.Remove(this);
        }

        public virtual void SuccessfulRunCompleted()
        {
        }

        public virtual void PrepForRun()
        {
        }
    }
}
