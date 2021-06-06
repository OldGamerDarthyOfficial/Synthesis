﻿using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI
{
    public interface IProfileDisplayControllerVm
    {
        ViewModel? SelectedObject { get; set; }
    }

    public class ProfileDisplayControllerVm : ViewModel, IProfileDisplayControllerVm
    {
        [Reactive]
        public ViewModel? SelectedObject { get; set; }
    }
}