using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types.Relay
{
    public sealed class UsePagingAttribute : ObjectFieldDescriptorAttribute
    {
        private static readonly MethodInfo _generic = typeof(PagingObjectFieldDescriptorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name.Equals(
                nameof(PagingObjectFieldDescriptorExtensions.UsePaging),
                StringComparison.Ordinal)
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(IObjectFieldDescriptor));

        public Type? SchemaType { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            Type? type = SchemaType;
            ITypeReference returnType = context.Inspector.GetReturnType(
                member, TypeContext.Output);

            if (type is null
                && returnType is IClrTypeReference clr
                && TypeInspector.Default.TryCreate(clr.Type, out var typeInfo))
            {
                if (BaseTypes.IsSchemaType(typeInfo.ClrType))
                {
                    type = typeInfo.ClrType;
                }
                else if(SchemaTypeResolver.TryInferSchemaType(
                    clr.WithType(typeInfo.ClrType),
                    out IClrTypeReference schemaType))
                {
                    type = schemaType.Type;
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

            _generic.MakeGenericMethod(type).Invoke(null, new[] { descriptor });
        }
    }
}
