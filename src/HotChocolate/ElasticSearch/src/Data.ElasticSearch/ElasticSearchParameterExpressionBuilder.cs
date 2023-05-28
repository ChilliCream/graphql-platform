using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using Nest;

namespace HotChocolate.Data.ElasticSearch;

public class ElasticSearchParameterExpressionBuilder<T> : IParameterExpressionBuilder
{
    /// <inheritdoc />
    public bool CanHandle(ParameterInfo parameter) =>
        parameter.ParameterType == typeof(SearchRequest<T>);

    /// <inheritdoc />
    public Expression Build(ParameterExpressionBuilderContext context)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ArgumentKind Kind => ArgumentKind.Service;

    /// <inheritdoc />
    public bool IsPure => false;

    /// <inheritdoc />
    public bool IsDefaultHandler => false;
}
