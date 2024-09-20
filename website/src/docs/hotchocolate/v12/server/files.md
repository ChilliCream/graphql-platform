---
title: Files
---

Handling files is traditionally not a concern of a GraphQL server, which is also why the [GraphQL over HTTP](https://github.com/graphql/graphql-over-http/blob/main/spec/GraphQLOverHTTP.md) specification doesn't mention it.

That being said, we recognize that at some point in the development of a new application you'll likely have to deal with files in some way or another. Which is why we want to give you some guidance on this topic.

# Uploading files

When it comes to uploading files there are a couple of options we have.

## Completely decoupled

We could handle file uploads completely decoupled from our GraphQL server, for example using a dedicated web application offering a HTTP endpoint for us to upload our files to.

This however has a couple of downsides:

- Authentication and authorization need to be handled by this dedicated endpoint as well.
- The process of uploading a file would need to be documented outside of our GraphQL schema.

## Upload scalar

Hot Chocolate implements the [GraphQL multipart request specification](https://github.com/jaydenseric/graphql-multipart-request-spec) which adds a new `Upload` scalar and allows our GraphQL server to handle file upload streams.

<Video videoId="XeF3IuGDq4A" />

> Warning: Files can not yet be uploaded through a gateway to stitched services using the `Upload` scalar.

### Usage

In order to use file upload streams in our input types or as an argument register the `Upload` scalar like the following:

```csharp
services
    .AddGraphQLServer()
    .AddType<UploadType>();
```

> Note: The `Upload` scalar can only be used as an input type and does not work on output types.

We can use the `Upload` scalar as an argument like the following:

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

        // We can now work with standard stream functionality of .NET
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

                // We can now work with standard stream functionality of .NET
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

[Learn more about arguments](/docs/hotchocolate/v12/defining-a-schema/arguments)

In input object types it can be used like the following.

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

[Learn more about input object types](/docs/hotchocolate/v12/defining-a-schema/input-object-types)

If you need to upload a list of files, it works exactly as you would expect. You just use a `List<IFile>` or `ListType<UploadType>`.

[Learn more about lists](/docs/hotchocolate/v12/defining-a-schema/lists)

### Client usage

When performing a mutation with the `Upload` scalar, we need to use variables.

An example mutation could look like the following:

```graphql
mutation ($file: Upload!) {
  uploadFile(file: $file) {
    success
  }
}
```

If we now want to send this request to our GraphQL server, we need to do so using HTTP multipart:

```bash
curl localhost:5000/graphql \
  -F operations='{ "query": "mutation ($file: Upload!) { uploadFile(file: $file) { success } }", "variables": { "file": null } }' \
  -F map='{ "0": ["variables.file"] }' \
  -F 0=@file.txt

```

> Note: The `$file` variable is intentionally `null`. It is filled in by Hot Chocolate on the server.

[More examples can be found here](https://github.com/jaydenseric/graphql-multipart-request-spec#examples)

You can check if your GraphQL client supports the specification [here](https://github.com/jaydenseric/graphql-multipart-request-spec#client).

Both Relay and Apollo support this specification through community packages:

- [react-relay-network-modern](https://github.com/relay-tools/react-relay-network-modern) using the `uploadMiddleware`
- [apollo-upload-client](https://github.com/jaydenseric/apollo-upload-client)

> Warning: [Strawberry Shake](/products/strawberryshake) does not yet support the `Upload` scalar.

### Options

If you need to upload larger files or set custom upload size limits, you can configure those by registering custom [`FormOptions`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.features.formoptions).

```csharp
services.Configure<FormOptions>(options =>
{
    // Set the limit to 256 MB
    options.MultipartBodyLengthLimit = 268435456;
});
```

Based on our WebServer we might need to configure these limits elsewhere as well. [Kestrel](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#kestrel-maximum-request-body-size) and [IIS](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#iis) are covered in the ASP.NET Core Documentation.

## Presigned upload URLs

The arguably best solution for uploading files is a hybrid of the above. Our GraphQL server still provides a mutation for uploading files, **but** the mutation is only used to setup a file upload. The actual file upload is done through a dedicated endpoint.

We can accomplish this by returning _presigned upload URLs_ from our mutations. _Presigned upload URLs_ are URLs that point to an endpoint, through which we can upload our files. Files can only be uploaded to this endpoint, if the URL to this endpoint contains a valid token. Our mutation generates said token, appends the token to the upload URL and returns the _presigned_ URL to the client.

Let's take a look at a quick example. We have built the following mutation resolver:

```csharp
public record ProfilePictureUploadPayload(string UploadUrl);

public class Mutation
{
    [Authorize]
    public ProfilePictureUploadPayload UploadProfilePicture()
    {
        var baseUrl = "https://blob.chillicream.com/upload";

        // Here we can handle our authorization logic

        // If the user is allowed to upload the profile picture
        // we generate the token
        var token = "myUploadToken";

        var uploadUrl = QueryHelpers.AddQueryString(baseUrl, "token", token);

        return new(uploadUrl);
    }
}
```

If you are using any of the big cloud providers for storing your BLOBs, chances are they already come with support for _presigned upload URLs_:

- [Azure Storage shared access signatures](https://docs.microsoft.com/azure/storage/common/storage-sas-overview)
- [AWS presigned URLS](https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html)
- [GCP signed URLs](https://cloud.google.com/storage/docs/access-control/signed-urls)

If you need to implement the file upload endpoint yourself, you can research best practices for creating _presigned upload URLs_.

Let's take a look at how a client would upload a new profile picture.

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

Given the `uploadUrl` our client can now HTTP POST the file to this endpoint to upload his profile picture.

This solution offers the following benefits:

- Uploading files is treated as a separate concern and our GraphQL server is kept _pure_ in a sense.
- The GraphQL server maintains control over authorization and all of the business logic regarding granting a file upload stays in one place.
- The action of uploading a profile picture is described by the schema and therefore more discoverable for developers.

There is still some uncertainty about how the actual file upload happens, e.g. which HTTP verb to use or which headers to send using the `uploadUrl`. These additional parameters can either be documented somewhere or be made queryable using our mutation.

# Serving files

Let's imagine we want to expose the file we just uploaded as the user's profile picture. How would we query for this file?

We _could_ make the profile picture a queryable field in our graph that returns the Base64 encoded image. While this _can_ work it has a number of downsides:

- Since the image is part of the JSON serialized GraphQL response, caching is incredibly hard.
- A query for the user's name might take a couple of milliseconds to transfer from the server to the client. Additionally querying for the image data might increase the response time by seconds.
- Let's not even think about how video playback, i.e. streaming, would work...

The recommended solution is to serve files through a different HTTP endpoint and only referencing this endpoint in our GraphQL response. So instead of querying for the profile picture we would query for an URL that points to the profile picture.

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

Serving the file through a dedicated HTTP endpoint makes caching a lot easier and also allows for features like streaming video. Ultimately it gives control to the client on how a resource should be handled, given its URL. In the case of a web application we can pass the `imageUrl` as `src` to a HTML `img` element and let the browser handle the fetching and caching of the image.

If you are using a cloud provider for file storage, chances are you are already accessing the files using an URL and you can simply expose this URL as a `String` field in your graph. If infrastructure for serving files is not already in place, you can look into how files can be served using ASP.NET Core or how to setup a dedicated web server like nginx to serve the files.
