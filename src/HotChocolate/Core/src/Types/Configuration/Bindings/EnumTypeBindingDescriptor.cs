using System;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration.Bindings
{
    internal class EnumTypeBindingDescriptor : IEnumTypeBindingDescriptor
    {
        private readonly IDescriptorContext _context;
        private readonly EnumTypeBindingDefinition _definition;

        public EnumTypeBindingDescriptor(
            IDescriptorContext context,
            Type runtimeType)
        {
            _context = context;

            _definition = new EnumTypeBindingDefinition
            {
                RuntimeType = runtimeType,
                TypeName = context.Naming.GetTypeName(runtimeType, TypeKind.Enum)
            };

            foreach (object value in context.TypeInspector.GetEnumValues(runtimeType))
            {
                _definition.Values.Add(new EnumValueDefinition
                {
                    Name = context.Naming.GetEnumValueName(value),
                    Value = value,
                    Member = context.TypeInspector.GetEnumValueMember(value)
                });
            }
        }

        internal EnumTypeBindingDefinition Definition => _definition;

        public IEnumTypeBindingDescriptor To(NameString typeName)
        {
            _definition.TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            return this;
        }

        public IEnumValueBindingDescriptor Value(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            EnumValueDefinition? definition =
                _definition.Values.FirstOrDefault(v => v.Value?.Equals(value) ?? false);

            if (definition is null)
            {
                definition = new EnumValueDefinition
                {
                    Name = _context.Naming.GetEnumValueName(value),
                    Value = value,
                    Member = _context.TypeInspector.GetEnumValueMember(value)
                };
            }

            return new EnumValueBindingDescriptor(_context, this, definition);
        }
    }
}
