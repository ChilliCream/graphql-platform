---
title: HTTP transport
---

Hot Chocolate implements the latest (February 2023) version of the [GraphQL over HTTP specification](https://github.com/graphql/graphql-over-http/blob/a1e6d8ca248c9a19eb59a2eedd988c204909ee3f/spec/GraphQLOverHTTP.md).

<!--
todo:
- types of responses (application/graphql-response+json, application/json, text/event-stream, multipart/mixed)
- HTTP status codes to expect
- incremental deliver via @defer / @stream (might be its own document)
 -->

<!-- todo: clean up the following section -->

# Types of requests

GraphQL requests over HTTP can be performed either via the POST or GET HTTP verb.

## POST requests

The GraphQL HTTP POST request is the most commonly used variant for GraphQL requests over HTTP and is specified [here](https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#post).

**request:**

```http
POST /graphql
HOST: foo.example
Content-Type: application/json

{
  "query": "query($id: ID!){user(id:$id){name}}",
  "variables": { "id": "QVBJcy5ndXJ1" }
}
```

**response:**

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "data": {
    "user": {
      "name": "Jon Doe"
    }
  }
}
```

## GET requests

GraphQL can also be served through an HTTP GET request. You have the same options as the HTTP POST request, just that the request properties are provided as query parameters. GraphQL HTTP GET requests can be a good choice if you are looking to cache GraphQL requests.

For example, if we wanted to execute the following GraphQL query:

```graphql
query ($id: ID!) {
  user(id: $id) {
    name
  }
}
```

With the following query variables:

```json
{
  "id": "QVBJcy5ndXJ1"
}
```

This request could be sent via an HTTP GET as follows:

**request:**

```http
GET /graphql?query=query(%24id%3A%20ID!)%7Buser(id%3A%24id)%7Bname%7D%7D&variables=%7B%22id%22%3A%22QVBJcy5ndXJ1%22%7D`
HOST: foo.example
```

**response:**

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "data": {
    "user": {
      "name": "Jon Doe"
    }
  }
}
```

> Note: {query} and {operationName} parameters are encoded as raw strings in the query component. Therefore if the query string contained operationName=null then it should be interpreted as the {operationName} being the string "null". If a literal null is desired, the parameter (e.g. {operationName}) should be omitted.

The GraphQL HTTP GET request is specified [here](https://github.com/graphql/graphql-over-http/blob/master/spec/GraphQLOverHTTP.md#get).

# DefaultHttpResponseFormatter

The `DefaultHttpResponseFormatter` abstracts how responses are delivered over HTTP.

You can override certain aspects of the formatter, by creating your own formatter, inheriting from the `DefaultHttpResponseFormatter`.

```csharp
public class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
{
    // ...
}
```

This formatter then needs to be registered.

```csharp
builder.Services.AddHttpResponseFormatter<CustomHttpResponseFormatter>();
```

If you want to pass `HttpResponseFormatterOptions` to a custom formatter, you need to make some slight adjustments.

```csharp
var options = new HttpResponseFormatterOptions();

builder.Services.AddHttpResponseFormatter(_ => new CustomHttpResponseFormatter(options));

public class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
{
    public CustomHttpResponseFormatter(HttpResponseFormatterOptions options) : base(options)
    {

    }
}
```

## Customizing status codes

You can use a custom formatter to alter the HTTP status code in certain conditions.

> Warning: Altering status codes can break the assumptions of your server's clients and might lead to issues. Proceed with caution!

```csharp
public class CustomHttpResponseFormatter : DefaultHttpResponseFormatter
{
    protected override HttpStatusCode OnDetermineStatusCode(
        IQueryResult result, FormatInfo format,
        HttpStatusCode? proposedStatusCode)
    {
        if (result.Errors?.Count > 0 &&
            result.Errors.Any(error => error.Code == "SOME_AUTH_ISSUE"))
        {
            return HttpStatusCode.Forbidden;
        }

        // In all other cases let Hot Chocolate figure out the
        // appropriate status code.
        return base.OnDetermineStatusCode(result, format, proposedStatusCode);
    }
}
```

# JSON serialization

You can alter some JSON serialization settings, when configuring the `HttpResponseFormatter`.

## Stripping nulls from response

Per default the JSON in your GraphQL responses contains `null`. If you want to save a couple bytes and your clients can handle it, you can strip these nulls from your responses:

```csharp
var options = new HttpResponseFormatterOptions
{
    Json = new JsonResultFormatterOptions
    {
        NullIgnoreCondition = JsonNullIgnoreCondition.All
    }
};

builder.Services.AddHttpResponseFormatter(options);
```

## Indenting JSON in response

Per default the JSON in your GraphQL responses isn't indented or what you'd call "pretty". If you want to indent your JSON, you can do so as follows:

```csharp
builder.Services.AddHttpResponseFormatter(indented: true);
```

Just be aware that indenting your JSON results in a _slightly_ larger response size.

If you are defining any other `HttpResponseFormatterOptions`, you can also configure the indentation through the `Json` property:

```csharp
var options = new HttpResponseFormatterOptions
{
    Json = new JsonResultFormatterOptions
    {
        Indented = true
    }
};

builder.Services.AddHttpResponseFormatter(options);
```

# Supporting legacy clients

Your clients might not yet support the new [GraphQL over HTTP specification](https://github.com/graphql/graphql-over-http/blob/a1e6d8ca248c9a19eb59a2eedd988c204909ee3f/spec/GraphQLOverHTTP.md). This can be problematic, since they might not be able to handle a different response `Content-Type` or HTTP status codes besides `200` and run into ugly errors.

If you have control over the client, you can either

- Update the client to support the GraphQL over HTTP specification
- Send the `Accept: application/json` request header in your HTTP requests, signaling that your client only understands the "legacy format"

If you can't update or change the `Accept` header your clients are sending, you can configure that a missing `Accept` header or a wildcard like `*/*` should be treated as `application/json`.

```csharp
builder.Services.AddHttpResponseFormatter(new HttpResponseFormatterOptions {
    HttpTransportVersion = HttpTransportVersion.Legacy
});
```

An `Accept` header with the value `application/json` will opt you out of the [GraphQL over HTTP](https://github.com/graphql/graphql-over-http/blob/a1e6d8ca248c9a19eb59a2eedd988c204909ee3f/spec/GraphQLOverHTTP.md) specification. The response `Content-Type` will now be `application/json` and a status code of 200 will be returned for every request, even if it had validation errors or a valid response could not be produced.

<!-- spell-checker:ignore Bname, Buser -->
