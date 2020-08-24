using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using TypeInfo = HotChocolate.Internal.TypeInfo;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public sealed class UsePagingAttribute : DescriptorAttribute
    {
        private static readonly MethodInfo _off = typeof(PagingObjectFieldDescriptorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name.Equals(
                nameof(PagingObjectFieldDescriptorExtensions.UsePaging),
                StringComparison.Ordinal)
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(IObjectFieldDescriptor));

        private static readonly MethodInfo _iff = typeof(PagingObjectFieldDescriptorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name.Equals(
                nameof(PagingObjectFieldDescriptorExtensions.UsePaging),
                StringComparison.Ordinal)
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(IInterfaceFieldDescriptor));

        public Type? SchemaType { get; set; }

        protected internal override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (element is MemberInfo m)
            {
                Type schemaType = GetSchemaType(context, m);
                if (descriptor is IObjectFieldDescriptor ofd)
                {
                    _off.MakeGenericMethod(schemaType).Invoke(null, new object?[] { ofd });
                }
                else if (descriptor is IInterfaceFieldDescriptor ifd)
                {
                    _iff.MakeGenericMethod(schemaType).Invoke(null, new object?[] { ifd });
                }
            }
        }

        private Type GetSchemaType(
            IDescriptorContext context,
            MemberInfo member)
        {
            Type? type = SchemaType;
            ITypeReference returnType = context.Inspector.GetReturnTypeRef(
                member, TypeContext.Output);

            if (type is null
                && returnType is ClrTypeReference clr
                && TypeInfo.TryCreate(clr.Type, out TypeInfo? typeInfo))
            {
                if (typeInfo.IsSchemaType)
                {
                    type = typeInfo.NamedType;
                }
                else if (SchemaTypeResolver.TryInferSchemaType(
                    clr.WithType(typeInfo.NamedType),
                    out ClrTypeReference schemaType))
                {
                    type = schemaType.Type.Source;
                }
            }

            if (type is null || !typeof(IType).IsAssignableFrom(type))
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("The UsePaging attribute needs a valid node schema type.")
                        .SetCode("ATTR_USEPAGING_SCHEMATYPE_INVALID")
                        .Build());
            }

            return type;
        }


    }
}
