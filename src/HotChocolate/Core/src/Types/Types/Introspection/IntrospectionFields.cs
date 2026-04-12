using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Introspection;

internal static class IntrospectionFields
{
    private const int MaxFirstLimit = 150;

    private static readonly PureFieldDelegate s_typeNameResolver =
        ctx => ctx.ObjectType.Name;

    internal static ObjectFieldConfiguration CreateSchemaField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, IntrospectionFieldNames.Schema);

        descriptor
            .Description(TypeResources.SchemaField_Description)
            .Type<NonNullType<__Schema>>();

        var configuration = descriptor.Configuration;
        configuration.PureResolver = Resolve;
        configuration.Flags |= CoreFieldFlags.SchemaIntrospectionField | CoreFieldFlags.Introspection;

        static ISchemaDefinition Resolve(IResolverContext ctx)
            => ctx.Schema;

        return CreateConfiguration(descriptor);
    }

    internal static ObjectFieldConfiguration CreateTypeField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, IntrospectionFieldNames.Type);

        descriptor
            .Description(TypeResources.TypeField_Description)
            .Argument("name", a => a.Type<NonNullType<StringType>>())
            .Type<__Type>()
            .Resolve(Resolve);

        var configuration = descriptor.Configuration;
        configuration.PureResolver = Resolve;
        configuration.Flags |= CoreFieldFlags.TypeIntrospectionField | CoreFieldFlags.Introspection;

        static ITypeDefinition? Resolve(IResolverContext ctx)
        {
            var name = ctx.ArgumentValue<string>("name");
            return ctx.Schema.Types.TryGetType(name, out var type) ? type : null;
        }

        return CreateConfiguration(descriptor);
    }

    internal static ObjectFieldConfiguration CreateTypeNameField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, IntrospectionFieldNames.TypeName);

        descriptor
            .Description(TypeResources.TypeNameField_Description)
            .Type<NonNullType<StringType>>();

        var configuration = descriptor.Extend().Configuration;
        configuration.PureResolver = s_typeNameResolver;
        configuration.Flags |= CoreFieldFlags.TypeNameIntrospectionField | CoreFieldFlags.Introspection;

        return CreateConfiguration(descriptor);
    }

    internal static ObjectFieldConfiguration CreateSearchField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, IntrospectionFieldNames.Search);

        descriptor
            .Argument("query", a => a.Type<NonNullType<StringType>>())
            .Argument("first", a => a.Type<NonNullType<IntType>>().DefaultValue(10))
            .Argument("after", a => a.Type<StringType>())
            .Argument("min_score", a => a.Type<FloatType>())
            .Type<NonNullType<ListType<NonNullType<__SearchResult>>>>()
            .Resolve(Resolve);

        var configuration = descriptor.Configuration;
        configuration.Flags |= CoreFieldFlags.Introspection;

        static async ValueTask<object?> Resolve(IResolverContext ctx)
        {
            var provider = ctx.Schema.Services.GetService<ISchemaSearchProvider>();

            if (provider is null)
            {
                return Array.Empty<SchemaSearchResult>();
            }

            var query = ctx.ArgumentValue<string>("query");
            var first = ctx.ArgumentValue<int>("first");
            var after = ctx.ArgumentOptional<string?>("after");
            var minScore = ctx.ArgumentOptional<float?>("min_score");

            if (first <= 0)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("The `first` argument must be greater than zero.")
                        .Build());
            }

            if (first > MaxFirstLimit)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage($"The `first` argument must not exceed {MaxFirstLimit}.")
                        .Build());
            }

            try
            {
                return await provider.SearchAsync(
                    query,
                    first,
                    after.HasValue ? after.Value : null,
                    minScore.HasValue ? minScore.Value : null,
                    ctx.RequestAborted);
            }
            catch (InvalidSearchCursorException)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("The value of `after` is not a valid cursor.")
                        .Build());
            }
            catch (SearchQueryTooLargeException)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("The search query exceeds the maximum allowed length.")
                        .Build());
            }
        }

        return CreateConfiguration(descriptor);
    }

    internal static ObjectFieldConfiguration CreateDefinitionsField(IDescriptorContext context)
    {
        var descriptor = ObjectFieldDescriptor.New(context, IntrospectionFieldNames.Definitions);

        descriptor
            .Argument("coordinates", a => a.Type<NonNullType<ListType<NonNullType<StringType>>>>())
            .Type<NonNullType<ListType<NonNullType<__SchemaDefinition>>>>()
            .Resolve(Resolve);

        var configuration = descriptor.Configuration;
        configuration.Flags |= CoreFieldFlags.Introspection;

        static ValueTask<object?> Resolve(IResolverContext ctx)
        {
            var coordinates = ctx.ArgumentValue<string[]>("coordinates");

            if (coordinates.Length > MaxFirstLimit)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage($"The `coordinates` argument must not exceed {MaxFirstLimit} items.")
                        .Build());
            }

            var definitions = new List<object>(coordinates.Length);

            foreach (var coordinateString in coordinates)
            {
                if (!SchemaCoordinate.TryParse(coordinateString, out var coordinate))
                {
                    continue;
                }

                if (ctx.Schema.TryGetMember(coordinate.Value, out var definition))
                {
                    definitions.Add(definition);
                }
            }

            return new ValueTask<object?>(definitions);
        }

        return CreateConfiguration(descriptor);
    }

    private static ObjectFieldConfiguration CreateConfiguration(ObjectFieldDescriptor descriptor)
    {
        var configuration = descriptor.CreateConfiguration();
        configuration.IsIntrospectionField = true;
        return configuration;
    }
}
