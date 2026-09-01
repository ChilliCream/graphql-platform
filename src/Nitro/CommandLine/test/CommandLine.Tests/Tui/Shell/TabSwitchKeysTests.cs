using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Shell;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

public sealed class TabSwitchKeysTests
{
    [Fact]
    public void Resolve_Should_ReturnMinusOne_ForOpenBracket()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.Oem4, ConsoleModifiers.None, '[');

        // act
        var delta = TabSwitchKeys.Resolve(chord);

        // assert
        Assert.Equal(-1, delta);
    }

    [Fact]
    public void Resolve_Should_ReturnPlusOne_ForCloseBracket()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.Oem6, ConsoleModifiers.None, ']');

        // act
        var delta = TabSwitchKeys.Resolve(chord);

        // assert
        Assert.Equal(1, delta);
    }

    [Fact]
    public void Resolve_Should_ReturnMinusOne_ForOpenBracket_WithNoConsoleKey()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.None, ConsoleModifiers.None, '[');

        // act
        var delta = TabSwitchKeys.Resolve(chord);

        // assert
        Assert.Equal(-1, delta);
    }

    [Fact]
    public void Resolve_Should_ReturnPlusOne_ForCloseBracket_WithNoConsoleKey()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.None, ConsoleModifiers.None, ']');

        // act
        var delta = TabSwitchKeys.Resolve(chord);

        // assert
        Assert.Equal(1, delta);
    }

    [Fact]
    public void Resolve_Should_ReturnNull_ForOpenBracket_WithAModifier()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.Oem4, ConsoleModifiers.Alt, '[');

        // act
        var delta = TabSwitchKeys.Resolve(chord);

        // assert
        Assert.Null(delta);
    }

    [Fact]
    public void Resolve_Should_ReturnNull_ForAnUnrelatedChord()
    {
        // arrange
        var chord = new KeyChord(ConsoleKey.J, ConsoleModifiers.None, 'j');

        // act
        var delta = TabSwitchKeys.Resolve(chord);

        // assert
        Assert.Null(delta);
    }

    [Fact]
    public void Resolve_Should_NotCollide_WithTheGlobalTaskKeyMap()
    {
        // arrange
        var keyMap = KeyMap.CreateDefaultGlobal();
        var previous = new KeyChord(ConsoleKey.Oem4, ConsoleModifiers.None, '[');
        var next = new KeyChord(ConsoleKey.Oem6, ConsoleModifiers.None, ']');

        // act
        var previousBound = keyMap.TryResolve(previous, out _);
        var nextBound = keyMap.TryResolve(next, out _);

        // assert
        Assert.False(previousBound);
        Assert.False(nextBound);
    }

    [Fact]
    public void Resolve_Should_NotCollide_WithTheMailKeyMap()
    {
        // arrange
        var keyMap = MailKeyMap.CreateDefault();
        var previous = new KeyChord(ConsoleKey.Oem4, ConsoleModifiers.None, '[');
        var next = new KeyChord(ConsoleKey.Oem6, ConsoleModifiers.None, ']');

        // act
        var previousBound = keyMap.TryResolve(previous, out _);
        var nextBound = keyMap.TryResolve(next, out _);

        // assert
        Assert.False(previousBound);
        Assert.False(nextBound);
    }

    private static TuiTab Tab(string title, char mnemonic) =>
        new(title, mnemonic, new FakeTuiMode(), new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

    [Fact]
    public void ResolveMnemonic_Should_ReturnTheMatchingTabsIndex_ForShiftPlusItsMnemonic()
    {
        // arrange
        var tabs = new[] { Tab("Tasks", 'T'), Tab("Mail", 'M'), Tab("Agents", 'A') };
        var chord = new KeyChord(ConsoleKey.A, ConsoleModifiers.Shift, 'A');

        // act
        var index = TabSwitchKeys.ResolveMnemonic(chord, tabs);

        // assert
        Assert.Equal(2, index);
    }

    [Fact]
    public void ResolveMnemonic_Should_MatchCaseInsensitively()
    {
        // arrange: a terminal is expected to report the shifted letter
        // already uppercase, but the match itself does not depend on that.
        var tabs = new[] { Tab("Tasks", 'T') };
        var chord = new KeyChord(ConsoleKey.T, ConsoleModifiers.Shift, 't');

        // act
        var index = TabSwitchKeys.ResolveMnemonic(chord, tabs);

        // assert
        Assert.Equal(0, index);
    }

    [Fact]
    public void ResolveMnemonic_Should_ReturnNull_When_ModifiersAreNotExactlyShift()
    {
        // arrange
        var tabs = new[] { Tab("Tasks", 'T') };
        var chord = new KeyChord(ConsoleKey.T, ConsoleModifiers.None, 't');

        // act
        var index = TabSwitchKeys.ResolveMnemonic(chord, tabs);

        // assert
        Assert.Null(index);
    }

    [Fact]
    public void ResolveMnemonic_Should_ReturnNull_When_NoHostedTabClaimsTheLetter()
    {
        // arrange
        var tabs = new[] { Tab("Tasks", 'T'), Tab("Mail", 'M') };
        var chord = new KeyChord(ConsoleKey.A, ConsoleModifiers.Shift, 'A');

        // act
        var index = TabSwitchKeys.ResolveMnemonic(chord, tabs);

        // assert
        Assert.Null(index);
    }
}
