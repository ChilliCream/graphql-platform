# Command Testing Guidelines (Nitro CommandLine)

This document defines the baseline test standard for command tests.
It is derived from the patterns used in CreateApiKeyCommandTests.

## Goal

Every user-visible command behavior must be covered by tests.
A user should not be able to end up in a command state that is not tested.

## Required test categories

Every command test suite must include all categories below.

1. Help output

- Add one test that executes command help (for example: `--help`).
- Snapshot the help output text.

2. Interaction mode coverage

- Test all three interaction types:
  - Interactive
  - Non-interactive
  - JSON output
- The happy-path success case must always be covered in all three interaction types.
- For success and error flows, validate mode-specific output behavior.

3. Authentication and session prerequisites

- Test unauthenticated execution paths.
- Test paths where workspace/session fallback is required.

4. Required options

- Add exactly one test that verifies missing required options that are enforced by command option parsing and do not have custom validation in ExecuteAsync.
- If ExecuteAsync contains extra business validation, those cases need their own dedicated tests.

5. Input combinations and validation errors

- Test all meaningful combinations of inputs that affect branching.
- Test every validation error path.
- Include permutations where values come from:
  - Explicit options
  - Session fallback
  - Interactive prompts
- If a list command exposes a `--cursor` option, add success-path tests for each interaction mode with the cursor explicitly specified.
- For cursor list tests, mock data must represent a terminal page:
  - `hasNextPage: false`
  - `endCursor: null`

6. GraphQL operation exception handling

- For commands that execute a GraphQL client call, always test:
  - NitroClientException
  - NitroClientAuthorizationException
- Validate each exception in all three interaction types.
- Write these as six distinct tests (not a shared mode theory):
  - `ClientThrowsException_ReturnsError_Interactive`
  - `ClientThrowsException_ReturnsError_NonInteractive`
  - `ClientThrowsException_ReturnsError_JsonOutput`
  - `ClientThrowsAuthorizationException_ReturnsError_Interactive`
  - `ClientThrowsAuthorizationException_ReturnsError_NonInteractive`
  - `ClientThrowsAuthorizationException_ReturnsError_JsonOutput`

7. GraphQL mutation typed errors

- For commands that invoke a GraphQL mutation, open the corresponding `.graphql` file (e.g., `CreateApiKeyCommand.graphql`) to find the union error types on the mutation return type.
- Read `ExecuteAsync` to identify every explicitly handled error type in the error switch/if-else.
- Each explicitly handled branch must have a corresponding test case. Also cover the fallback `IError` branch using a mock.
- Test the full set of typed errors across all three interaction modes. Create **three distinct `[Theory]` tests**, one per mode:
  - `MutationReturnsTypedError_ReturnsError_Interactive`
  - `MutationReturnsTypedError_ReturnsError_NonInteractive`
  - `MutationReturnsTypedError_ReturnsError_JsonOutput`
- Drive the error cases through a shared `[MemberData]` factory. The factory must be `public static` and return `IEnumerable<object[]>` (xUnit's `[MemberData]` requires a public member). Each row is a pair of the concrete error instance and the expected stderr string.
- Place the factory at the bottom of the file, after all test methods.
- In the interactive mode test, pass all required values via command arguments. Do not simulate interactive prompts for error flows.

8. Branch completeness

- Enumerate command branches from ExecuteAsync and prove each branch is covered by at least one test.
- Cover both success and failure branches.

## Minimum mode matrix

For each behavior below, cover the required modes.

| Behavior                               | Interactive                                             | Non-interactive                          | JSON output                              |
| -------------------------------------- | ------------------------------------------------------- | ---------------------------------------- | ---------------------------------------- |
| Help output                            | Required                                                | Required by default help parser behavior | Required by default help parser behavior |
| Missing required option (parser-level) | Optional if parser blocks before prompt path is reached | Required                                 | Required                                 |
| Prompt-based path                      | Required                                                | Not applicable                           | Not applicable                           |
| Non-prompt path                        | Required if reachable                                   | Required                                 | Required                                 |
| NitroClientException                   | Required                                                | Required                                 | Required                                 |
| NitroClientAuthorizationException      | Required                                                | Required                                 | Required                                 |
| Mutation typed errors                  | Required (args-only, no interactive prompts)            | Required                                 | Required                                 |

Note: If a behavior is unreachable in a mode by design, document that explicitly in test names or comments.

## Branch-driven test design workflow

1. Read ExecuteAsync and list every branch condition.
2. For each condition, list true/false outcomes.
3. Convert each unique end-state into one test case.
4. Mark each test with interaction mode.
5. Add snapshots/assertions for stdout, stderr, and exit code.
6. Verify no uncovered end-state remains.

## Test naming conventions

Use PascalCase method names with underscore-delimited semantic blocks.

Preferred base pattern:

- `<ConditionOrInput>_Returns<Outcome>[_<Mode>]`

Examples:

- `Help_ReturnSuccess`
- `NoSession_Or_ApiKey_ReturnsError`
- `MissingRequiredOptions_ReturnsError`
- `WithWorkspaceId_ReturnSuccess_NonInteractive`
- `WithWorkspaceId_ReturnSuccess_JsonOutput`

Mode suffix rules:

- When a behavior is validated per mode in separate tests, always suffix with one of:
  - `_Interactive`
  - `_NonInteractive`
  - `_JsonOutput`
- Do not mix mode naming variants in the same suite (for example, avoid `_OutputJson` when `_JsonOutput` is used elsewhere).

Mutation and exception naming:

- Mutation branch errors:
  - `MutationReturns<BranchName>Error_ReturnsError_<Mode>`
  - Example: `MutationReturnsChangeError_ReturnsError_NonInteractive`
- Top-level mutation payload errors:
  - `MutationReturnsError_ReturnsError_<Mode>`
- Client exceptions:
  - `ClientThrowsException_ReturnsError_<Mode>`
  - `ClientThrowsAuthorizationException_ReturnsError_<Mode>`

Interactive prompt-path naming:

- Use explicit prompt-path wording when testing prompt branches:
  - `MissingRequiredOptions_PromptsUser_<Branch>_ReturnSuccess`
  - Example: `MissingRequiredOptions_PromptsUser_SelectsApi_ReturnSuccess`

General naming rules:

- Keep suffixes stable: use `ReturnSuccess` for success and `ReturnsError` for failure.
- Prefer describing the trigger first (`NoSession`, `WithWorkspaceId`, `MutationReturns...`) and the outcome second.
- If a behavior is intentionally mode-specific, encode that in the method name.

## Assertions and style

- Use explicit `// arrange`, `// act`, and `// assert` comments in each test method.
- If a test combines steps (for example help tests), `// arrange & act` is allowed.
- Use strict mocks for GraphQL clients where practical.
- Verify expected calls and verify no unexpected calls for negative paths.
- Fully assert stdout and stderr for every test. Do not use partial assertions like `Assert.Contains` for command output when the full output can be asserted.
- Prefer `MatchInlineSnapshot` over `Assert.Equal` for `StdOut` and `StdErr` assertions, for example: `result.StdOut.MatchInlineSnapshot("""...""")`.
- For error paths, assert:
  - stderr text
  - exit code
  - stdout shape when applicable (activity output differs by mode)
- For success paths, assert full output snapshot.

### Interactive mode: args vs. prompts

For error flows (exceptions, mutation errors, validation failures), supply all required values through command arguments. Do not simulate interactive prompts for these paths — prompts add noise and make tests harder to read.

Interactive prompt simulation (via `AddInput`) should be used sparingly. Test the interactive happy flow with prompts, because that exercises the real prompt-driven path. For everything else, prefer arguments.

## Suggested test grouping order

Use a stable order to keep suites readable.

1. Help test
2. Prerequisite/authentication tests
3. Required options tests
4. Validation and branching tests
5. Success tests by mode
6. Exception tests by mode

## Completion checklist (must pass before merge)

- [ ] Help output test exists
- [ ] Interactive mode covered
- [ ] Non-interactive mode covered
- [ ] JSON output mode covered
- [ ] Parser-level required options test exists (single consolidated test)
- [ ] All ExecuteAsync custom validation errors tested
- [ ] All meaningful input combinations tested
- [ ] Cursor option covered in all interaction modes for list commands that expose `--cursor`
- [ ] NitroClientException tested in all three modes
- [ ] NitroClientAuthorizationException tested in all three modes
- [ ] Six explicit ClientThrows tests exist (2 per exception type, one per mode)
- [ ] Test method names follow the naming conventions in this document
- [ ] All mutation typed error branches tested in all three modes (three distinct theories + shared MemberData factory)
- [ ] Every command branch/end-state mapped to at least one test

## Optional branch coverage map template

Use this in PR descriptions or comments when adding/changing command tests.

- Branch: <condition>
  - True path: <test name>
  - False path: <test name>
- Branch: <condition>
  - True path: <test name>
  - False path: <test name>
