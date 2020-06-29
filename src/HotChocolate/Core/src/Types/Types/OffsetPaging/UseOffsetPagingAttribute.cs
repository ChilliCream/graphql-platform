using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;
using TypeInfo = HotChocolate.Utilities.TypeInfo;

namespace HotChocolate.Types.OffsetPaging
{
    public class UseOffsetPagingAttribute : DescriptorAttribute
    {
        protected internal override void TryConfigure(IDescriptorContext context, IDescriptor descriptor, ICustomAttributeProvider element)
        {
            if (element is MemberInfo m)
            {
                Type schemaType = GetSchemaType(context, m);

                if (descriptor is IObjectFieldDescriptor objectFieldDescriptor)
                    objectFieldDescriptor.UseObjectFieldOffsetPaging(schemaType);
                else if (descriptor is IInterfaceFieldDescriptor interfaceFieldDescriptor)
                    interfaceFieldDescriptor.UseInterfaceFieldOffsetPaging(schemaType);
            }
        }

        public Type? SchemaType { get; set; }

        private Type GetSchemaType(IDescriptorContext context, MemberInfo member)
        {
            Type? type = SchemaType;
            ITypeReference returnType = context.Inspector.GetReturnType(member, TypeContext.Output);

            if (type is null &&
                returnType is IClrTypeReference clr &&
                TypeInspector.Default.TryCreate(clr.Type, out TypeInfo typeInfo))
            {
                if (BaseTypes.IsSchemaType(typeInfo.ClrType))
                    type = typeInfo.ClrType;
                else if (SchemaTypeResolver.TryInferSchemaType(clr.WithType(typeInfo.ClrType), out IClrTypeReference schemaType))
                    type = schemaType.Type;
            }

            if (type is null || !typeof(IType).IsAssignableFrom(type))
                throw new SchemaException(SchemaErrorBuilder.New()
                    .SetMessage("The UseOffsetPaging attribute needs a valid node schema type")
                    .SetCode("ATTR_USEOFFSETPAGING_SCHEMATYPE_INVALID")
                    .Build());

            return type;
        }
    }
}