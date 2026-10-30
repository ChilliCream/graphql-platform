# Updating .NET

What to check when `TargetFrameworks` in `src/Directory.Build.props` or the SDK version in `global.json` changes. Each section covers code that follows the source of ASP.NET Core or .NET, so an upstream change can call for the same change here.

## Multipart form reader

`MultipartFormReader` in `src/HotChocolate/AspNetCore/src/AspNetCore.Pipeline/Parsers` reads the form of a multipart request for `HttpMultipartMiddleware`. It follows the multipart branch of `FormFeature.InnerReadFormAsync`, and its antiforgery check, in `src/Http/Http/src/Features/FormFeature.cs` on `dotnet/aspnetcore`. `MultipartReader`, `FileBufferingReadStream`, `FormFile`, and header parsing stay the framework's, so their fixes reach applications through servicing releases.

The reader was last compared with these tags, whose multipart branches are identical:

| Target framework | Tag                      |
| ---------------- | ------------------------ |
| `net8.0`         | `v8.0.31`                |
| `net9.0`         | `v9.0.20`                |
| `net10.0`        | `v10.0.12`               |
| `net11.0`        | `v11.0.0-rc.1.26425.128` |

To list the upstream changes between a recorded tag and a new one, here from `v10.0.12` to `v10.0.13`:

```bash
git clone --filter=blob:none --no-checkout https://github.com/dotnet/aspnetcore.git
git -C aspnetcore log --oneline v10.0.12..v10.0.13 -- src/Http/Http/src/Features/FormFeature.cs
```

Apply a change to the section loop, its limits, the file buffering, or the antiforgery check to the reader, and record the new tag in the table.

The reader differs from `FormFeature` in these ways:

- Each limit has its own error code. `MultipartBodyLengthLimit` applies to every section (`HC0135`), and `maxAllowedRequestSize` also applies to the `operations` field (`HC0010`); whichever of the two is smaller limits `operations` and gives its code. `BufferBodyLengthLimit`, when `BufferBody` is set, applies to the whole body (`HC0136`). `FormFeature` does not apply `maxAllowedRequestSize`; it throws `InvalidDataException` for `MultipartBodyLengthLimit` and `IOException` for `BufferBodyLengthLimit`.
- With `BufferBody` set, it buffers the body even when an earlier component already made the body seekable, so `BufferBodyLengthLimit` applies in that case too.
- It refuses a request whose `IAntiforgeryValidationFeature` has `IsValid` set to `false`, without the `HttpContext.Items` keys that `FormFeature` checks first.
- It does not read URL-encoded forms, which never reach the multipart middleware.
- It does not apply endpoint `IFormOptionsMetadata`, which the public `FormFeature` constructor does not apply either.
