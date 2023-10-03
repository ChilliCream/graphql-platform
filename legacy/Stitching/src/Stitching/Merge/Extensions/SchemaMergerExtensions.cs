using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Properties;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge;

public static class SchemaMergerExtensions
{
    public static ISchemaMerger IgnoreRootTypes(
        this ISchemaMerger schemaMerger,
        string? schemaName = null)
    {
        if (schemaMerger == null)
        {
            throw new ArgumentNullException(nameof(schemaMerger));
        }

        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return schemaMerger.AddDocumentRewriter(
            new RemoveRootTypeRewriter(schemaName));
    }

    public static ISchemaMerger IgnoreType(
        this ISchemaMerger schemaMerger,
        string typeName,
        string? schemaName = null)
    {
        if (schemaMerger == null)
        {
            throw new ArgumentNullException(nameof(schemaMerger));
        }

        typeName.EnsureGraphQLName(nameof(typeName));
        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return schemaMerger.AddDocumentRewriter(
            new RemoveTypeRewriter(typeName, schemaName));
    }

    public static ISchemaMerger IgnoreField(
        this ISchemaMerger schemaMerger,
        FieldReference field,
        string? schemaName = null)
    {
        if (schemaMerger is null)
        {
            throw new ArgumentNullException(nameof(schemaMerger));
        }

        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return schemaMerger.AddTypeRewriter(
            new RemoveFieldRewriter(field, schemaName));
    }

    public static ISchemaMerger RenameType(
        this ISchemaMerger schemaMerger,
        string originalTypeName,
        string newTypeName,
        string? schemaName = null)
    {
        if (schemaMerger is null)
        {
            throw new ArgumentNullException(nameof(schemaMerger));
        }

        originalTypeName.EnsureGraphQLName(nameof(originalTypeName));
        newTypeName.EnsureGraphQLName(nameof(newTypeName));
        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return schemaMerger.AddTypeRewriter(
            new RenameTypeRewriter(originalTypeName, newTypeName, schemaName));
    }

    public static ISchemaMerger RenameField(
        this ISchemaMerger schemaMerger,
        FieldReference field,
        string newFieldName,
        string? schemaName = null)
    {
        if (schemaMerger is null)
        {
            throw new ArgumentNullException(nameof(schemaMerger));
        }

        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        newFieldName.EnsureGraphQLName(nameof(newFieldName));
        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return schemaMerger.AddTypeRewriter(
            new RenameFieldRewriter(field, newFieldName, schemaName));
    }

    [Obsolete("Use AddTypeMergeHandler")]
    public static ISchemaMerger AddMergeHandler<T>(
        this ISchemaMerger merger)
        where T : class, ITypeMergeHandler =>
        AddTypeMergeHandler<T>(merger);

    public static ISchemaMerger AddTypeMergeHandler<T>(
        this ISchemaMerger merger)
        where T : class, ITypeMergeHandler
    {
        if (merger == null)
        {
            throw new ArgumentNullException(nameof(merger));
        }

        merger.AddTypeMergeRule(CreateTypeMergeRule<T>());

        return merger;
    }

    public static ISchemaMerger AddDirectiveMergeHandler<T>(
        this ISchemaMerger merger)
        where T : class, IDirectiveMergeHandler
    {
        if (merger == null)
        {
            throw new ArgumentNullException(nameof(merger));
        }

        merger.AddDirectiveMergeRule(CreateDirectiveMergeRule<T>());

        return merger;
    }

    internal static MergeTypeRuleFactory CreateTypeMergeRule<T>()
        where T : class, ITypeMergeHandler
    {
        var constructor =
            CreateHandlerInternal<T, MergeTypeRuleDelegate>();

        return next =>
        {
            var handler = (ITypeMergeHandler)constructor
                .Invoke(new object[] { next });
            return handler.Merge;
        };
    }

    internal static MergeDirectiveRuleFactory CreateDirectiveMergeRule<T>()
        where T : class, IDirectiveMergeHandler
    {
        var constructor =
            CreateHandlerInternal<T, MergeDirectiveRuleDelegate>();

        return next =>
        {
            var handler = (IDirectiveMergeHandler)constructor
                .Invoke(new object[] { next });
            return handler.Merge;
        };
    }

    private static ConstructorInfo CreateHandlerInternal<THandler, TRule>()
        where THandler : class
    {
        var constructor = typeof(THandler).GetTypeInfo()
            .DeclaredConstructors.SingleOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 1
                    && parameters[0].ParameterType ==
                    typeof(TRule);
            });

        if (constructor == null)
        {
            throw new ArgumentException(StitchingResources
                .SchemaMergerExtensions_NoValidConstructor);
        }

        return constructor;
    }
}
