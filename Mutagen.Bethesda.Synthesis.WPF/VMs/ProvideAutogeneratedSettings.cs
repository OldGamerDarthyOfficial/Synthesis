﻿using System;
using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using ReactiveUI;
using Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public interface IProvideAutogeneratedSettings
    {
        AutogeneratedSettingsVm Get(
            SettingsConfiguration config, 
            string projPath, 
            IObservable<IChangeSet<IModListingGetter>> loadOrder, 
            IObservable<ILinkCache?> linkCache);
    }

    public class ProvideAutogeneratedSettings : IProvideAutogeneratedSettings
    {
        private readonly IProvideReflectionSettingsBundle _ProvideBundle;

        public ProvideAutogeneratedSettings(
            IProvideReflectionSettingsBundle provideBundle)
        {
            _ProvideBundle = provideBundle;
        }
        
        public AutogeneratedSettingsVm Get(
            SettingsConfiguration config, 
            string projPath, 
            IObservable<IChangeSet<IModListingGetter>> loadOrder, 
            IObservable<ILinkCache?> linkCache)
        {
            return new AutogeneratedSettingsVm(
                config, projPath, loadOrder.ObserveOn(RxApp.MainThreadScheduler), linkCache,
                _ProvideBundle);
        }
    }
}