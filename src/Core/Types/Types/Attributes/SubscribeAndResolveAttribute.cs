using System;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.DotNetTypeInfoFactory;

#nullable enable

namespace HotChocolate.Types
{
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
                ITypeReference typeReference = context.Inspector.GetReturnType(
                    member, TypeContext.Output);

                if (typeReference is IClrTypeReference clrTypeRef
                    && !NamedTypeInfoFactory.Default.TryCreate(clrTypeRef.Type, out _))
                {
                    Type rewritten = Unwrap(UnwrapNonNull(Unwrap(clrTypeRef.Type)));
                    rewritten = GetInnerListType(rewritten);

                    if (rewritten is null)
                    {
                        throw new SchemaException(
                            SchemaErrorBuilder.New()
                                .SetMessage(
                                    "The specified type `{0}` is not a valid subscription type.",
                                    clrTypeRef.Type.ToString())
                                .SetExtension("ClrMember", member)
                                .SetExtension("ClrType", member.DeclaringType)
                                .Build());
                    }

                    typeReference = new ClrTypeReference(rewritten, TypeContext.Output);
                }

                d.SubscribeResolver = ResolverCompiler.Subscribe.Compile(
                    d.SourceType, d.ResolverType, member);
                d.Resolver = ctx => Task.FromResult(
                    ctx.CustomProperty<object>(WellKnownContextData.EventMessage));
                d.Type = typeReference;
                d.Member = null;
            });
        }
    }
}
