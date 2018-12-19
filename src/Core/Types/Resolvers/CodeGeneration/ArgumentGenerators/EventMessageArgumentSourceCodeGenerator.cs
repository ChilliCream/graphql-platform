using HotChocolate.Subscriptions;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class EventMessageArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.EventMessage;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"ctx.{nameof(IResolverContext.CustomProperty)}<{descriptor.Type.GetTypeName()}>(\"{typeof(IEventMessage).FullName}\")";
        }
    }
}
