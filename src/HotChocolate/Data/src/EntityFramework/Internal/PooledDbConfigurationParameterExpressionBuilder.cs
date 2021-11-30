using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using Microsoft.EntityFrameworkCore;
using static HotChocolate.Data.Internal.EntityFrameworkContextData;

namespace HotChocolate.Data.Internal;

internal sealed class PooledDbConfigurationParameterExpressionBuilder<TDbContext>
    : IParameterExpressionBuilder
    , IParameterConfigurationBuilder
    where TDbContext : DbContext
{
    private readonly IParameterExpressionBuilder _innerBuilder =
        new CustomServiceScopeParameterExpressionBuilder<TDbContext>();

    public ArgumentKind Kind => _innerBuilder.Kind;

    public bool IsPure => _innerBuilder.IsPure;

    public bool CanHandle(ParameterInfo parameter)
        => _innerBuilder.CanHandle(parameter);

    public Expression Build(ParameterInfo parameter, Expression context)
        => _innerBuilder.Build(parameter, context);

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
        => descriptor.Extend()
            .Definition
            .ContextData[DbContextType] = typeof(TDbContext);
}
