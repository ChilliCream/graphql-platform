using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

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
                    _off.MakeGenericMethod(schemaType).Invoke(null, new[] { ofd });
                }
                else if (descriptor is IInterfaceFieldDescriptor ifd)
                {
                    _iff.MakeGenericMethod(schemaType).Invoke(null, new[] { ifd });
                }
            }
        }

        private Type GetSchemaType(
            IDescriptorContext context,
            MemberInfo member)
        {
            ITypeReference returnType = context.Inspector.GetReturnType(member, TypeContext.Output);
            return GetSchemaType(context, returnType);
        }

        private Type GetSchemaType(
            IDescriptorContext context,
            ITypeReference returnType)
        {
            Type? type = SchemaType;

            if (type is null &&
                returnType is IClrTypeReference clr &&
                TypeInspector.Default.TryCreate(clr.Type, out Utilities.TypeInfo typeInfo))
            {
                if (BaseTypes.IsSchemaType(typeInfo.ClrType))
                {
                    type = typeInfo.ClrType;
                }
                else if (SchemaTypeResolver.TryInferSchemaType(
                    clr.WithType(typeInfo.ClrType),
                    out IClrTypeReference schemaType))
                {
                    type = schemaType.Type;
                }
            }

            if (type is null || !typeof(IType).IsAssignableFrom(type))
            {
                // TODO : ThrowHelper
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
