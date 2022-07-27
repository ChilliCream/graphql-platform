using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb.Filters;

/// <summary>
/// Base class for extensions on <see cref="MongoDbFilterProvider"/>
/// </summary>
public class MongoDbFilterProviderExtension
    : FilterProviderExtensions<MongoDbFilterVisitorContext>
{
    /// <summary>
    /// Creates a new instance of <see cref="MongoDbFilterProviderExtension"/>
    /// </summary>
    public MongoDbFilterProviderExtension()
    {
    }

    /// <summary>
    /// Creates and configures a new instance of <see cref="MongoDbFilterProviderExtension"/>
    /// </summary>
    public MongoDbFilterProviderExtension(
        Action<IFilterProviderDescriptor<MongoDbFilterVisitorContext>> configure)
        : base(configure)
    {
    }
}
