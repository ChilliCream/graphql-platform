using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching.Delegation.ScopedVariables;
using HotChocolate.Stitching.Requests;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Types;
using static HotChocolate.Stitching.WellKnownContextData;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching.Delegation
{
    public class DelegateToRemoteSchemaMiddleware
    {
        private const string _remoteErrorField = "remote";
        private const string _schemaNameErrorField = "schemaName";

        private static readonly RootScopedVariableResolver _resolvers =
            new RootScopedVariableResolver();

        private readonly FieldDelegate _next;

        public DelegateToRemoteSchemaMiddleware(FieldDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            DelegateDirective? delegateDirective = context.Field
                .Directives[DirectiveNames.Delegate]
                .FirstOrDefault()?.ToObject<DelegateDirective>();

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

                IReadOnlyQueryRequest request =
                    CreateQuery(context, delegateDirective.Schema, path, reversePath);

                IReadOnlyQueryResult result = await ExecuteQueryAsync(
                        context, request, delegateDirective.Schema)
                    .ConfigureAwait(false);

                UpdateContextData(context, result, delegateDirective);

                object? value = ExtractData(result.Data, reversePath, context.ResponseName);
                context.Result = value is null or NullValueNode ? null : new SerializedData(value);
                if (result.Errors is not null)
                {
                    ReportErrors(delegateDirective.Schema, context, result.Errors);
                }
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }

        private void UpdateContextData(
            IResolverContext context,
            IReadOnlyQueryResult result,
            DelegateDirective delegateDirective)
        {
            if (result.ContextData is { Count: > 0 })
            {
                ImmutableDictionary<string, object?>.Builder builder =
                    ImmutableDictionary.CreateBuilder<string, object?>();
                builder.AddRange(context.ScopedContextData);
                builder[SchemaName] = delegateDirective.Schema;
                builder.AddRange(result.ContextData);
                context.ScopedContextData = builder.ToImmutableDictionary();
            }
            else
            {
                context.SetScopedValue(SchemaName, delegateDirective.Schema);
            }
        }

        private static IReadOnlyQueryRequest CreateQuery(
            IMiddlewareContext context,
            NameString schemaName,
            IImmutableStack<SelectionPathComponent> path,
            IImmutableStack<SelectionPathComponent> reversePath)
        {
            var fieldRewriter = new ExtractFieldQuerySyntaxRewriter(
                context.Schema,
                context.Service<IEnumerable<IQueryDelegationRewriter>>());

            OperationType operationType =
                context.Schema.IsRootType(context.ObjectType)
                    ? context.Operation.Operation
                    : OperationType.Query;

            ExtractedField extractedField = fieldRewriter.ExtractField(
                schemaName, context.Document, context.Operation,
                context.FieldSelection, context.ObjectType);

            IEnumerable<VariableValue> scopedVariables =
                ResolveScopedVariables(
                    context, schemaName, operationType,
                    reversePath, fieldRewriter);

            IReadOnlyCollection<VariableValue> variableValues =
                CreateVariableValues(
                    context, schemaName, scopedVariables,
                    extractedField, fieldRewriter);

            DocumentNode query = RemoteQueryBuilder.New()
                .SetOperation(context.Operation.Name, operationType)
                .SetSelectionPath(path)
                .SetRequestField(extractedField.Field)
                .AddVariables(CreateVariableDefs(variableValues))
                .AddFragmentDefinitions(extractedField.Fragments)
                .Build(schemaName, context.Schema.GetNameLookup());

            var requestBuilder = QueryRequestBuilder.New();

            AddVariables(requestBuilder, query, variableValues);

            requestBuilder.SetQuery(query);
            requestBuilder.AddProperty(IsAutoGenerated, true);

            return requestBuilder.Create();
        }

        private static async Task<IReadOnlyQueryResult> ExecuteQueryAsync(
            IResolverContext context,
            IReadOnlyQueryRequest request,
            string schemaName)
        {
            IStitchingContext stitchingContext = context.Service<IStitchingContext>();
            IRemoteRequestExecutor executor = stitchingContext.GetRemoteRequestExecutor(schemaName);
            IExecutionResult result = await executor.ExecuteAsync(request).ConfigureAwait(false);

            if (result is IReadOnlyQueryResult queryResult)
            {
                return queryResult;
            }

            throw new GraphQLException(DelegationMiddleware_OnlyQueryResults);
        }

        private static object? ExtractData(
            IReadOnlyDictionary<string, object?>? data,
            IImmutableStack<SelectionPathComponent> reversePath,
            NameString fieldName)
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
                reversePath = reversePath.Pop(out SelectionPathComponent component);

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
                        NameString key = reversePath.IsEmpty
                            ? fieldName
                            : (NameString)component.Name.Value;

                        if (list[i] is IReadOnlyDictionary<string, object?> lobj &&
                            lobj.TryGetValue(key, out object? item))
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
            NameString schemaName,
            IResolverContext context,
            IEnumerable<IError> errors)
        {
            foreach (IError error in errors)
            {
                IErrorBuilder builder = ErrorBuilder.FromError(error)
                    .SetExtension(_remoteErrorField, error.RemoveException())
                    .SetExtension(_schemaNameErrorField, schemaName.Value);

                if (error.Path != null)
                {
                    Path path = RewriteErrorPath(error, context.Path);
                    builder.SetPath(path)
                        .ClearLocations()
                        .AddLocation(context.FieldSelection);
                }
                else if (IsHttpError(error))
                {
                    builder.SetPath(context.Path)
                        .ClearLocations()
                        .AddLocation(context.FieldSelection);
                }

                context.ReportError(builder.Build());
            }
        }

        private static Path RewriteErrorPath(IError error, Path path)
        {
            // TODO : FIX THIS
            Path current = path;

            /*
            if (error.Path.Depth > 0 &&
                error.Path is NamePathSegment p1 &&
                path is NamePathSegment p2 &&
                p1.Name.Equals(p2.Name))
            {
                while()
            }

            if (error.Path.Depth > 0
                && error.Path[0] is string s
                && current.Name.Equals(s))
            {
                for (int i = 1; i < error.Path.Count; i++)
                {
                    if (error.Path[i] is string name)
                    {
                        current = current.Append(name);
                    }

                    if (error.Path[i] is int index)
                    {
                        current = current.Append(index);
                    }
                }
            }
            */

            return current;
        }

        private static bool IsHttpError(IError error) =>
            error.Code == ErrorCodes.Stitching.HttpRequestException;

        private static IReadOnlyCollection<VariableValue> CreateVariableValues(
            IMiddlewareContext context,
            NameString schemaName,
            IEnumerable<VariableValue> scopedVariables,
            ExtractedField extractedField,
            ExtractFieldQuerySyntaxRewriter rewriter)
        {
            var values = new Dictionary<string, VariableValue>();

            foreach (VariableValue value in scopedVariables)
            {
                values[value.Name] = value;
            }

            foreach (VariableValue value in ResolveUsedRequestVariables(
                context.Schema, schemaName, extractedField,
                context.Variables, rewriter))
            {
                values[value.Name] = value;
            }

            return values.Values;
        }

        private static IReadOnlyList<VariableValue> ResolveScopedVariables(
            IResolverContext context,
            NameString schemaName,
            OperationType operationType,
            IImmutableStack<SelectionPathComponent> reversePath,
            ExtractFieldQuerySyntaxRewriter rewriter)
        {
            var variables = new List<VariableValue>();

            IStitchingContext stitchingContext = context.Service<IStitchingContext>();
            ISchema remoteSchema = stitchingContext.GetRemoteSchema(schemaName);
            IComplexOutputType type = remoteSchema.GetOperationType(operationType);
            IImmutableStack<SelectionPathComponent> path = reversePath;

            while (!path.IsEmpty)
            {
                path = path.Pop(out SelectionPathComponent component);
                IOutputField field = ResolveFieldFromComponent(type, component);
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
            if (!type.Fields.TryGetField(component.Name.Value, out IOutputField? field))
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
            NameString schemaName,
            SelectionPathComponent component,
            IOutputField field,
            ICollection<VariableValue> variables,
            ExtractFieldQuerySyntaxRewriter rewriter)
        {
            foreach (ArgumentNode argument in component.Arguments)
            {
                if (!field.Arguments.TryGetField(argument.Name.Value, out IInputField? arg))
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
                    VariableValue variable = _resolvers.Resolve(context, sv, arg.Type);
                    IValueNode value = rewriter.RewriteValueNode(
                        schemaName, arg.Type, variable.Value!);
                    variables.Add(variable.WithValue(value));
                }
            }
        }

        private static IEnumerable<VariableValue> ResolveUsedRequestVariables(
            ISchema schema,
            NameString schemaName,
            ExtractedField extractedField,
            IVariableValueCollection requestVariables,
            ExtractFieldQuerySyntaxRewriter rewriter)
        {
            foreach (VariableDefinitionNode variable in extractedField.Variables)
            {
                string name = variable.Variable.Name.Value;
                INamedInputType namedType = schema.GetType<INamedInputType>(
                    variable.Type.NamedType().Name.Value);

                if (!requestVariables.TryGetVariable(name, out IValueNode value))
                {
                    value = NullValueNode.Default;
                }

                value = rewriter.RewriteValueNode(
                    schemaName,
                    (IInputType)variable.Type.ToType(namedType),
                    value);

                yield return new VariableValue
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
            IEnumerable<VariableValue> variableValues)
        {
            OperationDefinitionNode operation =
                query.Definitions.OfType<OperationDefinitionNode>().First();

            var usedVariables = new HashSet<string>(
                operation.VariableDefinitions.Select(t =>
                    t.Variable.Name.Value));

            foreach (VariableValue variableValue in variableValues)
            {
                if (usedVariables.Contains(variableValue.Name))
                {
                    builder.AddVariableValue(variableValue.Name, variableValue.Value);
                }
            }
        }

        private static IReadOnlyList<VariableDefinitionNode> CreateVariableDefs(
            IReadOnlyCollection<VariableValue> variableValues)
        {
            var definitions = new List<VariableDefinitionNode>();

            foreach (VariableValue variableValue in variableValues)
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
}
