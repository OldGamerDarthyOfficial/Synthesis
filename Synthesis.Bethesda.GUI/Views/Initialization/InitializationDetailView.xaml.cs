﻿using Noggog.WPF;
using System.Windows.Controls;
using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views
{
    public class InitializationDetailViewBase : NoggogUserControl<PatcherInitVM> { }

    /// <summary>
    /// Interaction logic for InitializationDetailView.xaml
    /// </summary>
    public partial class InitializationDetailView : InitializationDetailViewBase
    {
        public InitializationDetailView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel.Patcher.DisplayName)
                    .BindToStrict(this, x => x.PatcherDetailName.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.Patcher)
                    .BindToStrict(this, x => x.PatcherIconDisplay.DataContext)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel)
                    .BindToStrict(this, x => x.ConfigDetailPane.Content)
                    .DisposeWith(disposable);
            });
        }
    }
}