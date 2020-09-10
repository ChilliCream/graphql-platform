using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types.Relay
{
    /// <summary>
    /// Applies a cursor paging middleware to a resolver.
    /// </summary>
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

        public UsePagingAttribute(Type? schemaType = null)
        {
            SchemaType = schemaType;
        }

        /// <summary>
        /// The schema type representation of the entity.
        /// </summary>
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
            ITypeReference returnType = context.TypeInspector.GetOutputReturnTypeRef(member);

            if (type is null
                && returnType is ExtendedTypeReference clr
                && context.TypeInspector.TryCreateTypeInfo(clr.Type, out ITypeInfo? typeInfo))
            {
                if (typeInfo.IsSchemaType)
                {
                    type = typeInfo.NamedType;
                }
                else if (SchemaTypeResolver.TryInferSchemaType(
                    context.TypeInspector,
                    clr.WithType(context.TypeInspector.GetType(typeInfo.NamedType)),
                    out ExtendedTypeReference schemaType))
                {
                    type = schemaType.Type.Source;
                }
            }

            if (type is null || !typeof(IType).IsAssignableFrom(type))
            {
                throw UsePagingAttribute_NodeTypeUnknown(member);
            }

            return type;
        }
    }
}
