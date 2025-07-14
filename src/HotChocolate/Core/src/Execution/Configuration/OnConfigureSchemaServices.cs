using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration;

public delegate void OnConfigureSchemaServices(
    ConfigurationContext context,
    IServiceCollection services);
