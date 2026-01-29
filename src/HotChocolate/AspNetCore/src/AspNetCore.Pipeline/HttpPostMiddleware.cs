#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using HttpRequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public sealed class HttpPostMiddleware(HttpRequestDelegate next, HttpRequestExecutorProxy executor)
    : HttpPostMiddlewareBase(next, executor);
