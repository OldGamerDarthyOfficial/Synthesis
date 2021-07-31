﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Cli
{
    public class GetSettingsStyleTests
    {
        [Theory, SynthAutoData]
        public async Task PassesParametersToStartFactory(
            string path,
            bool directExe,
            CancellationToken cancel,
            bool build,
            GetSettingsStyle sut)
        {
            await sut.Get(path, directExe, cancel, build);
            sut.GetRunProcessStartInfoProvider.Received(1).GetStart(path, directExe, new SettingsQuery(), build);
        }

        [Theory, SynthAutoData]
        public async Task PassesStartInfoToRun(
            string path,
            bool directExe,
            CancellationToken cancel,
            bool build,
            ProcessStartInfo startInfo,
            GetSettingsStyle sut)
        {
            sut.GetRunProcessStartInfoProvider.GetStart<SettingsQuery>(default!, default, default!)
                .ReturnsForAnyArgs(startInfo);
            await sut.Get(path, directExe, cancel, build);
            await sut.ProcessRunner.Received(1).RunAndCapture(startInfo, cancel);
        }

        [Theory, SynthAutoData]
        public async Task OpenSettingsResultReturnsOpenResult(
            string path,
            bool directExe,
            CancellationToken cancel,
            bool build,
            GetSettingsStyle sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
                new ProcessRunReturn((int) Codes.OpensForSettings, default!, default!));
            var resp = await sut.Get(path, directExe, cancel, build);
            resp.Style.Should().Be(SettingsStyle.Open);
            resp.Targets.Should().BeEmpty();
        }

        [Theory, SynthAutoData]
        public async Task AutoGenSettingsPassesOutLinesToParser(
            string path,
            bool directExe,
            CancellationToken cancel,
            bool build,
            List<string> outLines,
            GetSettingsStyle sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
                new ProcessRunReturn((int) Codes.AutogeneratedSettingsClass, outLines, default!));
            await sut.Get(path, directExe, cancel, build);
            sut.LinesToConfigs.Received(1).Parse(outLines);
        }

        [Theory, SynthAutoData]
        public async Task AutoGenSettingsReturnsParseResults(
            string path,
            bool directExe,
            CancellationToken cancel,
            bool build,
            ReflectionSettingsConfigs configs,
            GetSettingsStyle sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
                new ProcessRunReturn((int) Codes.AutogeneratedSettingsClass, default!, default!));
            sut.LinesToConfigs.Parse(default!).ReturnsForAnyArgs(configs);
            var resp = await sut.Get(path, directExe, cancel, build);
            resp.Style.Should().Be(SettingsStyle.SpecifiedClass);
            resp.Targets.Should().Equal(configs.Configs);
        }

        [Theory, SynthAutoData]
        public async Task OtherResultsReturnNone(
            string path,
            bool directExe,
            CancellationToken cancel,
            bool build,
            GetSettingsStyle sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
                new ProcessRunReturn((int) Codes.Unsupported, default!, default!));
            var resp = await sut.Get(path, directExe, cancel, build);
            resp.Style.Should().Be(SettingsStyle.None);
            resp.Targets.Should().BeEmpty();
        }
    }
}