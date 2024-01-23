using static HotChocolate.OpenApi.Properties.OpenApiResources;

namespace HotChocolate.OpenApi;

public sealed class OpenApiFieldNameNullException() 
    : Exception(OpenApiFieldNameNullException_DefaultMessage);
