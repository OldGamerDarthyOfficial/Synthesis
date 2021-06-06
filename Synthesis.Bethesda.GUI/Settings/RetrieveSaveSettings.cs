﻿using System;
using System.Reactive.Subjects;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.Settings
{
    public interface IRetrieveSaveSettings
    {
        void Retrieve(out SynthesisGuiSettings gui, out PipelineSettings pipe);
    }

    public interface ISaveSignal
    {
        IObservable<(SynthesisGuiSettings Gui, PipelineSettings Pipe)> Saving { get; }
    }

    public class RetrieveSaveSettings : IRetrieveSaveSettings, ISaveSignal
    {
        private readonly Subject<(SynthesisGuiSettings Gui, PipelineSettings Pipe)> _Save = new();
        public IObservable<(SynthesisGuiSettings Gui, PipelineSettings Pipe)> Saving => _Save;
        
        public void Retrieve(out SynthesisGuiSettings gui, out PipelineSettings pipe)
        {
            gui = new();
            pipe = new();
            _Save.OnNext((gui, pipe));
        }
    }
}