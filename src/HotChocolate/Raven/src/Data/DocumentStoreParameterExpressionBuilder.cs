using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Raven.Client.Documents.Session;
using static HotChocolate.Resolvers.FieldClassMiddlewareFactory;

namespace HotChocolate.Data.Raven;

internal sealed class DocumentStoreParameterExpressionBuilder()
    : LambdaParameterExpressionBuilder<IAsyncDocumentSession>(ctx => ctx.AsyncSession(), isPure: false)
    , IParameterFieldConfiguration
{
    public override ArgumentKind Kind => ArgumentKind.Service;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(IAsyncDocumentSession);

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
    {
        if (descriptor.Extend().Definition is { ResultType: { } resultType, } definition &&
            TryExtractEntityType(resultType, out var entityType))
        {
            var middleware = new FieldMiddlewareDefinition(
                Create(typeof(ToListMiddleware<>).MakeGenericType(entityType)),
                key: WellKnownMiddleware.ToList);

            definition.MiddlewareDefinitions.Insert(0, middleware);
        }
    }

    private static bool TryExtractEntityType(
        Type resultType,
        [NotNullWhen(true)] out Type? entityType)
    {
        if (!resultType.IsGenericType)
        {
            entityType = null;

            return false;
        }

        if (typeof(IQueryable).IsAssignableFrom(resultType))
        {
            entityType = resultType.GenericTypeArguments[0];

            return true;
        }

        var resultTypeDefinition = resultType.GetGenericTypeDefinition();

        if (typeof(IAsyncDocumentQuery<>) == resultTypeDefinition)
        {
            entityType = resultType.GenericTypeArguments[0];

            return true;
        }

        if (resultTypeDefinition == typeof(Task<>) || resultTypeDefinition == typeof(ValueTask))
        {
            return TryExtractEntityType(resultType.GenericTypeArguments[0], out entityType);
        }

        entityType = null;
        return false;
    }
}
