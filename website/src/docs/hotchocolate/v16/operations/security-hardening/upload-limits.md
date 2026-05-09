---
title: Harden upload limits
---

This page helps you harden Hot Chocolate v16 GraphQL endpoints that accept file uploads. It applies to ASP.NET Core servers that use the `Upload` scalar and `IFile` with the GraphQL multipart request specification. Fusion gateway upload routing is not covered here.

Uploads change server state. Model them as mutations, protect them like other state-changing operations, and decide where binary data should flow before you raise any limits.

# Prerequisites

This page applies when:

- You host Hot Chocolate v16 on ASP.NET Core.
- Your schema has, or will have, an authenticated mutation that accepts an uploaded file.
- You understand your ASP.NET Core middleware, endpoint, CORS, proxy, and hosting configuration.
- You can test upload behavior from the same network path your clients use in production.

A minimal upload-capable schema registers the upload scalar and accepts `IFile` on a mutation:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddMutationType<Mutation>()
    .AddUploadType();

public sealed class Mutation
{
    public Task<UploadAvatarPayload> UploadAvatarAsync(
        IFile file,
        CancellationToken cancellationToken)
    {
        // Validate, stream, scan, store, and return a domain payload.
        throw new NotImplementedException();
    }
}

public sealed record UploadAvatarPayload(string Url);
```

The schema exposes `Upload` as an input-only scalar:

```graphql
scalar Upload

type Mutation {
  uploadAvatar(file: Upload!): UploadAvatarPayload!
}
```

# Choose presigned URLs unless GraphQL must process the stream

Start with an architecture decision. Do not send binary streams through GraphQL when GraphQL only needs to authorize the upload or create metadata.

| Choose this approach                      | When it fits                                                                                                             |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Presigned URL or dedicated upload service | GraphQL checks authorization, returns upload constraints, and records metadata, but storage receives the bytes directly. |
| Multipart GraphQL upload                  | The resolver must inspect, transform, scan, or transactionally process the stream during the mutation.                   |

Presigned URL flows reduce CPU, memory, bandwidth, connection, timeout, and scanner pressure on the GraphQL server. A typical flow is:

1. The client calls an authenticated GraphQL mutation.
2. The mutation checks authorization and returns a short-lived upload URL plus constraints, such as size, content type, and expiry.
3. The client uploads bytes to object storage or an upload service.
4. The client or a storage event confirms completion.
5. The application scans, validates, and promotes the object before publishing it.

Use multipart GraphQL uploads when the mutation needs the stream as part of the domain operation. In that case, your GraphQL endpoint becomes an upload surface. Keep the surface narrow and set limits at every layer.

See [Files](/docs/hotchocolate/v16/server/files#presigned-upload-urls) for the basic presigned URL pattern.

# Enable multipart upload handling only where needed

Register `AddUploadType()` only for schemas that contain upload mutations. Then disable multipart handling on endpoints that do not accept files.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMutationType<Mutation>()
    .AddUploadType()
    .ModifyServerOptions(options =>
    {
        options.EnableMultipartRequests = false;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });
```

`EnableMultipartRequests` defaults to `true`. Set it explicitly so future maintainers can see that uploads are a deliberate choice.

When uploads need different limits, authentication policies, CORS headers, rate limits, or monitoring, expose a dedicated HTTP endpoint for upload mutations:

```csharp
app.MapGraphQLHttp("/graphql")
    .WithOptions(options =>
    {
        options.EnableMultipartRequests = false;
    });

app.MapGraphQLHttp("/graphql/uploads")
    .WithOptions(options =>
    {
        options.EnableMultipartRequests = true;
        options.EnforceMultipartRequestsPreflightHeader = true;
    });
```

Expected result:

- Normal GraphQL requests use `application/json` on `/graphql`.
- Upload requests use `multipart/form-data` on `/graphql/uploads`.
- Multipart requests without `GraphQL-Preflight: 1` receive `400 Bad Request`.

Keep `EnforceMultipartRequestsPreflightHeader` enabled. The preflight header protects against CSRF-style multipart form posts. Browser clients must send `GraphQL-Preflight: 1`, and your CORS policy must allow the `GraphQL-Preflight` request header.

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("GraphQLUploads", policy =>
    {
        policy
            .WithOrigins("https://app.example.com")
            .WithMethods("POST", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "GraphQL-Preflight")
            .AllowCredentials();
    });
});

app.MapGraphQLHttp("/graphql/uploads")
    .RequireCors("GraphQLUploads");
```

# Set request and file size limits at every layer

Use layered limits so the cheapest layer rejects first. Budget separate values for maximum single file size, total multipart body size, total files per mutation, concurrent uploads, request rate, and scanner or storage queue depth.

| Layer                          | Example setting                           | Protects                                     | Typical failure symptom                             | Owner                 |
| ------------------------------ | ----------------------------------------- | -------------------------------------------- | --------------------------------------------------- | --------------------- |
| Cloud edge, CDN, load balancer | Provider upload or body-size limit        | Public bandwidth and edge connection slots   | `413 Payload Too Large` before app logs             | Platform team         |
| Reverse proxy                  | `client_max_body_size 50m;` for nginx     | Proxy memory, disk buffering, upstream slots | `413`, proxy error page, truncated upstream request | Platform team         |
| IIS                            | `maxAllowedContentLength`                 | IIS request filtering                        | `404.13` or `413` depending on hosting              | Windows hosting owner |
| Kestrel                        | `serverOptions.Limits.MaxRequestBodySize` | ASP.NET Core request body size               | `413` from Kestrel                                  | App or hosting team   |
| ASP.NET Core forms             | `FormOptions.MultipartBodyLengthLimit`    | Multipart form parsing and buffering         | Invalid form or `413` during form parsing           | App team              |
| Hot Chocolate JSON parsing     | `AddGraphQL(maxAllowedRequestSize: ...)`  | Parsed GraphQL request bodies                | `413` for oversized JSON GraphQL bodies             | App team              |
| Resolver                       | `IFile.Length`, file count, domain rules  | Business limits per file and per mutation    | GraphQL error from mutation                         | App team              |

Configure hosting and form limits for multipart uploads:

```csharp
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 50L * 1024 * 1024;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50L * 1024 * 1024;
});
```

Configure Hot Chocolate's GraphQL request body limit separately:

```csharp
builder
    .AddGraphQL(maxAllowedRequestSize: 1 * 1000 * 1024)
    .AddMutationType<Mutation>();
```

`maxAllowedRequestSize` protects GraphQL request bodies parsed by Hot Chocolate, such as JSON `application/json` requests. It is not the multipart file-size limit. Multipart parsing happens through ASP.NET Core form handling before Hot Chocolate executes the mutation.

Enforce per-file limits in the resolver as a domain rule:

```csharp
const long MaxAvatarBytes = 5L * 1024 * 1024;

if (file.Length is null or 0 || file.Length > MaxAvatarBytes)
{
    throw new GraphQLException("The uploaded file is empty or too large.");
}
```

Expected result: an oversized upload fails at the earliest configured layer. If your edge limit is 50 MB, Kestrel and `FormOptions` should not be lower unless you intentionally want ASP.NET Core to reject the request.

# Validate the multipart request shape

A valid upload request is a `POST multipart/form-data` request with `operations`, `map`, and file parts. The client must use variables. Upload literals are not sent inline in the GraphQL document.

```bash
curl http://localhost:5000/graphql/uploads \
  -H "GraphQL-Preflight: 1" \
  -F operations='{ "query": "mutation ($file: Upload!) { uploadAvatar(file: $file) { url } }", "variables": { "file": null } }' \
  -F map='{ "0": ["variables.file"] }' \
  -F 0=@avatar.png
```

Expected response shape:

```json
{
  "data": {
    "uploadAvatar": {
      "url": "https://cdn.example.com/avatars/123.png"
    }
  }
}
```

Hot Chocolate expects:

- `operations` contains the GraphQL document and variables as JSON.
- Upload variables are `null` placeholders.
- `map` is JSON where each file key maps to one or more variable paths, for example `{ "0": ["variables.file"] }`.
- `map` appears after `operations`.
- Every mapped file key has a matching file part.
- Variable paths start under `variables`.

Common malformed requests include:

| Problem                   | What to check                                                             |
| ------------------------- | ------------------------------------------------------------------------- |
| Missing `operations`      | The multipart field is present, named `operations`, and contains JSON.    |
| `map` before `operations` | The client sends fields in the required order.                            |
| Invalid map JSON          | `map` deserializes to an object whose values are string arrays.           |
| Missing file part         | Each map key, such as `0`, has a multipart file field with the same name. |
| Invalid variable path     | Paths start with `variables` and point to an existing upload variable.    |
| Resolver receives `null`  | The mutation uses variables, and the map path matches the variable path.  |

For batched operations, multipart paths can include an operation index before `variables`. Avoid batching uploads unless you have a tested client and a clear per-batch limit.

# Validate file metadata and file contents in the mutation

Treat `IFile.Name`, `IFile.ContentType`, and file extensions as untrusted client input. Validate cheap metadata first, then inspect the stream without loading the whole file into memory.

```csharp
using HotChocolate.Authorization;

public sealed class Mutation
{
    private const long MaxAvatarBytes = 5L * 1024 * 1024;

    private static readonly HashSet<string> s_allowedContentTypes =
    [
        "image/png",
        "image/jpeg"
    ];

    private static readonly HashSet<string> s_allowedExtensions =
    [
        ".png",
        ".jpg",
        ".jpeg"
    ];

    [Authorize]
    public async Task<UploadAvatarPayload> UploadAvatarAsync(
        IFile file,
        IAvatarStorage storage,
        CancellationToken cancellationToken)
    {
        if (file.Length is null or 0 || file.Length > MaxAvatarBytes)
        {
            throw new GraphQLException("The uploaded file is empty or too large.");
        }

        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!s_allowedExtensions.Contains(extension) ||
            file.ContentType is null ||
            !s_allowedContentTypes.Contains(file.ContentType))
        {
            throw new GraphQLException("The uploaded file type is not allowed.");
        }

        var objectKey = $"avatars/{Guid.NewGuid():N}{extension}";
        await using var source = file.OpenReadStream();

        await storage.WriteAvatarAsync(
            objectKey,
            source,
            expectedContentTypes: s_allowedContentTypes,
            cancellationToken);

        return new UploadAvatarPayload(storage.GetPublicUrl(objectKey));
    }
}
```

The storage service should validate file signatures while copying. A forward-only stream is safer than code that assumes the upload stream can seek back to the beginning.

```csharp
public sealed class AvatarStorage
{
    public async Task WriteAvatarAsync(
        string objectKey,
        Stream source,
        IReadOnlySet<string> expectedContentTypes,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var bytesRead = await source.ReadAtLeastAsync(
            header,
            minimumBytes: 3,
            throwOnEndOfStream: false,
            cancellationToken);

        if (!HasAllowedImageSignature(header.AsSpan(0, bytesRead)))
        {
            throw new GraphQLException("The uploaded file type is not allowed.");
        }

        await using var destination = File.Create(objectKey);
        await destination.WriteAsync(header.AsMemory(0, bytesRead), cancellationToken);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private static bool HasAllowedImageSignature(ReadOnlySpan<byte> header)
    {
        var png = header.StartsWith([0x89, 0x50, 0x4E, 0x47]);
        var jpeg = header.StartsWith([0xFF, 0xD8, 0xFF]);

        return png || jpeg;
    }

    public string GetPublicUrl(string objectKey) => $"https://cdn.example.com/{objectKey}";
}
```

In production, write outside the web root or to object storage, not to an arbitrary path from the client. Generate storage keys on the server. Keep the original file name only as sanitized display metadata when the domain needs it.

Add domain-specific validation after you read enough data. Examples include image dimensions, pixel count, document page count, archive entry count, decompressed size, media duration, or scanner verdict. For archives, reject unsafe paths and decompression bombs before extracting any entry.

Return safe GraphQL errors. Do not echo raw file names, storage keys, scanner details, or path information back to the client.

# Stream, scan, and store safely

Handle uploads as a pipeline. Do not buffer the full file into `byte[]`, `MemoryStream`, logs, or GraphQL response data.

```csharp
public async Task<UploadAvatarPayload> UploadAvatarAsync(
    IFile file,
    IUploadStorage storage,
    IMalwareScanner scanner,
    CancellationToken cancellationToken)
{
    ValidateMetadata(file);

    var quarantineKey = storage.CreateQuarantineKey("avatars");
    var promoted = false;

    try
    {
        await using var source = file.OpenReadStream();
        await storage.WriteQuarantineAsync(quarantineKey, source, cancellationToken);

        var scan = await scanner.ScanAsync(quarantineKey, cancellationToken);
        if (!scan.IsClean)
        {
            throw new GraphQLException("The uploaded file did not pass validation.");
        }

        var publicKey = await storage.PromoteAsync(quarantineKey, cancellationToken);
        promoted = true;

        await storage.SaveMetadataAsync(publicKey, file.Length, cancellationToken);

        return new UploadAvatarPayload(storage.GetPublicUrl(publicKey));
    }
    finally
    {
        if (!promoted)
        {
            await storage.DeleteIfExistsAsync(quarantineKey, CancellationToken.None);
        }
    }
}
```

Expected outcome: uploaded bytes stay private until validation, scanning, promotion, and metadata persistence finish. If the copy, scan, storage call, or request is canceled, partial objects are cleaned up.

Use a pending or quarantined state when scanning can outlive the HTTP request. In that design, the mutation stores a private object, returns an upload or scan status, and a background process promotes the object after the scan passes.

# Protect upload mutations with authorization and tenant boundaries

Require authentication before issuing presigned URLs or accepting multipart uploads. Use mutation authorization, not only endpoint authorization.

```csharp
using HotChocolate.Authorization;

public sealed class Mutation
{
    [Authorize(Policy = "CanUploadAvatar")]
    public async Task<UploadAvatarPayload> UploadAvatarAsync(
        IFile file,
        IUserContext user,
        IAvatarStorage storage,
        CancellationToken cancellationToken)
    {
        var objectKey = $"tenants/{user.TenantId}/avatars/{Guid.NewGuid():N}";

        await using var stream = file.OpenReadStream();
        await storage.WriteAvatarAsync(objectKey, stream, cancellationToken);

        return new UploadAvatarPayload(storage.GetPublicUrl(objectKey));
    }
}
```

Enforce tenant and user ownership before accepting bytes and again before publishing stored files. Build object keys and metadata from trusted server-side values, not from client form fields.

Log upload decisions with correlation ID, authenticated user or tenant ID, size bucket, content type category, and rejection reason category. Avoid logging raw file names when they may contain personal or sensitive data.

See [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) for Hot Chocolate authorization setup.

# Rate limit and isolate upload capacity

Uploads consume bandwidth, disk, scanner capacity, storage API calls, and connection slots. Give upload endpoints a smaller abuse budget than normal query endpoints.

```csharp
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("graphql-uploads", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.AddConcurrencyLimiter("graphql-upload-concurrency", limiterOptions =>
    {
        limiterOptions.PermitLimit = 4;
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseRateLimiter();

app.MapGraphQLHttp("/graphql/uploads")
    .RequireRateLimiting("graphql-uploads")
    .RequireRateLimiting("graphql-upload-concurrency");
```

Use policies per user, IP, tenant, or API key when your application has the data needed for partitioning. Also consider infrastructure-level concurrency limits around scanner and storage queues.

Hot Chocolate `MaxConcurrentExecutions` controls concurrent GraphQL executions. It is useful, but it is not a byte-stream throttle. Keep separate rate, body-size, and storage/scanner limits.

# Avoid timeout, buffering, and memory pitfalls

Multipart parsing is performed by ASP.NET Core form handling before Hot Chocolate executes the mutation. `FormOptions` controls multipart parsing and buffering behavior. Hot Chocolate execution timeout protects GraphQL execution after the request has been parsed, but proxies, Kestrel, form parsing, scanners, and storage calls have their own limits and timeouts.

Watch these resource consumers:

- Edge and proxy buffering before the request reaches ASP.NET Core.
- ASP.NET Core multipart parsing and form buffering.
- Resolver code that copies into `MemoryStream` or `byte[]`.
- Malware scanner clients that buffer before sending to the scanner.
- Slow storage writes that occupy request and execution slots.
- Logs that accidentally record file content or sensitive file names.

Avoid these anti-patterns:

- Reading a large upload into memory to compute a hash or inspect type.
- Raising only `maxAllowedRequestSize` to fix a multipart `413`.
- Disabling multipart preflight enforcement to make browser uploads work.
- Returning file bytes from GraphQL.
- Storing unscanned files under the web root.

Prefer these fixes:

- Align edge, proxy, Kestrel, IIS, `FormOptions`, and resolver limits.
- Pass `CancellationToken` to stream, scanner, and storage operations.
- Hash, inspect, and copy incrementally while streaming.
- Use quarantine and pending states for slow scanning.
- Delete partial objects on failure or cancellation.

# Troubleshoot upload failures by symptom

Use the symptom to find the rejecting layer. Do not weaken the control until you know which layer produced the response.

| Symptom                                                         | Likely layer                                                                           | Checks                                                                                                                 | Safe fix                                                                                                                                           |
| --------------------------------------------------------------- | -------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `413 Payload Too Large`                                         | Edge, proxy, Kestrel, ASP.NET Core forms, or Hot Chocolate JSON body parser            | Compare response headers and logs from each layer. Check whether the request is multipart or JSON.                     | Change the intended layer consistently, or reduce file size. For multipart, check `FormOptions` and hosting limits before `maxAllowedRequestSize`. |
| `400 Bad Request` with `HC0077` or `MultiPartPreflightRequired` | Hot Chocolate multipart preflight enforcement                                          | Confirm the request sends `GraphQL-Preflight: 1`. For browsers, inspect the CORS preflight response.                   | Add the header and allow it in CORS. Keep enforcement enabled.                                                                                     |
| Browser CORS preflight fails before upload                      | Browser and ASP.NET Core CORS policy                                                   | Check allowed origin, `POST`, `OPTIONS`, `Authorization`, `Content-Type`, `GraphQL-Preflight`, and credentials policy. | Update the trusted-origin CORS policy for the upload endpoint.                                                                                     |
| `415 Unsupported Media Type`                                    | HTTP transport or client request shape                                                 | Verify upload operations use `multipart/form-data` and normal GraphQL POST requests use `application/json`.            | Send the correct content type for the operation.                                                                                                   |
| `No operations specified` or map errors                         | Multipart request shape                                                                | Verify `operations`, field order, valid `map` JSON, file keys, and variable paths.                                     | Fix the client multipart serializer or request construction.                                                                                       |
| Resolver receives `null` or the wrong file                      | Multipart map or GraphQL variables                                                     | Check that the mutation uses variables and the map path matches the variable path.                                     | Use `variables.file` or the correct nested path.                                                                                                   |
| Upload starts, then fails or truncates                          | Client abort, proxy timeout, Kestrel limit, form limit, scanner, storage, cancellation | Correlate client, proxy, app, scanner, and storage logs. Check cleanup logs.                                           | Align timeouts and limits. Propagate cancellation and clean partial objects.                                                                       |
| Memory spikes                                                   | App, form buffering, scanner client, high concurrency                                  | Look for `MemoryStream`, `byte[]`, large buffering thresholds, or logging file content.                                | Stream forward, lower concurrency, and use scanner/storage clients that stream.                                                                    |
| Upload succeeds but file is not visible                         | Quarantine, scanner, promotion, metadata, storage ACL, CDN cache                       | Check scan status, promotion step, metadata transaction, object ACL, and CDN propagation.                              | Keep a pending status until the object is scanned and promoted.                                                                                    |

# Operational checklist before enabling uploads

Before you enable multipart uploads in production, verify:

- The presigned URL decision is documented.
- Multipart is disabled on endpoints that do not need it.
- `Upload` is registered only for schemas with upload mutations.
- Multipart preflight header enforcement remains enabled.
- CORS allows `GraphQL-Preflight` only for trusted browser origins.
- Edge, proxy, Kestrel, IIS, `FormOptions`, Hot Chocolate JSON body, and resolver limits are aligned.
- Per-file, per-request, per-user, and per-tenant budgets are documented.
- Upload mutations require authentication and domain authorization.
- Content type, extension, magic bytes, and domain-specific file checks are implemented.
- Files stream to quarantine or storage without full-memory buffering.
- Malware scanning or equivalent business review happens before publication.
- Partial upload cleanup is tested.
- Rate and concurrency limits apply to upload capacity.
- Monitoring tracks accepted and rejected uploads by status, layer, size bucket, user or tenant, and failure category.
- The runbook covers `413`, missing preflight, malformed maps, CORS failures, scanner failures, storage failures, and cleanup failures.

# Next steps

- Learn the basic `Upload` scalar and `IFile` flow in [Files](/docs/hotchocolate/v16/server/files).
- Review multipart preflight behavior in [HTTP Transport](/docs/hotchocolate/v16/server/http-transport#preflight-header-enforcement).
- Configure endpoint options in [Endpoints](/docs/hotchocolate/v16/server/endpoints).
- Tune parser, validation, execution, and timeout limits in [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits).
- Protect mutations with [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization).
- Review ASP.NET Core file upload guidance for `FormOptions`, Kestrel, IIS, buffering, and request body limits.
- Review ASP.NET Core rate limiting when you need per-user, per-tenant, or per-IP upload budgets.
- Use the GraphQL multipart request specification when you debug client request shape.
