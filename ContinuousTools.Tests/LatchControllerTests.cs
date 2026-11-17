using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace ContinuousTools.Tests;

public sealed class LatchControllerTests
{
    [Theory]
    [InlineData("BlockIntegrityScreen")]
    [InlineData("InteractionHintScreen")]
    public void NormalToolHudDoesNotInterruptLatch(string screenName)
        => Assert.False(ContinuousToolsInstaller.ShouldStopForScreen(screenName));

    [Theory]
    [InlineData("TerminalScreen")]
    [InlineData("InventoryScreen")]
    [InlineData("PauseScreen")]
    public void RealScreensStillInterruptLatch(string screenName)
        => Assert.True(ContinuousToolsInstaller.ShouldStopForScreen(screenName));

    [Fact]
    public void NormalClickPassesNativePressAndRelease()
    {
        var events = new List<bool>();
        var latch = NewLatch();
        var tool = new object();

        latch.Input(tool, ToolKind.Drill, true, events.Add);
        latch.Input(tool, ToolKind.Drill, false, events.Add);

        Assert.Equal(new[] { true, false }, events);
    }

    [Fact]
    public void DoubleClickSuppressesSecondReleaseUntilStopClick()
    {
        var events = new List<bool>();
        var latch = NewLatch();
        var tool = new object();

        latch.Input(tool, ToolKind.Welder, true, events.Add);
        latch.Input(tool, ToolKind.Welder, false, events.Add);
        latch.Input(tool, ToolKind.Welder, true, events.Add);
        latch.Input(tool, ToolKind.Welder, false, events.Add);
        Assert.Equal(new[] { true, false, true }, events);

        latch.Input(tool, ToolKind.Welder, true, events.Add);
        latch.Input(tool, ToolKind.Welder, false, events.Add);
        Assert.Equal(new[] { true, false, true, false, false }, events);
    }

    [Fact]
    public void InterruptionForcesReleaseAndDoesNotResume()
    {
        var events = new List<bool>();
        var latch = NewLatch();
        var tool = new object();

        latch.Input(tool, ToolKind.Grinder, true, events.Add);
        latch.Input(tool, ToolKind.Grinder, false, events.Add);
        latch.Input(tool, ToolKind.Grinder, true, events.Add);
        latch.Input(tool, ToolKind.Grinder, false, events.Add);
        latch.Stop("test interruption");

        Assert.Equal(new[] { true, false, true, false }, events);
    }

    [Fact]
    public void DisabledToolNeverLatches()
    {
        var settings = new Settings { EnableDrill = false };
        var latch = new LatchController(settings);
        var events = new List<bool>();
        var tool = new object();

        for (var i = 0; i < 2; i++)
        {
            latch.Input(tool, ToolKind.Drill, true, events.Add);
            latch.Input(tool, ToolKind.Drill, false, events.Add);
        }

        Assert.Equal(new[] { true, false, true, false }, events);
    }

    private static LatchController NewLatch()
        => new(new Settings { DoubleClickWindowMs = 1000 });
}
