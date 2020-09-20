using System;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Method,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class SubscribeAndResolveAttribute
        : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Extend().OnBeforeCreate(d =>
            {
                ITypeReference typeReference =
                    context.TypeInspector.GetReturnTypeRef(member, TypeContext.Output);

                if (typeReference is ExtendedTypeReference typeRef &&
                    context.TypeInspector.TryCreateTypeInfo(typeRef.Type, out ITypeInfo? typeInfo) &&
                    !typeInfo.IsSchemaType)
                {
                    IExtendedType? rewritten = typeRef.Type.IsArrayOrList
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

                d.SubscribeResolver = ResolverCompiler.Subscribe.Compile(
                    d.SourceType!, d.ResolverType, member);
                d.Resolver = ctx => new ValueTask<object?>(
                    ctx.GetEventMessage<object>());
                d.Type = typeReference;
                d.Member = null;
            });
        }
    }
}
