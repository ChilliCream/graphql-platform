---
title: "Stuck in the tutorial"
description: "Recover from build failures, schema mismatches, unexpected query results, data source problems, and chapter state drift in the Hot Chocolate v16 server tutorial."
---

If your tutorial project is not producing the expected results for a chapter, use this page to get back on track. This guide addresses issues that can arise from progressing through chapters, editing the project, or encountering checkpoint drift. For problems related to SDK installation, template setup, port conflicts, localhost connection issues, package source access, or Nitro loading, refer to [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/).

# How to recover your tutorial progress

This page is intended for the full tutorial project. If you are following the [Get Started](/docs/hotchocolate/v16/get-started/) guide, use [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) instead.

Before diving into detailed troubleshooting, start with the most direct recovery option for your situation:

| Symptom | Where to begin |
| --- | --- |
| Build fails after a code edit | [Fix build and package errors](#fix-build-and-package-errors) |
| Server starts but the schema is wrong or a field is missing | [Fix schema startup and schema shape errors](#fix-schema-startup-and-schema-shape-errors) |
| Schema looks correct but the query result differs | [Fix query result differences in Nitro](#fix-query-result-differences-in-nitro) |
| Data is empty, duplicated, or from an earlier chapter | [Fix data source and seeded data problems](#fix-data-source-and-seeded-data-problems) |
| Related data, paging, or filtering does not behave as expected | [Fix DataLoader, paging, and filtering surprises](#fix-dataloader-paging-and-filtering-surprises) |
| Mutation or subscription does not work | [Fix mutations and subscriptions](#fix-mutations-and-subscriptions) |
| Local edits are tangled or unclear | [Restore or compare a checkpoint](#restore-or-compare-a-checkpoint) |

Before making changes, gather this information. It will help you or others diagnose the problem if you need to ask for help:

- Current chapter number and title
- The last checkpoint name you verified
- The full terminal command you ran and its output
- The browser URL and port in use
- The GraphQL operation and variables you executed
- The files you edited most recently

Useful links:

- [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/): for restoring, comparing, and reporting issues
- [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/): for SDK, template, port, package source, and Nitro setup issues

It is normal to encounter blocks during a long tutorial. Each section below is organized by symptom, likely cause, solution, and how to verify the fix, so you can spend less time searching and more time making progress.

# First steps: basic checks

Work through these initial checks before using the symptom-specific sections. Many issues are resolved here.

## Check your chapter and checkpoint

Identify which chapter you are working on and the last checkpoint you successfully verified. You can find the chapter map in [Build your first GraphQL server](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/).

You should know which chapter last passed a verification step and which one you are currently editing.

## Confirm your terminal location

Make sure your terminal is in the folder containing `LibraryServer.csproj`.

```bash
pwd
ls
```

For Windows PowerShell:

```powershell
Get-Location
Get-ChildItem
```

You should see `LibraryServer.csproj` listed among the files.

If your terminal is in a parent directory or the repository root, move into the project folder:

```bash
cd LibraryServer
```

## Restore packages and build the project

```bash
dotnet restore
dotnet build
```

You should see:

```text
Build succeeded.
```

If restore or build fails, see [Fix build and package errors](#fix-build-and-package-errors).

## Stop any running server processes and restart

If a previous server process is still running on the same port, a new `dotnet run` will not be able to bind to it.

1. In any terminal running `dotnet run`, press <kbd>Ctrl</kbd> + <kbd>C</kbd> to stop the process.
2. Start the server again:

```bash
dotnet run
```

You should see:

```text
Now listening on: http://localhost:<port>
```

Use the port from this line, not a port from a previous run.

## Refresh Nitro

If the schema in Nitro looks stale or the endpoint points at a previous session:

1. Copy the `Now listening on:` URL from the terminal.
2. Append `/graphql`.
3. Open or paste that URL in Nitro.
4. In the Nitro schema view, use the reload or refresh button to fetch the current schema.

If Nitro shows a connection error or does not load the schema after the server is running, see [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/).

## Compare local files with the chapter checkpoint

If the checks above pass but the result still does not match, compare your local files with the checkpoint for the current chapter before changing more code:

```bash
git fetch --all --tags
git diff <checkpoint-name>
```

For a focused comparison of one chapter's files:

```bash
git diff <checkpoint-name> -- Program.cs
git diff <checkpoint-name> -- Types/
```

If the diff shows unexpected changes, apply the correction and rerun the build. If the diff is too large to interpret, restore the checkpoint. See [Restore or compare a checkpoint](#restore-or-compare-a-checkpoint) or the full instructions in [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/).

# Restore or compare a checkpoint

Use a checkpoint when manual debugging is slower than returning to a known tutorial state.

**When to restore:** many edits with an unclear error origin, skipped chapter, or mismatched output after several attempts.

**When to compare instead:** one failing chapter, or you want to preserve personal notes and optional edits.

The canonical steps are in [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/). The short recovery flow is:

1. Run `git status` and save or discard local work.
2. Fetch the latest checkpoint names: `git fetch --all --tags`.
3. Switch to the checkpoint for the last chapter that passed: `git switch --detach <checkpoint-name>` or `git switch <checkpoint-name>`.
4. Enter the tutorial project folder: `cd LibraryServer`.
5. Restore and build:

```bash
dotnet restore
dotnet build
```

6. Start the server: `dotnet run`.
7. Open Nitro at the printed URL with `/graphql` appended.
8. Run the chapter's verification query or mutation.

Expected outcome: `dotnet build` prints `Build succeeded.`, `dotnet run` prints a listening URL, and the verification operation returns the expected response.

**Caution:** do not mix files from different checkpoints. If you copy individual files, copy only from the same checkpoint folder or tag.

To compare without resetting, use:

```bash
git diff <checkpoint-name> -- <file>
```

After you identify the difference, apply the correction, rebuild, restart, and rerun the chapter verification step.

# Fix build and package errors

## `dotnet build` fails with a compiler error after a code edit

**Symptom:** `dotnet build` reports one or more compile errors in a tutorial file.

**Likely causes:**

- A typo in a type name, attribute name, or namespace.
- A class that Hot Chocolate requires to be `partial` is not marked `partial`.
- An attribute is on the wrong class or method.
- A namespace in the file does not match the project structure.

**Fix:**

1. Read the first compiler error line. It includes the file name and line number.
2. Open that file and compare the type name, attribute, and namespace with the matching file in the chapter checkpoint.
3. Confirm that resolver classes and query/mutation type classes are `partial` where the chapter shows them as `partial`.
4. Rebuild:

```bash
dotnet build
```

**Verification:** `dotnet build` prints `Build succeeded.` with no error lines.

## Source-generated registration code is missing or not recognized by the editor

**Symptom:** the editor shows red underlines in generated type references, or the project builds but the IDE reports that a registration method does not exist.

**Likely causes:**

- The class that should trigger source generation is missing the `partial` modifier.
- The class has the wrong attribute.
- The IDE language service has stale cached output.

**Fix:**

1. Confirm that the affected class matches the chapter example - correct attribute, correct `partial` modifier, correct namespace.
2. Run a clean build from the terminal, not the editor:

```bash
dotnet build
```

3. If the terminal build succeeds but the editor still shows errors, restart the editor or its language service. The source-generated output is on disk after a terminal build.

**Verification:** `dotnet build` succeeds in the terminal. Editor diagnostics clear after a language-service restart.

## Restore fails after adding a package in a chapter

**Symptom:** `dotnet restore` or `dotnet build` reports a package not found, a version conflict, or a feed authentication error after a chapter step that adds a NuGet package.

**Likely causes:**

- The package was added with the wrong version or a version that conflicts with existing Hot Chocolate packages.
- The package was added to the wrong project file.
- NuGet access is blocked.

**Fix:**

1. Confirm the package name and version from the chapter. Compare with:

```bash
dotnet list package
```

2. Confirm your terminal is in `LibraryServer` before running `dotnet add package`.
3. For general NuGet access and package-version alignment problems, use [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/).

**Verification:** `dotnet restore` completes without errors, and `dotnet build` succeeds.

# Fix schema startup and schema shape errors

## The server fails on startup with a schema error

**Symptom:** `dotnet run` exits immediately and the terminal shows an exception related to schema construction. The error may mention a type name, a duplicate field, a missing service, or an invalid configuration.

**Likely causes:**

- A type or resolver class is registered but the class itself has a naming error or missing attribute.
- A required service (such as `DbContext`) is not registered before the schema builds.
- Two fields resolve to the same GraphQL name.
- Middleware is configured in the wrong order for the chapter.

**Fix:**

1. Read the full exception message. The first line usually names the type, field, or service involved.
2. Open `Program.cs` and confirm that required services (`AddDbContext`, `AddInMemorySubscriptions`, and so on) appear before `AddGraphQL()` or in the correct position for the chapter.
3. Compare the failing class against the chapter checkpoint.
4. Rebuild and restart:

```bash
dotnet build
dotnet run
```

**Verification:** `dotnet run` prints a `Now listening on:` URL without an exception.

## A field is missing in the Nitro schema browser

**Symptom:** Nitro's schema view does not show a field that the chapter says should be present. The server started without errors.

**Likely causes:**

- The resolver method or type was not saved before the build.
- The server is still running old code from a previous `dotnet run`.
- The resolver class is not registered through `AddTypes()` or an equivalent call.
- The C# method or property name convention maps to a different GraphQL name.

**Fix:**

1. Stop the server (<kbd>Ctrl</kbd> + <kbd>C</kbd>) and restart it:

```bash
dotnet run
```

2. Refresh the schema in Nitro.
3. Open `Program.cs` and confirm that `.AddTypes()` or the equivalent chapter registration is present.
4. Open the resolver class and confirm that the method name and attributes match the chapter. Hot Chocolate removes common resolver prefixes and suffixes such as `Get` and `Async`, then lower-camel cases the remaining name. A method named `GetBooks` becomes `books` in the schema.

**Verification:** the field appears in the Nitro schema explorer. The chapter operation validates against the current schema.

## The field name in the schema differs from the chapter

**Symptom:** a field appears in the schema but with a different name than the chapter expects.

**Likely cause:** the C# method name does not follow the convention the chapter uses, or an explicit `[GraphQLName]` attribute is missing or incorrect.

**Fix:**

1. Compare the method name and any `[GraphQLName]` attribute with the chapter checkpoint.
2. Hot Chocolate removes common resolver prefixes and suffixes before lower-camel casing. A method named `GetBooks` produces `books`. A method named `GetBookByIdAsync` produces `bookById`.
3. Rename the method or add `[GraphQLName("books")]` to match the expected name.
4. Rebuild and restart.

**Verification:** the Nitro schema browser shows the correct field name.

## An input type, payload type, or mutation field is missing

**Symptom:** the `Mutation` type is absent from the schema, or an input or payload type generated by mutation conventions does not appear.

**Likely causes:**

- Mutation conventions are not enabled in `Program.cs`.
- The mutation class is not registered.
- The mutation method has a naming or attribute problem.

**Fix:**

1. Open `Program.cs` and confirm that `.AddMutationConventions(applyToAllMutations: true)` is chained after `.AddGraphQL()`.
2. Confirm that `.AddTypes()` registers the mutation class.
3. Compare the mutation class attributes and method signature with the chapter checkpoint.

**Verification:** the Nitro schema browser shows the `Mutation` type with the expected field, and the generated input and payload types are visible.

# Fix query result differences in Nitro

## Nitro shows a validation error when you run the chapter operation

**Symptom:** Nitro highlights an error before or during execution. The error message says the field does not exist, the argument type is wrong, or the operation is not valid for the current schema.

**Likely causes:**

- The Nitro document contains an operation copied from a different chapter, and the schema has since changed.
- The schema is stale because the server was not restarted after the last code change.
- A field was renamed between chapters.

**Fix:**

1. Restart the server and refresh the Nitro schema.
2. Paste the exact operation from the current chapter page. Do not reuse an operation from an earlier chapter unless the chapter page says it is unchanged.
3. Use the Nitro schema explorer to confirm the field names and argument names.

**Verification:** the operation validates without underline errors in Nitro, and execution returns a response with a top-level `data` key.

## The response contains `errors` with a path

**Symptom:** the response JSON includes an `errors` array with a `path` pointing to a field, and the `data` for that path is `null`.

**Likely causes:**

- The resolver threw an exception.
- The resolver returned `null` for a non-null field.
- A service injected into the resolver is not registered, causing an exception at runtime.

**Fix:**

1. Read the error `message` field. It often names the missing service, null reference, or database error.
2. If the message mentions a missing service, open `Program.cs` and confirm the required registration from the chapter step.
3. If the message mentions a database error, go to [Fix data source and seeded data problems](#fix-data-source-and-seeded-data-problems).
4. Compare the resolver code with the chapter checkpoint.

**Verification:** running the same operation returns a response with `data` and no `errors` array, or the `errors` array contains only the domain errors the chapter page documents as expected.

## The response `data` shape differs from the chapter

**Symptom:** the operation runs without errors but the JSON shape differs from the expected response shown in the chapter. For example, a list appears where a connection was expected, or nested fields are missing.

**Likely causes:**

- The operation in Nitro is from a previous chapter that used a different field shape.
- A pagination or middleware chapter changed the shape (for example, `books` became a connection returning `edges` and `pageInfo`), and the operation was not updated.

**Fix:**

1. Paste the operation exactly as shown in the current chapter page.
2. Check that the variables pane contains the variables the chapter uses, if any.
3. Confirm that the server is running the code from the current chapter, not a previous build.

**Verification:** the response shape matches the chapter example, allowing for non-deterministic values such as identifiers, timestamps, or row order.

## An old result still appears after a code change

**Symptom:** the operation returns the same result as before, even after you edited a resolver or data file.

**Likely causes:**

- The server process was not restarted after the code change.
- The build succeeded in the editor but `dotnet run` is still running an older binary.

**Fix:**

1. Press <kbd>Ctrl</kbd> + <kbd>C</kbd> to stop the running server.
2. Run `dotnet build`, then `dotnet run`.
3. Re-send the operation in Nitro.

**Verification:** the response reflects the code change.

# Fix data source and seeded data problems

## The query returns an empty list

**Symptom:** `books`, `authors`, or another collection field returns `[]` or an empty connection after the data chapter.

**Likely causes:**

- The seed data from `OnModelCreating` was not added to the project.
- The database file (`library.db`) is from a previous run that was not seeded.
- The database file was created from an earlier version of the tutorial model.

**Fix:**

1. Stop the server.
2. Delete the existing `library.db` file from the `LibraryServer` directory if one exists:

```bash
rm library.db
```

3. Restart the server. The chapter's seed logic runs on startup and recreates the database:

```bash
dotnet run
```

4. Run the list query in Nitro.

**Verification:** the query returns the seeded books and authors documented in the chapter.

## The query returns duplicate rows

**Symptom:** a list query returns the same book or author more than once.

**Likely causes:**

- You ran the mutation chapter more than once with different titles or before adding the duplicate-title check.
- You changed the seed code from the chapter to insert generated IDs or additional rows.

Repeated `EnsureCreatedAsync` calls with the chapter's fixed seed IDs do not duplicate the seed rows.

**Fix:**

1. Stop the server.
2. Delete `library.db` from the `LibraryServer` directory:

```bash
rm library.db
```

3. Restart the server so the chapter's fixed seed data is created again.

**Verification:** each expected entity appears once in the query result.

## SQLite database error on startup

**Symptom:** `dotnet run` starts but the terminal shows a SQLite database exception before the `Now listening on:` line.

**Likely causes:**

- `EnsureCreatedAsync()` failed because the database file is in a locked or corrupt state.
- The `Data Source` path in the connection string does not resolve to the expected directory.

**Fix:**

1. Stop the server.
2. Delete `library.db` from the `LibraryServer` directory.
3. Confirm that the connection string in the chapter's `Program.cs` step uses `Data Source=library.db` (a relative path resolved from the working directory when the app starts).
4. Run `dotnet run` from the `LibraryServer` project directory.

**Verification:** the terminal prints `Now listening on:` without a database exception, and the list query returns seeded data.

## DataLoader returns missing or null related entities

**Symptom:** `book.author` or another related field resolves to `null` or is absent after chapter 6.

**Likely causes:**

- The author IDs stored in the `Book` table do not match the IDs in the `Author` table, because mutation-created data or edited seed data introduced an inconsistent relationship.
- The DataLoader is registered but the resolver that calls it is using an old code path that does not pass the correct ID.

**Fix:**

1. Reset the data store as described in the empty-list entry above. This ensures author and book IDs are consistent.
2. Compare the resolver code that calls the DataLoader with the chapter checkpoint. Confirm the ID property used as the DataLoader key matches the relationship column in the entity.

**Verification:** `book { author { name } }` returns the expected author name for each book in the seeded data set.

# Fix DataLoader, paging, and filtering surprises

## Related data still loads one item at a time after chapter 6

**Symptom:** the chapter says related data should batch, but server logs or a test still show individual queries for each item.

**Likely causes:**

- The DataLoader class is registered but the resolver method still injects `LibraryDbContext` directly instead of the DataLoader.
- The DataLoader is not injected into the correct resolver method.

**Fix:**

1. Compare the resolver method signature with the chapter checkpoint. The chapter resolver accepts the DataLoader as a parameter, not the `DbContext`.
2. Confirm that the DataLoader class is registered through `AddTypes()` or an explicit call.
3. Rebuild and restart.

**Verification:** a list query that returns books with authors does not produce one database round-trip per book. If the chapter includes a test or log assertion, that assertion passes.

## Paged query result is missing `edges` or `pageInfo`

**Symptom:** a query against `books` returns a flat list instead of a connection shape with `edges`, `node`, and `pageInfo`.

**Likely causes:**

- The Nitro document still uses the pre-pagination operation from before chapter 7.
- The `[UsePaging]` attribute was not added to the resolver in the chapter step.

**Fix:**

1. Open the resolver method for `books` and confirm that `[UsePaging]` appears in the attribute list at the same position shown in the chapter.
2. Rebuild and restart the server, then refresh the Nitro schema.
3. Paste the connection operation from the chapter page, which requests `edges { node { ... } }` and `pageInfo { ... }`.

**Verification:** the query validates in Nitro and the response includes `edges`, `node`, and `pageInfo` under `data.books`.

## Filtering argument is missing from the schema

**Symptom:** `books(where: ...)` is not recognized, or the `where` argument does not appear in the Nitro schema for the `books` field.

**Likely causes:**

- `AddFiltering()` is not chained in `Program.cs`.
- The `[UseFiltering]` attribute is missing or placed on the wrong method.
- Middleware order is wrong - `[UsePaging]` must appear above `[UseFiltering]` in the attribute stack when both are used.

**Fix:**

1. Open `Program.cs` and confirm `.AddFiltering()` is present in the `AddGraphQL()` chain.
2. Open the resolver and compare the attribute order with the chapter checkpoint.
3. Rebuild and restart.

**Verification:** the Nitro schema explorer shows the `where` argument on the `books` field. A filter query returns fewer results than an unfiltered query.

# Fix mutations and subscriptions

## The `Mutation` type is missing from the schema

**Symptom:** the Nitro schema browser shows `Query` and possibly `Subscription`, but not `Mutation`.

**Likely causes:**

- Mutation conventions are not enabled in `Program.cs`.
- The mutation class is not registered through `AddTypes()`.

**Fix:**

1. Open `Program.cs`. Confirm that `.AddMutationConventions(applyToAllMutations: true)` is chained in the GraphQL builder from the chapter step.
2. Confirm that the mutation class file is in the `Types/` folder and that `AddTypes()` discovers it.
3. Rebuild and restart.

**Verification:** the Nitro schema browser shows `Mutation` with the `addBook` field.

## The mutation input fails validation

**Symptom:** the mutation returns a validation error when you send it, or Nitro reports an argument type mismatch.

**Likely causes:**

- The `input` variable JSON shape does not match the generated `AddBookInput` type.
- A required field in the input is missing from the variables pane.

**Fix:**

1. In Nitro, open the variables pane. Confirm the variable name and JSON structure exactly match the input type from the chapter. For example:

```json
{
  "input": {
    "title": "New Book",
    "authorId": 1
  }
}
```

2. Confirm the operation text passes the variable to the mutation argument. For example:

```graphql
mutation AddBook($input: AddBookInput!) {
  addBook(input: $input) {
    book {
      id
      title
    }
    errors {
      ... on Error {
        message
      }
    }
  }
}
```

**Verification:** the mutation returns a payload with `book` populated and no validation errors.

## The mutation saves no data

**Symptom:** the mutation returns the expected payload shape but a follow-up query does not show the new book, or the title appears missing from the list.

**Likely causes:**

- The resolver calls `db.Books.Add(book)` but does not call `SaveChangesAsync`.
- A transaction or save error was swallowed and the payload was returned before the write completed.

**Fix:**

1. Compare the mutation resolver with the chapter checkpoint. The resolver should call `await db.SaveChangesAsync(cancellationToken)` after adding the entity.
2. Check the terminal for any exception output that appeared after the mutation ran.

**Verification:** after the mutation, the `books` query returns the new title.

## A subscription starts but receives no events

**Symptom:** the subscription tab in Nitro shows `Listening...` or `Connected`, but no event arrives after the `addBook` mutation runs.

**Likely causes:**

- The subscription was not open when the mutation ran. The in-memory provider delivers events only to subscribers that are connected at the time of publication.
- The topic name in the subscription does not match the topic name used by the mutation publisher.
- `AddInMemorySubscriptions()` is not registered in `Program.cs`.
- WebSockets are not enabled for the endpoint.

**Fix:**

1. Open the subscription tab in Nitro first, confirm it shows a connected state, then run the mutation in a separate tab.
2. Open `Program.cs` and confirm `.AddInMemorySubscriptions()` is present in the builder chain from the chapter step.
3. Confirm `.UseWebSockets()` appears before `app.MapGraphQL()` in `Program.cs`, as shown in the chapter.
4. Compare the topic string in the subscription resolver and the topic string in the mutation publisher. They must be identical.

**Verification:** after the subscription tab shows a connected state, running `addBook` in a second tab delivers one event result in the subscription tab.

## WebSocket connection fails

**Symptom:** the subscription tab in Nitro shows a connection error or cannot establish a WebSocket to the server.

**Likely causes:**

- `app.UseWebSockets()` is not in `Program.cs`, or it appears after `app.MapGraphQL()`.
- The Nitro endpoint URL uses a scheme that does not match the server's configuration.

**Fix:**

1. Open `Program.cs` and confirm that `app.UseWebSockets()` appears before `app.MapGraphQL()`.
2. In Nitro, confirm the endpoint URL. For local development, Nitro handles the protocol upgrade automatically from an HTTP endpoint. If you typed the endpoint manually, use `http://localhost:<port>/graphql`.
3. Rebuild, restart, and reopen the subscription tab.

**Verification:** the subscription tab connects and shows a waiting state.

# Ask for help with useful diagnostics

When this page does not resolve the failure, report the issue with enough detail for others to reproduce it.

Collect this information before you post:

| Detail | How to get it |
| --- | --- |
| Tutorial chapter and chapter title | Chapter number and heading from the tutorial page |
| Checkpoint used | Branch or tag name from the tutorial repository |
| Last chapter with a passing verification step | The chapter where the query or test last matched the docs |
| Operating system | Windows, macOS, or Linux |
| .NET SDK version | `dotnet --version` |
| Hot Chocolate package versions | `dotnet list package` |
| Command that failed | The exact command from the terminal |
| Command output | First relevant error line and any following context lines |
| GraphQL operation | The operation text and variables JSON |
| Response JSON | The `data` and `errors` fields from the Nitro response |
| Schema snippet | Relevant SDL lines from the Nitro schema browser, when the failure is schema-related |

Remove secrets, personal access tokens, production connection strings, and private data from any output before sharing.

Where to report:

- Use the issue tracker or feedback path linked by the tutorial repository README.
- If the repository has no separate path, use the [ChilliCream GraphQL platform issue tracker](https://github.com/ChilliCream/graphql-platform/issues) and include the tutorial page URL.
- For documentation feedback or text corrections, use the feedback link at the bottom of the affected page.

When you post, state whether the issue appears to be a documentation mismatch (the page says one thing, the code does another), a tooling failure (SDK, template, or package behaves unexpectedly), or a code behavior question (you followed the steps but do not understand the result).

Return to [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) if you need the checkpoint restore or compare workflow before you report.
