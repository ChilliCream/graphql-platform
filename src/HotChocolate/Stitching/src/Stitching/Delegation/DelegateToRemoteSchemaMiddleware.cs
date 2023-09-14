using System.Buffers;
using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Delegation.ScopedVariables;
using HotChocolate.Stitching.Requests;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Types;
using static HotChocolate.Stitching.WellKnownContextData;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Delegation;

public sealed class DelegateToRemoteSchemaMiddleware
{
    private const string _remoteErrorField = "remote";
    private const string _schemaNameErrorField = "schemaName";

    private static readonly RootScopedVariableResolver _resolvers = new();

    private readonly FieldDelegate _next;

    public DelegateToRemoteSchemaMiddleware(FieldDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var delegateDirective = context.Selection
            .Field
            .Directives[DirectiveNames.Delegate]
            .FirstOrDefault()?.AsValue<DelegateDirective>();

        if (delegateDirective != null)
        {
            IImmutableStack<SelectionPathComponent> path;
            IImmutableStack<SelectionPathComponent> reversePath;

            if (delegateDirective.Path is null)
            {
                path = ImmutableStack<SelectionPathComponent>.Empty;
                reversePath = ImmutableStack<SelectionPathComponent>.Empty;
            }
            else
            {
                path = SelectionPathParser.Parse(delegateDirective.Path);
                reversePath = ImmutableStack.CreateRange(path);
            }

            var request = CreateQuery(context, delegateDirective.Schema, path, reversePath);

            var result = await ExecuteQueryAsync(context, request, delegateDirective.Schema)
                .ConfigureAwait(false);
            context.RegisterForCleanup(result.DisposeAsync, cleanAfter: CleanAfter.Request);

            UpdateContextData(context, result, delegateDirective);

            var value = ExtractData(result.Data, reversePath, context.ResponseName);
            context.Result = value is null or NullValueNode ? null : new SerializedData(value);
            if (result.Errors is not null)
            {
                ReportErrors(delegateDirective.Schema, context, path, result.Errors);
            }
        }

        await _next.Invoke(context).ConfigureAwait(false);
    }

    private static void UpdateContextData(
        IResolverContext context,
        IQueryResult result,
        DelegateDirective delegateDirective)
    {
        if (result.ContextData is { Count: > 0 })
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object?>();
            builder.AddRange(context.ScopedContextData);
            builder[SchemaName] = delegateDirective.Schema;
            builder.AddRange(result.ContextData);
            context.ScopedContextData = builder.ToImmutableDictionary();
        }
        else
        {
            context.SetScopedState(SchemaName, delegateDirective.Schema);
        }
    }

    private static IQueryRequest CreateQuery(
        IMiddlewareContext context,
        string schemaName,
        IImmutableStack<SelectionPathComponent> path,
        IImmutableStack<SelectionPathComponent> reversePath)
    {
        var fieldRewriter = new ExtractFieldQuerySyntaxRewriter(
            context.Schema,
            context.Service<IEnumerable<IQueryDelegationRewriter>>());

        var operationType =
            context.Schema.IsRootType(context.ObjectType)
                ? context.Operation.Type
                : OperationType.Query;

        var extractedField = fieldRewriter.ExtractField(
            schemaName, context.Operation.Document, context.Operation.Definition,
            context.Selection, context.ObjectType);

        IEnumerable<ScopedVariableValue> scopedVariables =
            ResolveScopedVariables(
                context, schemaName, operationType,
                reversePath, fieldRewriter);

        var variableValues =
            CreateVariableValues(
                context, schemaName, scopedVariables,
                extractedField, fieldRewriter);

        var builder =
            RemoteQueryBuilder.New()
                .SetRequestField(extractedField.SyntaxNodes[0])
                .SetOperation(context.Operation.Definition.Name, operationType)
                .SetSelectionPath(path)
                .AddVariables(CreateVariableDefs(variableValues))
                .AddFragmentDefinitions(extractedField.Fragments);

        if (extractedField.SyntaxNodes.Count > 1)
        {
            for (var i = 1; i < extractedField.SyntaxNodes.Count; i++)
            {
                builder.AddAdditionalField(extractedField.SyntaxNodes[i]);
            }
        }

        var query = builder.Build(schemaName, context.Schema.GetNameLookup());

        var requestBuilder = QueryRequestBuilder.New();

        AddVariables(requestBuilder, query, variableValues);

        requestBuilder
            .SetQuery(query)
            .AddGlobalState(IsAutoGenerated, true);

        return requestBuilder.Create();
    }

    private static async Task<IQueryResult> ExecuteQueryAsync(
        IResolverContext context,
        IQueryRequest request,
        string schemaName)
    {
        var stitchingContext = context.GetGlobalState<IStitchingContext>(nameof(IStitchingContext));

        if (stitchingContext is null)
        {
            throw new MissingStateException(
                "Stitching",
                nameof(IStitchingContext),
                StateKind.Global);
        }

        var executor = stitchingContext.GetRemoteRequestExecutor(schemaName);
        var result = await executor.ExecuteAsync(request).ConfigureAwait(false);

        if (result is IQueryResult queryResult)
        {
            return queryResult;
        }

        throw new GraphQLException(DelegationMiddleware_OnlyQueryResults);
    }

    private static object? ExtractData(
        IReadOnlyDictionary<string, object?>? data,
        IImmutableStack<SelectionPathComponent> reversePath,
        string fieldName)
    {
        if (data is null || data.Count == 0)
        {
            return null;
        }

        if (reversePath.IsEmpty)
        {
            return data.First().Value;
        }

        object? current = data;

        while (!reversePath.IsEmpty && current is not null)
        {
            reversePath = reversePath.Pop(out var component);

            if (current is IReadOnlyDictionary<string, object?> obj)
            {
                if (reversePath.IsEmpty)
                {
                    current = obj.First().Value;
                }
                else
                {
                    obj.TryGetValue(component.Name.Value, out current);
                }
            }
            else if (current is IList list)
            {
                var aggregated = new List<object?>();
                for (var i = 0; i < list.Count; i++)
                {
                    var key = reversePath.IsEmpty
                        ? fieldName
                        : component.Name.Value;

                    if (list[i] is IReadOnlyDictionary<string, object?> lobj &&
                        lobj.TryGetValue(key, out var item))
                    {
                        aggregated.Add(item);
                    }
                    else
                    {
                        aggregated.Add(null);
                    }
                }

                current = aggregated;
            }
            else
            {
                current = null;
            }
        }

        return current;
    }

    private static void ReportErrors(
        string schemaName,
        IResolverContext context,
        IImmutableStack<SelectionPathComponent> fetchPath,
        IEnumerable<IError> errors)
    {
        foreach (var error in errors)
        {
            var builder = ErrorBuilder
                .FromError(error)
                .SetExtension(_remoteErrorField, error.RemoveException())
                .SetExtension(_schemaNameErrorField, schemaName);

            if (error.Path is not null)
            {
                builder
                    .SetPath(RewriteErrorPath(error.Path, context.Path, fetchPath))
                    .ClearLocations()
                    .AddLocation(context.Selection.SyntaxNode);
            }
            else if (IsHttpError(error))
            {
                builder
                    .SetPath(context.Path)
                    .ClearLocations()
                    .AddLocation(context.Selection.SyntaxNode);
            }

            context.ReportError(builder.Build());
        }
    }

    private static Path RewriteErrorPath(
        Path errorPath,
        Path fieldPath,
        IImmutableStack<SelectionPathComponent> fetchPath)
    {
        var depth = errorPath.Length;
        var buffer = ArrayPool<Path>.Shared.Rent(depth);
        var paths = buffer.AsSpan().Slice(0, depth);

        try
        {
            var current = errorPath;

            do
            {
                paths[--depth] = current;
                current = current.Parent;
            } while (!current .IsRoot);

            depth = 0;
            while (!fetchPath.IsEmpty)
            {
                fetchPath = fetchPath.Pop(out var fp);
                if (paths[depth] is NamePathSegment np && np.Name.Equals(fp.Name.Value))
                {
                    depth++;
                }
                else
                {
                    return fieldPath;
                }
            }

            paths = depth == 0 ? paths.Slice(1) : paths.Slice(depth);

            if (paths.Length == 0)
            {
                return fieldPath;
            }

            current = fieldPath;

            for (var i = 0; i < paths.Length; i++)
            {
                if (paths[i] is IndexerPathSegment index)
                {
                    current = current.Append(index.Index);
                }
                else if (paths[i] is NamePathSegment name)
                {
                    current = current.Append(name.Name);
                }
            }

            return current;
        }
        finally
        {
            ArrayPool<Path>.Shared.Return(buffer);
        }
    }

    private static bool IsHttpError(IError error) =>
        error.Code == ErrorCodes.Stitching.HttpRequestException;

    private static IReadOnlyCollection<ScopedVariableValue> CreateVariableValues(
        IMiddlewareContext context,
        string schemaName,
        IEnumerable<ScopedVariableValue> scopedVariables,
        ExtractedField extractedField,
        ExtractFieldQuerySyntaxRewriter rewriter)
    {
        var values = new Dictionary<string, ScopedVariableValue>();

        foreach (var value in scopedVariables)
        {
            values[value.Name] = value;
        }

        foreach (var value in ResolveUsedRequestVariables(
            context.Schema, schemaName, extractedField,
            context.Variables, rewriter))
        {
            values[value.Name] = value;
        }

        return values.Values;
    }

    private static IReadOnlyList<ScopedVariableValue> ResolveScopedVariables(
        IResolverContext context,
        string schemaName,
        OperationType operationType,
        IImmutableStack<SelectionPathComponent> reversePath,
        ExtractFieldQuerySyntaxRewriter rewriter)
    {
        var variables = new List<ScopedVariableValue>();

        var stitchingContext = context.GetGlobalState<IStitchingContext>(nameof(IStitchingContext));

        if (stitchingContext is null)
        {
            throw new MissingStateException(
                "Stitching",
                nameof(IStitchingContext),
                StateKind.Global);
        }

        var remoteSchema = stitchingContext.GetRemoteSchema(schemaName);
        IComplexOutputType type = remoteSchema.GetOperationType(operationType)!;
        var path = reversePath;

        while (!path.IsEmpty)
        {
            path = path.Pop(out var component);
            var field = ResolveFieldFromComponent(type, component);
            ResolveScopedVariableArguments(
                context, schemaName, component,
                field, variables, rewriter);

            if (!path.IsEmpty)
            {
                if (!(field.Type.NamedType() is IComplexOutputType complexOutputType))
                {
                    throw new GraphQLException(
                        new Error(DelegationMiddleware_PathElementTypeUnexpected));
                }

                type = complexOutputType;
            }
        }

        return variables;
    }

    private static IOutputField ResolveFieldFromComponent(
        IComplexOutputType type,
        SelectionPathComponent component)
    {
        if (!type.Fields.TryGetField(component.Name.Value, out var field))
        {
            // throw helper
            throw new GraphQLException(new Error
            (
                string.Format(
                    CultureInfo.InvariantCulture,
                    DelegationMiddleware_PathElementInvalid,
                    component.Name.Value,
                    type.Name)
            ));
        }

        return field;
    }

    private static void ResolveScopedVariableArguments(
        IResolverContext context,
        string schemaName,
        SelectionPathComponent component,
        IOutputField field,
        ICollection<ScopedVariableValue> variables,
        ExtractFieldQuerySyntaxRewriter rewriter)
    {
        foreach (var argument in component.Arguments)
        {
            if (!field.Arguments.TryGetField(argument.Name.Value, out var arg))
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(
                            DelegationMiddleware_ArgumentNotFound,
                            argument.Name.Value)
                        .SetExtension("argument", argument.Name.Value)
                        .SetCode(ErrorCodes.Stitching.ArgumentNotFound)
                        .Build());
            }

            if (argument.Value is ScopedVariableNode sv)
            {
                var variable = _resolvers.Resolve(context, sv, arg.Type);
                var value = rewriter.RewriteValueNode(
                    schemaName, arg.Type, variable.Value!);
                variables.Add(variable.WithValue(value));
            }
        }
    }

    private static IEnumerable<ScopedVariableValue> ResolveUsedRequestVariables(
        ISchema schema,
        string schemaName,
        ExtractedField extractedField,
        IVariableValueCollection requestVariables,
        ExtractFieldQuerySyntaxRewriter rewriter)
    {
        foreach (var variable in extractedField.Variables)
        {
            var name = variable.Variable.Name.Value;
            var namedType = schema.GetType<INamedInputType>(
                variable.Type.NamedType().Name.Value);

            if (!requestVariables.TryGetVariable(name, out IValueNode? value))
            {
                value = NullValueNode.Default;
            }

            value = rewriter.RewriteValueNode(
                schemaName,
                (IInputType)variable.Type.ToType(namedType),
                value!);

            yield return new ScopedVariableValue
            (
                name,
                variable.Type,
                value,
                variable.DefaultValue
            );
        }
    }

    private static void AddVariables(
        IQueryRequestBuilder builder,
        DocumentNode query,
        IEnumerable<ScopedVariableValue> variableValues)
    {
        var operation =
            query.Definitions.OfType<OperationDefinitionNode>().First();

        var usedVariables = new HashSet<string>(
            operation.VariableDefinitions.Select(t =>
                t.Variable.Name.Value));

        foreach (var variableValue in variableValues)
        {
            if (usedVariables.Contains(variableValue.Name))
            {
                builder.AddVariableValue(variableValue.Name, variableValue.Value);
            }
        }
    }

    private static IReadOnlyList<VariableDefinitionNode> CreateVariableDefs(
        IReadOnlyCollection<ScopedVariableValue> variableValues)
    {
        var definitions = new List<VariableDefinitionNode>();

        foreach (var variableValue in variableValues)
        {
            definitions.Add(new VariableDefinitionNode(
                null,
                new VariableNode(new NameNode(variableValue.Name)),
                variableValue.Type,
                variableValue.DefaultValue,
                Array.Empty<DirectiveNode>()
            ));
        }

        return definitions;
    }
}
