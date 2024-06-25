using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Internal;

public interface IParameterBinding
{
    ArgumentKind Kind { get; }

    bool IsPure { get; }

    T Execute<T>(IResolverContext context);
}

public interface IParameterBindingFactory : IParameterHandler
{
    IParameterBinding Create(ParameterBindingContext context);
}

public interface IParameterBindingResolver
{
    public IParameterBinding GetBinding(ParameterInfo parameter);
}

public readonly ref struct ParameterBindingContext(ParameterInfo parameter, string argumentName)
{
    public ParameterInfo Parameter { get; } = parameter;

    public string ArgumentName { get; } = argumentName;
}
