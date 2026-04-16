using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Console;

public sealed class InteractiveNitroConsoleActivityTests
{
    private static (INitroConsole Console, StringWriter Writer) CreateConsole(
        int width = Constants.DefaultPrintWidth)
    {
        var writer = new StringWriter();

        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(writer);
        outConsole.Profile.Width = width;
        outConsole.Profile.Capabilities.Interactive = true;

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
    public async Task Success_Should_RenderCheckGlyph_When_ActivityCompletes()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
        {
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Doing work
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task Fail_Should_RenderCrossGlyph_When_CalledWithMessage()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
        {
            activity.Fail("Something went wrong");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✕ Doing work
            └── ✕ Something went wrong
            """);
    }

    [Fact]
    public async Task FailAllAsync_Should_RenderCrossGlyphWithDetails_When_CalledWithRenderable()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
        {
            await activity.FailAllAsync(new Text("Error detail line 1\nError detail line 2"));
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✕ Doing work
            └── ✕ Work failed
                Error detail line 1
                Error detail line 2
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_RenderTreeStructure_When_ChildSucceeds()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
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
            ✓ Root
            ├── ✓ Child done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task ChildSuccess_Should_CollapseToSingleLine_When_ChildHasNoUpdates()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child step", "Child failed"))
            {
                child.Success("Child done");
            }

            activity.Success("Root done");
        }

        // assert — child collapses: entry text replaced with success message, no nested terminator
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Root
            ├── ✓ Child done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task ChildSuccess_Should_NotCollapse_When_ChildHasUpdates()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child step", "Child failed"))
            {
                child.Update("working...");
                child.Success("Child done");
            }

            activity.Success("Root done");
        }

        // assert — child entry stays, success appended as a nested terminator
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Root
            ├── ✓ Child step
            │   ├── working...
            │   └── ✓ Child done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_RenderTreeStructure_When_ChildFails()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
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
            ✕ Root
            ├── ✕ Child step
            │   └── ✕ Child error
            └── ✕ Root error
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_RenderNestedTreeStructure_When_GrandchildSucceeds()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
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
            ✓ Root
            ├── ✓ Child
            │   ├── ✓ Grandchild done
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
        await using (var activity = console.StartActivity("Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                // child is not completed — DisposeAsync will call FailAllAsync
            }
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✕ Root
            └── ✕ Child
            """);
    }

    [Fact]
    public async Task Update_Should_BeIgnored_When_CalledAfterSuccess()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
        {
            activity.Success("Done");
            activity.Update("This should be ignored");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Doing work
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task Update_Should_RenderMultipleUpdates_When_CalledWithDifferentKinds()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
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
            ✓ Doing work
            ├── Regular update
            ├── ! Warning update
            ├── ⏳ Waiting update
            ├── ✓ Success update
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task Warning_Should_RenderExclamationGlyph_When_ActivityCompletes()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
        {
            activity.Warning("Something is off");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ! Doing work
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
            ✓ This is a very long root
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
        await using (var activity = console.StartActivity("Root", "Failed"))
        {
            activity.Update("This update message is long enough to wrap at narrow width");
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Root
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
        await using (var activity = console.StartActivity("Root", "Failed"))
        {
            activity.Success("This success message is long enough to wrap at narrow width");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Root
            └── ✓ This success message is
                  long enough to wrap at
                  narrow width
            """);
    }

    [Fact]
    public async Task Fail_Should_WrapMessage_When_MessageExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity("Root", "Failed"))
        {
            activity.Fail("This failure message is long enough to wrap at narrow width");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✕ Root
            └── ✕ This failure message is
                  long enough to wrap at
                  narrow width
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_WrapTitle_When_TitleExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity("Root", "Failed"))
        {
            await using (var child = activity.StartChildActivity(
                "This child title is long enough to wrap",
                "Child failed"))
            {
                // give child a sub-update so its title is preserved on success
                child.Update("Working");
                child.Success("Done");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Root
            ├── ✓ This child title is long
            │   │ enough to wrap
            │   ├── Working
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
        await using (var activity = console.StartActivity("Root", "Failed"))
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
            ✓ Root
            ├── ✓ Child
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
        await using (var activity = console.StartActivity("Root", "Failed"))
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
            ✓ Root
            ├── ✓ This child success
            │     message wraps
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task ChildFail_Should_WrapMessage_When_MessageExceedsWidth()
    {
        // arrange
        var (console, writer) = CreateConsole(width: 30);

        // act
        await using (var activity = console.StartActivity("Root", "Failed"))
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
            ✕ Root
            ├── ✕ Child
            │   └── ✕ This child failure
            │         message wraps
            └── ✕ Root error
            """);
    }

    [Fact]
    public async Task Update_Should_RenderDetails_When_CalledWithRenderable()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
        {
            activity.Update("Status", details: new Text("Detail line 1\nDetail line 2"));
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Doing work
            ├── Status
            │   Detail line 1
            │   Detail line 2
            └── ✓ Done
            """);
    }

    [Fact]
    public async Task ChildUpdate_Should_RenderDetails_When_CalledWithRenderable()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
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
            ✓ Root
            ├── ✓ Child
            │   ├── Status
            │   │   Detail line 1
            │   │   Detail line 2
            │   └── ✓ Child done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task ChildFailAllAsync_Should_RenderCrossGlyphWithDetails_When_CalledWithRenderable()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                await child.FailAllAsync(new Text("Error detail line 1\nError detail line 2"));
            }
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✕ Root
            └── ✕ Child
                └── ✕ Child failed
                    Error detail line 1
                    Error detail line 2
            """);
    }

    [Fact]
    public async Task ChildWarning_Should_RenderExclamationGlyph_When_ActivityCompletes()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
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
            ✓ Root
            ├── ! Child
            │   └── ! Something is off
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task StartChildActivity_Should_RenderTreeStructure_When_MultipleSiblings()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
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
            ✓ Root
            ├── ✓ First done
            ├── ✓ Second done
            └── ✓ Root done
            """);
    }

    [Fact]
    public async Task FailAllAsync_Should_PropagateFailure_When_GrandchildDisposesWithoutCompletion()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                await using (var grandchild = child.StartChildActivity("Grandchild", "Grandchild failed"))
                {
                    // grandchild not completed — DisposeAsync cascades up
                }
            }
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✕ Root
            └── ✕ Child
                └── ✕ Grandchild
            """);
    }

    [Fact]
    public async Task Update_Should_RenderClockGlyph_When_CalledWithWaitingKind()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
        {
            activity.Update("Please wait", ActivityUpdateKind.Waiting);
            activity.Success("Done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Doing work
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
        await using (var activity = console.StartActivity("Doing work", "Work failed"))
        {
            // no explicit completion — DisposeAsync should trigger failure
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✕ Doing work
            └── ✕ Work failed
            """);
    }

    [Fact]
    public async Task ChildSuccess_Should_FailActiveDescendants_When_LeakedGrandchild()
    {
        // arrange
        var (console, writer) = CreateConsole();

        // act
        await using (var activity = console.StartActivity("Root", "Root failed"))
        {
            await using (var child = activity.StartChildActivity("Child", "Child failed"))
            {
                // leak grandchild — its active state is flipped to Failed by child.Success
                var grandchild = child.StartChildActivity("Grandchild", "Grandchild failed");
                _ = grandchild;

                child.Success("Child done");
            }

            activity.Success("Root done");
        }

        // assert
        GetOutput(writer).MatchInlineSnapshot(
            """
            ✓ Root
            ├── ✓ Child
            │   ├── ✕ Grandchild
            │   └── ✓ Child done
            └── ✓ Root done
            """);
    }
}
