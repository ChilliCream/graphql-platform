---
title: Files
---

# Uploading files

Hot Chocolate implements the GraphQL multipart request specification which allows it to handle file upload streams.

[Learn more about the specification](https://github.com/jaydenseric/graphql-multipart-request-spec)

## Usage

In order to use file upload streams in our input types or as an argument register the `Upload` scalar like the following.

```csharp
services
    .AddGraphQLServer()
    .AddType<UploadType>();
```

> ⚠️ Note: Files can not yet be uploaded from a gateway to stitched services using the `Upload` scalar.

In our resolver or input type we can then use the `IFile` interface to use the upload scalar.

```csharp
public class Mutation
{
    public async Task<bool> UploadFileAsync(IFile file)
    {
        using Stream stream = file.OpenReadStream();
        // we can now work with standard stream functionality of .NET
        // to handle the file.
    }

    public async Task<bool> UploadFiles(List<IFile> files)
    {
        // Omitted code for brevity
    }

    public async Task<bool> UploadFileInInput(ExampleInput input)
    {
        // Omitted code for brevity
    }
}

public class ExampleInput
{
    [GraphQLType(typeof(NonNullType<UploadType>))]
    public IFile File { get; set; }
}
```

> Note: The `Upload` scalar can only be used as an input type and does not work on output types.

## Configuration

If we need to upload large files or set custom upload size limits, we can configure those by registering custom [`FormOptions`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.features.formoptions).

```csharp
services.Configure<FormOptions>(options =>
{
    // Set the limit to 256 MB
    options.MultipartBodyLengthLimit = 268435456;
});
```

Based on our WebServer we might need to configure these limits elsewhere as well. [Kestrel](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#kestrel-maximum-request-body-size) and [IIS](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#iis) are covered in the ASP.NET Core Documentation.

## Client usage

TODO

> ⚠️ Note: [Strawberry Shake](/docs/strawberryshake) does not yet support the `Upload` scalar.

# Serving files

Lets imagine we have an application where we want to display user profiles with information such as a name and a profile picture. How would we query for the image?

We _could_ make the profile picture a queryable field in our graph that returns the Base64 encoded image. While this _can_ work it has a number of downsides:

- Since the image is part of the JSON serialized GraphQL response, caching is incredibly hard.
- A query for the user's name might take a couple of milliseconds to transfer from the server to the client. Additionally querying for the image data might increase the response time by seconds.
- Let's not even think about how video playback, i.e. streaming, would be done...

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
      "imageUrl": "http://img.chillicream.com/john-doe.png"
    }
  }
}
```

Serving the file through a dedicated HTTP endpoint makes caching a lot easier and also allows for features like streaming video. Ultimately it gives control to the client on how a resource should be handled, given its URL. In the case of a web application we can pass the `imageUrl` as `src` to a HTML `img` element and let the browser handle the fetching and caching of the image.

If you are using a cloud provider for file storage, chances are you are already accessing the files using an URL and you can simply expose this URL as a `String` field in your graph. If infrastructure for serving files is not already in place, you can look into how files can be served using ASP.NET Core or how to setup a dedicated web server like nginx to serve the files.
