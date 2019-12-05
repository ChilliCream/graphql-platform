using System;
using System.Linq;
using System.Reflection;

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

        public UsePagingAttribute(Type schemaType)
        {
            SchemaType = schemaType;
        }

        public Type SchemaType { get; }

        public override void OnConfigure(IObjectFieldDescriptor descriptor)
        {
            if (SchemaType is null || !typeof(IType).IsAssignableFrom(SchemaType))
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("The UsePaging attribute needs a valid node schema type.")
                        .SetCode("ATTR_USEPAGING_SCHEMATYPE_INVALID")
                        .Build());
            }
            _generic.MakeGenericMethod(SchemaType).Invoke(null, new[] { descriptor });
        }
    }
}