using HotChocolate.Data.Filters;

namespace HotChocolate.Data.ElasticSearch.Filters;

/// <summary>
/// Provides extensions for <see cref="IFilterFieldDescriptor"/> to configure
/// <see cref="IElasticFilterMetadata"/>
/// </summary>
public static class ElasticFilterMetadataFilterFieldExtensions
{
    /// <summary>
    /// Configures the metadata for this field
    /// </summary>
    /// <code lang="csharp">
    /// descriptor
    ///      .Field(x => x.Name)
    ///      .ConfigureElastic(x => x.Path("thisIsTheOverride"))
    /// </code>
    /// <code lang="json">
    /// {
    ///    "match": {
    ///       "deep.thisIsTheOverride": {
    ///             "query": "The value of the field"
    ///       }
    ///    }
    /// }
    /// </code>
    public static IFilterFieldDescriptor ConfigureElastic(
        this IFilterFieldDescriptor field,
        Action<IElasticFilterMetadataDescriptor> configure)
    {
        field.Extend().OnBeforeCreate(Configure);

        void Configure(FilterFieldDefinition d)
        {
            if (!d.ContextData.TryGetValue(nameof(ElasticFilterMetadata), out object? m) ||
                m is not ElasticFilterMetadata metadata)
            {
                metadata = new ElasticFilterMetadata();
                d.ContextData[nameof(ElasticFilterMetadata)] = metadata;
            }

            configure(ElasticFilterMetadataDescriptor.Create(metadata));
        }

        return field;
    }
}
