using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Reflection;
using System.Text;
using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;
using ConditionFlags = HotChocolate.Execution.ConditionFlags;
using static HotChocolate.WellKnownMiddleware;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// Paging utilities.
/// </summary>
public static class PagingHelper
{
    private const int MaxStackallocPartitionKeySize = 256;
    private const string FirstArgumentName = "first";
    private const string AfterArgumentName = "after";
    private const string LastArgumentName = "last";
    private const string BeforeArgumentName = "before";

    internal static IObjectFieldDescriptor UsePaging(
        IObjectFieldDescriptor descriptor,
        Type? entityType,
        GetPagingProvider resolvePagingProvider,
        PagingOptions? options)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        FieldMiddlewareConfiguration placeholder = new(_ => _ => default, key: Paging);
        BatchFieldMiddlewareConfiguration batchPlaceholder = new(_ => _ => default, key: Paging);

        var definition = descriptor.Extend().Configuration;
        definition.MiddlewareConfigurations.Add(placeholder);
        definition.BatchMiddlewareConfigurations.Add(batchPlaceholder);
        definition.Tasks.Add(
            new OnCompleteTypeSystemConfigurationTask<ObjectFieldConfiguration>(
                (c, d) => ApplyConfiguration(
                    c,
                    d,
                    entityType,
                    options?.ProviderName,
                    resolvePagingProvider,
                    options,
                    placeholder,
                    batchPlaceholder),
                definition,
                ApplyConfigurationOn.BeforeCompletion));

        return descriptor;
    }

    private static void ApplyConfiguration(
        ITypeCompletionContext context,
        ObjectFieldConfiguration definition,
        Type? entityType,
        string? name,
        GetPagingProvider resolvePagingProvider,
        PagingOptions? options,
        FieldMiddlewareConfiguration placeholder,
        BatchFieldMiddlewareConfiguration batchPlaceholder)
    {
        options = context.GetPagingOptions(options);
        entityType ??= context.GetType<IOutputType>(definition.Type!).ToRuntimeType();

#pragma warning disable IL3050
        var source = GetSourceType(context.TypeInspector, definition, entityType);
#pragma warning restore IL3050
        var pagingProvider = resolvePagingProvider(context.Services, source, name);
        var pagingHandler = pagingProvider.CreateHandler(source, options);
        var middleware = CreateMiddleware(pagingHandler);
        var batchMiddleware = CreateBatchMiddleware(pagingHandler, source);

        var index = definition.MiddlewareConfigurations.IndexOf(placeholder);
        definition.MiddlewareConfigurations[index] = new(middleware, key: Paging);

        var batchIndex = definition.BatchMiddlewareConfigurations.IndexOf(batchPlaceholder);
        definition.BatchMiddlewareConfigurations[batchIndex] = new(batchMiddleware, key: Paging);
        definition.BatchPartitionKeyResolver ??= (definition.Flags & CoreFieldFlags.CollectionSegment) != 0
            ? GetOffsetPagingBatchPartitionKey
            : GetPagingBatchPartitionKey;
        definition.Features.Set(options);
    }

    [RequiresDynamicCode("Uses MakeGenericType to create generic types at runtime.")]
    private static IExtendedType GetSourceType(
        ITypeInspector typeInspector,
        ObjectFieldConfiguration definition,
        Type entityType)
    {
        var type = ResolveType();

        if (typeof(IFieldResult).IsAssignableFrom(type.Type))
        {
            return type.TypeArguments[0];
        }

        return type;

        IExtendedType ResolveType()
        {
            // if an explicit result type is defined we will type it since it expresses the
            // intent.
            if (definition.ResultType is not null)
            {
                return typeInspector.GetType(definition.ResultType);
            }

            // Otherwise we will look at specified members and extract the return type.
            var member = definition.ResolverMember ?? definition.Member;

            if (member is not null)
            {
                return typeInspector.GetReturnType(member, true);
            }

            // if we were not able to resolve the source type we will assume that it is
            // an enumerable of the entity type.
            return typeInspector.GetType(typeof(IEnumerable<>).MakeGenericType(entityType));
        }
    }

    private static FieldMiddleware CreateMiddleware(
        IPagingHandler handler)
        => next =>
        {
            var middleware = new PagingMiddleware(next, handler);
            return context => middleware.InvokeAsync(context);
        };

    private static BatchFieldMiddleware CreateBatchMiddleware(IPagingHandler handler, IExtendedType sourceType)
        => next => async contexts =>
        {
            foreach (var context in contexts)
            {
                if (HasErrorResult(context) || (context.IsResultModified && context.Result is null))
                {
                    continue;
                }

                try
                {
                    handler.ValidateContext(context);
                    handler.PublishPagingArguments(context);
                }
                catch (Exception ex)
                {
                    ReportPagingError(context, ex);
                }
            }

            await next(contexts).ConfigureAwait(false);

            foreach (var context in contexts)
            {
                if (HasErrorResult(context))
                {
                    continue;
                }

                try
                {
                    if (context.Result is IFieldResult fieldResult)
                    {
                        context.Result = fieldResult.Value;
                    }

                    if (context.Result is { } source and not IPage
                        && (sourceType.Type.IsInstanceOfType(source)
                            || (sourceType.IsArrayOrList && source is IEnumerable or IExecutable)))
                    {
                        context.Result = await handler
                            .SliceAsync(context, source)
                            .ConfigureAwait(false);
                    }

                    var observers = context.GetLocalStateOrDefault(
                        WellKnownContextData.PagingObserver,
                        ImmutableArray<IPageObserver>.Empty);

                    if (context.Result is IPage page)
                    {
                        foreach (var observer in observers)
                        {
                            page.Accept(observer);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ReportPagingError(context, ex);
                }
            }
        };

    private static bool HasErrorResult(IMiddlewareContext context)
        => context.Result is IError or IEnumerable<IError> or IFieldResult { IsError: true }
            || (context.HasErrors && context.Result is null);

    internal static void ReportPagingError(IMiddlewareContext context, Exception exception)
    {
        if (exception is GraphQLException graphQLException)
        {
            foreach (var error in graphQLException.Errors)
            {
                context.ReportError(error.WithPath(context.Path));
            }
        }
        else
        {
            context.ReportError(exception);
        }

        context.Result = null;
    }

    /// <summary>
    /// Gets the default page size bounded by the maximum page size.
    /// Unspecified sizes use the paging defaults.
    /// </summary>
    public static int GetEffectiveDefaultPageSize(PagingOptions options)
        => Math.Min(
            options.DefaultPageSize ?? PagingDefaults.DefaultPageSize,
            options.MaxPageSize ?? PagingDefaults.MaxPageSize);

    internal static ulong GetPagingBatchPartitionKey(IMiddlewareContext context)
    {
        var options = GetPagingOptions(context.Schema, context.Selection.Field);
        var first = context.ArgumentValue<int?>(FirstArgumentName);
        var after = context.ArgumentValue<string?>(AfterArgumentName);
        int? last = null;
        string? before = null;

        if (options.AllowBackwardPagination ?? PagingDefaults.AllowBackwardPagination)
        {
            last = context.ArgumentValue<int?>(LastArgumentName);
            before = context.ArgumentValue<string?>(BeforeArgumentName);
        }

        if (first is null && last is null
            && !(options.RequirePagingBoundaries ?? PagingDefaults.RequirePagingBoundaries))
        {
            first = GetEffectiveDefaultPageSize(options);
        }

        var flags = ConnectionFlagsHelper.GetConnectionFlags(context);

        if (first is null
            && after is null
            && last is null
            && before is null
            && flags is ConnectionFlags.None)
        {
            return 0;
        }

        var length =
            GetIntPartitionKeySize(first)
            + GetStringPartitionKeySize(after)
            + GetIntPartitionKeySize(last)
            + GetStringPartitionKeySize(before)
            + GetFlagsPartitionKeySize(flags);
        byte[]? rented = null;
        var buffer = length <= MaxStackallocPartitionKeySize
            ? stackalloc byte[length]
            : rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            var written = 0;
            written = WriteIntPartitionKey(buffer, written, (byte)'f', first);
            written = WriteStringPartitionKey(buffer, written, (byte)'a', after);
            written = WriteIntPartitionKey(buffer, written, (byte)'l', last);
            written = WriteStringPartitionKey(buffer, written, (byte)'b', before);
            written = WriteFlagsPartitionKey(buffer, written, flags);

            var hash = ComputePartitionKeyHash(buffer[..written]);
            // This method returns 0 to signal "no paging arguments", so a computed hash must
            // never equal 0 or it would merge into that default partition.
            return hash == 0 ? 1 : hash;
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    internal static ulong GetOffsetPagingBatchPartitionKey(IMiddlewareContext context)
    {
        var options = GetPagingOptions(context.Schema, context.Selection.Field);
        var skip = context.ArgumentValue<int?>("skip");
        var take = context.ArgumentValue<int?>("take");

        if (take is null
            && !(options.RequirePagingBoundaries ?? PagingDefaults.RequirePagingBoundaries))
        {
            take = GetEffectiveDefaultPageSize(options);
        }

        var flags = context.Selection.Features.GetOrSetSafe(
            static c => new OffsetPagingFlags(c.IncludeConditionFlags, c.IsSelected("totalCount")),
            context);
        var totalCount = (options.IncludeTotalCount ?? PagingDefaults.IncludeTotalCount)
            && (flags.Conditions.Word0 == context.IncludeConditionFlags.Word0
                && flags.Conditions.Overflow.AsSpan().SequenceEqual(context.IncludeConditionFlags.Overflow)
                ? flags.TotalCount
                : context.IsSelected("totalCount"));

        if (skip is null && take is null && !totalCount)
        {
            return 0;
        }

        Span<byte> buffer = stackalloc byte[11];
        var written = WriteIntPartitionKey(buffer, 0, (byte)'s', skip);
        written = WriteIntPartitionKey(buffer, written, (byte)'t', take);
        if (totalCount)
        {
            buffer[written++] = 1;
        }

        var hash = ComputePartitionKeyHash(buffer[..written]);
        return hash == 0 ? 1 : hash;
    }

    private readonly record struct OffsetPagingFlags(ConditionFlags Conditions, bool TotalCount);

    private static int GetIntPartitionKeySize(int? value)
        => value.HasValue ? 5 : 0;

    private static int GetStringPartitionKeySize(string? value)
        => value is null ? 0 : 5 + Encoding.UTF8.GetByteCount(value);

    private static int GetFlagsPartitionKeySize(ConnectionFlags flags)
        => flags is ConnectionFlags.None ? 0 : 5;

    private static int WriteIntPartitionKey(
        Span<byte> buffer,
        int offset,
        byte tag,
        int? value)
    {
        if (!value.HasValue)
        {
            return offset;
        }

        buffer[offset++] = tag;
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], value.GetValueOrDefault());
        return offset + 4;
    }

    private static int WriteStringPartitionKey(
        Span<byte> buffer,
        int offset,
        byte tag,
        string? value)
    {
        if (value is null)
        {
            return offset;
        }

        buffer[offset++] = tag;
        var length = Encoding.UTF8.GetByteCount(value);
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], length);
        offset += 4;
        return offset + Encoding.UTF8.GetBytes(value, buffer[offset..]);
    }

    private static int WriteFlagsPartitionKey(
        Span<byte> buffer,
        int offset,
        ConnectionFlags flags)
    {
        if (flags is ConnectionFlags.None)
        {
            return offset;
        }

        buffer[offset++] = (byte)'c';
        BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], (int)flags);
        return offset + 4;
    }

    private static ulong ComputePartitionKeyHash(ReadOnlySpan<byte> buffer)
    {
        return XxHash64.HashToUInt64(buffer);
    }

    [RequiresDynamicCode("Uses MakeGenericType to create generic schema types at runtime.")]
    internal static IExtendedType GetSchemaType(
        IDescriptorContext context,
        MemberInfo? member,
        Type? type = null)
    {
        var typeInspector = context.TypeInspector;

        if (type is null
            && member is not null
            && typeInspector.GetOutputReturnTypeRef(member) is ExtendedTypeReference r
            && typeInspector.TryCreateTypeInfo(r.Type, out var typeInfo))
        {
            // if the member has already associated a schema type we will just take it.
            // Since we want the entity element we are going to take
            // the element type of the list or array as our entity type.
            if (r.Type is { IsSchemaType: true, IsArrayOrList: true })
            {
                return r.Type.ElementType;
            }

            // if the member type is unknown we will try to infer it by extracting
            // the named type component from it and running the type inference.
            // It might be that we either are unable to infer or get the wrong type
            // in special cases. In the case we are getting it wrong the user has
            // to explicitly bind the type.
            if (context.TryInferSchemaType(
                    r.WithType(typeInspector.GetType(typeInfo.NamedType)),
                    out var schemaTypeRefs)
                && schemaTypeRefs is { Length: > 0 }
                && schemaTypeRefs[0] is ExtendedTypeReference schemaTypeRef)
            {
                // if we are able to infer the type we will reconstruct its structure so that
                // we can correctly extract from it the element type with the correct
                // nullability information.
                var current = schemaTypeRef.Type.Type;

                foreach (var component in typeInfo.Components.Reverse())
                {
                    if (component.Kind == TypeComponentKind.NonNull)
                    {
                        current = typeof(NonNullType<>).MakeGenericType(current);
                    }
                    else if (component.Kind == TypeComponentKind.List)
                    {
                        current = typeof(ListType<>).MakeGenericType(current);
                    }
                }

                if (typeInspector.GetType(current) is { IsArrayOrList: true } schemaType)
                {
                    return schemaType.ElementType;
                }
            }
        }

        if (type is null || !typeof(IType).IsAssignableFrom(type))
        {
            throw ThrowHelper.UsePagingAttribute_NodeTypeUnknown(member);
        }

        return typeInspector.GetType(type);
    }

    internal static bool TryGetNamedType(
        ITypeInspector typeInspector,
        MemberInfo? member,
        [NotNullWhen(true)] out Type? namedType)
    {
        if (member is not null
            && typeInspector.GetReturnType(member) is { } returnType
            && typeInspector.TryCreateTypeInfo(returnType, out var typeInfo))
        {
            namedType = typeInfo.NamedType;
            return true;
        }

        namedType = null;
        return false;
    }

    public static PagingOptions GetPagingOptions(ISchemaDefinition schema, IOutputFieldDefinition field)
        => field.Features.TryGet<PagingOptions>(out var options)
            ? options
            : schema.Features.GetRequired<PagingOptions>();

    internal static PagingOptions GetPagingOptions(
        this ITypeCompletionContext context,
        PagingOptions? options)
        => context.DescriptorContext.GetPagingOptions(options);

    public static PagingOptions GetPagingOptions(
        this IDescriptorContext context,
        PagingOptions? options)
    {
        options = options?.Copy() ?? new();

        if (context.Features.TryGet<PagingOptions>(out var global))
        {
            options.Merge(global);
        }

        return options;
    }

    public static void RegisterPageObserver(
        this IMiddlewareContext context,
        IPageObserver observer)
    {
        var observers = context.GetLocalStateOrDefault(
            WellKnownContextData.PagingObserver,
            ImmutableArray<IPageObserver>.Empty);
        context.SetLocalState(
            WellKnownContextData.PagingObserver,
            observers.Add(observer));
    }
}
