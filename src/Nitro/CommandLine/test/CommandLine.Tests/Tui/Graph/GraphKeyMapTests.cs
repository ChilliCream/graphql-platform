using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph;

public sealed class GraphKeyMapTests
{
    [Fact]
    public void CreateDefault_Should_MapEveryGraphActionToAUniqueUnmodifiedChord()
    {
        // arrange
        var keyMap = GraphKeyMap.CreateDefault();

        // act
        var messages = GraphKeyMap.Chords
            .Select(chord =>
            {
                keyMap.TryResolve(chord, out var message);
                return message?.GetType().Name ?? "<unresolved>";
            })
            .ToArray();

        // assert
        Assert.Equal(
            [
                nameof(TuiMessage.ToggleGraphProjection),
                nameof(TuiMessage.ToggleGraphCompact),
                nameof(TuiMessage.ToggleGraphParentChild),
                nameof(TuiMessage.ToggleGraphClosed),
                nameof(TuiMessage.CollapseSelectedGraphEpic),
                nameof(TuiMessage.ExpandSelectedGraphEpic),
                nameof(TuiMessage.CollapseAllGraphEpics),
                nameof(TuiMessage.ExpandAllGraphEpics)
            ],
            messages);
    }

    [Fact]
    public void CreateDefault_Should_NotCollideWithGlobalTabSwitchOrMnemonicChords()
    {
        // arrange
        var global = KeyMap.CreateDefaultGlobal();
        const string tabMnemonics = "TMAEG";

        // act
        var collisions = GraphKeyMap.Chords
            .Where(chord =>
                chord.Modifiers != ConsoleModifiers.None
                || global.TryResolve(chord, out _)
                || TabSwitchKeys.Resolve(chord) is not null
                || tabMnemonics.Contains(char.ToUpperInvariant(chord.KeyChar)))
            .ToArray();

        // assert
        Assert.Empty(collisions);
    }

    [Fact]
    public void KeyDispatcher_Should_ResolveGraphBindingsBeforeGlobalFallback()
    {
        // arrange
        var dispatcher = new KeyDispatcher(KeyMap.CreateDefaultGlobal());
        var graph = GraphKeyMap.CreateDefault();

        // act
        var projection = dispatcher.Dispatch(new ConsoleKeyInfo('v', ConsoleKey.V, false, false, false), graph);
        var refresh = dispatcher.Dispatch(new ConsoleKeyInfo('r', ConsoleKey.R, false, false, false), graph);

        // assert
        Assert.IsType<TuiMessage.ToggleGraphProjection>(projection);
        Assert.IsType<TuiMessage.RefreshRequested>(refresh);
    }
}
