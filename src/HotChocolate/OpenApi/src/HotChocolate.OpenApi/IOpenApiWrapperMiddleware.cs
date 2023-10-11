namespace HotChocolate.OpenApi;

internal interface IOpenApiWrapperMiddleware
{
    void Invoke(OpenApiWrapperContext context, OpenApiWrapperDelegate next);
}

internal delegate OpenApiWrapperDelegate OpenApiWrapperMiddleware(OpenApiWrapperDelegate next);

internal delegate void OpenApiWrapperDelegate(OpenApiWrapperContext context);
