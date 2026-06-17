using Basic.Reference.Assemblies;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

/// <summary>
/// Provides the metadata references that mirror the HotChocolate package graph an
/// application using the source generator would compile against. The list is shared by
/// every in-memory compilation in this test project so that the generated registration
/// code, the HotChocolate runtime, and the test process all bind to the same loaded
/// assemblies (a requirement for type identity to hold across assembly load contexts).
/// </summary>
internal static class GeneratorReferences
{
    /// <summary>
    /// Gets the metadata references for an in-memory HotChocolate compilation. The list
    /// combines the framework reference assemblies for the active target framework with
    /// metadata references to the loaded HotChocolate assemblies.
    /// </summary>
    public static IReadOnlyList<PortableExecutableReference> All { get; } =
    [
#if NET8_0
        .. Net80.References.All,
#elif NET9_0
        .. Net90.References.All,
#elif NET10_0
        .. Net100.References.All,
#endif
        // HotChocolate.Primitives
        MetadataReference.CreateFromFile(typeof(ITypeSystemMember).Assembly.Location),

        // HotChocolate.Execution
        MetadataReference.CreateFromFile(typeof(RequestDelegate).Assembly.Location),

        // HotChocolate.Execution.Abstractions
        MetadataReference.CreateFromFile(typeof(RequestContext).Assembly.Location),

        // HotChocolate.Execution.Processing
        MetadataReference.CreateFromFile(typeof(HotChocolateExecutionSelectionExtensions).Assembly.Location),

        // HotChocolate.Execution.Abstractions
        MetadataReference.CreateFromFile(typeof(IRequestExecutorBuilder).Assembly.Location),

        // HotChocolate.Execution.Operation.Abstractions
        MetadataReference.CreateFromFile(typeof(ISelection).Assembly.Location),

        // HotChocolate.Types
        MetadataReference.CreateFromFile(typeof(ObjectTypeAttribute).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(QueryTypeAttribute).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Connection).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(PageConnection<>).Assembly.Location),

        // HotChocolate.Types.Abstractions
        MetadataReference.CreateFromFile(typeof(ISchemaDefinition).Assembly.Location),

        // HotChocolate.Features
        MetadataReference.CreateFromFile(typeof(IFeatureProvider).Assembly.Location),

        // HotChocolate.Language
        MetadataReference.CreateFromFile(typeof(OperationType).Assembly.Location),

        // HotChocolate.Language.Utf8
        MetadataReference.CreateFromFile(typeof(ParserOptions).Assembly.Location),

        // HotChocolate.Language.Visitors
        MetadataReference.CreateFromFile(typeof(SyntaxVisitor).Assembly.Location),

        // HotChocolate.Abstractions
        MetadataReference.CreateFromFile(typeof(ParentAttribute).Assembly.Location),

        // HotChocolate.AspNetCore
        MetadataReference.CreateFromFile(
            typeof(HotChocolateAspNetCoreServiceCollectionExtensions).Assembly.Location),

        // GreenDonut
        MetadataReference.CreateFromFile(typeof(DataLoaderBase<,>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IDataLoader).Assembly.Location),

        // GreenDonut.Data
        MetadataReference.CreateFromFile(typeof(PagingArguments).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(IPredicateBuilder).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(DefaultPredicateBuilder).Assembly.Location),

        // HotChocolate.Data
        MetadataReference.CreateFromFile(typeof(IFilterContext).Assembly.Location),

        // Microsoft.AspNetCore
        MetadataReference.CreateFromFile(typeof(WebApplication).Assembly.Location),

        // Microsoft.Extensions.DependencyInjection.Abstractions
        MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),

        // Microsoft.AspNetCore.Authorization
        MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute).Assembly.Location),

        // HotChocolate.Authorization
        MetadataReference.CreateFromFile(typeof(Authorization.AuthorizeAttribute).Assembly.Location),

        // HotChocolate.Types.OffsetPagination
        MetadataReference.CreateFromFile(typeof(UseOffsetPagingAttribute).Assembly.Location)
    ];
}
