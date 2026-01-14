using System.Reflection;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class LocalStateParameterExpressionBuilder
    : ScopedStateParameterExpressionBuilder
{
    public override ArgumentKind Kind => ArgumentKind.LocalState;

    private static readonly PropertyInfo s_localContextDataProperty =
        ContextType.GetProperty(nameof(IResolverContext.LocalContextData))!;

    protected override PropertyInfo ContextDataProperty
        => s_localContextDataProperty;

    private static readonly MethodInfo s_setLocalState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.SetLocalState))!;
    private static readonly MethodInfo s_setLocalStateGeneric =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.SetLocalStateGeneric))!;

    protected override MethodInfo SetStateMethod => s_setLocalState;

    protected override MethodInfo SetStateGenericMethod => s_setLocalStateGeneric;

    public override bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(LocalStateAttribute));

    public override bool CanHandle(ParameterDescriptor parameter)
        => parameter.Attributes.Any(t => t is LocalStateAttribute);

    protected override string? GetKey(ParameterInfo parameter)
        => parameter.GetCustomAttribute<LocalStateAttribute>()!.Key;

    public override IParameterBinding Create(ParameterDescriptor parameter)
        => new ParameterBinding(this, parameter);

    private sealed class ParameterBinding : IParameterBinding
    {
        private readonly ScopedStateParameterExpressionBuilder _parent;
        private readonly string _key;

        public ParameterBinding(
            ScopedStateParameterExpressionBuilder parent,
            ParameterDescriptor parameter)
        {
            _parent = parent;

            LocalStateAttribute? globalState = null;
            foreach (var attribute in parameter.Attributes)
            {
                if (attribute is LocalStateAttribute casted)
                {
                    globalState = casted;
                    break;
                }
            }

            _key = globalState?.Key ?? parameter.Name;
        }

        public ArgumentKind Kind => _parent.Kind;

        public bool IsPure => _parent.IsPure;

        public T Execute<T>(IResolverContext context)
            => context.GetLocalStateOrDefault<T>(_key, default!);
    }
}
