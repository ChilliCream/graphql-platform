---
title: Uploading files
---

Hot Chocolate implements the GraphQL multipart request specification which allows it to handle file upload streams.

[Learn more about the specification](https://github.com/jaydenseric/graphql-multipart-request-spec)

# Usage

In order to use file upload streams in our input types or as an argument register the `Upload` scalar like the following.

```csharp
services
    .AddGraphQLServer()
    .AddType<UploadType>();
```

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

# Configuration

If we need to upload large files or set custom upload size limits, we can configure those by registering custom [`FormOptions`](https://docs.microsoft.com/dotnet/api/microsoft.aspnetcore.http.features.formoptions).

```csharp
services.Configure<FormOptions>(options =>
{
    // Set the limit to 256 MB
    options.MultipartBodyLengthLimit = 268435456;
});
```

Based on our WebServer we might need to configure these limits elsewhere as well. [Kestrel](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#kestrel-maximum-request-body-size) and [IIS](https://docs.microsoft.com/aspnet/core/mvc/models/file-uploads#iis) are covered in the ASP.NET Core Documentation.
