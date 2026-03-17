---
title: Files
---

Handling files is traditionally not a concern of a GraphQL server, which is also why the [GraphQL over HTTP](https://github.com/graphql/graphql-over-http/blob/main/spec/GraphQLOverHTTP.md) specification does not mention it.

That said, at some point in the development of a new application you will likely have to deal with files in some way. This page gives you guidance on the available approaches.

# Uploading Files

When it comes to uploading files, you have several options.

## Completely Decoupled

You can handle file uploads completely decoupled from your GraphQL server, for example using a dedicated web application that offers an HTTP endpoint for uploads.

This has a couple of downsides:

- Authentication and authorization need to be handled by the dedicated endpoint as well.
- The process of uploading a file needs to be documented outside of your GraphQL schema.

## Upload Scalar

Hot Chocolate implements the [GraphQL multipart request specification](https://github.com/jaydenseric/graphql-multipart-request-spec) which adds a new `Upload` scalar and lets your GraphQL server handle file upload streams.

<Video videoId="XeF3IuGDq4A"></Video>

> Warning: Files cannot yet be uploaded through a gateway to stitched services using the `Upload` scalar.

### Usage

Register the `Upload` scalar to use file upload streams in your input types or as an argument:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddType<UploadType>();
```

> Note: The `Upload` scalar can only be used as an input type and does not work on output types.

Use the `Upload` scalar as an argument:

<ExampleTabs>
<Implementation>

```csharp
public class Mutation
{
    public async Task<bool> UploadFileAsync(IFile file)
    {
        var fileName = file.Name;
        var fileSize = file.Length;

        await using Stream stream = file.OpenReadStream();

        // You can now work with standard stream functionality of .NET
        // to handle the file.
    }
}
```

</Implementation>
<Code>

```csharp
public class MutationType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("uploadFile")
            .Argument("file", a => a.Type<UploadType>())
            .Resolve(async context =>
            {
                var file = context.ArgumentValue<IFile>("file");

                var fileName = file.Name;
                var fileSize = file.Length;

                await using Stream stream = file.OpenReadStream();

                // You can now work with standard stream functionality of .NET
                // to handle the file.
            });
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

[Learn more about arguments](/docs/hotchocolate/v16/building-a-schema/arguments)

In input object types you can use it as follows:

<ExampleTabs>
<Implementation>

```csharp
public class ExampleInput
{
    [GraphQLType(typeof(NonNullType<UploadType>))]
    public IFile File { get; set; }
}
```

</Implementation>
<Code>

```csharp
public class ExampleInput
{
    public IFile File { get; set; }
}

public class ExampleInputType : InputObjectType<ExampleInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<ExampleInput> descriptor)
    {
        descriptor.Field(f => f.File).Type<UploadType>();
    }
}
```

</Code>
<Schema>

Take a look at the implementation-first or code-first example.

</Schema>
</ExampleTabs>

[Learn more about input object types](/docs/hotchocolate/v16/building-a-schema/input-object-types)

If you need to upload a list of files, use a `List<IFile>` or `ListType<UploadType>`.

[Learn more about lists](/docs/hotchocolate/v16/building-a-schema/lists)

### UploadValueNode

In v16, the upload literal node was renamed from `FileValueNode` to `UploadValueNode`. If you reference this type in custom scalar logic or tests, update your code:

```csharp
if (valueLiteral is UploadValueNode uploadValue)
{
    var file = uploadValue.File;
    var key = uploadValue.Key;
}
```

When constructing upload value nodes manually, the constructor now also requires the multipart key:

```csharp
var valueNode = new UploadValueNode("0", file);
```

### Client Usage

When performing a mutation with the `Upload` scalar, you need to use variables.

An example mutation:

```graphql
mutation ($file: Upload!) {
  uploadFile(file: $file) {
    success
  }
}
```

Send this request to your GraphQL server using HTTP multipart:

```bash
curl localhost:5000/graphql \
  -H "GraphQL-preflight: 1" \
  -F operations='{ "query": "mutation ($file: Upload!) { uploadFile(file: $file) { success } }", "variables": { "file": null } }' \
  -F map='{ "0": ["variables.file"] }' \
  -F 0=@file.txt

```

> Note 1: The `$file` variable is intentionally `null`. Hot Chocolate fills it in on the server.

> Note 2: The `GraphQL-preflight: 1` HTTP header is required since version 13.2 for security reasons.

[More examples can be found here](https://github.com/jaydenseric/graphql-multipart-request-spec#examples)

You can check if your GraphQL client supports the specification [here](https://github.com/jaydenseric/graphql-multipart-request-spec#client).

Both Relay and Apollo support this specification through community packages:

- [react-relay-network-modern](https://github.com/relay-tools/react-relay-network-modern) using the `uploadMiddleware`
- [apollo-upload-client](https://github.com/jaydenseric/apollo-upload-client)

### Options

If you need to upload larger files or set custom upload size limits, configure [`FormOptions`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.features.formoptions):

```csharp
builder.Services.Configure<FormOptions>(options =>
{
    // Set the limit to 256 MB
    options.MultipartBodyLengthLimit = 268435456;
});
```

Depending on your web server, you might need to configure these limits elsewhere as well. [Kestrel](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#kestrel-maximum-request-body-size) and [IIS](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#iis) are covered in the ASP.NET Core documentation.

## Presigned Upload URLs

The best solution for uploading files is a hybrid approach. Your GraphQL server provides a mutation for uploading files, **but** the mutation only sets up the file upload. The actual file upload happens through a dedicated endpoint.

You accomplish this by returning _presigned upload URLs_ from your mutations. These are URLs that point to an endpoint through which files can be uploaded. Files can only be uploaded to this endpoint if the URL contains a valid token. Your mutation generates the token, appends it to the upload URL, and returns the presigned URL to the client.

Here is an example mutation resolver:

```csharp
public record ProfilePictureUploadPayload(string UploadUrl);

public class Mutation
{
    [Authorize]
    public ProfilePictureUploadPayload UploadProfilePicture()
    {
        var baseUrl = "https://blob.chillicream.com/upload";

        // Handle authorization logic here

        // If the user is allowed to upload, generate the token
        var token = "myUploadToken";

        var uploadUrl = QueryHelpers.AddQueryString(baseUrl, "token", token);

        return new(uploadUrl);
    }
}
```

If you are using a major cloud provider for storing your BLOBs, they likely support presigned upload URLs:

- [Azure Storage shared access signatures](https://docs.microsoft.com/azure/storage/common/storage-sas-overview)
- [AWS presigned URLs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html)
- [GCP signed URLs](https://cloud.google.com/storage/docs/access-control/signed-urls)

Here is how a client would upload a new profile picture:

**Request**

```graphql
mutation {
  uploadProfilePicture {
    uploadUrl
  }
}
```

**Response**

```json
{
  "data": {
    "uploadProfilePicture": {
      "uploadUrl": "https://blob.chillicream.com/upload?token=myUploadToken"
    }
  }
}
```

Given the `uploadUrl`, the client can HTTP POST the file to this endpoint to upload the profile picture.

This solution offers the following benefits:

- Uploading files is treated as a separate concern and your GraphQL server stays focused on GraphQL.
- The GraphQL server maintains control over authorization and all business logic regarding granting a file upload stays in one place.
- The action of uploading a profile picture is described by the schema and therefore more discoverable for developers.

There is still some uncertainty about how the actual file upload happens, such as which HTTP verb to use or which headers to send with the `uploadUrl`. These additional parameters can be documented separately or made queryable through your mutation.

# Serving Files

Imagine you want to expose the file you uploaded as the user's profile picture. How do you query for this file?

You _could_ make the profile picture a queryable field that returns the Base64-encoded image. While this _can_ work, it has several downsides:

- Since the image is part of the JSON serialized GraphQL response, caching is very difficult.
- A query for the user's name might take a few milliseconds. Adding the image data might increase the response time by seconds.
- Streaming (for example, video playback) would not work.

The recommended solution is to serve files through a different HTTP endpoint and reference that endpoint in your GraphQL response. Instead of querying for the profile picture data, query for a URL that points to the profile picture.

**Request**

```graphql
{
  user {
    name
    imageUrl
  }
}
```

**Response**

```json
{
  "data": {
    "user": {
      "name": "John Doe",
      "imageUrl": "https://blob.chillicream.com/john-doe.png"
    }
  }
}
```

Serving the file through a dedicated HTTP endpoint makes caching much easier and supports features like streaming. It gives the client control over how a resource is handled given its URL. In a web application, you pass the `imageUrl` as `src` to an HTML `img` element and let the browser handle fetching and caching.

If you are using a cloud provider for file storage, you are likely already accessing files using a URL and can expose this URL as a `String` field in your graph. If infrastructure for serving files is not in place, you can set up file serving with ASP.NET Core or a dedicated web server like nginx.

# Troubleshooting

## "Upload scalar can only be used as input"

The `Upload` scalar is only valid as an input type. You cannot use it as a return type for a field. To expose files in responses, return a URL string pointing to the file instead.

## File upload fails with 413 or size limit error

Configure `FormOptions` to increase `MultipartBodyLengthLimit`. Depending on your hosting environment, you may also need to adjust Kestrel or IIS request body size limits.

## "FileValueNode" not found after upgrading to v16

The type was renamed to `UploadValueNode`. Update all references in your custom scalar logic or tests.

# Next Steps

- [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments) for details on defining input arguments.
- [Input Object Types](/docs/hotchocolate/v16/building-a-schema/input-object-types) for defining complex input types.
- [Migrate from v15 to v16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#filevaluenode-renamed-to-uploadvaluenode) for the `FileValueNode` rename details.
