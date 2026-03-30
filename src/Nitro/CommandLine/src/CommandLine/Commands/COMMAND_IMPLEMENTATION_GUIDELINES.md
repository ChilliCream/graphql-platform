# Command Implementation Guidelines (Nitro CommandLine)

This document defines the implementation standard for commands.
It is derived from the patterns used in `CreateApiKeyCommand`.

## Command structure

Every command is a sealed class that inherits from `Command`.
The constructor wires up options, the description, and the action handler.
All logic lives in a private `ExecuteAsync` method.

```csharp
internal sealed class MyCommand : Command
{
    public MyCommand(
        INitroConsole console,
        IMyClient client,
        ISessionService sessionService,
        IResultHolder resultHolder) : base("my-command")
    {
        Description = "Does the thing.";

        Options.Add(Opt<MyNameOption>.Instance);
        Options.Add(Opt<OptionalWorkspaceIdOption>.Instance);

        this.AddGlobalNitroOptions();

        this.SetActionWithExceptionHandling(
            console,
            async (parseResult, cancellationToken)
                => await ExecuteAsync(
                    parseResult,
                    console,
                    client,
                    sessionService,
                    resultHolder,
                    cancellationToken));
    }

    private async Task<int> ExecuteAsync(
        ParseResult parseResult,
        INitroConsole console,
        IMyClient client,
        ISessionService sessionService,
        IResultHolder resultHolder,
        CancellationToken cancellationToken)
    {
        // ...
    }
}
```

### `SetActionWithExceptionHandling`

Always use `this.SetActionWithExceptionHandling(console, ...)` instead of `SetAction` directly.
It provides uniform handling for:

- `ExitException` — writes the message to stderr and returns exit code 1
- `NitroClientAuthorizationException` — writes a permission error to stderr and returns exit code 1
- `NitroClientException` — writes the server error to stderr and returns exit code 1
- Cancellation — returns exit code 1 silently

### `AddGlobalNitroOptions`

Always call `this.AddGlobalNitroOptions()` after registering command-specific options.
It adds the `--cloud-url`, `--api-key`, and `--output` options present on every command.

## ExecuteAsync structure

The method follows a fixed top-to-bottom order:

1. Assert authentication
2. Resolve input (options and/or interactive prompts)
3. Execute async work inside an activity
4. Handle errors
5. Mark activity as success, emit output, set result, return exit code 0

### Exit codes

Always use the `ExitCodes` constants:

```csharp
return ExitCodes.Success; // 0
return ExitCodes.Error;   // 1
```

Never return a raw integer.

## Authentication

Only commands that require an authenticated user need to assert authentication. If the command can run without a session or API key, skip this call entirely.

When authentication is required, call `parseResult.AssertHasAuthentication(sessionService)` at the very top of `ExecuteAsync`, before touching any options or prompts. It throws an `ExitException` if neither a session nor a `--api-key` option is present, and `SetActionWithExceptionHandling` renders it to stderr automatically.

```csharp
parseResult.AssertHasAuthentication(sessionService);
```

## Error messages

- **Option references**: Render option names as `'--option-name'` (single-quoted, kebab-case).
- **Value references**: When including a runtime value in a message, wrap it in angle-bracket quotes: `'value'`.
- **Sentence format**: Messages must end with a period and be written in proper English.
- **Use `throw Exit(...)`**: For guard-style errors inside `ExecuteAsync`, use `throw Exit("...")` (imported via `using static ChilliCream.Nitro.CommandLine.ThrowHelper`). `SetActionWithExceptionHandling` catches the `ExitException` and writes it to stderr automatically.

```csharp
// Good — option name in single quotes, value wrapped in angle-bracket quotes
throw Exit($"The '--workspace-id' or '--api-id' option is required in non-interactive mode.");
throw Exit($"The API with ID '<{apiId}>' was not found.");

// Bad — no quotes, no period, raw exception type
throw new Exception($"api {apiId} not found");
```

## Input resolution

Resolve options using `parseResult.GetValue(Opt<TOption>.Instance)`. When a required value is missing in non-interactive mode, throw an `ExitException` before entering the activity block. Never let a missing required value surface as a null reference inside the activity.

For interactive fallbacks use `console.PromptAsync(...)` or `console.PromptForApiIdAsync(...)` after confirming `console.IsInteractive`.

```csharp
if (workspaceId is null && apiId is null)
{
    if (!console.IsInteractive)
    {
        throw Exit(
            $"The '--workspace-id' or '--api-id' option is required in non-interactive mode.");
    }

    // interactive prompts here
}
```

## Activities

Wrap async operations around GraphQL mutations that do visible, state-changing work inside an activity. Use `await using` so the activity is disposed (and its spinner stopped) even on exceptions.

Do **not** wrap pure GraphQL queries or subscriptions in activities. Queries and subscriptions are exempt from this rule.

```csharp
await using (var activity = console.StartActivity("Creating API key..."))
{
    // ... async work ...

    activity.Success("Successfully created the API key!");
    // ...
    return ExitCodes.Success;
}
```

### Rules

- **Use activities for mutations only.** Queries and subscriptions are exempt and should run without a mutation-style activity wrapper.
- **Always call `activity.Fail()` before writing to stderr.** The activity spinner must be stopped and marked failed before any error output is written.
- **Always call `activity.Success("...")` explicitly before continuing to output.** Activities are not implicitly marked as successful when the `using` block exits; you must call it.
- **Never leave an activity without a terminal state.** Every code path inside the activity block must end in either `activity.Fail()` (followed by stderr output and `return ExitCodes.Error`) or `activity.Success("...")` (followed by stdout output and `return ExitCodes.Success`).

Example error path:

```csharp
if (data.Errors?.Count > 0)
{
    activity.Fail();

    foreach (var error in data.Errors)
    {
        console.Error.WriteErrorLine(error.Message);
        return ExitCodes.Error;
    }
}
```

## GraphQL mutations — typed error handling

When a command executes a GraphQL mutation, open the corresponding `.graphql` file (located under `src/CommandLine.Client/`) to identify all error fragment spreads on the mutation's `errors` field. Every error type listed there must be handled explicitly in the command.

The generated client returns `data.Errors`, which is a list of union members. Iterate it and switch on the concrete interface types generated by Strawberry Shake.

```csharp
if (data.Errors?.Count > 0)
{
    activity.Fail();

    foreach (var error in data.Errors)
    {
        var errorMessage = error switch
        {
            IApiNotFoundError err               => err.Message,
            IWorkspaceNotFound err              => err.Message,
            IPersonalWorkspaceNotSupportedError err => err.Message,
            IRoleNotFoundError err              => err.Message,
            IValidationError err                => err.Message,
            IError err                          => "Unexpected mutation error: " + err.Message,
            _                                   => "Unexpected mutation error."
        };

        console.Error.WriteErrorLine(errorMessage);
        return ExitCodes.Error;
    }
}
```

Rules:

- One `switch` arm per explicitly named fragment in the `.graphql` file
- Include a catch-all `IError` arm for fragments spread via `...Error` or any unrecognised error
- Include the `_` default arm as a final safety net
- Always call `activity.Fail()` before the loop
- Return `ExitCodes.Error` after writing the first error message

### Legacy pattern — `PrintMutationErrorsAndExit`

`console.PrintMutationErrorsAndExit(data.Errors)` is a legacy helper that predates typed error handling. It writes to stdout (not stderr), does not call `activity.Fail()`, and throws a generic `ExitException` without a message. **Do not use it in new or updated commands.** Replace any existing call with the explicit inline switch pattern shown above.

## Result output

After a successful operation, call `activity.Success("...")`, set the result on `resultHolder`, and return `ExitCodes.Success`. The blank line before the result is handled automatically by the result formatting infrastructure in `RootCommandExtensions` — do **not** call `console.WriteLine()` before `resultHolder.SetResult(...)`.

```csharp
activity.Success("Successfully created API key!");

resultHolder.SetResult(new ObjectResult(new MyResult
{
    Id = result.Id,
}));

return ExitCodes.Success;
```

The `IResultHolder` writes the result in the format requested by the `--output` option (plain text or JSON). Always populate it for commands that produce data the caller may want to consume programmatically.

## Checklist

- [ ] `SetActionWithExceptionHandling` used (not raw `SetAction`)
- [ ] `AddGlobalNitroOptions` called
- [ ] `AssertHasAuthentication` is the first call in `ExecuteAsync` (only if the command requires a user)
- [ ] All option references in error messages use `'--option-name'` format with single quotes
- [ ] All error messages are complete sentences ending with a period
- [ ] `throw Exit(...)` used for guard errors (via `using static ThrowHelper`)
- [ ] All async work is wrapped in `await using (var activity = ...)`
- [ ] `activity.Fail()` is called before every stderr write inside an activity
- [ ] `activity.Success("...")` is called explicitly before returning `ExitCodes.Success`
- [ ] Every mutation error fragment from the `.graphql` file has a corresponding switch arm
- [ ] A blank line is written before setting a result on `IResultHolder`
- [ ] All return paths use `ExitCodes.Success` or `ExitCodes.Error`
