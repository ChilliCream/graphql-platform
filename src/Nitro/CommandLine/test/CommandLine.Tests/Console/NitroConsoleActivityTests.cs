using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Console;

public sealed class NitroConsoleActivityTests
{
    private static (INitroConsole Console, StringWriter Writer) CreateConsole(
        int width = Constants.DefaultPrintWidth)
    {
        var writer = new StringWriter();

        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(writer);
        outConsole.Profile.Width = width;
        outConsole.Profile.Capabilities.Interactive = false;

        var errConsole = new TestConsole();

        var envProvider = new Mock<IEnvironmentVariableProvider>();
        envProvider
            .Setup(x => x.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns((string?)null);

        var console = new NitroConsole(
            outConsole,
            errConsole,
            envProvider.Object,
            new SnapshotActivitySinkFactory());
        return (console, writer);
    }

    private static string GetOutput(StringWriter writer)
    {
        return writer.ToString().TrimEnd();
    }

    [Fact]
    public async Task Success_Should_WriteCheckGlyph_When_ActivityCompletes()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task Fail_Should_WriteCrossGlyph_When_CalledWithMessage()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            activity.Fail("Something went wrong");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            └── ✕ Something went wrong
            """);
    }

    [Fact]
    public async Task FailAllAsync_Should_WriteCrossGlyphWithDetails_When_CalledWithRenderable()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            await activity.FailAllAsync(new Text("Error detail line 1\nError detail line 2"));
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            └── ✕ Work failed
                Error detail line 1
                Error detail line 2
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_WriteTreeStructure_When_ChildSucceeds()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child step", "Child failed"))
            {
                child.Success("Child done");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child step
            │   └── ✓ Child done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_WriteTreeStructure_When_ChildFails()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child step", "Child failed"))
            {
                child.Fail("Child error");
            }

            activity.Fail("Root error");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child step
            │   └── ✕ Child error
            └── ✕ Root error
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_WriteNestedTreeStructure_When_GrandchildSucceeds()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                await using (var grandchild = child.StartChildActivity("Grandchild", "Grandchild failed"))
                {
                    grandchild.Success("Grandchild done");
                }

                child.Success("Child done");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   ├── Grandchild
            │   │   └── ✓ Grandchild done
            │   └── ✓ Child done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task FailAllAsync_Should_PropagateFailure_When_ChildDisposesWithoutCompletion()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                // child is not completed — DisposeAsync will call FailAllAsync
            }

            // root is already failed via FailAllAsync, so this is a no-op
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   └── ✕ Child failed
            └── ✕ Root failed
            """);
    }

    [Fact]
    public async Task Update_Should_BeIgnored_When_CalledAfterSuccess()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            activity.Success("Done");
            activity.Update("This should be ignored");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task Update_Should_WriteMultipleUpdates_When_CalledWithDifferentKinds()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            activity.Update("Regular update");
            activity.Update("Warning update", ActivityUpdateKind.Warning);
            activity.Update("Waiting update", ActivityUpdateKind.Waiting);
            activity.Update("Success update", ActivityUpdateKind.Success);
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            ├── Regular update
            ├── ! Warning update
            ├── ⏳ Waiting update
            ├── ✓ Success update
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task Warning_Should_WriteExclamationGlyph_When_ActivityCompletes()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            activity.Warning("Something is off");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            └── ! Something is off
            """);
    }

    [Fact]
    public async Task Start_Should_WrapTitle_When_TitleExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity(
            "This is a very long root title that should wrap",
            "Failed"))
        {
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            This is a very long root
            │ title that should wrap
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task Update_Should_WrapMessage_When_MessageExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity( "Root", "Failed"))
        {
            activity.Update("This update message is long enough to wrap at narrow width");
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── This update message is
            │   long enough to wrap at
            │   narrow width
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task Success_Should_WrapMessage_When_MessageExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity( "Root", "Failed"))
        {
            activity.Success("This success message is long enough to wrap at narrow width");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            └── ✓ This success message
                  is long enough to wrap
                  at narrow width
            """);
    }

    [Fact]
    public async Task Fail_Should_WrapMessage_When_MessageExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity( "Root", "Failed"))
        {
            activity.Fail("This failure message is long enough to wrap at narrow width");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            └── ✕ This failure message
                  is long enough to wrap
                  at narrow width
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_WrapTitle_When_TitleExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity( "Root", "Failed"))
        {
            await using (var child = activity.StartChildActivity(
                "This child title is long enough to wrap",
                "Child failed"))
            {
                child.Success("Done");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── This child title is long
            │   enough to wrap
            │   └── ✓ Done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task ChildUpdate_Should_WrapMessage_When_MessageExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity( "Root", "Failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                child.Update("This child update is long enough to wrap");
                child.Success("Done");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   ├── This child update is
            │   │   long enough to wrap
            │   └── ✓ Done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task ChildSuccess_Should_WrapMessage_When_MessageExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity( "Root", "Failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                child.Success("This child success message wraps");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   └── ✓ This child success
            │         message wraps
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task ChildFail_Should_WrapMessage_When_MessageExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity( "Root", "Failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                child.Fail("This child failure message wraps");
            }

            activity.Fail("Root error");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   └── ✕ This child failure
            │         message wraps
            └── ✕ Root error
            """);
    }

    [Fact]
    public async Task Update_Should_WriteDetails_When_CalledWithRenderable()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            activity.Update("Status", details: new Text("Detail line 1\nDetail line 2"));
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            ├── Status
            │   Detail line 1
            │   Detail line 2
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task ChildUpdate_Should_WriteDetails_When_CalledWithRenderable()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                child.Update("Status", details: new Text("Detail line 1\nDetail line 2"));
                child.Success("Child done");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   ├── Status
            │   │   Detail line 1
            │   │   Detail line 2
            │   └── ✓ Child done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task ChildFailAllAsync_Should_WriteCrossGlyphWithDetails_When_CalledWithRenderable()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                await child.FailAllAsync(new Text("Error detail line 1\nError detail line 2"));
            }
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   └── ✕ Child failed
            │       Error detail line 1
            │       Error detail line 2
            └── ✕ Root failed
            """);
    }

    [Fact]
    public async Task ChildWarning_Should_WriteExclamationGlyph_When_ActivityCompletes()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                child.Warning("Something is off");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   └── ! Something is off
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_WriteTreeStructure_When_MultipleSiblings()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var first = activity.StartChildActivity("First child", "First failed"))
            {
                first.Success("First done");
            }

            await using (var second = activity.StartChildActivity("Second child", "Second failed"))
            {
                second.Success("Second done");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── First child
            │   └── ✓ First done
            ├── Second child
            │   └── ✓ Second done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task FailAllAsync_Should_PropagateFailure_When_GrandchildDisposesWithoutCompletion()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                await using (var grandchild = child.StartChildActivity("Grandchild", "Grandchild failed"))
                {
                    // grandchild not completed — DisposeAsync cascades up
                }

                // child already failed via FailAllAsync
            }

            // root already failed via FailAllAsync
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Root
            ├── Child
            │   ├── Grandchild
            │   │   └── ✕ Grandchild failed
            │   └── ✕ Child failed
            └── ✕ Root failed
            """);
    }

    [Fact]
    public async Task Update_Should_WriteClockGlyph_When_CalledWithWaitingKind()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            activity.Update("Please wait", ActivityUpdateKind.Waiting);
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            ├── ⏳ Please wait
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task DisposeAsync_Should_TriggerFailure_When_NoCompletionCalled()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity( "Doing work", "Work failed"))
        {
            // no explicit completion — DisposeAsync should trigger failure
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            Doing work
            └── ✕ Work failed
            """);
    }
}
