using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

public sealed class TuiTabTests
{
    private static KeyDispatcher CreateDispatcher() => new(KeyMap.CreateDefaultGlobal());

    [Fact]
    public void Constructor_Should_SetActiveMode_ToRootMode()
    {
        // arrange
        var root = new FakeTuiMode();

        // act
        var tab = new TuiTab("Tasks", mnemonic: 'T', root, CreateDispatcher());

        // assert
        Assert.Same(root, tab.RootMode);
        Assert.Same(root, tab.ActiveMode);
    }

    [Fact]
    public void Title_Should_ReadTheFactory_OnEveryAccess()
    {
        // arrange
        var count = 0;
        var tab = new TuiTab(() => $"Mail ({++count})", mnemonic: 'M', new FakeTuiMode(), CreateDispatcher());

        // act
        var first = tab.Title;
        var second = tab.Title;

        // assert: the factory is a live callback, not captured once.
        Assert.Equal("Mail (1)", first);
        Assert.Equal("Mail (2)", second);
    }

    [Fact]
    public void SwitchTo_Should_PushCurrentMode_AndEnterTheNewOne()
    {
        // arrange
        var root = new FakeTuiMode();
        var detail = new FakeTuiMode();
        var tab = new TuiTab("Tasks", mnemonic: 'T', root, CreateDispatcher());

        // act
        tab.SwitchTo(detail, 80, 24);

        // assert
        Assert.Same(detail, tab.ActiveMode);
        Assert.True(detail.EnterCalled);
        Assert.Equal((80, 24), Assert.Single(detail.ResizeCalls));
    }

    [Fact]
    public void SwitchTo_Should_BeNoOp_When_TargetIsAlreadyActive()
    {
        // arrange
        var root = new FakeTuiMode();
        var tab = new TuiTab("Tasks", mnemonic: 'T', root, CreateDispatcher());

        // act
        tab.SwitchTo(root, 80, 24);

        // assert: no redundant resize/enter beyond what a real switch would do.
        Assert.Empty(root.ResizeCalls);
    }

    [Fact]
    public void PopMode_Should_ReturnToThePreviousMode()
    {
        // arrange
        var root = new FakeTuiMode();
        var detail = new FakeTuiMode();
        var tab = new TuiTab("Tasks", mnemonic: 'T', root, CreateDispatcher());
        tab.SwitchTo(detail, 80, 24);

        // act
        var popped = tab.PopMode(80, 24);

        // assert
        Assert.True(popped);
        Assert.Same(root, tab.ActiveMode);
    }

    [Fact]
    public void PopMode_Should_ReturnFalse_When_StackIsEmpty()
    {
        // arrange
        var tab = new TuiTab("Tasks", mnemonic: 'T', new FakeTuiMode(), CreateDispatcher());

        // act
        var popped = tab.PopMode(80, 24);

        // assert
        Assert.False(popped);
    }

    [Fact]
    public void Activate_Should_ResizeAndReEnter_TheCurrentlyActiveMode()
    {
        // arrange
        var root = new FakeTuiMode();
        var tab = new TuiTab("Tasks", mnemonic: 'T', root, CreateDispatcher());

        // act
        tab.Activate(100, 30);

        // assert
        Assert.Equal((100, 30), Assert.Single(root.ResizeCalls));
        Assert.True(root.EnterCalled);
    }
}
