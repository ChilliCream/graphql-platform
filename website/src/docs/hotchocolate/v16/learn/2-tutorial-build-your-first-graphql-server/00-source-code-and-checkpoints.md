---
title: "Source code and checkpoints"
description: "Find the tutorial source code, choose a checkpoint, restore or compare your project safely, and report tutorial issues with useful details."
---

If your project no longer matches the tutorial chapter you are following, use this page to get back on track.

You have several options to recover:

1. Restore a known checkpoint and continue from there.
2. Compare your local files with a checkpoint before making changes.
3. Report a tutorial issue with enough detail for maintainers to help.

Checkpoints are recovery points for your project. You do not need them if your local files match the expected chapter output. Use a checkpoint if you encounter a failed build, an unexpected schema, a different query result, or if you missed a step.

# Locating the tutorial source and recovery files

The tutorial source repository contains the complete state for each chapter.

The final repository URL and checkpoint names are not yet included in this documentation. Until the tutorial repository is published, use the following placeholders as conventions:

| Placeholder | Replace it with |
| --- | --- |
| `<tutorial-repository-url>` | The source repository URL linked from the tutorial overview or repository README. |
| `<tutorial-folder>` | The folder containing the tutorial `.csproj` file. |
| `<checkpoint-name>` | The branch or tag name listed in the tutorial repository for the chapter state you need. |
| `<previous-checkpoint>` | The completed checkpoint before the chapter you want to start. |
| `<current-checkpoint>` | The completed checkpoint for the chapter you want to verify. |

After cloning or opening the tutorial repository, look for these files and folders:

| File or folder | Purpose |
| --- | --- |
| `README.md` | Repository setup, checkpoint list, and issue instructions. |
| The tutorial project folder | Where you run `dotnet restore`, `dotnet build`, and `dotnet run`. |
| `Program.cs` | GraphQL server registration and endpoint setup. |
| `Types/` or similar GraphQL folder | Query, mutation, object type, resolver, and input files for the tutorial. |
| Data or seed folders, if present | Sample data for later chapters. |
| Test project, if present | Used in the testing chapter. |

Unless a chapter instructs otherwise, run tutorial commands from the folder containing the tutorial `.csproj` file.

# Selecting the right checkpoint for your chapter

Each checkpoint represents the completed state of a chapter.

Refer to the checkpoint list in the tutorial repository README. If the repository uses branches or tags, their names should correspond to chapter numbers or outcomes. If folders or release assets are used instead, follow the repository's instructions for starting and comparing checkpoints.

| Your goal | Which checkpoint to use |
| --- | --- |
| Start a chapter from a clean state | The completed checkpoint for the previous chapter. |
| Check if you finished a chapter correctly | The completed checkpoint for the current chapter. |
| Skip to a later chapter | The completed checkpoint for the chapter immediately before it. |
| Inspect the final result | The final tutorial checkpoint. |

For example, to start [Define your first types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/), restore the completed checkpoint from [Set up the project](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/01-set-up-the-project/). If you have finished the types chapter but your schema does not match, compare your work to the completed checkpoint for that chapter.

# Restoring a checkpoint without losing your work

Before switching checkpoints, check your local changes:

```bash
git status
```

Review the output before proceeding.

| `git status` output | Next step |
| --- | --- |
| `working tree clean` | You can change checkpoints without saving local edits. |
| Modified files you want to keep | Commit or stash them before switching. |
| Modified files you do not want to keep | Discard them only if you are sure you do not need them. |
| Untracked files | Move, commit, or remove them as needed. |

To temporarily save your changes with a Git stash:

```bash
git stash push -m "save tutorial work before checkpoint restore"
```

To make a local commit instead:

```bash
git add .
git commit -m "Save tutorial work before checkpoint restore"
```

Fetch the latest checkpoint information:

```bash
git fetch --all --tags
```

If you need to confirm the checkpoint name, list available branches and tags:

```bash
git branch -a
git tag
```

Restore the selected checkpoint:

- If checkpoints are tags:

  ```bash
  git switch --detach <checkpoint-name>
  ```

- If checkpoints are branches:

  ```bash
  git switch <checkpoint-name>
  ```

- If the repository uses checkpoint folders, copy or open the folder for the checkpoint. Do not mix files from different checkpoint folders.

After switching or changing folders, move to the tutorial project folder:

```bash
cd <tutorial-folder>
```

Restore packages and build the project:

```bash
dotnet restore
dotnet build
```

You should see:

```text
Build succeeded.
```

Start the server:

```bash
dotnet run
```

You should see output like:

```text
Now listening on: http://localhost:<port>
```

Open the printed URL in your browser, appending `/graphql`. Nitro should connect to your running server. Then return to the next tutorial chapter and run its verification step.

# Comparing your code with a checkpoint

If you want to understand what changed or keep your local work, compare before resetting.

First, confirm your current files:

```bash
git status
```

Fetch the latest checkpoint names:

```bash
git fetch --all --tags
```

To compare your current working tree with a checkpoint:

```bash
git diff <checkpoint-name>
```

If the full diff is too large, compare only the files changed by the chapter. For example:

```bash
git diff <checkpoint-name> -- Program.cs
git diff <checkpoint-name> -- Types/
```

If your tutorial uses a different GraphQL folder name, substitute `Types/` with that folder.

To compare two completed tutorial states, use:

```bash
git diff <previous-checkpoint> <current-checkpoint>
```

This shows what the chapter was intended to change.

You can also use your editor's compare view. Open your local file on one side and the checkpoint version on the other. Focus on the files the chapter asked you to edit, and avoid changing unrelated files while recovering.

After finding the differences:

1. Apply the missing or corrected changes from the chapter.
2. Run `dotnet build`.
3. Restart the server with `dotnet run`.
4. Rerun the chapter's query, mutation, subscription, or test.

# Troubleshooting common checkpoint recovery issues

This section covers checkpoint workflow problems. For broader build, Nitro, data, or query result issues, see [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) or [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/).

## Checkpoint name not found

Possible reasons:

- The repository has not fetched tags or remote branches.
- The checkpoint name is different from the placeholder in the docs.
- Your terminal is in a different Git repository.
- The tutorial repository has not published final checkpoint names yet.

To resolve:

```bash
git fetch --all --tags
git branch -a
git tag
```

You should see the checkpoint name in either the branch or tag list. Use the exact spelling from the repository.

## Checkout blocked by local changes

This happens when Git is protecting files that would be overwritten.

To resolve:

```bash
git status
```

Then choose one of these actions:

- Commit the work you want to keep.
- Stash the work you want to keep temporarily.
- Discard work only if you are sure you do not need it.

Run `git status` again to confirm. Make sure you understand every changed or untracked file before retrying checkout.

## Commands run in the wrong folder

This usually means your terminal is not in the folder containing the tutorial `.csproj` file.

To check your location:

```bash
pwd
ls
```

On Windows PowerShell, use:

```powershell
Get-Location
Get-ChildItem
```

Move to the tutorial project folder:

```bash
cd <tutorial-folder>
```

Verify that the folder contains the tutorial `.csproj` file and that `dotnet build` starts building that project.

## The restored project does not build or run

Possible causes:

- The selected checkpoint does not match the chapter you are following.
- The .NET SDK is missing or outdated.
- Package restore cannot reach the configured NuGet sources.
- Files from different checkpoints were mixed.

To fix:

1. Confirm the selected checkpoint in the tutorial repository README.
2. Run `dotnet --info` to check your SDK version.
3. Run `dotnet restore`.
4. Run `dotnet build`.
5. If you see errors about SDK, package restore, templates, ports, or Nitro startup, see [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/).

You should see `Build succeeded.` from `dotnet build` and a local listening URL from `dotnet run`.

# Reporting a tutorial issue

If the documentation, source, or checkpoint appears incorrect after verifying your repository state, report an issue.

Before reporting:

1. Fetch the latest tutorial source.
2. Confirm the checkpoint name from the tutorial repository README.
3. Run the chapter's verification command again.
4. Remove secrets, tokens, connection strings, and private data from any output you share.

Include the following details:

| Detail | Example |
| --- | --- |
| Tutorial page URL | `/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/` |
| Chapter number and title | `02 - Define your first types` |
| Checkpoint used | `<checkpoint-name>` |
| Last successful chapter | `01 - Set up the project` |
| Operating system | Windows, macOS, or Linux |
| .NET SDK | Output from `dotnet --version` or relevant `dotnet --info` lines |
| Hot Chocolate package version | Output from `dotnet list package` if package versions may matter |
| Command that failed | `dotnet build`, `dotnet run`, or the exact command from the chapter |
| Error output | The first relevant error and any related lines |
| Expected result | What the chapter says should happen |
| Actual result | What happened on your machine |

Use the issue tracker or feedback path linked by the tutorial repository. If there is no separate issue path, use the [ChilliCream GraphQL platform issue tracker](https://github.com/ChilliCream/graphql-platform/issues) and include the page URL.

# Verifying you are back on the tutorial path

Before leaving this page, check that:

- `dotnet build` succeeds from the tutorial project folder.
- `dotnet run` starts the server and prints a local URL.
- Nitro opens at the printed URL with `/graphql`.
- The chapter's verification query, mutation, subscription, or test matches the expected result.
- You know which chapter to open next.

If you restored the previous completed checkpoint, continue with the next chapter. If you compared against the current completed checkpoint, return to the chapter step where your files differed.

If you are still unable to recover, visit [Stuck in the tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/) and start with the symptom that matches your current state.
