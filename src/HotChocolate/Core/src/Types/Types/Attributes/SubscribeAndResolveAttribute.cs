using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types;

[Obsolete("Use the SubscribeAttribute.")]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class SubscribeAndResolveAttribute
    : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Extend().OnBeforeCreate(d =>
        {
            var typeReference =
                context.TypeInspector.GetReturnTypeRef(member, TypeContext.Output);

            if (typeReference is ExtendedTypeReference typeRef &&
                context.TypeInspector.TryCreateTypeInfo(typeRef.Type, out var typeInfo) &&
                !typeInfo.IsSchemaType)
            {
                var rewritten = typeRef.Type.IsArrayOrList
                    ? typeRef.Type.ElementType
                    : null;

                if (rewritten is null)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                                "The specified type `{0}` is not a valid subscription type.",
                                typeRef.Type.Source.ToString())
                            .SetExtension("ClrMember", member)
                            .SetExtension("ClrType", member.DeclaringType)
                            .Build());
                }

                typeReference = TypeReference.Create(rewritten, TypeContext.Output);
            }

            d.SubscribeResolver = context.ResolverCompiler.CompileSubscribe(
                member, d.SourceType!, d.ResolverType);
            d.Resolver = ctx => new ValueTask<object?>(
                ctx.GetEventMessage<object>());
            d.Type = typeReference;
            d.Member = null;
        });
    }
}
