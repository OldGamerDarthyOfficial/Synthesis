using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Autofac;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel
{
    public abstract class PatcherVm : ViewModel, ISelected
    {
        public ILifetimeScope Scope { get; }
        public IPatcherNameVm NameVm { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        public Guid InternalID { get; }

        [Reactive]
        public bool IsOn { get; set; } = true;

        public ICommand DeleteCommand { get; }

        public abstract ConfigurationState State { get; }

        public virtual bool IsNameEditable => true;

        [Reactive] 
        public GroupVm? Group { get; set; } 
        
        public ErrorDisplayVm ErrorDisplayVm { get; }

        public PatcherVm(
            ILifetimeScope scope,
            IPatcherNameVm nameVm,
            IProfileDisplayControllerVm selPatcher,
            IConfirmationPanelControllerVm confirmation,
            IPatcherIdProvider idProvider,
            PatcherSettings? settings)
        {
            Scope = scope;
            Scope.DisposeWith(this);
            NameVm = nameVm;
            InternalID = idProvider.InternalId;

            _IsSelected = selPatcher.WhenAnyValue(x => x.SelectedObject)
                .Select(x => x == this)
                // Not GuiProperty, as it interacts with drag/drop oddly
                .ToProperty(this, nameof(IsSelected));

            // Set to settings
            IsOn = settings?.On ?? false;

            DeleteCommand = ReactiveCommand.Create(() =>
            {
                confirmation.TargetConfirmation = new ConfirmationActionVm(
                    "Confirm",
                    $"Are you sure you want to delete {NameVm.Name}?",
                    Delete);
            });
            
            ErrorDisplayVm = new ErrorDisplayVm(this, this.WhenAnyValue(x => x.State));
        }

        public abstract PatcherSettings Save();

        protected void CopyOverSave(PatcherSettings settings)
        {
            settings.On = IsOn;
            settings.Nickname = NameVm.Name;
        }

        public virtual void Delete()
        {
            Group?.Remove(this);
            this.Dispose();
        }

        public virtual void SuccessfulRunCompleted()
        {
        }

        public virtual void PrepForRun()
        {
        }

        public override string ToString()
        {
            return NameVm.Name;
        }
    }
}