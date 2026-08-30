using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

public sealed class ToasterTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.UnixEpoch;

    private static string RenderToText(Toaster toaster)
    {
        var renderable = toaster.Render();
        if (renderable is null)
        {
            return string.Empty;
        }

        var console = new TestConsole().Width(80);
        console.Write(renderable);
        return console.Output;
    }

    [Fact]
    public void Render_Should_ReturnNull_When_NoToastQueued()
    {
        // arrange
        var toaster = new Toaster();

        // act
        var renderable = toaster.Render();

        // assert
        Assert.Null(renderable);
    }

    [Fact]
    public void Enqueue_Should_ShowImmediately_When_NoCurrentToast()
    {
        // arrange
        var toaster = new Toaster();

        // act
        toaster.Enqueue("saved", ToastStyle.Success, Start);

        // assert
        Assert.Contains("saved", RenderToText(toaster));
    }

    [Fact]
    public void Enqueue_Should_QueueSecondToast_When_ToastAlreadyShowing()
    {
        // arrange
        var toaster = new Toaster();
        toaster.Enqueue("first", ToastStyle.Info, Start);

        // act
        toaster.Enqueue("second", ToastStyle.Info, Start);

        // assert
        var text = RenderToText(toaster);
        Assert.Contains("first", text);
        Assert.DoesNotContain("second", text);
    }

    [Fact]
    public void Tick_Should_ReturnFalse_When_DurationNotElapsed()
    {
        // arrange
        var toaster = new Toaster();
        toaster.Enqueue("saved", ToastStyle.Success, Start);

        // act
        var dirty = toaster.Tick(Start + TimeSpan.FromSeconds(1));

        // assert
        Assert.False(dirty);
        Assert.Contains("saved", RenderToText(toaster));
    }

    [Fact]
    public void Tick_Should_AdvanceToQueuedToast_When_DurationElapsed()
    {
        // arrange
        var toaster = new Toaster();
        toaster.Enqueue("first", ToastStyle.Info, Start);
        toaster.Enqueue("second", ToastStyle.Info, Start);

        // act
        var dirty = toaster.Tick(Start + Toaster.Duration);

        // assert
        Assert.True(dirty);
        var text = RenderToText(toaster);
        Assert.Contains("second", text);
        Assert.DoesNotContain("first", text);
    }

    [Fact]
    public void Tick_Should_ClearCurrentToast_When_DurationElapsedAndNoneQueued()
    {
        // arrange
        var toaster = new Toaster();
        toaster.Enqueue("saved", ToastStyle.Success, Start);

        // act
        var dirty = toaster.Tick(Start + Toaster.Duration);

        // assert
        Assert.True(dirty);
        Assert.Null(toaster.Render());
    }

    [Fact]
    public void Tick_Should_ReturnFalse_When_NoToastShowing()
    {
        // arrange
        var toaster = new Toaster();

        // act
        var dirty = toaster.Tick(Start);

        // assert
        Assert.False(dirty);
    }

    [Fact]
    public void Render_Should_IncludeStyleIcon_When_Success()
    {
        AssertStyleIcon(ToastStyle.Success, "✔");
    }

    [Fact]
    public void Render_Should_IncludeStyleIcon_When_Warn()
    {
        AssertStyleIcon(ToastStyle.Warn, "!");
    }

    [Fact]
    public void Render_Should_IncludeStyleIcon_When_Error()
    {
        AssertStyleIcon(ToastStyle.Error, "✘");
    }

    [Fact]
    public void Render_Should_IncludeStyleIcon_When_Info()
    {
        AssertStyleIcon(ToastStyle.Info, "i");
    }

    private static void AssertStyleIcon(ToastStyle style, string icon)
    {
        // arrange
        var toaster = new Toaster();
        toaster.Enqueue("message", style, Start);

        // act
        var text = RenderToText(toaster);

        // assert
        Assert.Contains(icon, text);
    }
}
